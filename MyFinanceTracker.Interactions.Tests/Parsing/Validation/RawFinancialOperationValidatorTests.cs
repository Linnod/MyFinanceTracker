using FluentAssertions;
using MyFinanceTracker.Domain.Entities;
using MyFinanceTracker.Interactions.Parsing.Models;
using MyFinanceTracker.Interactions.Parsing.Validation;
using MyFinanceTracker.Interactions.Parsing.Validation.Exceptions;

namespace MyFinanceTracker.Interactions.Tests.Parsing.Validation;

public class RawFinancialOperationValidatorTests
{
    private readonly RawFinancialOperationValidator validator = new();

    [Fact]
    public void Validate_ShouldNotThrow_WhenOperationIsValid()
    {
        // arrange
        var raw = new RawFinancialOperation(
            FinancialOperationType.Expense,
            "food",
            [10.5m],
            null,
            "Lunch"
        );

        // act
        var act = () => validator.Validate(raw);

        // assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_ShouldThrowNoAmountsFound_WhenAmountsArrayIsEmpty()
    {
        // arrange
        var raw = new RawFinancialOperation(null, null, [], null, "");

        // act & assert
        validator.Invoking(x => x.Validate(raw))
            .Should().Throw<ValidationException>()
            .WithMessage("I couldn't find any amounts. Please specify the sum.");
    }

    [Theory]
    [InlineData(FinancialOperationType.Expense, "Expense")]
    [InlineData(FinancialOperationType.Return, "Return")]
    [InlineData(null, "Operation")]
    public void Validate_ShouldThrowCategoryRequired_WhenCategoryIsMissing(
        FinancialOperationType? type,
        string expectedTypeName)
    {
        // arrange
        var raw = new RawFinancialOperation(type, "  ", [100], null, "");

        // act & assert
        validator.Invoking(x => x.Validate(raw))
            .Should().Throw<ValidationException>()
            .WithMessage($"{expectedTypeName} requires a category.");
    }

    [Fact]
    public void Validate_ShouldThrowIncomeShouldNotHaveCategory_WhenIncomeHasCategory()
    {
        // arrange
        var raw = new RawFinancialOperation(
            FinancialOperationType.Income,
            "salary",
            [500],
            null,
            ""
        );

        // act & assert
        validator.Invoking(x => x.Validate(raw))
            .Should().Throw<ValidationException>()
            .WithMessage("Income transactions should not have a category assigned.");
    }

    [Fact]
    public void Validate_ShouldNotThrow_WhenIncomeHasNoCategory()
    {
        // arrange
        var raw = new RawFinancialOperation(
            FinancialOperationType.Income,
            null,
            [500],
            null,
            ""
        );

        // act
        var act = () => validator.Validate(raw);

        // assert
        act.Should().NotThrow();
    }
}