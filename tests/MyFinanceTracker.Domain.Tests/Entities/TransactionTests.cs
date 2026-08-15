using MyFinanceTracker.Domain.Entities;
using MyFinanceTracker.Domain.Exceptions;

namespace MyFinanceTracker.Domain.Tests.Entities;

public class TransactionTests
{
    private static Category CreateTestCategory() =>
        new("food", "Food", ["food"]);

    [Fact]
    void Constructor_WithExpenseType_SetsNegativeAmountAndInitializesProperties()
    {
        // arrange
        var id = Guid.NewGuid();
        var type = TransactionType.Expense;
        var category = CreateTestCategory();
        var amount = 100.50m;
        var date = new DateOnly(2026, 5, 10);
        var note = "Groceries";

        // act
        var transaction = new Transaction(id, type, category, amount, date, note);

        // assert
        Assert.Equal(id, transaction.Id);
        Assert.Equal(type, transaction.Type);
        Assert.Equal(category, transaction.Category);
        Assert.Equal(-100.50m, transaction.Amount);
        Assert.Equal(date, transaction.Date);
        Assert.Equal(note, transaction.Note);
    }

    [Fact]
    void Constructor_WithIncomeType_SetsPositiveAmountAndInitializesProperties()
    {
        // arrange
        var id = Guid.NewGuid();
        var type = TransactionType.Income;
        var category = new Category("salary", "Salary", ["salary"], isIncome: true);
        var amount = 1500.00m;
        var date = new DateOnly(2026, 5, 10);
        var note = "Monthly salary";

        // act
        var transaction = new Transaction(id, type, category, amount, date, note);

        // assert
        Assert.Equal(id, transaction.Id);
        Assert.Equal(type, transaction.Type);
        Assert.Equal(category, transaction.Category);
        Assert.Equal(1500.00m, transaction.Amount);
        Assert.Equal(date, transaction.Date);
        Assert.Equal(note, transaction.Note);
    }

    [Fact]
    void Constructor_WithEmptyId_ThrowsDomainException()
    {
        // arrange
        var id = Guid.Empty;
        var category = CreateTestCategory();

        // act
        Transaction act() => new(id, TransactionType.Expense, category, 10m, new DateOnly(2026, 5, 10), null);

        // assert
        var exception = Assert.Throws<DomainException>((Func<Transaction>)act);
        Assert.Equal("Transaction ID cannot be empty.", exception.Message);
    }

    [Fact]
    void Constructor_WithNullCategory_ThrowsDomainException()
    {
        // arrange
        var id = Guid.NewGuid();

        // act
        Transaction act() => new(id, TransactionType.Expense, null!, 10m, new DateOnly(2026, 5, 10), null);

        // assert
        var exception = Assert.Throws<DomainException>(act);
        Assert.Equal("Transaction category is required.", exception.Message);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(-1.0)]
    [InlineData(-100.50)]
    void Constructor_WithNonPositiveAmount_ThrowsDomainException(decimal invalidAmount)
    {
        // arrange
        var id = Guid.NewGuid();
        var category = CreateTestCategory();

        // act
        Transaction act() => new(
            id,
            TransactionType.Expense,
            category,
            invalidAmount,
            new DateOnly(2026, 5, 10),
            null);

        // assert
        var exception = Assert.Throws<DomainException>(act);
        Assert.Equal("Transaction amount must be greater than zero.", exception.Message);
    }

    [Theory]
    [InlineData(1899)]
    [InlineData(2101)]
    void Constructor_WithInvalidDateYear_ThrowsDomainException(int invalidYear)
    {
        // arrange
        var id = Guid.NewGuid();
        var category = CreateTestCategory();
        var invalidDate = new DateOnly(invalidYear, 1, 1);
        var expectedMessage = $"Transaction date year must be between {FinancialRules.MinAllowedYear} and {FinancialRules.MaxAllowedYear}.";

        // act
        Transaction act() => new(id, TransactionType.Expense, category, 10m, invalidDate, null);

        // assert
        var exception = Assert.Throws<DomainException>(act);
        Assert.Equal(expectedMessage, exception.Message);
    }
}