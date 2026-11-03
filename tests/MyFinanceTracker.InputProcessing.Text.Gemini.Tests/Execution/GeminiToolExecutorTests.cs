using FluentAssertions;
using Google.GenAI.Types;
using MediatR;
using Microsoft.Extensions.Logging;
using MyFinanceTracker.Domain.Entities;
using MyFinanceTracker.InputProcessing.Text.Gemini.Declarations;
using MyFinanceTracker.InputProcessing.Text.Gemini.Execution;
using MyFinanceTracker.UseCases;
using MyFinanceTracker.UseCases.Transaction.Create;
using MyFinanceTracker.UseCases.Transaction.Delete;
using MyFinanceTracker.UseCases.Transaction.Get;
using NSubstitute;
using Xunit;

namespace MyFinanceTracker.InputProcessing.Text.Gemini.Tests.Execution;

public class GeminiToolExecutorTests
{
    private readonly ILogger<GeminiToolExecutor> loggerMock;
    private readonly IMediator mediator = Substitute.For<IMediator>();
    private readonly GeminiToolExecutor executor;

    public GeminiToolExecutorTests()
    {
        loggerMock = Substitute.For<ILogger<GeminiToolExecutor>>();
        executor = new GeminiToolExecutor(mediator, loggerMock);
    }

    [Fact]
    async Task ExecuteToolCall_WhenAddTransactionCall_ShouldSendCreateTransactionRequest()
    {
        // Arrange
        var functionCall = new FunctionCall
        {
            Name = GeminiToolDeclarationProvider.ToolNames.AddTransaction,
            Args = new Dictionary<string, object>
            {
                ["type"] = "expense",
                ["amounts"] = new[] { 100m },
                ["categoryAlias"] = "food",
                ["date"] = "2026-08-08"
            }
        };

        var category = new Category(Guid.NewGuid().ToString(), "Groceries", ["food"], isIncome: false);
        var createdTransactions = new List<Transaction>
        {
            new(
                id: Guid.NewGuid(),
                type: TransactionType.Expense,
                category: category,
                amount: 100m,
                date: new DateOnly(2026, 8, 8),
                note: null
            )
        };

        mediator.Send(Arg.Any<CreateTransactionsRequest>(), Arg.Any<CancellationToken>())
            .Returns(new CreateTransactionsResponse.Success(createdTransactions));

        // Act
        var response = await executor.ExecuteToolCall(functionCall, CancellationToken.None);

        // Assert
        response.Should().BeOfType<ActionResult.Transaction.Added>();

        var result = (ActionResult.Transaction.Added)response;
        result.Transactions.Should().HaveCount(1);
        result.Transactions[0].Amount.Should().Be(100m);

        await mediator.Received(1).Send(
            Arg.Is<CreateTransactionsRequest>(r =>
                r.Items.Count == 1 &&
                r.Items[0].TransactionType == TransactionType.Expense &&
                r.Items[0].CategoryAlias == "food" &&
                r.Items[0].Amount == 100m &&
                r.Items[0].Date == new DateOnly(2026, 8, 8)),
            Arg.Any<CancellationToken>()
        );
    }

    [Fact]
    async Task ExecuteToolCall_WhenAddTransactionCallWithoutAmounts_ShouldSendRequestWithEmptyItemsAndReturnInvalidInput()
    {
        // Arrange
        var functionCall = new FunctionCall
        {
            Name = GeminiToolDeclarationProvider.ToolNames.AddTransaction,
            Args = new Dictionary<string, object>
            {
                ["type"] = "expense",
                ["categoryAlias"] = "food",
                ["recognizedInput"] = "Expense in food category"
            }
        };

        var errors = new[]
        {
            new ValidationErrorItem(ValidationErrorCode.Transaction.AmountRequired)
        };

        mediator.Send(Arg.Any<CreateTransactionsRequest>(), Arg.Any<CancellationToken>())
            .Returns(new CreateTransactionsResponse.ValidationError(errors));

        // Act
        var response = await executor.ExecuteToolCall(functionCall, CancellationToken.None);

        // Assert
        response.Should().BeOfType<ActionResult.InvalidInput>();
        var result = (ActionResult.InvalidInput)response;
        result.Errors.Should().BeEquivalentTo(errors);
        result.RawInput.Should().Be("Expense in food category");

        await mediator.Received(1).Send(
            Arg.Is<CreateTransactionsRequest>(r => r.Items.Count == 0),
            Arg.Any<CancellationToken>()
        );
    }

    [Fact]
    async Task ExecuteToolCall_WhenGetTransactionsCall_ShouldSendGetTransactionsRequest()
    {
        // Arrange
        var functionCall = new FunctionCall
        {
            Name = GeminiToolDeclarationProvider.ToolNames.GetTransactions,
            Args = new Dictionary<string, object>
            {
                ["fromDate"] = "2026-08-01",
                ["toDate"] = "2026-08-10",
                ["categoryAliases"] = new[] { "food", "entertainment" },
                ["type"] = "expense",
                ["recognizedInput"] = "View expenses in food and entertainment"
            }
        };

        var category = new Category(Guid.NewGuid().ToString(), "Groceries", ["food"], isIncome: false);
        var transactions = new List<Transaction>
        {
            new(
                id: Guid.NewGuid(),
                type: TransactionType.Expense,
                category: category,
                amount: 50m,
                date: new DateOnly(2026, 8, 5),
                note: "Lunch"
            )
        };

        mediator.Send(Arg.Any<GetTransactionsRequest>(), Arg.Any<CancellationToken>())
            .Returns(new GetTransactionsResponse.Success(
                new DateOnly(2026, 8, 1),
                new DateOnly(2026, 8, 10),
                ["Food", "Entertainment"],
                TransactionType.Expense,
                transactions));

        // Act
        var response = await executor.ExecuteToolCall(functionCall, CancellationToken.None);

        // Assert
        response.Should().BeOfType<ActionResult.Transaction.Listed>();
        var listed = (ActionResult.Transaction.Listed)response;
        listed.FromDate.Should().Be(new DateOnly(2026, 8, 1));
        listed.ToDate.Should().Be(new DateOnly(2026, 8, 10));
        listed.CategoryNames.Should().Equal("Food", "Entertainment");
        listed.Type.Should().Be(TransactionType.Expense);
        listed.Transactions.Should().BeEquivalentTo(transactions);
        listed.RawInput.Should().Be("View expenses in food and entertainment");

        await mediator.Received(1).Send(
            Arg.Is<GetTransactionsRequest>(r =>
                r.FromDate == new DateOnly(2026, 8, 1) &&
                r.ToDate == new DateOnly(2026, 8, 10) &&
                r.CategoryAliases != null &&
                r.CategoryAliases.SequenceEqual(new[] { "food", "entertainment" }) &&
                r.Type == TransactionType.Expense),
            Arg.Any<CancellationToken>()
        );
    }

    [Fact]
    async Task ExecuteToolCall_WhenDeleteTransactionsCall_ShouldSendDeleteTransactionsRequest()
    {
        // Arrange
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();

        var functionCall = new FunctionCall
        {
            Name = GeminiToolDeclarationProvider.ToolNames.DeleteTransactions,
            Args = new Dictionary<string, object>
            {
                ["ids"] = new[] { id1.ToString(), id2.ToString() },
                ["recognizedInput"] = "Delete 2 transactions"
            }
        };

        mediator.Send(Arg.Any<DeleteTransactionsRequest>(), Arg.Any<CancellationToken>())
            .Returns(new DeleteTransactionsResponse.Success(2));

        // Act
        var response = await executor.ExecuteToolCall(functionCall, CancellationToken.None);

        // Assert
        response.Should().BeOfType<ActionResult.Transaction.Deleted>();
        var deleted = (ActionResult.Transaction.Deleted)response;
        deleted.DeletedCount.Should().Be(2);
        deleted.RawInput.Should().Be("Delete 2 transactions");

        await mediator.Received(1).Send(
            Arg.Is<DeleteTransactionsRequest>(r =>
                r.Ids != null &&
                r.Ids.SequenceEqual(new[] { id1, id2 })),
            Arg.Any<CancellationToken>()
        );
    }
}
