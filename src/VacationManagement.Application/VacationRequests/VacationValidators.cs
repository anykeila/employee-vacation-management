using FluentValidation;

namespace VacationManagement.Application.VacationRequests;

public class CreateVacationRequestRequestValidator : AbstractValidator<CreateVacationRequestRequest>
{
    public CreateVacationRequestRequestValidator()
    {
        RuleFor(x => x.StartDate).NotEqual(default(DateOnly)).WithMessage("Start date is required.");
        RuleFor(x => x.EndDate).NotEqual(default(DateOnly)).WithMessage("End date is required.");
        RuleFor(x => x.EndDate)
            .GreaterThanOrEqualTo(x => x.StartDate)
            .WithMessage("End date cannot be earlier than start date.");
        RuleFor(x => x.Notes).MaximumLength(1000);
    }
}
