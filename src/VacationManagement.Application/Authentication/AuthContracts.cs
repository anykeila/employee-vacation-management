namespace VacationManagement.Application.Authentication;

public record LoginRequest(string Email, string Password);

public record LoginResponse(string AccessToken, DateTimeOffset ExpiresAtUtc, string TokenType = "Bearer");

public interface IAuthService
{
    Task<LoginResponse?> LoginAsync(LoginRequest request, CancellationToken ct = default);
}
