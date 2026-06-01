using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using VacationManagement.Infrastructure.Persistence;

namespace VacationManagement.Infrastructure.Authentication;

// Periodically deletes refresh tokens whose lifetime has fully elapsed so the table does
// not grow without bound. Only expired rows are removed: a revoked-but-still-valid token is
// kept on purpose so replaying it within its window still triggers the theft response in
// AuthService.RefreshAsync.
public sealed class RefreshTokenCleanupService : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromHours(6);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<RefreshTokenCleanupService> _logger;

    public RefreshTokenCleanupService(
        IServiceScopeFactory scopeFactory,
        TimeProvider timeProvider,
        ILogger<RefreshTokenCleanupService> logger)
    {
        _scopeFactory = scopeFactory;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval, _timeProvider);
        do
        {
            try
            {
                await PruneAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Refresh-token cleanup failed; will retry next interval.");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    public async Task PruneAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var now = _timeProvider.GetUtcNow();
        var removed = await db.RefreshTokens.Where(t => t.ExpiresAt < now).ExecuteDeleteAsync(ct);

        if (removed > 0)
        {
            _logger.LogInformation("Pruned {Count} expired refresh token(s).", removed);
        }
    }
}
