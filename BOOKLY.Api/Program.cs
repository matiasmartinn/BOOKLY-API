using BOOKLY.Application.DependencyInjection;
using BOOKLY.Domain.DomainServices;
using BOOKLY.Infrastructure;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, services, cfg) =>
{
    cfg.MinimumLevel.Information()
       .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
       .MinimumLevel.Override("Microsoft.AspNetCore", Serilog.Events.LogEventLevel.Warning)
       .Enrich.FromLogContext()
       .WriteTo.Console();
});

// Add services to the container
builder.Services.AddControllers();
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

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();