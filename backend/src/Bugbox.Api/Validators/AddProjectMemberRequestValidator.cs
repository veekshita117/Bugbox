using Bugbox.Core.DTOs.Projects;
using FluentValidation;

namespace Bugbox.Api.Validators;

public class AddProjectMemberRequestValidator : AbstractValidator<AddProjectMemberRequest>
{
    public AddProjectMemberRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("A valid email address is required.");

        RuleFor(x => x.Role)
            .IsInEnum().WithMessage("A valid project role (Admin or Member) is required.");
    }
}
