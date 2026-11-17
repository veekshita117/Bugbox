using Bugbox.Core.DTOs.Comments;
using FluentValidation;

namespace Bugbox.Api.Validators;

public class CreateCommentRequestValidator : AbstractValidator<CreateCommentRequest>
{
    public CreateCommentRequestValidator()
    {
        RuleFor(x => x.Body)
            .NotEmpty().WithMessage("Comment body cannot be empty.")
            .MaximumLength(2000).WithMessage("Comment body cannot exceed 2000 characters.");
    }
}
