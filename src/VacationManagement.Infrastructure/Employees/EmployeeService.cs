using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using VacationManagement.Application.Abstractions;
using VacationManagement.Application.Common;
using VacationManagement.Application.Employees;
using VacationManagement.Domain.Entities;
using VacationManagement.Domain.Enums;
using VacationManagement.Infrastructure.Persistence;

namespace VacationManagement.Infrastructure.Employees;

public class EmployeeService : IEmployeeService
{
    private readonly AppDbContext _db;
    private readonly IPasswordHasher _passwordHasher;

    public EmployeeService(AppDbContext db, IPasswordHasher passwordHasher)
    {
        _db = db;
        _passwordHasher = passwordHasher;
    }

    public async Task<IReadOnlyList<EmployeeResponse>> GetAllAsync(CancellationToken ct = default)
    {
        return await _db.Employees
            .AsNoTracking()
            .OrderBy(e => e.Id)
            .Select(Projection)
            .ToListAsync(ct);
    }

    public async Task<EmployeeResponse?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        return await _db.Employees
            .AsNoTracking()
            .Where(e => e.Id == id)
            .Select(Projection)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<Result<EmployeeResponse>> CreateAsync(CreateEmployeeRequest request, CancellationToken ct = default)
    {
        var email = Normalize(request.Email);

        if (await _db.Employees.AnyAsync(e => e.Email == email, ct))
        {
            return Result<EmployeeResponse>.Failure(ResultError.Conflict, $"Email '{email}' is already in use.");
        }

        var managerCheck = await ValidateManagerAsync(request.ManagerId, employeeId: null, ct);
        if (managerCheck is not null)
        {
            return managerCheck;
        }

        var employee = new Employee
        {
            Name = request.Name.Trim(),
            Email = email,
            Role = request.Role,
            ManagerId = request.ManagerId,
            PasswordHash = _passwordHasher.Hash(request.Password)
        };

        _db.Employees.Add(employee);
        await _db.SaveChangesAsync(ct);

        return Result<EmployeeResponse>.Success(await LoadResponseAsync(employee.Id, ct));
    }

    public async Task<Result<EmployeeResponse>> UpdateAsync(int id, UpdateEmployeeRequest request, CancellationToken ct = default)
    {
        var employee = await _db.Employees.FirstOrDefaultAsync(e => e.Id == id, ct);
        if (employee is null)
        {
            return Result<EmployeeResponse>.Failure(ResultError.NotFound, $"Employee {id} was not found.");
        }

        var email = Normalize(request.Email);
        if (await _db.Employees.AnyAsync(e => e.Email == email && e.Id != id, ct))
        {
            return Result<EmployeeResponse>.Failure(ResultError.Conflict, $"Email '{email}' is already in use.");
        }

        if (request.ManagerId == id)
        {
            return Result<EmployeeResponse>.Failure(ResultError.Validation, "An employee cannot be their own manager.");
        }

        var managerCheck = await ValidateManagerAsync(request.ManagerId, employeeId: id, ct);
        if (managerCheck is not null)
        {
            return managerCheck;
        }

        employee.Name = request.Name.Trim();
        employee.Email = email;
        employee.Role = request.Role;
        employee.ManagerId = request.ManagerId;

        await _db.SaveChangesAsync(ct);

        return Result<EmployeeResponse>.Success(await LoadResponseAsync(employee.Id, ct));
    }

    public async Task<Result<bool>> DeleteAsync(int id, CancellationToken ct = default)
    {
        var employee = await _db.Employees.FirstOrDefaultAsync(e => e.Id == id, ct);
        if (employee is null)
        {
            return Result<bool>.Failure(ResultError.NotFound, $"Employee {id} was not found.");
        }

        if (await _db.Employees.AnyAsync(e => e.ManagerId == id, ct))
        {
            return Result<bool>.Failure(ResultError.Conflict, "Cannot delete an employee who still manages other employees.");
        }

        _db.Employees.Remove(employee);
        await _db.SaveChangesAsync(ct);

        return Result<bool>.Success(true);
    }

    private async Task<Result<EmployeeResponse>?> ValidateManagerAsync(int? managerId, int? employeeId, CancellationToken ct)
    {
        if (managerId is null)
        {
            return null;
        }

        var manager = await _db.Employees.AsNoTracking().FirstOrDefaultAsync(e => e.Id == managerId, ct);
        if (manager is null)
        {
            return Result<EmployeeResponse>.Failure(ResultError.Validation, $"Manager {managerId} was not found.");
        }

        if (manager.Role is not (Role.Manager or Role.Administrator))
        {
            return Result<EmployeeResponse>.Failure(ResultError.Validation, "The assigned manager must have the Manager or Administrator role.");
        }

        return null;
    }

    private async Task<EmployeeResponse> LoadResponseAsync(int id, CancellationToken ct)
    {
        return (await _db.Employees
            .AsNoTracking()
            .Where(e => e.Id == id)
            .Select(Projection)
            .FirstAsync(ct));
    }

    private static string Normalize(string email) => email.Trim().ToLowerInvariant();

    private static readonly Expression<Func<Employee, EmployeeResponse>> Projection = e =>
        new EmployeeResponse(e.Id, e.Name, e.Email, e.Role.ToString(), e.ManagerId, e.Manager != null ? e.Manager.Name : null);
}
