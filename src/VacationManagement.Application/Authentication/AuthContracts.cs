namespace VacationManagement.Application.Authentication;

public record LoginRequest(string Email, string Password);

public record RefreshRequest(string RefreshToken);

public record LoginResponse(
    string AccessToken,
    DateTimeOffset ExpiresAtUtc,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAtUtc,
    string TokenType = "Bearer");

public interface IAuthService
{
    Task<LoginResponse?> LoginAsync(LoginRequest request, CancellationToken ct = default);
    Task<LoginResponse?> RefreshAsync(RefreshRequest request, CancellationToken ct = default);
}
