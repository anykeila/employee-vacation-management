using FluentAssertions;
using VacationManagement.Application.Abstractions;
using VacationManagement.Application.Common;
using VacationManagement.Application.VacationRequests;
using VacationManagement.Domain.Entities;
using VacationManagement.Domain.Enums;
using VacationManagement.Infrastructure.Persistence;
using VacationManagement.Infrastructure.VacationRequests;
using VacationManagement.UnitTests.Support;
using Xunit;

namespace VacationManagement.UnitTests.Services;

public class VacationRequestServiceTests
{
    // Seeded roles: 1 Ana (Administrator), 2 João (Manager of 3), 5 Joana (Manager of 4),
    // 3 Marta (Employee, manager 2), 4 Henrique (Employee, manager 5).
    private const int AdminId = 1;
    private const int ManagerOf3 = 2;
    private const int ManagerOf4 = 5;
    private const int Employee3 = 3;
    private const int Employee4 = 4;

    private static readonly DateTimeOffset Now = new(2026, 6, 1, 0, 0, 0, TimeSpan.Zero);

    private static VacationRequestService Service(AppDbContext db, int userId, Role role)
        => new(db, new FakeCurrentUser(userId, role), new FixedTimeProvider(Now));

    private static VacationRequest Seed(AppDbContext db, int empId, DateOnly start, DateOnly end, VacationStatus status)
    {
        var entity = new VacationRequest
        {
            EmployeeId = empId,
            StartDate = start,
            EndDate = end,
            Status = status,
            CreatedAt = Now
        };
        db.VacationRequests.Add(entity);
        db.SaveChanges();
        return entity;
    }

    [Fact]
    public async Task Create_WithFutureDates_CreatesPendingRequestForCurrentUser()
    {
        using var db = TestDb.NewSeededContext();
        var service = Service(db, Employee3, Role.Employee);

        var result = await service.CreateAsync(new CreateVacationRequestRequest(
            new DateOnly(2026, 12, 1), new DateOnly(2026, 12, 5), "Christmas"));

        result.Succeeded.Should().BeTrue();
        result.Value!.Status.Should().Be(nameof(VacationStatus.Pending));
        result.Value.EmployeeId.Should().Be(Employee3);
        result.Value.TotalDays.Should().Be(5);
    }

    [Fact]
    public async Task Create_WithStartDateInThePast_ReturnsValidationError()
    {
        using var db = TestDb.NewSeededContext();
        var service = Service(db, Employee3, Role.Employee);

        var result = await service.CreateAsync(new CreateVacationRequestRequest(
            new DateOnly(2025, 1, 1), new DateOnly(2025, 1, 5), null));

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be(ResultError.Validation);
    }

    [Fact]
    public async Task Approve_ByDirectManager_TransitionsToApproved()
    {
        using var db = TestDb.NewSeededContext();
        var request = Seed(db, Employee3, new DateOnly(2026, 12, 1), new DateOnly(2026, 12, 5), VacationStatus.Pending);
        var service = Service(db, ManagerOf3, Role.Manager);

        var result = await service.ApproveAsync(request.Id, new DecisionRequest(null));

        result.Succeeded.Should().BeTrue();
        result.Value!.Status.Should().Be(nameof(VacationStatus.Approved));
    }

    [Fact]
    public async Task Approve_ByManagerOfAnotherTeam_ReturnsForbidden()
    {
        using var db = TestDb.NewSeededContext();
        var request = Seed(db, Employee3, new DateOnly(2026, 12, 1), new DateOnly(2026, 12, 5), VacationStatus.Pending);
        var service = Service(db, ManagerOf4, Role.Manager); // not the manager of employee 3

        var result = await service.ApproveAsync(request.Id, new DecisionRequest(null));

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be(ResultError.Forbidden);
        db.VacationRequests.Single(v => v.Id == request.Id).Status.Should().Be(VacationStatus.Pending);
    }

    [Fact]
    public async Task Approve_ByAdministrator_IsAllowedForAnyEmployee()
    {
        using var db = TestDb.NewSeededContext();
        var request = Seed(db, Employee4, new DateOnly(2026, 12, 1), new DateOnly(2026, 12, 5), VacationStatus.Pending);
        var service = Service(db, AdminId, Role.Administrator);

        var result = await service.ApproveAsync(request.Id, new DecisionRequest(null));

        result.Succeeded.Should().BeTrue();
        result.Value!.Status.Should().Be(nameof(VacationStatus.Approved));
    }

    [Fact]
    public async Task Approve_WhenOverlapsAnotherEmployeesApprovedVacation_ReturnsConflict()
    {
        using var db = TestDb.NewSeededContext();
        // Employee 3 already approved for Dec 1-5; employee 4 requests an overlapping Dec 3-7.
        Seed(db, Employee3, new DateOnly(2026, 12, 1), new DateOnly(2026, 12, 5), VacationStatus.Approved);
        var pending = Seed(db, Employee4, new DateOnly(2026, 12, 3), new DateOnly(2026, 12, 7), VacationStatus.Pending);
        var service = Service(db, ManagerOf4, Role.Manager);

        var result = await service.ApproveAsync(pending.Id, new DecisionRequest(null));

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be(ResultError.Conflict);
        db.VacationRequests.Single(v => v.Id == pending.Id).Status.Should().Be(VacationStatus.Pending);
    }

    [Fact]
    public async Task Approve_WhenAdjacentToApprovedVacation_IsAllowed()
    {
        using var db = TestDb.NewSeededContext();
        // Employee 3 approved Dec 1-5; employee 4 requests Dec 6-10 (adjacent, no shared day).
        Seed(db, Employee3, new DateOnly(2026, 12, 1), new DateOnly(2026, 12, 5), VacationStatus.Approved);
        var pending = Seed(db, Employee4, new DateOnly(2026, 12, 6), new DateOnly(2026, 12, 10), VacationStatus.Pending);
        var service = Service(db, ManagerOf4, Role.Manager);

        var result = await service.ApproveAsync(pending.Id, new DecisionRequest(null));

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task Approve_WhenAlreadyDecided_ReturnsConflict()
    {
        using var db = TestDb.NewSeededContext();
        var request = Seed(db, Employee3, new DateOnly(2026, 12, 1), new DateOnly(2026, 12, 5), VacationStatus.Approved);
        var service = Service(db, ManagerOf3, Role.Manager);

        var result = await service.ApproveAsync(request.Id, new DecisionRequest(null));

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be(ResultError.Conflict);
    }

    [Fact]
    public async Task Reject_ByDirectManager_TransitionsToRejected()
    {
        using var db = TestDb.NewSeededContext();
        var request = Seed(db, Employee4, new DateOnly(2026, 12, 1), new DateOnly(2026, 12, 5), VacationStatus.Pending);
        var service = Service(db, ManagerOf4, Role.Manager);

        var result = await service.RejectAsync(request.Id, new DecisionRequest("Team coverage"));

        result.Succeeded.Should().BeTrue();
        result.Value!.Status.Should().Be(nameof(VacationStatus.Rejected));
    }

    [Fact]
    public async Task GetById_WhenEmployeeRequestsAnotherEmployeesRecord_ReturnsForbidden()
    {
        using var db = TestDb.NewSeededContext();
        var request = Seed(db, Employee4, new DateOnly(2026, 12, 1), new DateOnly(2026, 12, 5), VacationStatus.Pending);
        var service = Service(db, Employee3, Role.Employee);

        var result = await service.GetByIdAsync(request.Id);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be(ResultError.Forbidden);
    }
}
