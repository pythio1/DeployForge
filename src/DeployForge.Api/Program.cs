using DeployForge.Infrastructure.Security;
using DeployForge.Application.PipelineRuns;
using DeployForge.Application.Pipelines;
using DeployForge.Api.Endpoints;
using DeployForge.Application.Projects;
using DeployForge.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using DeployForge.Infrastructure.Messaging;
using RabbitMQ.Client;
var builder = WebApplication.CreateBuilder(args);
var keysDirectory = builder.Configuration[
    "DEPLOYFORGE_DATAPROTECTION_KEYS_PATH"]
    ?? throw new InvalidOperationException(
        "DEPLOYFORGE_DATAPROTECTION_KEYS_PATH is not configured.");

builder.Services.AddDeployForgeDataProtection(keysDirectory);
var connectionString = builder.Configuration
    .GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "Connection string 'DefaultConnection' was not found.");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));
builder.Services.AddScoped<IProjectRepository, ProjectRepository>();
builder.Services.AddScoped<IPipelineRepository, PipelineRepository>();
builder.Services.AddScoped<CreateProject>();
builder.Services.AddScoped<GetProjects>();
// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddScoped<GetProjectById>();
builder.Services.AddScoped<CreatePipeline>();
builder.Services.AddScoped<GetPipelinesByProject>();
builder.Services.AddScoped<GetPipelineById>();
builder.Services.AddScoped<DeletePipeline>();
builder.Services.AddScoped<IPipelineRunRepository, PipelineRunRepository>();
builder.Services.AddScoped<CreatePipelineRun>();
builder.Services.AddScoped<GetPipelineRuns>();
builder.Services.AddScoped<GetPipelineRunById>();
builder.Services.AddSingleton(_ =>
    new ConnectionFactory
    {
        HostName = "localhost",
        Port = 5672,
        UserName = "deployforge",
        Password = "dev_password"
    });

builder.Services.AddSingleton<IPipelineRunQueue, RabbitMqPipelineRunQueue>();
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast =  Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast")
.WithOpenApi();

app.MapProjectsEndpoints();
app.MapPipelinesEndpoints();
app.Run();
public partial class Program
{
}
record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
