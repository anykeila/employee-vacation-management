using Microsoft.EntityFrameworkCore;
using VacationManagement.Application.Abstractions;

namespace VacationManagement.Infrastructure.Persistence;

public static class DbInitializer
{
    // Dev-only password for the seeded accounts. Documented in the README.
    public const string DefaultSeedPassword = "Password123!";

    /// <summary>
    /// Backfills password hashes for seeded employees (inserted with an empty hash by the
    /// migration). Idempotent: only touches rows that still have no password.
    /// </summary>
    public static async Task EnsureSeedPasswordsAsync(
        AppDbContext db, IPasswordHasher passwordHasher, CancellationToken ct = default)
    {
        var pending = await db.Employees.Where(e => e.PasswordHash == string.Empty).ToListAsync(ct);
        if (pending.Count == 0)
        {
            return;
        }

        var hash = passwordHasher.Hash(DefaultSeedPassword);
        foreach (var employee in pending)
        {
            employee.PasswordHash = hash;
        }

        await db.SaveChangesAsync(ct);
    }
}
