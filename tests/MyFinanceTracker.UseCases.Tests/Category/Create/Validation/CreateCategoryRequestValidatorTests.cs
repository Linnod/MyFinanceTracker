using FluentValidation.TestHelper;
using MyFinanceTracker.UseCases.Category.Create;
using MyFinanceTracker.UseCases.Category.Create.Validation;

namespace MyFinanceTracker.UseCases.Tests.Category.Create.Validation;

public class CreateCategoryRequestValidatorTests
{
    private readonly CreateCategoryRequestValidator sut = new();

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_EmptyId_ReturnsIdRequiredError(string? id)
    {
        // arrange
        var request = new CreateCategoryRequest(id!, "Food");

        // act
        var result = sut.TestValidate(request);

        // assert
        result.ShouldHaveValidationErrorFor(x => x.Id)
            .WithErrorCode(ValidationErrorCode.Category.CategoryIdRequired);
    }

    [Theory]
    [InlineData("Food")]
    [InlineData("food!")]
    [InlineData("food 1")]
    [InlineData("food@")]
    [InlineData("FOOD_1")]
    [InlineData("food-1")]
    public void Validate_InvalidIdFormat_ReturnsInvalidIdFormatError(string id)
    {
        // arrange
        var request = new CreateCategoryRequest(id, "Food");

        // act
        var result = sut.TestValidate(request);

        // assert
        result.ShouldHaveValidationErrorFor(x => x.Id)
            .WithErrorCode(ValidationErrorCode.Category.InvalidIdFormat);
    }

    [Theory]
    [InlineData("food")]
    [InlineData("food_1")]
    [InlineData("groceries_2026")]
    [InlineData("123")]
    [InlineData("_category_")]
    public void Validate_ValidId_DoesNotReturnIdErrors(string id)
    {
        // arrange
        var request = new CreateCategoryRequest(id, "Food");

        // act
        var result = sut.TestValidate(request);

        // assert
        result.ShouldNotHaveValidationErrorFor(x => x.Id);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_EmptyName_ReturnsNameRequiredError(string? name)
    {
        // arrange
        var request = new CreateCategoryRequest("food", name!);

        // act
        var result = sut.TestValidate(request);

        // assert
        result.ShouldHaveValidationErrorFor(x => x.Name)
            .WithErrorCode(ValidationErrorCode.Category.CategoryNameRequired);
    }

    [Fact]
    public void Validate_ValidRequest_PassesValidation()
    {
        // arrange
        var request = new CreateCategoryRequest("food_groceries", "Groceries & Food");

        // act
        var result = sut.TestValidate(request);

        // assert
        result.ShouldNotHaveAnyValidationErrors();
    }
}
