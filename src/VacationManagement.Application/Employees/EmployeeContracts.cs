using VacationManagement.Application.Common;
using VacationManagement.Domain.Enums;

namespace VacationManagement.Application.Employees;

public record EmployeeResponse(int Id, string Name, string Email, string Role, int? ManagerId, string? ManagerName);

public record CreateEmployeeRequest(string Name, string Email, string Password, Role Role, int? ManagerId);

public record UpdateEmployeeRequest(string Name, string Email, Role Role, int? ManagerId);

public interface IEmployeeService
{
    Task<IReadOnlyList<EmployeeResponse>> GetAllAsync(CancellationToken ct = default);
    Task<EmployeeResponse?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<Result<EmployeeResponse>> CreateAsync(CreateEmployeeRequest request, CancellationToken ct = default);
    Task<Result<EmployeeResponse>> UpdateAsync(int id, UpdateEmployeeRequest request, CancellationToken ct = default);
    Task<Result<bool>> DeleteAsync(int id, CancellationToken ct = default);
}
