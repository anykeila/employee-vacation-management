using Microsoft.EntityFrameworkCore;
using Npgsql;
using VacationManagement.Application.Abstractions;
using VacationManagement.Application.Common;
using VacationManagement.Application.VacationRequests;
using VacationManagement.Domain.Entities;
using VacationManagement.Domain.Enums;
using VacationManagement.Infrastructure.Persistence;

namespace VacationManagement.Infrastructure.VacationRequests;

public class VacationRequestService : IVacationRequestService
{
    // PostgreSQL SQLSTATE for an exclusion_violation (raised by the GiST overlap constraint).
    private const string ExclusionViolation = "23P01";

    private readonly AppDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _timeProvider;

    public VacationRequestService(AppDbContext db, ICurrentUser currentUser, TimeProvider timeProvider)
    {
        _db = db;
        _currentUser = currentUser;
        _timeProvider = timeProvider;
    }

    public async Task<Result<PagedResult<VacationRequestResponse>>> GetAllAsync(VacationRequestQuery query, CancellationToken ct = default)
    {
        if (_currentUser.Id is not int userId || _currentUser.Role is not Role role)
        {
            return Result<PagedResult<VacationRequestResponse>>.Failure(ResultError.Forbidden, "The authenticated user could not be resolved.");
        }

        var requests = _db.VacationRequests.AsNoTracking().Include(v => v.Employee).AsQueryable();

        requests = role switch
        {
            Role.Administrator => requests,
            Role.Manager => requests.Where(v => v.EmployeeId == userId || v.Employee.ManagerId == userId),
            _ => requests.Where(v => v.EmployeeId == userId)
        };

        if (query.Status is VacationStatus status)
        {
            requests = requests.Where(v => v.Status == status);
        }

        if (query.EmployeeId is int employeeId)
        {
            requests = requests.Where(v => v.EmployeeId == employeeId);
        }

        if (query.From is DateOnly from)
        {
            requests = requests.Where(v => v.EndDate >= from);
        }

        if (query.To is DateOnly to)
        {
            requests = requests.Where(v => v.StartDate <= to);
        }

        var totalCount = await requests.CountAsync(ct);

        var page = await requests
            .OrderByDescending(v => v.StartDate)
            .Skip(query.Skip)
            .Take(query.PageSize)
            .ToListAsync(ct);

        IReadOnlyList<VacationRequestResponse> items = page.Select(ToResponse).ToList();
        var result = new PagedResult<VacationRequestResponse>(items, query.Page, query.PageSize, totalCount);
        return Result<PagedResult<VacationRequestResponse>>.Success(result);
    }

    public async Task<Result<VacationRequestResponse>> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var request = await _db.VacationRequests.AsNoTracking().Include(v => v.Employee).FirstOrDefaultAsync(v => v.Id == id, ct);
        if (request is null)
        {
            return Result<VacationRequestResponse>.Failure(ResultError.NotFound, $"Vacation request {id} was not found.");
        }

        if (!CanView(request))
        {
            return Result<VacationRequestResponse>.Failure(ResultError.Forbidden, "You are not allowed to view this vacation request.");
        }

        return Result<VacationRequestResponse>.Success(ToResponse(request));
    }

    public async Task<Result<VacationRequestResponse>> CreateAsync(CreateVacationRequestRequest request, CancellationToken ct = default)
    {
        if (_currentUser.Id is not int employeeId)
        {
            return Result<VacationRequestResponse>.Failure(ResultError.Forbidden, "The authenticated user could not be resolved.");
        }

        if (request.EndDate < request.StartDate)
        {
            return Result<VacationRequestResponse>.Failure(ResultError.Validation, "End date cannot be earlier than start date.");
        }

        var today = DateOnly.FromDateTime(_timeProvider.GetUtcNow().UtcDateTime);
        if (request.StartDate < today)
        {
            return Result<VacationRequestResponse>.Failure(ResultError.Validation, "Vacation cannot start in the past.");
        }

        var entity = new VacationRequest
        {
            EmployeeId = employeeId,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Status = VacationStatus.Pending,
            Notes = request.Notes,
            CreatedAt = _timeProvider.GetUtcNow()
        };

        _db.VacationRequests.Add(entity);
        await _db.SaveChangesAsync(ct);

        return await LoadResponseAsync(entity.Id, ct);
    }

    public Task<Result<VacationRequestResponse>> ApproveAsync(int id, DecisionRequest request, CancellationToken ct = default)
        => DecideAsync(id, VacationStatus.Approved, request, ct);

    public Task<Result<VacationRequestResponse>> RejectAsync(int id, DecisionRequest request, CancellationToken ct = default)
        => DecideAsync(id, VacationStatus.Rejected, request, ct);

    private async Task<Result<VacationRequestResponse>> DecideAsync(int id, VacationStatus decision, DecisionRequest request, CancellationToken ct)
    {
        var entity = await _db.VacationRequests.Include(v => v.Employee).FirstOrDefaultAsync(v => v.Id == id, ct);
        if (entity is null)
        {
            return Result<VacationRequestResponse>.Failure(ResultError.NotFound, $"Vacation request {id} was not found.");
        }

        if (!CanDecide(entity))
        {
            return Result<VacationRequestResponse>.Failure(ResultError.Forbidden, "You can only decide on vacation requests of your direct reports.");
        }

        if (entity.Status != VacationStatus.Pending)
        {
            return Result<VacationRequestResponse>.Failure(ResultError.Conflict, $"Vacation request {id} is already {entity.Status} and cannot be changed.");
        }

        if (decision == VacationStatus.Approved)
        {
            var overlaps = await _db.VacationRequests.AnyAsync(v =>
                v.Id != entity.Id &&
                v.Status == VacationStatus.Approved &&
                v.StartDate <= entity.EndDate &&
                entity.StartDate <= v.EndDate, ct);

            if (overlaps)
            {
                return Result<VacationRequestResponse>.Failure(ResultError.Conflict, "Approving this request would overlap an already approved vacation.");
            }
        }

        entity.Status = decision;
        if (!string.IsNullOrWhiteSpace(request.Notes))
        {
            entity.Notes = request.Notes;
        }

        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: ExclusionViolation })
        {
            // Backstop for the race between the pre-check above and the commit:
            // the database GiST exclusion constraint is the source of truth.
            return Result<VacationRequestResponse>.Failure(ResultError.Conflict, "Approving this request would overlap an already approved vacation.");
        }

        return await LoadResponseAsync(entity.Id, ct);
    }

    private bool CanView(VacationRequest request) => _currentUser.Role switch
    {
        Role.Administrator => true,
        Role.Manager => request.EmployeeId == _currentUser.Id || request.Employee.ManagerId == _currentUser.Id,
        _ => request.EmployeeId == _currentUser.Id
    };

    private bool CanDecide(VacationRequest request) => _currentUser.Role switch
    {
        Role.Administrator => true,
        Role.Manager => request.Employee.ManagerId == _currentUser.Id,
        _ => false
    };

    private async Task<Result<VacationRequestResponse>> LoadResponseAsync(int id, CancellationToken ct)
    {
        var entity = await _db.VacationRequests.AsNoTracking().Include(v => v.Employee).FirstAsync(v => v.Id == id, ct);
        return Result<VacationRequestResponse>.Success(ToResponse(entity));
    }

    private static VacationRequestResponse ToResponse(VacationRequest v) => new(
        v.Id,
        v.EmployeeId,
        v.Employee.Name,
        v.StartDate,
        v.EndDate,
        v.Period.TotalDays,
        v.Status.ToString(),
        v.Notes,
        v.CreatedAt);
}
