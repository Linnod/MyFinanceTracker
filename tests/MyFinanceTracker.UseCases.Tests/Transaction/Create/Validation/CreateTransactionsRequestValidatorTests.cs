using FluentAssertions;
using FluentValidation.TestHelper;
using MyFinanceTracker.Domain.Entities;
using MyFinanceTracker.UseCases.Transaction.Create;
using MyFinanceTracker.UseCases.Transaction.Create.Validation;

namespace MyFinanceTracker.UseCases.Tests.Transaction.Create.Validation;

public class CreateTransactionsHandlerTests
{
    private readonly CreateTransactionsRequestValidator sut;

    public CreateTransactionsHandlerTests()
    {
        sut = new CreateTransactionsRequestValidator();
    }

    [Fact]
    void Validate_RequestIsValid_ReturnsNoErrors()
    {
        // arrange
        var request = new CreateTransactionsRequest(
        [
            new CreateTransactionItem(
                TransactionType.Expense,
                100m,
                "food",
                new DateOnly(2026, 1, 27),
                "Lunch")
        ]);

        // act
        var result = sut.TestValidate(request);

        // assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    void Validate_ItemsEmpty_ReturnsRequiredError()
    {
        // arrange
        var request = new CreateTransactionsRequest([]);

        // act
        var result = sut.TestValidate(request);

        // assert
        result.ShouldHaveValidationErrorFor(x => x.Items)
            .WithErrorCode(ValidationErrorCode.Common.Required);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    void Validate_AmountIsNotPositive_ReturnsMustBePositiveError(decimal amount)
    {
        // arrange
        var request = new CreateTransactionsRequest(
        [
            new CreateTransactionItem(
            TransactionType.Expense,
            amount,
            "food",
            new DateOnly(2026, 1, 27),
            "Lunch")
        ]);

        // act
        var result = sut.TestValidate(request);

        // assert
        result.ShouldHaveValidationErrorFor("Items[0].Amount")
            .WithErrorCode(ValidationErrorCode.Common.MustBePositive);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(1)]
    void Validate_DateOutOfRange_ReturnsError(int offset)
    {
        // arrange
        var year = offset < 0
            ? FinancialRules.MinAllowedYear - 1
            : FinancialRules.MaxAllowedYear + 1;

        var request = new CreateTransactionsRequest(
        [
            new CreateTransactionItem(
                TransactionType.Expense,
                100m,
                "food",
                new DateOnly(year, 1, 1),
                "Lunch")
        ]);

        // act
        var result = sut.TestValidate(request);

        // assert
        result.Errors.Should().ContainSingle(e =>
            e.PropertyName == "Items[0].Date.Value.Year" &&
            e.ErrorCode == ValidationErrorCode.Common.DateOutOfRange);
    }
}