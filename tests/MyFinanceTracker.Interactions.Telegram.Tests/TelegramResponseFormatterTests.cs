using FluentAssertions;
using MyFinanceTracker.Domain.Common;
using MyFinanceTracker.Domain.Entities;
using MyFinanceTracker.InputProcessing;
using MyFinanceTracker.UseCases.Transaction.Create;
using MyFinanceTracker.UseCases.Transaction.Get;

namespace MyFinanceTracker.Interactions.Telegram.Tests;

public sealed class TelegramResponseFormatterTests
{
    private static readonly Category TestCategory = new("groceries", "Groceries");

    [Fact]
    public void FormatResponse_Completed_HappyPath_FormatsTransactionsWithoutErrors()
    {
        // arrange
        var t1 = new Transaction(
            Guid.NewGuid(),
            TransactionType.Expense,
            TestCategory,
            16.50m,
            new DateTime(2026, 11, 7, 14, 30, 0),
            "Carrefour");

        var t2 = new Transaction(
            Guid.NewGuid(),
            TransactionType.Income,
            TestCategory,
            100m,
            new DateTime(2026, 11, 7, 15, 0, 0),
            null);

        var response = new CreateTransactionsResponse.Success([t1, t2]);
        var result = new ProcessingResult.Completed([response]);

        // act
        var formatted = TelegramResponseFormatter.FormatResponse(result);

        // assert
        formatted.Should().Contain("<b>Created 2 transaction(s)</b>");
        formatted.Should().Contain("-16.50");
        formatted.Should().Contain("Carrefour");
        formatted.Should().Contain(t1.Id.ToString());
        formatted.Should().Contain("+100.00");
        formatted.Should().Contain(t2.Id.ToString());
    }

    [Fact]
    public void FormatResponse_ListedExceedingMessageLength_CapsTransactionsAndShowsOverflowMessage()
    {
        // arrange: 50 transactions to exceed MaxMessageLength
        var transactions = Enumerable.Range(1, 50)
            .Select(i => new Transaction(
                Guid.NewGuid(),
                TransactionType.Expense,
                TestCategory,
                10m,
                new DateTime(2026, 11, 1, 10, 0, 0),
                $"Item {i} with a longer note to comfortably exceed the limit"))
            .ToList();

        var response = new GetTransactionsResponse.Success(
            new DateTime(2026, 11, 1),
            new DateTime(2026, 11, 30),
            [],
            new PagedResult<Transaction>(transactions, transactions.Count));

        var result = new ProcessingResult.Completed([response]);

        // act
        var formatted = TelegramResponseFormatter.FormatResponse(result);

        // assert
        formatted.Should().Contain("more transaction(s) hidden");
        formatted.Length.Should().BeLessThanOrEqualTo(4096);
    }

    [Fact]
    public void FormatResponse_SpecialCharacters_AreHtmlEncoded()
    {
        // arrange
        var category = new Category("test", "Food & Drinks <A>");
        var transaction = new Transaction(
            Guid.NewGuid(),
            TransactionType.Expense,
            category,
            10m,
            new DateTime(2026, 11, 7, 13, 0, 0),
            "<script>alert('xss')</script> & more");

        var response = new CreateTransactionsResponse.Success([transaction]);
        var result = new ProcessingResult.Completed([response]);

        // act
        var formatted = TelegramResponseFormatter.FormatResponse(result);

        // assert
        formatted.Should().Contain("Food &amp; Drinks &lt;A&gt;</b>");
        formatted.Should().Contain("&lt;script&gt;alert(&#39;xss&#39;)&lt;/script&gt; &amp; more");
        formatted.Should().NotContain("<script>");
    }
}
