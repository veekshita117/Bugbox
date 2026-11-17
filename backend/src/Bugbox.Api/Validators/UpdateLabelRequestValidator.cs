using Bugbox.Core.DTOs.Labels;
using FluentValidation;

namespace Bugbox.Api.Validators;

public class UpdateLabelRequestValidator : AbstractValidator<UpdateLabelRequest>
{
    public UpdateLabelRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Label name is required.")
            .MaximumLength(50).WithMessage("Label name cannot exceed 50 characters.");

        RuleFor(x => x.Color)
            .NotEmpty().WithMessage("Color is required.")
            .MaximumLength(20).WithMessage("Color cannot exceed 20 characters.");
    }
}
