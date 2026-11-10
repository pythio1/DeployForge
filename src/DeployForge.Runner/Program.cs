using DeployForge.Infrastructure.Security;
using DeployForge.Application.Projects;
using DeployForge.Infrastructure.Execution;
using DeployForge.Application.Pipelines;
using DeployForge.Application.PipelineRuns;
using DeployForge.Infrastructure.Messaging;
using DeployForge.Infrastructure.Persistence;
using DeployForge.Runner;
using Docker.DotNet;
using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client;

var builder = Host.CreateApplicationBuilder(args);
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

builder.Services.AddScoped<IPipelineRunRepository, PipelineRunRepository>();
builder.Services.AddScoped<IPipelineRepository, PipelineRepository>();
builder.Services.AddScoped<IProjectRepository, ProjectRepository>();

builder.Services.AddSingleton(_ =>
    new ConnectionFactory
    {
        HostName = builder.Configuration["RabbitMQ:HostName"] ?? "localhost",
        Port = builder.Configuration.GetValue<int>("RabbitMQ:Port", 5672),
        UserName = builder.Configuration["RabbitMQ:UserName"] ?? "guest",
        Password = builder.Configuration["RabbitMQ:Password"] ?? "guest"
    });

// Docker client
builder.Services.AddSingleton<IDockerClient>(_ =>
{
    var dockerEndpoint = builder.Configuration["Docker:Endpoint"]
        ?? "unix:///var/run/docker.sock";

    return new DockerClientConfiguration(new Uri(dockerEndpoint))
        .CreateClient();
});
builder.Services.Configure<DockerExecutorOptions>(
    builder.Configuration.GetSection("DockerExecutor"));

builder.Services.AddScoped<
    IPipelineStepExecutor,
    DockerPipelineStepExecutor>();

builder.Services.AddHostedService<Worker>();

var host = builder.Build();

host.Run();
