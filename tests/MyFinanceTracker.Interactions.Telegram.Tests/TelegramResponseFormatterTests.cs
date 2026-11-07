using FluentAssertions;
using MyFinanceTracker.Common.Notifications;
using MyFinanceTracker.Domain.Entities;
using MyFinanceTracker.InputProcessing;
using MyFinanceTracker.UseCases;

namespace MyFinanceTracker.Interactions.Telegram.Tests;

public class TelegramResponseFormatterTests
{
    private static readonly Category TestCategory = new("groceries", "Groceries", ["food"]);

    [Fact]
    public void FormatResponse_Added_SingleTransactionWithNote_FormatsPropertiesAsCleanLines()
    {
        // arrange
        var id = Guid.NewGuid();
        var transaction = new Transaction(
            id,
            TransactionType.Expense,
            TestCategory,
            16.59m,
            new DateOnly(2026, 11, 7),
            "Carrefour");

        var action = new ActionResult.Transaction.Added([transaction])
        {
            RawInput = "groceries 16.59 Carrefour"
        };
        var result = new ProcessingResult.Completed([action]);

        // act
        var formatted = TelegramResponseFormatter.FormatResponse(result);

        // assert
        var expected = $"""
            <b>Created 1 transaction(s)</b>

            <b>-16.59</b>
            <b>Groceries</b>
            Carrefour
            07.11.2026
            <code>{id}</code>
            """;

        formatted.ReplaceLineEndings().Should().Be(expected.ReplaceLineEndings());
    }

    [Fact]
    public void FormatResponse_Added_WithoutNote_OmitsNoteLine()
    {
        // arrange
        var id = Guid.NewGuid();
        var transaction = new Transaction(
            id,
            TransactionType.Income,
            TestCategory,
            100m,
            new DateOnly(2026, 11, 7),
            null);

        var action = new ActionResult.Transaction.Added([transaction])
        {
            RawInput = "salary 100"
        };
        var result = new ProcessingResult.Completed([action]);

        // act
        var formatted = TelegramResponseFormatter.FormatResponse(result);

        // assert
        var expected = $"""
            <b>Created 1 transaction(s)</b>

            <b>+100.00</b>
            <b>Groceries</b>
            07.11.2026
            <code>{id}</code>
            """;

        formatted.ReplaceLineEndings().Should().Be(expected.ReplaceLineEndings());
    }

    [Fact]
    public void FormatResponse_Added_MultipleTransactions_SeparatesBlocksWithEmptyLine()
    {
        // arrange
        var t1 = new Transaction(
            Guid.NewGuid(),
            TransactionType.Expense,
            TestCategory,
            10m,
            new DateOnly(2026, 11, 7),
            "Coffee");

        var t2 = new Transaction(
            Guid.NewGuid(),
            TransactionType.Expense,
            TestCategory,
            20m,
            new DateOnly(2026, 11, 7),
            null);

        var action = new ActionResult.Transaction.Added([t1, t2])
        {
            RawInput = "coffee 10, groceries 20"
        };
        var result = new ProcessingResult.Completed([action]);

        // act
        var formatted = TelegramResponseFormatter.FormatResponse(result);

        // assert
        formatted.Should().Contain("<b>Created 2 transaction(s)</b>");
        formatted.Should().Contain($"<code>{t1.Id}</code>");
        formatted.Should().Contain($"<code>{t2.Id}</code>");
        formatted.ReplaceLineEndings().Should().Contain($"<code>{t1.Id}</code>\n\n<b>-20.00</b>".ReplaceLineEndings());
    }

    [Fact]
    public void FormatResponse_Added_SpecialCharacters_AreHtmlEncoded()
    {
        // arrange
        var category = new Category("test", "A & B <C>", ["test"]);
        var transaction = new Transaction(
            Guid.NewGuid(),
            TransactionType.Expense,
            category,
            15m,
            new DateOnly(2026, 11, 7),
            "<script>alert('xss')</script> & more");

        var action = new ActionResult.Transaction.Added([transaction])
        {
            RawInput = "hack 15"
        };
        var result = new ProcessingResult.Completed([action]);

        // act
        var formatted = TelegramResponseFormatter.FormatResponse(result);

        // assert
        formatted.Should().Contain("<b>A &amp; B &lt;C&gt;</b>");
        formatted.Should().Contain("&lt;script&gt;alert(&#39;xss&#39;)&lt;/script&gt; &amp; more");
        formatted.Should().NotContain("<script>");
    }

    [Fact]
    public void FormatResponse_Listed_WithSmallCount_ShowsAllTransactions()
    {
        // arrange
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

        // act
        var formatted = TelegramResponseFormatter.FormatResponse(result);

        // assert
        formatted.Should().Contain(transactions[0].Id.ToString());
        formatted.Should().Contain(transactions[1].Id.ToString());
        formatted.Should().Contain("Total: <code>-300.00</code>");
        formatted.Should().NotContain("more transaction(s) hidden");
        formatted.Length.Should().BeLessThanOrEqualTo(4096);
    }

    [Fact]
    public void FormatResponse_Listed_WithMoreThanMax_CapsDisplayedAndPreservesTotal()
    {
        // arrange: 50 transactions
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

        // act
        var formatted = TelegramResponseFormatter.FormatResponse(result);

        // assert
        formatted.Length.Should().BeLessThanOrEqualTo(4096);
        formatted.Should().Contain("Total: <code>-500.00</code>"); // 50 * 10
        formatted.Should().Contain("more transaction(s) hidden");
        formatted.Should().Contain("and 25 more transaction(s) hidden");
    }

    [Fact]
    public void FormatResponse_Listed_WithLongNotes_NeverExceedsTelegramLimit()
    {
        // arrange: 30 transactions with long notes (150 chars each)
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

        // act
        var formatted = TelegramResponseFormatter.FormatResponse(result);

        // assert
        formatted.Length.Should().BeLessThanOrEqualTo(4096);
        formatted.Should().Contain("more transaction(s) hidden");
    }

    [Fact]
    public void FormatNotification_MessageMovedToDeadLetter_FormatsHtmlAndEncodesSpecialCharacters()
    {
        // arrange
        var notification = new OutboxSyncNotification.MessageMovedToDeadLetter(
            42,
            "CreateTransaction",
            "<error & details>");

        // act
        var formatted = TelegramResponseFormatter.FormatNotification(notification);

        // assert
        formatted.Should().Contain("🚨 <b>Outbox Message Moved to Dead Letter</b>");
        formatted.Should().Contain("ID: 42");
        formatted.Should().Contain("Operation: CreateTransaction");
        formatted.Should().Contain("&lt;error &amp; details&gt;");
        formatted.Should().NotContain("<error & details>");
    }

    [Fact]
    public void FormatNotification_SyncFailed_FormatsHtmlAndEncodesSpecialCharacters()
    {
        // arrange
        var notification = new OutboxSyncNotification.SyncFailed("<sync failed & details>");

        // act
        var formatted = TelegramResponseFormatter.FormatNotification(notification);

        // assert
        formatted.Should().Contain("🚨 <b>Outbox Sync Failed</b>");
        formatted.Should().Contain("&lt;sync failed &amp; details&gt;");
        formatted.Should().NotContain("<sync failed & details>");
    }

    [Fact]
    public void FormatResponse_Updated_SingleTransaction_FormatsPropertiesWithHeader()
    {
        // arrange
        var id = Guid.NewGuid();
        var transaction = new Transaction(
            id,
            TransactionType.Expense,
            TestCategory,
            25.50m,
            new DateOnly(2026, 11, 7),
            "Supermarket");

        var action = new ActionResult.Transaction.Updated(transaction)
        {
            RawInput = "update 25.50"
        };
        var result = new ProcessingResult.Completed([action]);

        // act
        var formatted = TelegramResponseFormatter.FormatResponse(result);

        // assert
        var expected = $"""
            <b>Updated transaction</b>

            <b>-25.50</b>
            <b>Groceries</b>
            Supermarket
            07.11.2026
            <code>{id}</code>
            """;

        formatted.ReplaceLineEndings().Should().Be(expected.ReplaceLineEndings());
    }

    [Fact]
    public void FormatResponse_InvalidInput_NothingToUpdate_FormatsErrorMessage()
    {
        // arrange
        var action = new ActionResult.InvalidInput([
            new ValidationErrorItem(ValidationErrorCode.Transaction.NothingToUpdate)
        ])
        {
            RawInput = "update 123"
        };
        var result = new ProcessingResult.Completed([action]);

        // act
        var formatted = TelegramResponseFormatter.FormatResponse(result);

        // assert
        var expected = """
            ⚠️ <b>Validation Error(s)</b> for '<b>update 123</b>':
            • At least one field must be provided to update the transaction.
            """;

        formatted.ReplaceLineEndings().Should().Be(expected.ReplaceLineEndings());
    }
}
