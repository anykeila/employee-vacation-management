using VacationManagement.Domain.Enums;

namespace VacationManagement.Application.Abstractions;

public interface ICurrentUser
{
    bool IsAuthenticated { get; }
    int? Id { get; }
    string? Email { get; }
    Role? Role { get; }
}
