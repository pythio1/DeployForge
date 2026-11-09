using DeployForge.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DeployForge.IntegrationTests;

public sealed class CustomWebApplicationFactory
    : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureServices(services =>
        {
            // Удаляем регистрацию основной базы данных.
            services.RemoveAll<DbContextOptions<AppDbContext>>();

            // Подключаем отдельную тестовую базу.
            services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(
                    "Host=localhost;Port=5432;Database=deployforge_test;" +
                    "Username=deployforge;Password=dev_password"));
        });
    }
}
