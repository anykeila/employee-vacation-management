using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using VacationManagement.Application.Abstractions;
using VacationManagement.Application.Authentication;
using VacationManagement.Domain.Entities;
using VacationManagement.Infrastructure.Persistence;
using VacationManagement.Infrastructure.Security;

namespace VacationManagement.Infrastructure.Authentication;

public class AuthService : IAuthService
{
    private readonly AppDbContext _db;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _tokenGenerator;
    private readonly TimeProvider _timeProvider;
    private readonly JwtSettings _settings;

    public AuthService(
        AppDbContext db,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator tokenGenerator,
        TimeProvider timeProvider,
        IOptions<JwtSettings> settings)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _tokenGenerator = tokenGenerator;
        _timeProvider = timeProvider;
        _settings = settings.Value;
    }

    public async Task<LoginResponse?> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var employee = await _db.Employees.FirstOrDefaultAsync(e => e.Email == email, ct);

        // Verify always runs against a hash to avoid leaking which emails exist via timing.
        if (employee is null || !_passwordHasher.Verify(request.Password, employee.PasswordHash))
        {
            return null;
        }

        var response = await IssueAsync(employee, ct);
        await _db.SaveChangesAsync(ct);
        return response;
    }

    public async Task<LoginResponse?> RefreshAsync(RefreshRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            return null;
        }

        var hash = Hash(request.RefreshToken);
        var stored = await _db.RefreshTokens.Include(t => t.Employee).FirstOrDefaultAsync(t => t.TokenHash == hash, ct);
        if (stored is null)
        {
            return null;
        }

        var now = _timeProvider.GetUtcNow();
        if (!stored.IsActive(now))
        {
            // A revoked token presented again means it was already rotated: treat it as
            // theft and revoke every still-active token for that employee.
            if (stored.RevokedAt is not null)
            {
                await RevokeAllActiveAsync(stored.EmployeeId, now, ct);
                await _db.SaveChangesAsync(ct);
            }

            return null;
        }

        stored.RevokedAt = now;
        var response = await IssueAsync(stored.Employee, ct);
        await _db.SaveChangesAsync(ct);
        return response;
    }

    private async Task<LoginResponse> IssueAsync(Employee employee, CancellationToken ct)
    {
        var access = _tokenGenerator.Generate(employee);

        var now = _timeProvider.GetUtcNow();
        var rawRefreshToken = GenerateRawToken();
        var refresh = new RefreshToken
        {
            TokenHash = Hash(rawRefreshToken),
            EmployeeId = employee.Id,
            CreatedAt = now,
            ExpiresAt = now.AddDays(_settings.RefreshTokenDays)
        };

        await _db.RefreshTokens.AddAsync(refresh, ct);
        return new LoginResponse(access.AccessToken, access.ExpiresAtUtc, rawRefreshToken, refresh.ExpiresAt);
    }

    private async Task RevokeAllActiveAsync(int employeeId, DateTimeOffset now, CancellationToken ct)
    {
        var active = await _db.RefreshTokens
            .Where(t => t.EmployeeId == employeeId && t.RevokedAt == null && t.ExpiresAt > now)
            .ToListAsync(ct);

        foreach (var token in active)
        {
            token.RevokedAt = now;
        }
    }

    private static string GenerateRawToken()
        => Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

    private static string Hash(string token)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token))).ToLowerInvariant();
}
