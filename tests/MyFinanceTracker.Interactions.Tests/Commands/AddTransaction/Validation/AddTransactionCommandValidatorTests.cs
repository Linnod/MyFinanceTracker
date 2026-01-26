using FluentAssertions;
using MyFinanceTracker.Domain.Entities;
using MyFinanceTracker.Interactions.Commands.AddTransaction.Parsing;
using MyFinanceTracker.Interactions.Commands.AddTransaction.Validation;

namespace MyFinanceTracker.Interactions.Tests.Commands.AddTransaction.Validation;

public class AddTransactionCommandValidatorTests
{
    private readonly AddTransactionCommandValidator validator;

    public AddTransactionCommandValidatorTests()
    {
        validator = new AddTransactionCommandValidator();
    }

    [Fact]
    void Validate_EmptyAmounts_ReturnsMissingAmounts()
    {
        // arrange
        var raw = new RawAddTransactionCommand(TransactionType.Expense, [], "food");

        // act
        var result = validator.Validate(raw);

        // assert
        result.Should().BeOfType<AddTransactionCommandValidationResult.MissingAmounts>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    void Validate_ExpenseWithoutCategory_ReturnsCategoryRequired(string? invalidAlias)
    {
        // arrange
        var raw = new RawAddTransactionCommand(TransactionType.Expense, [100], invalidAlias);

        // act
        var result = validator.Validate(raw);

        // assert
        result.Should().BeOfType<AddTransactionCommandValidationResult.CategoryRequired>()
            .Which.Type.Should().Be(TransactionType.Expense);
    }

    [Fact]
    void Validate_IncomeWithoutCategory_DefaultsToIncomeAlias()
    {
        // arrange
        var raw = new RawAddTransactionCommand(TransactionType.Income, [500], null);

        // act
        var result = validator.Validate(raw);

        // assert
        var success = result.Should().BeOfType<AddTransactionCommandValidationResult.Success>().Subject;
        success.Transaction.CategoryAlias.Should().Be(FinancialRules.DefaultIncomeCategoryAlias);
    }

    [Fact]
    void Validate_WhenDateIsNull_SetsCurrentDate()
    {
        // arrange
        var raw = new RawAddTransactionCommand(TransactionType.Income, [100], "gift", Date: null);
        var today = DateOnly.FromDateTime(DateTime.Now);

        // act
        var result = validator.Validate(raw);

        // assert
        var success = result.Should().BeOfType<AddTransactionCommandValidationResult.Success>().Subject;
        success.Transaction.Date.Should().Be(today);
    }

    [Fact]
    void Validate_ValidData_ReturnsSuccessWithAllFields()
    {
        // arrange
        var amounts = new[] { 10.0m, 20.5m };
        var date = new DateOnly(2026, 1, 1);
        var raw = new RawAddTransactionCommand(TransactionType.Expense, amounts, "transport", date, "Taxi to work");

        // act
        var result = validator.Validate(raw);

        // assert
        var success = result.Should().BeOfType<AddTransactionCommandValidationResult.Success>().Subject;
        
        success.Transaction.Should().BeEquivalentTo(new
        {
            Type = TransactionType.Expense,
            Amounts = amounts,
            CategoryAlias = "transport",
            Date = date,
            Note = "Taxi to work"
        });
    }
}