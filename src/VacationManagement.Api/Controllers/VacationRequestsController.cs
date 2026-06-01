using Asp.Versioning;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using VacationManagement.Application.Common;
using VacationManagement.Application.VacationRequests;

namespace VacationManagement.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/vacation-requests")]
[Authorize]
public class VacationRequestsController : ControllerBase
{
    private readonly IVacationRequestService _requests;
    private readonly IValidator<CreateVacationRequestRequest> _createValidator;

    public VacationRequestsController(
        IVacationRequestService requests,
        IValidator<CreateVacationRequestRequest> createValidator)
    {
        _requests = requests;
        _createValidator = createValidator;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<VacationRequestResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var result = await _requests.GetAllAsync(ct);
        return result.Succeeded ? Ok(result.Value) : MapFailure(result.Error, result.Message);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(VacationRequestResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var result = await _requests.GetByIdAsync(id, ct);
        return result.Succeeded ? Ok(result.Value) : MapFailure(result.Error, result.Message);
    }

    [HttpPost]
    [ProducesResponseType(typeof(VacationRequestResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateVacationRequestRequest request, CancellationToken ct)
    {
        var validation = await _createValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            return ValidationProblem(BuildModelState(validation));
        }

        var result = await _requests.CreateAsync(request, ct);
        return result.Succeeded
            ? CreatedAtAction(nameof(GetById), new { id = result.Value!.Id, version = "1.0" }, result.Value)
            : MapFailure(result.Error, result.Message);
    }

    [HttpPost("{id:int}/approve")]
    [Authorize(Roles = "Administrator,Manager")]
    [ProducesResponseType(typeof(VacationRequestResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Approve(int id, [FromBody] DecisionRequest? request, CancellationToken ct)
    {
        var result = await _requests.ApproveAsync(id, request ?? new DecisionRequest(null), ct);
        return result.Succeeded ? Ok(result.Value) : MapFailure(result.Error, result.Message);
    }

    [HttpPost("{id:int}/reject")]
    [Authorize(Roles = "Administrator,Manager")]
    [ProducesResponseType(typeof(VacationRequestResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Reject(int id, [FromBody] DecisionRequest? request, CancellationToken ct)
    {
        var result = await _requests.RejectAsync(id, request ?? new DecisionRequest(null), ct);
        return result.Succeeded ? Ok(result.Value) : MapFailure(result.Error, result.Message);
    }

    private static ModelStateDictionary BuildModelState(FluentValidation.Results.ValidationResult validation)
    {
        var modelState = new ModelStateDictionary();
        foreach (var error in validation.Errors)
        {
            modelState.AddModelError(error.PropertyName, error.ErrorMessage);
        }

        return modelState;
    }

    private IActionResult MapFailure(ResultError error, string? message) => error switch
    {
        ResultError.NotFound => NotFound(new { error = message }),
        ResultError.Conflict => Conflict(new { error = message }),
        ResultError.Forbidden => StatusCode(StatusCodes.Status403Forbidden, new { error = message }),
        _ => BadRequest(new { error = message })
    };
}
