using FluentValidation;
using FluentValidation.Results;

namespace MyFinanceTracker.UseCases.Category.Update.Validation;

internal sealed class UpdateCategoryRequestValidator : AbstractValidator<UpdateCategoryRequest>
{
    public UpdateCategoryRequestValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithErrorCode(ValidationErrorCode.Category.CategoryIdRequired);

        RuleFor(x => x)
            .Custom((request, context) =>
            {
                if (!HasAnyFieldToUpdate(request))
                {
                    context.AddFailure(new ValidationFailure
                    {
                        PropertyName = string.Empty,
                        ErrorCode = ValidationErrorCode.Category.NoChangesSpecified,
                        AttemptedValue = null
                    });
                }
            });
    }

    private static bool HasAnyFieldToUpdate(UpdateCategoryRequest request)
    {
        return !string.IsNullOrWhiteSpace(request.Name) || request.IsArchived.HasValue;
    }
}
