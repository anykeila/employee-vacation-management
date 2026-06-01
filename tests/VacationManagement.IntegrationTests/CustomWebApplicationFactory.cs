using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Testcontainers.PostgreSql;
using Xunit;

namespace VacationManagement.IntegrationTests;

// Boots the real ASP.NET Core pipeline against a throwaway PostgreSQL 16 container,
// so the GiST exclusion constraint, EF migrations and JWT/RBAC are exercised end-to-end.
public sealed class CustomWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _database = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .Build();

    public async Task InitializeAsync() => await _database.StartAsync();

    public new async Task DisposeAsync()
    {
        await _database.DisposeAsync();
        await base.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("ConnectionStrings:DefaultConnection", _database.GetConnectionString());

        // The suite issues many logins in seconds from the same loopback address; lift the
        // auth rate limit so throttling never masks a functional assertion. A dedicated test
        // covers the limiter itself.
        builder.UseSetting("RateLimiting:AuthPermitLimit", "10000");
    }
}
