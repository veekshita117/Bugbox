using Bugbox.Core.DTOs.Issues;
using FluentValidation;

namespace Bugbox.Api.Validators;

public class MoveIssueRequestValidator : AbstractValidator<MoveIssueRequest>
{
    public MoveIssueRequestValidator()
    {
        RuleFor(x => x.Status)
            .IsInEnum().WithMessage("Invalid issue status.");

        RuleFor(x => x.BoardOrder)
            .GreaterThanOrEqualTo(0).WithMessage("BoardOrder must be greater than or equal to 0.");
    }
}
