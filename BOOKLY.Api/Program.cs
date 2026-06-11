using System.Threading.RateLimiting;
using BOOKLY.Api.Middleware;
using BOOKLY.Api.Security;
using BOOKLY.Application.Common;
using BOOKLY.Application.Common.Security;
using BOOKLY.Application.DependencyInjection;
using BOOKLY.Domain.DomainServices;
using BOOKLY.Infrastructure;
using BOOKLY.Infrastructure.Persistence;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration
    .SetBasePath(builder.Environment.ContentRootPath)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();


// ── Servicios ─────────────────────────────────────────────────────────────────

builder.Services.Configure<AuthOptions>(
    builder.Configuration.GetSection(AuthOptions.SectionName));

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders =
        ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
});

const string FrontendPolicy = "FrontendPolicy";
var allowedOrigins = GetAllowedOrigins(builder.Configuration);

builder.Services.AddCors(options =>
{
    options.AddPolicy(FrontendPolicy, policy =>
    {
        policy
            .WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

builder.Services.AddControllers();

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.OnRejected = async (ctx, token) =>
    {
        ctx.HttpContext.Response.StatusCode = 429;

        var hasRetryAfter = ctx.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter);

        if (hasRetryAfter)
            ctx.HttpContext.Response.Headers.RetryAfter =
                ((int)retryAfter.TotalSeconds).ToString();

        await ctx.HttpContext.Response.WriteAsJsonAsync(new
        {
            type = "https://datatracker.ietf.org/doc/html/rfc6585#section-4",
            title = "Demasiadas solicitudes",
            status = 429,
            detail = "Has superado el límite de peticiones. Intentá más tarde.",
            retryAfterSeconds = hasRetryAfter ? (int?)retryAfter.TotalSeconds : null
        }, token);
    };

    // 5 req/min · sin cola → corta fuerza bruta inmediatamente
    options.AddFixedWindowLimiter("auth-policy", o =>
    {
        o.Window = TimeSpan.FromMinutes(1);
        o.PermitLimit = 5;
        o.QueueLimit = 0;
        o.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
    });

    // 20 req/min · sliding · cola 2 → tolera bursts legítimos
    options.AddSlidingWindowLimiter("booking-policy", o =>
    {
        o.Window = TimeSpan.FromMinutes(1);
        o.SegmentsPerWindow = 6;
        o.PermitLimit = 20;
        o.QueueLimit = 2;
        o.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
    });


    // GlobalLimiter: corre en paralelo con las políticas nombradas.
    // El límite más restrictivo (global vs política) gana.
    //   - Autenticado → 300 req/min por userId
    //   - Anónimo     →  60 req/min por IP
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(ctx =>
    {
        var userId =
            ctx.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
            ?? ctx.User?.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value;

        if (!string.IsNullOrEmpty(userId))
            return RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: $"user:{userId}",
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    Window = TimeSpan.FromMinutes(1),
                    PermitLimit = 300,
                    QueueLimit = 10,
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst
                });

        var ip = ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: $"ip:{ip}",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                Window = TimeSpan.FromMinutes(1),
                PermitLimit = 60,
                QueueLimit = 0,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst
            });
    });
});

builder.Services.AddHealthChecks();       
builder.Services.AddProblemDetails();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    var xmlFilename = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename));
});

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplicationServices();
builder.Services.AddScoped<IAvailabilityService, AvailabilityService>();

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserContext, CurrentUserContext>();

// ── Pipeline ──────────────────────────────────────────────────────────────────
var app = builder.Build();

if (app.Environment.IsDevelopment() ||
    builder.Configuration.GetValue<bool>("Seed:RunOnStartup"))
{
    await app.Services.SeedBooklyDataAsync();
}

app.UseForwardedHeaders();  
app.UseExceptionHandling();  

if (!app.Environment.IsDevelopment())
    app.UseHsts();               

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();          
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();        
app.UseCors(FrontendPolicy);      

app.UseAuthentication();          
app.UseRateLimiter();         
app.UseAuthorization();           

app.MapHealthChecks("/health");
app.MapControllers();

app.Run();

static string[] GetAllowedOrigins(IConfiguration configuration)
{
    var configuredOrigins = configuration
        .GetSection("Cors:AllowedOrigins")
        .Get<string[]>() ?? Array.Empty<string>();

    var frontendBaseUrl = configuration["Frontend:BaseUrl"];

    var origins = configuredOrigins
        .Concat(string.IsNullOrWhiteSpace(frontendBaseUrl)
            ? Array.Empty<string>()
            : new[] { frontendBaseUrl })
        .Where(o => !string.IsNullOrWhiteSpace(o) && o != "*")
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToArray();

    return origins.Length > 0
        ? origins
        : new[] { "http://localhost:5173" };
}