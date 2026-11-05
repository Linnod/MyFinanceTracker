using FluentAssertions;
using MyFinanceTracker.Domain.Entities;
using MyFinanceTracker.InputProcessing;

namespace MyFinanceTracker.Interactions.Telegram.Tests;

public class TelegramResponseFormatterTests
{
    private static readonly Category TestCategory = new("groceries", "Groceries", ["food"]);

    [Fact]
    public void FormatResponse_Listed_WithSmallCount_ShowsAllTransactions()
    {
        // Arrange
        var transactions = new List<Transaction>
        {
            new(Guid.NewGuid(), TransactionType.Expense, TestCategory, 100m, new DateOnly(2026, 11, 1), "Milk"),
            new(Guid.NewGuid(), TransactionType.Expense, TestCategory, 200m, new DateOnly(2026, 11, 2), "Bread"),
        };

        var action = new ActionResult.Transaction.Listed(
            new DateOnly(2026, 11, 1),
            new DateOnly(2026, 11, 2),
            [],
            transactions)
        {
            RawInput = "get transactions"
        };

        var result = new ProcessingResult.Completed([action]);

        // Act
        var formatted = TelegramResponseFormatter.FormatResponse(result);

        // Assert
        formatted.Should().Contain(transactions[0].Id.ToString());
        formatted.Should().Contain(transactions[1].Id.ToString());
        formatted.Should().Contain("Total: <code>-300.00</code>");
        formatted.Should().NotContain("more transaction(s) hidden");
        formatted.Length.Should().BeLessThanOrEqualTo(4096);
    }

    [Fact]
    public void FormatResponse_Listed_WithMoreThanMax_CapsDisplayedAndPreservesTotal()
    {
        // Arrange: 50 transactions
        var transactions = Enumerable.Range(1, 50)
            .Select(i => new Transaction(
                Guid.NewGuid(),
                TransactionType.Expense,
                TestCategory,
                10m,
                new DateOnly(2026, 11, 1),
                $"Item {i}"))
            .ToList();

        var action = new ActionResult.Transaction.Listed(
            new DateOnly(2026, 11, 1),
            new DateOnly(2026, 11, 30),
            [],
            transactions)
        {
            RawInput = "get transactions"
        };

        var result = new ProcessingResult.Completed([action]);

        // Act
        var formatted = TelegramResponseFormatter.FormatResponse(result);

        // Assert
        formatted.Length.Should().BeLessThanOrEqualTo(4096);
        formatted.Should().Contain("Total: <code>-500.00</code>"); // 50 * 10
        formatted.Should().Contain("more transaction(s) hidden");
        formatted.Should().Contain("and 25 more transaction(s) hidden");
    }

    [Fact]
    public void FormatResponse_Listed_WithLongNotes_NeverExceedsTelegramLimit()
    {
        // Arrange: 30 transactions with long notes (150 chars each)
        var longNote = new string('A', 150);
        var transactions = Enumerable.Range(1, 30)
            .Select(_ => new Transaction(
                Guid.NewGuid(),
                TransactionType.Expense,
                TestCategory,
                50m,
                new DateOnly(2026, 11, 1),
                longNote))
            .ToList();

        var action = new ActionResult.Transaction.Listed(
            new DateOnly(2026, 11, 1),
            new DateOnly(2026, 11, 30),
            [],
            transactions)
        {
            RawInput = "get transactions"
        };

        var result = new ProcessingResult.Completed([action]);

        // Act
        var formatted = TelegramResponseFormatter.FormatResponse(result);

        // Assert
        formatted.Length.Should().BeLessThanOrEqualTo(4096);
        formatted.Should().Contain("more transaction(s) hidden");
    }
}
