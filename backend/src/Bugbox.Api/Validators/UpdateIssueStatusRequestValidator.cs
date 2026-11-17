using Bugbox.Core.DTOs.Issues;
using FluentValidation;

namespace Bugbox.Api.Validators;

public class UpdateIssueStatusRequestValidator : AbstractValidator<UpdateIssueStatusRequest>
{
    public UpdateIssueStatusRequestValidator()
    {
        RuleFor(x => x.Status)
            .IsInEnum().WithMessage("Invalid issue status.");
    }
}
