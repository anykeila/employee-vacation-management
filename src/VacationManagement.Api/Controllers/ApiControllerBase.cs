using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using VacationManagement.Application.Common;

namespace VacationManagement.Api.Controllers;

public abstract class ApiControllerBase : ControllerBase
{
    protected IActionResult Failure(ResultError error, string? message) => error switch
    {
        ResultError.NotFound => Problem(detail: message, statusCode: StatusCodes.Status404NotFound, title: "Not Found"),
        ResultError.Conflict => Problem(detail: message, statusCode: StatusCodes.Status409Conflict, title: "Conflict"),
        ResultError.Forbidden => Problem(detail: message, statusCode: StatusCodes.Status403Forbidden, title: "Forbidden"),
        ResultError.Validation => Problem(detail: message, statusCode: StatusCodes.Status400BadRequest, title: "Validation Error"),
        _ => Problem(detail: message, statusCode: StatusCodes.Status400BadRequest, title: "Bad Request")
    };

    protected IActionResult ValidationFailure(FluentValidation.Results.ValidationResult validation)
    {
        var modelState = new ModelStateDictionary();
        foreach (var error in validation.Errors)
        {
            modelState.AddModelError(error.PropertyName, error.ErrorMessage);
        }

        return ValidationProblem(modelState);
    }
}
