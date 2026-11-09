using DeployForge.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DeployForge.IntegrationTests;

public sealed class CustomWebApplicationFactory
    : WebApplicationFactory<Program>
{
    private const string TestConnectionString =
        "Host=localhost;Port=5432;Database=deployforge_test;" +
        "Username=deployforge;Password=dev_password";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        // Передаём строку подключения до запуска Program.cs.
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] =
                        TestConnectionString
                });
        });

        builder.ConfigureServices(services =>
        {
            // Удаляем регистрацию основной базы данных.
            services.RemoveAll<DbContextOptions<AppDbContext>>();

            // Подключаем отдельную тестовую базу.
            services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(TestConnectionString));
        });
    }
}
