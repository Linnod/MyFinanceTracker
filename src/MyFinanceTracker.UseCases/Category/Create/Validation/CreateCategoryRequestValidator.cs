using System.Text.RegularExpressions;
using FluentValidation;

namespace MyFinanceTracker.UseCases.Category.Create.Validation;

internal sealed partial class CreateCategoryRequestValidator : AbstractValidator<CreateCategoryRequest>
{
    public CreateCategoryRequestValidator()
    {
        RuleFor(x => x.Id)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithErrorCode(ValidationErrorCode.Category.CategoryIdRequired)
            .Matches(IdRegex())
            .WithErrorCode(ValidationErrorCode.Category.InvalidIdFormat);

        RuleFor(x => x.Name)
            .NotEmpty()
            .WithErrorCode(ValidationErrorCode.Category.CategoryNameRequired);
    }

    [GeneratedRegex("^[a-z0-9_]+$")]
    private static partial Regex IdRegex();
}
