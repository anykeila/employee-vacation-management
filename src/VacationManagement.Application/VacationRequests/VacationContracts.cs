using VacationManagement.Application.Common;

namespace VacationManagement.Application.VacationRequests;

public record VacationRequestResponse(
    int Id,
    int EmployeeId,
    string EmployeeName,
    DateOnly StartDate,
    DateOnly EndDate,
    int TotalDays,
    string Status,
    string? Notes,
    DateTimeOffset CreatedAt);

public record CreateVacationRequestRequest(DateOnly StartDate, DateOnly EndDate, string? Notes);

public record DecisionRequest(string? Notes);

public interface IVacationRequestService
{
    Task<Result<IReadOnlyList<VacationRequestResponse>>> GetAllAsync(CancellationToken ct = default);
    Task<Result<VacationRequestResponse>> GetByIdAsync(int id, CancellationToken ct = default);
    Task<Result<VacationRequestResponse>> CreateAsync(CreateVacationRequestRequest request, CancellationToken ct = default);
    Task<Result<VacationRequestResponse>> ApproveAsync(int id, DecisionRequest request, CancellationToken ct = default);
    Task<Result<VacationRequestResponse>> RejectAsync(int id, DecisionRequest request, CancellationToken ct = default);
}
