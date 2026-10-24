using LearnHub.Api.Data;
using LearnHub.Api.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace LearnHub.Tests;

/// <summary>Runs the real API against a SQLite in-memory database filled with TestData and a fixed clock.</summary>
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    public FakeClock Clock { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        _connection.Open();
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<LearnHubDbContext>>();
            services.AddDbContext<LearnHubDbContext>(options => options.UseSqlite(_connection));
            services.RemoveAll<IClock>();
            services.AddSingleton<IClock>(Clock);

            using var provider = services.BuildServiceProvider();
            using var scope = provider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<LearnHubDbContext>();
            db.Database.EnsureCreated();
            TestData.Seed(db);
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing) _connection.Dispose();
    }
}
