using Bugbox.Core.DTOs.Projects;
using FluentValidation;

namespace Bugbox.Api.Validators;

public class CreateProjectRequestValidator : AbstractValidator<CreateProjectRequest>
{
    public CreateProjectRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Project name is required.")
            .MaximumLength(100).WithMessage("Project name cannot exceed 100 characters.");

        RuleFor(x => x.Key)
            .NotEmpty().WithMessage("Project key is required.")
            .MinimumLength(2).WithMessage("Project key must be at least 2 characters.")
            .MaximumLength(10).WithMessage("Project key cannot exceed 10 characters.")
            .Matches("^[A-Za-z0-9]+$").WithMessage("Project key must contain only letters and numbers.");

        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage("Description cannot exceed 1000 characters.");
    }
}
