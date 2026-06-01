using Microsoft.EntityFrameworkCore;
using VacationManagement.Application.Abstractions;
using VacationManagement.Domain.Enums;
using VacationManagement.Infrastructure.Persistence;

namespace VacationManagement.UnitTests.Support;

internal sealed class FakeCurrentUser : ICurrentUser
{
    public FakeCurrentUser(int id, Role role)
    {
        Id = id;
        Role = role;
    }

    public bool IsAuthenticated => Id is not null;
    public int? Id { get; }
    public string? Email => null;
    public Role? Role { get; }
}

internal sealed class FakePasswordHasher : IPasswordHasher
{
    public string Hash(string password) => $"hashed:{password}";
    public bool Verify(string password, string hash) => hash == $"hashed:{password}";
}

internal sealed class FixedTimeProvider : TimeProvider
{
    private readonly DateTimeOffset _now;

    public FixedTimeProvider(DateTimeOffset now) => _now = now;

    public override DateTimeOffset GetUtcNow() => _now;
}

internal static class TestDb
{
    // Each test gets an isolated store; EnsureCreated applies the HasData seed
    // (employees 1-5 with their manager links and the three approved 2025 vacations).
    public static AppDbContext NewSeededContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"vac-tests-{Guid.NewGuid():N}")
            .Options;

        var context = new AppDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }
}
