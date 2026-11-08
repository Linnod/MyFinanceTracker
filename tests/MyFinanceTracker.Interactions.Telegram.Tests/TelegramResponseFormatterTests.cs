using FluentAssertions;
using MyFinanceTracker.Common.Notifications;
using MyFinanceTracker.Domain.Common;
using MyFinanceTracker.Domain.Entities;
using MyFinanceTracker.InputProcessing;
using MyFinanceTracker.UseCases;
using MyFinanceTracker.UseCases.Transaction.Create;
using MyFinanceTracker.UseCases.Transaction.Get;
using MyFinanceTracker.UseCases.Transaction.Update;

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
            new DateTime(2026, 11, 7, 14, 30, 0),
            "Carrefour");

        var action = new ActionResult("groceries 16.59 Carrefour", new CreateTransactionsResponse.Success([transaction]));
        var result = new ProcessingResult.Completed([action]);

        // act
        var formatted = TelegramResponseFormatter.FormatResponse(result);

        // assert
        var expected = $"""
            <b>Created 1 transaction(s)</b>

            <b>-16.59</b>
            <b>Groceries</b>
            Carrefour
            07.11.2026 14:30:00
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
            new DateTime(2026, 11, 7, 10, 0, 0),
            null);

        var action = new ActionResult("salary 100", new CreateTransactionsResponse.Success([transaction]));
        var result = new ProcessingResult.Completed([action]);

        // act
        var formatted = TelegramResponseFormatter.FormatResponse(result);

        // assert
        var expected = $"""
            <b>Created 1 transaction(s)</b>

            <b>+100.00</b>
            <b>Groceries</b>
            07.11.2026 10:00:00
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
            new DateTime(2026, 11, 7, 11, 0, 0),
            "Coffee");

        var t2 = new Transaction(
            Guid.NewGuid(),
            TransactionType.Expense,
            TestCategory,
            20m,
            new DateTime(2026, 11, 7, 12, 0, 0),
            null);

        var action = new ActionResult("coffee 10, groceries 20", new CreateTransactionsResponse.Success([t1, t2]));
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
            new DateTime(2026, 11, 7, 13, 0, 0),
            "<script>alert('xss')</script> & more");

        var action = new ActionResult("hack 15", new CreateTransactionsResponse.Success([transaction]));
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
            new(Guid.NewGuid(), TransactionType.Expense, TestCategory, 100m, new DateTime(2026, 11, 1, 9, 0, 0), "Milk"),
            new(Guid.NewGuid(), TransactionType.Expense, TestCategory, 200m, new DateTime(2026, 11, 2, 10, 0, 0), "Bread"),
        };

        var action = new ActionResult(
            "get transactions",
            new GetTransactionsResponse.Success(
                new DateTime(2026, 11, 1, 0, 0, 0),
                new DateTime(2026, 11, 2, 0, 0, 0),
                [],
                new PagedResult<Transaction>(transactions, transactions.Count)));

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
    public void FormatResponse_Listed_WithTimeWindow_ShowsHoursMinutesSecondsInHeader()
    {
        // arrange
        var transactions = new List<Transaction>
        {
            new(Guid.NewGuid(), TransactionType.Expense, TestCategory, 15m, new DateTime(2026, 11, 8, 11, 30, 0), "Coffee")
        };

        var action = new ActionResult(
            "get transactions for last hour",
            new GetTransactionsResponse.Success(
                new DateTime(2026, 11, 8, 11, 0, 0),
                new DateTime(2026, 11, 8, 12, 0, 0),
                [],
                new PagedResult<Transaction>(transactions, transactions.Count)));

        var result = new ProcessingResult.Completed([action]);

        // act
        var formatted = TelegramResponseFormatter.FormatResponse(result);

        // assert
        formatted.Should().Contain("from 08.11.2026 11:00:00 to 08.11.2026 12:00:00");
        formatted.Should().Contain("11:30:00");
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
                new DateTime(2026, 11, 1, 10, 0, 0),
                $"Item {i}"))
            .ToList();

        var action = new ActionResult(
            "get transactions",
            new GetTransactionsResponse.Success(
                new DateTime(2026, 11, 1, 0, 0, 0),
                new DateTime(2026, 11, 30, 0, 0, 0),
                [],
                new PagedResult<Transaction>(transactions, transactions.Count)));

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
                new DateTime(2026, 11, 1, 10, 0, 0),
                longNote))
            .ToList();

        var action = new ActionResult(
            "get transactions",
            new GetTransactionsResponse.Success(
                new DateTime(2026, 11, 1, 0, 0, 0),
                new DateTime(2026, 11, 30, 0, 0, 0),
                [],
                new PagedResult<Transaction>(transactions, transactions.Count)));

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
            new DateTime(2026, 11, 7, 15, 0, 0),
            "Supermarket");

        var action = new ActionResult("update 25.50", new UpdateTransactionResponse.Success(transaction));
        var result = new ProcessingResult.Completed([action]);

        // act
        var formatted = TelegramResponseFormatter.FormatResponse(result);

        // assert
        var expected = $"""
            <b>Updated transaction</b>

            <b>-25.50</b>
            <b>Groceries</b>
            Supermarket
            07.11.2026 15:00:00
            <code>{id}</code>
            """;

        formatted.ReplaceLineEndings().Should().Be(expected.ReplaceLineEndings());
    }

    [Fact]
    public void FormatResponse_InvalidInput_NothingToUpdate_FormatsErrorMessage()
    {
        // arrange
        var action = new ActionResult(
            "update 123",
            new UpdateTransactionResponse.ValidationError([
                new ValidationErrorItem(ValidationErrorCode.Transaction.NothingToUpdate)
            ]));
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

    [Fact]
    public void FormatResponse_UseCaseResponse_FormatsDirectlyWithoutActionResult()
    {
        // arrange
        var validationResponse = new UpdateTransactionResponse.ValidationError([
            new ValidationErrorItem(ValidationErrorCode.Transaction.NothingToUpdate)
        ]);

        // act
        var formattedWithRawInput = TelegramResponseFormatter.FormatResponse(validationResponse, "update 123");
        var formattedWithoutRawInput = TelegramResponseFormatter.FormatResponse(validationResponse);

        // assert
        var expectedWithRaw = """
            ⚠️ <b>Validation Error(s)</b> for '<b>update 123</b>':
            • At least one field must be provided to update the transaction.
            """;
        var expectedWithoutRaw = """
            ⚠️ <b>Validation Error(s):</b>
            • At least one field must be provided to update the transaction.
            """;

        formattedWithRawInput.ReplaceLineEndings().Should().Be(expectedWithRaw.ReplaceLineEndings());
        formattedWithoutRawInput.ReplaceLineEndings().Should().Be(expectedWithoutRaw.ReplaceLineEndings());
    }
}
