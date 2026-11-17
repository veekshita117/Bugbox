using Bugbox.Core.DTOs.Issues;
using FluentValidation;

namespace Bugbox.Api.Validators;

public class CreateIssueRequestValidator : AbstractValidator<CreateIssueRequest>
{
    public CreateIssueRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Issue title is required.")
            .MaximumLength(200).WithMessage("Issue title cannot exceed 200 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(5000).WithMessage("Description cannot exceed 5000 characters.");

        RuleFor(x => x.Type)
            .IsInEnum().WithMessage("A valid issue type is required.");

        RuleFor(x => x.Priority)
            .IsInEnum().WithMessage("A valid issue priority is required.");

        RuleFor(x => x.Status)
            .IsInEnum().WithMessage("A valid issue status is required.");
    }
}
