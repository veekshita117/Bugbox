using Bugbox.Core.DTOs.Sprints;
using FluentValidation;

namespace Bugbox.Api.Validators;

public class UpdateSprintRequestValidator : AbstractValidator<UpdateSprintRequest>
{
    public UpdateSprintRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Sprint name is required.")
            .MaximumLength(100).WithMessage("Sprint name cannot exceed 100 characters.");

        RuleFor(x => x.Goal)
            .MaximumLength(500).WithMessage("Goal cannot exceed 500 characters.");

        RuleFor(x => x)
            .Must(x => !x.StartDate.HasValue || !x.EndDate.HasValue || x.EndDate >= x.StartDate)
            .WithMessage("End date must be on or after start date.")
            .WithName("EndDate");
    }
}
