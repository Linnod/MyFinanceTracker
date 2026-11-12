using FluentAssertions;
using FluentValidation.TestHelper;
using MyFinanceTracker.UseCases.Category.Update;
using MyFinanceTracker.UseCases.Category.Update.Validation;

namespace MyFinanceTracker.UseCases.Tests.Category.Update.Validation;

public class UpdateCategoryRequestValidatorTests
{
    private readonly UpdateCategoryRequestValidator sut = new();

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_EmptyId_ReturnsIdRequiredError(string? id)
    {
        // arrange
        var request = new UpdateCategoryRequest(id!, Name: "Food");

        // act
        var result = sut.TestValidate(request);

        // assert
        result.ShouldHaveValidationErrorFor(x => x.Id)
            .WithErrorCode(ValidationErrorCode.Category.CategoryIdRequired);
    }

    [Fact]
    public void Validate_NoChangesSpecified_ReturnsNoChangesSpecifiedErrorWithNullAttemptedValue()
    {
        // arrange
        var request = new UpdateCategoryRequest("food");

        // act
        var result = sut.TestValidate(request);

        // assert
        var failure = result.Errors.Should()
            .ContainSingle(e => e.ErrorCode == ValidationErrorCode.Category.NoChangesSpecified)
            .Subject;

        failure.AttemptedValue.Should().BeNull();
    }

    [Fact]
    public void Validate_WhitespaceNameAndNoIsArchived_ReturnsNoChangesSpecifiedError()
    {
        // arrange
        var request = new UpdateCategoryRequest("food", Name: "   ");

        // act
        var result = sut.TestValidate(request);

        // assert
        result.Errors.Should()
            .ContainSingle(e => e.ErrorCode == ValidationErrorCode.Category.NoChangesSpecified);
    }

    [Fact]
    public void Validate_WhenOnlyNameProvided_PassesValidation()
    {
        // arrange
        var request = new UpdateCategoryRequest("food", Name: "New Name");

        // act
        var result = sut.TestValidate(request);

        // assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WhenOnlyIsArchivedProvided_PassesValidation()
    {
        // arrange
        var request = new UpdateCategoryRequest("food", IsArchived: true);

        // act
        var result = sut.TestValidate(request);

        // assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WhenBothNameAndIsArchivedProvided_PassesValidation()
    {
        // arrange
        var request = new UpdateCategoryRequest("food", Name: "New Name", IsArchived: false);

        // act
        var result = sut.TestValidate(request);

        // assert
        result.ShouldNotHaveAnyValidationErrors();
    }
}
