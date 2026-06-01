using Microsoft.EntityFrameworkCore;
using VacationManagement.Application.Abstractions;
using VacationManagement.Application.Authentication;
using VacationManagement.Infrastructure.Persistence;

namespace VacationManagement.Infrastructure.Authentication;

public class AuthService : IAuthService
{
    private readonly AppDbContext _db;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _tokenGenerator;

    public AuthService(AppDbContext db, IPasswordHasher passwordHasher, IJwtTokenGenerator tokenGenerator)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _tokenGenerator = tokenGenerator;
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

        var token = _tokenGenerator.Generate(employee);
        return new LoginResponse(token.AccessToken, token.ExpiresAtUtc);
    }
}
