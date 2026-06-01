using Asp.Versioning;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VacationManagement.Application.Common;
using VacationManagement.Application.Employees;

namespace VacationManagement.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/employees")]
[Authorize]
public class EmployeesController : ApiControllerBase
{
    private readonly IEmployeeService _employees;
    private readonly IValidator<CreateEmployeeRequest> _createValidator;
    private readonly IValidator<UpdateEmployeeRequest> _updateValidator;

    public EmployeesController(
        IEmployeeService employees,
        IValidator<CreateEmployeeRequest> createValidator,
        IValidator<UpdateEmployeeRequest> updateValidator)
    {
        _employees = employees;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    [HttpGet]
    [Authorize(Roles = "Administrator,Manager")]
    [ProducesResponseType(typeof(PagedResult<EmployeeResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] EmployeeQuery query, CancellationToken ct)
        => Ok(await _employees.GetAllAsync(query, ct));

    [HttpGet("{id:int}")]
    [Authorize(Roles = "Administrator,Manager")]
    [ProducesResponseType(typeof(EmployeeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var employee = await _employees.GetByIdAsync(id, ct);
        return employee is null ? NotFound() : Ok(employee);
    }

    [HttpPost]
    [Authorize(Roles = "Administrator")]
    [ProducesResponseType(typeof(EmployeeResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] CreateEmployeeRequest request, CancellationToken ct)
    {
        var validation = await _createValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            return ValidationFailure(validation);
        }

        var result = await _employees.CreateAsync(request, ct);
        return result.Succeeded
            ? CreatedAtAction(nameof(GetById), new { id = result.Value!.Id, version = "1.0" }, result.Value)
            : Failure(result.Error, result.Message);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Administrator")]
    [ProducesResponseType(typeof(EmployeeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateEmployeeRequest request, CancellationToken ct)
    {
        var validation = await _updateValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            return ValidationFailure(validation);
        }

        var result = await _employees.UpdateAsync(id, request, ct);
        return result.Succeeded
            ? Ok(result.Value)
            : Failure(result.Error, result.Message);
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Administrator")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var result = await _employees.DeleteAsync(id, ct);
        return result.Succeeded ? NoContent() : Failure(result.Error, result.Message);
    }
}
