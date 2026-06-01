using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using VacationManagement.Domain.Entities;
using VacationManagement.Infrastructure.Authentication;
using VacationManagement.Infrastructure.Persistence;
using Xunit;

namespace VacationManagement.IntegrationTests;

[Collection("Integration")]
public class RefreshTokenCleanupTests
{
    private readonly CustomWebApplicationFactory _factory;

    public RefreshTokenCleanupTests(CustomWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Prune_RemovesExpiredTokens_ButKeepsActiveOnes()
    {
        var scopeFactory = _factory.Services.GetRequiredService<IServiceScopeFactory>();
        var timeProvider = _factory.Services.GetRequiredService<TimeProvider>();
        var now = timeProvider.GetUtcNow();

        var expiredHash = $"expired-{Guid.NewGuid():N}";
        var activeHash = $"active-{Guid.NewGuid():N}";

        using (var scope = scopeFactory.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.RefreshTokens.Add(new RefreshToken
            {
                TokenHash = expiredHash, EmployeeId = 1, CreatedAt = now.AddDays(-10), ExpiresAt = now.AddDays(-1)
            });
            db.RefreshTokens.Add(new RefreshToken
            {
                TokenHash = activeHash, EmployeeId = 1, CreatedAt = now, ExpiresAt = now.AddDays(1)
            });
            await db.SaveChangesAsync();
        }

        var cleaner = new RefreshTokenCleanupService(
            scopeFactory, timeProvider, NullLogger<RefreshTokenCleanupService>.Instance);
        await cleaner.PruneAsync(CancellationToken.None);

        using (var scope = scopeFactory.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            (await db.RefreshTokens.AnyAsync(t => t.TokenHash == expiredHash)).Should().BeFalse();
            (await db.RefreshTokens.AnyAsync(t => t.TokenHash == activeHash)).Should().BeTrue();
        }
    }
}
