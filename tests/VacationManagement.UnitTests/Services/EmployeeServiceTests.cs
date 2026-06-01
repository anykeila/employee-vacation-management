using FluentAssertions;
using VacationManagement.Application.Common;
using VacationManagement.Application.Employees;
using VacationManagement.Domain.Enums;
using VacationManagement.Infrastructure.Employees;
using VacationManagement.Infrastructure.Persistence;
using VacationManagement.UnitTests.Support;
using Xunit;

namespace VacationManagement.UnitTests.Services;

public class EmployeeServiceTests
{
    // Seeded: 1 Ana (Administrator), 2 João (Manager of 3), 5 Joana (Manager of 4),
    // 3 Marta (Employee), 4 Henrique (Employee).
    private const int AdminId = 1;
    private const int ManagerId = 2;
    private const int EmployeeId = 3;

    private static EmployeeService Service(AppDbContext db)
        => new(db, new FakePasswordHasher());

    [Fact]
    public async Task Create_WithEmailAlreadyInUse_ReturnsConflict()
    {
        using var db = TestDb.NewSeededContext();
        var service = Service(db);

        // Mixed case proves emails are normalised before the uniqueness check.
        var result = await service.CreateAsync(new CreateEmployeeRequest(
            "Duplicate", "Ana.Silva@workflow.com", "Password123!", Role.Employee, null));

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be(ResultError.Conflict);
    }

    [Fact]
    public async Task Create_WithUnknownManager_ReturnsValidationError()
    {
        using var db = TestDb.NewSeededContext();
        var service = Service(db);

        var result = await service.CreateAsync(new CreateEmployeeRequest(
            "New", "new@workflow.com", "Password123!", Role.Employee, ManagerId: 999));

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be(ResultError.Validation);
    }

    [Fact]
    public async Task Create_WithManagerWhoIsNotAManager_ReturnsValidationError()
    {
        using var db = TestDb.NewSeededContext();
        var service = Service(db);

        // Employee 3 (Marta) holds the Employee role, so she cannot be a manager.
        var result = await service.CreateAsync(new CreateEmployeeRequest(
            "New", "new@workflow.com", "Password123!", Role.Employee, ManagerId: EmployeeId));

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be(ResultError.Validation);
    }

    [Fact]
    public async Task Create_WithValidManager_Succeeds()
    {
        using var db = TestDb.NewSeededContext();
        var service = Service(db);

        var result = await service.CreateAsync(new CreateEmployeeRequest(
            "New", "new@workflow.com", "Password123!", Role.Employee, ManagerId));

        result.Succeeded.Should().BeTrue();
        result.Value!.ManagerId.Should().Be(ManagerId);
    }

    [Fact]
    public async Task Update_SettingEmployeeAsTheirOwnManager_ReturnsValidationError()
    {
        using var db = TestDb.NewSeededContext();
        var service = Service(db);

        var result = await service.UpdateAsync(EmployeeId, new UpdateEmployeeRequest(
            "Marta Fernandes", "marta.fernandes@workflow.com", Role.Employee, ManagerId: EmployeeId));

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be(ResultError.Validation);
    }

    [Fact]
    public async Task Delete_EmployeeWhoStillManagesOthers_ReturnsConflict()
    {
        using var db = TestDb.NewSeededContext();
        var service = Service(db);

        // João (2) is the manager of Marta (3), so he cannot be deleted.
        var result = await service.DeleteAsync(ManagerId);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be(ResultError.Conflict);
    }

    [Fact]
    public async Task Delete_LeafEmployee_Succeeds()
    {
        using var db = TestDb.NewSeededContext();
        var service = Service(db);

        var result = await service.DeleteAsync(EmployeeId); // Marta manages nobody

        result.Succeeded.Should().BeTrue();
    }
}
