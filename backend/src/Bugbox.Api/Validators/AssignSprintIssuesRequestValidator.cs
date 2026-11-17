using Bugbox.Core.DTOs.Sprints;
using FluentValidation;

namespace Bugbox.Api.Validators;

public class AssignSprintIssuesRequestValidator : AbstractValidator<AssignSprintIssuesRequest>
{
    public AssignSprintIssuesRequestValidator()
    {
        RuleFor(x => x.IssueIds)
            .NotNull().WithMessage("IssueIds list is required.")
            .NotEmpty().WithMessage("At least one issue ID must be provided.");
    }
}
