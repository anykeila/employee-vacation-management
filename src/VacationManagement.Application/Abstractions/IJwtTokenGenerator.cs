using VacationManagement.Domain.Entities;

namespace VacationManagement.Application.Abstractions;

public interface IJwtTokenGenerator
{
    AuthToken Generate(Employee employee);
}

public record AuthToken(string AccessToken, DateTimeOffset ExpiresAtUtc);
