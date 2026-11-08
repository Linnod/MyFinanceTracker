using FluentAssertions;
using FluentValidation.TestHelper;
using MyFinanceTracker.Domain.Entities;
using MyFinanceTracker.UseCases.Transaction.Update;
using MyFinanceTracker.UseCases.Transaction.Update.Validation;

namespace MyFinanceTracker.UseCases.Tests.Transaction.Update.Validation;

public class UpdateTransactionRequestValidatorTests
{
    private readonly UpdateTransactionRequestValidator sut = new();

    [Fact]
    public void Validate_EmptyId_ReturnsInvalidIdError()
    {
        // arrange
        var request = new UpdateTransactionRequest(Guid.Empty, Amount: 100m);

        // act
        var result = sut.TestValidate(request);

        // assert
        result.ShouldHaveValidationErrorFor(x => x.Id)
            .WithErrorCode(ValidationErrorCode.Transaction.InvalidId);
    }

    [Fact]
    public void Validate_NoFieldsToUpdate_ReturnsNothingToUpdateErrorWithNullAttemptedValue()
    {
        // arrange
        var request = new UpdateTransactionRequest(Guid.NewGuid());

        // act
        var result = sut.TestValidate(request);

        // assert
        var failure = result.Errors.Should()
            .ContainSingle(e => e.ErrorCode == ValidationErrorCode.Transaction.NothingToUpdate)
            .Subject;

        failure.AttemptedValue.Should().BeNull();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-10.5)]
    public void Validate_AmountIsNotPositive_ReturnsAmountMustBePositiveError(decimal amount)
    {
        // arrange
        var request = new UpdateTransactionRequest(Guid.NewGuid(), Amount: amount);

        // act
        var result = sut.TestValidate(request);

        // assert
        result.ShouldHaveValidationErrorFor(x => x.Amount!.Value)
            .WithErrorCode(ValidationErrorCode.Transaction.AmountMustBePositive);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(1)]
    public void Validate_DateYearOutOfRange_ReturnsDateOutOfRangeError(int offset)
    {
        // arrange
        var year = offset < 0
            ? FinancialRules.MinAllowedYear - 1
            : FinancialRules.MaxAllowedYear + 1;

        var request = new UpdateTransactionRequest(Guid.NewGuid(), Date: new DateTime(year, 1, 1));

        // act
        var result = sut.TestValidate(request);

        // assert
        result.ShouldHaveValidationErrorFor(x => x.Date!.Value.Year)
            .WithErrorCode(ValidationErrorCode.Transaction.DateOutOfRange);
    }

    [Fact]
    public void Validate_WhenOnlyAmountProvided_ReturnsNoErrors()
    {
        // arrange
        var request = new UpdateTransactionRequest(Guid.NewGuid(), Amount: 50m);

        // act
        var result = sut.TestValidate(request);

        // assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WhenOnlyNoteProvided_ReturnsNoErrors()
    {
        // arrange
        var request = new UpdateTransactionRequest(Guid.NewGuid(), Note: "Updated note");

        // act
        var result = sut.TestValidate(request);

        // assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WhenOnlyClearNoteProvided_ReturnsNoErrors()
    {
        // arrange
        var request = new UpdateTransactionRequest(Guid.NewGuid(), ClearNote: true);

        // act
        var result = sut.TestValidate(request);

        // assert
        result.ShouldNotHaveAnyValidationErrors();
    }
}
