using FluentAssertions;
using VacationManagement.Application.Employees;
using VacationManagement.Application.VacationRequests;
using VacationManagement.Domain.Enums;
using Xunit;

namespace VacationManagement.UnitTests.Validation;

public class ValidatorTests
{
    private readonly CreateEmployeeRequestValidator _employeeValidator = new();
    private readonly CreateVacationRequestRequestValidator _vacationValidator = new();

    [Fact]
    public void CreateEmployee_WithValidPayload_Passes()
    {
        var request = new CreateEmployeeRequest("Ana", "ana@workflow.com", "Password123!", Role.Employee, null);

        _employeeValidator.Validate(request).IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("", "ana@workflow.com", "Password123!")]      // empty name
    [InlineData("Ana", "not-an-email", "Password123!")]       // invalid email
    [InlineData("Ana", "ana@workflow.com", "short")]          // password < 8
    public void CreateEmployee_WithInvalidPayload_Fails(string name, string email, string password)
    {
        var request = new CreateEmployeeRequest(name, email, password, Role.Employee, null);

        _employeeValidator.Validate(request).IsValid.Should().BeFalse();
    }

    [Fact]
    public void CreateEmployee_WithUndefinedRole_Fails()
    {
        var request = new CreateEmployeeRequest("Ana", "ana@workflow.com", "Password123!", (Role)99, null);

        _employeeValidator.Validate(request).IsValid.Should().BeFalse();
    }

    [Fact]
    public void CreateVacation_WithValidRange_Passes()
    {
        var request = new CreateVacationRequestRequest(new DateOnly(2026, 12, 1), new DateOnly(2026, 12, 5), null);

        _vacationValidator.Validate(request).IsValid.Should().BeTrue();
    }

    [Fact]
    public void CreateVacation_WithEndBeforeStart_Fails()
    {
        var request = new CreateVacationRequestRequest(new DateOnly(2026, 12, 5), new DateOnly(2026, 12, 1), null);

        _vacationValidator.Validate(request).IsValid.Should().BeFalse();
    }

    [Fact]
    public void CreateVacation_WithMissingDates_Fails()
    {
        var request = new CreateVacationRequestRequest(default, default, null);

        _vacationValidator.Validate(request).IsValid.Should().BeFalse();
    }

    [Fact]
    public void CreateVacation_WithOverlongNotes_Fails()
    {
        var request = new CreateVacationRequestRequest(
            new DateOnly(2026, 12, 1), new DateOnly(2026, 12, 5), new string('x', 1001));

        _vacationValidator.Validate(request).IsValid.Should().BeFalse();
    }
}
