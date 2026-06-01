using VacationManagement.Application.Common;
using VacationManagement.Domain.Enums;

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

public record VacationRequestQuery : PaginationQuery
{
    public VacationStatus? Status { get; init; }
    public int? EmployeeId { get; init; }
    public DateOnly? From { get; init; }
    public DateOnly? To { get; init; }
}

public interface IVacationRequestService
{
    Task<Result<PagedResult<VacationRequestResponse>>> GetAllAsync(VacationRequestQuery query, CancellationToken ct = default);
    Task<Result<VacationRequestResponse>> GetByIdAsync(int id, CancellationToken ct = default);
    Task<Result<VacationRequestResponse>> CreateAsync(CreateVacationRequestRequest request, CancellationToken ct = default);
    Task<Result<VacationRequestResponse>> ApproveAsync(int id, DecisionRequest request, CancellationToken ct = default);
    Task<Result<VacationRequestResponse>> RejectAsync(int id, DecisionRequest request, CancellationToken ct = default);
}
