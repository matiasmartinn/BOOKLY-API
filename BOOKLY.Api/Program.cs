using BOOKLY.Api.Middleware;
using BOOKLY.Application.DependencyInjection;
using BOOKLY.Domain.DomainServices;
using BOOKLY.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

const string FrontendPolicy = "FrontendPolicy";

builder.Services.AddCors(options =>
{
    options.AddPolicy(FrontendPolicy, policy =>
    {
        policy
            .WithOrigins(
                "http://localhost:5173",   // Vite
                "http://localhost:3000"   // React clásico
            )
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});



// Add services to the container
builder.Services.AddControllers();

// Register ProblemDetails
builder.Services.AddProblemDetails();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    var xmlFilename = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename));
});

// Infrastructure Layer (DbContext + Repositories)
builder.Services.AddInfrastructure(builder.Configuration);

// Application Layer (AutoMapper + Application Services)
builder.Services.AddApplicationServices();

// Domain Layer
builder.Services.AddScoped<IAvailabilityService, AvailabilityService>();

var app = builder.Build();

app.UseExceptionHandling();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors(FrontendPolicy);

app.UseAuthorization();
app.MapControllers();

app.Run();