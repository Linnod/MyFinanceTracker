using FluentAssertions;
using Google.GenAI.Types;
using MediatR;
using Microsoft.Extensions.Logging;
using MyFinanceTracker.Domain.Entities;
using MyFinanceTracker.InputProcessing.Text.Gemini.Declarations;
using MyFinanceTracker.InputProcessing.Text.Gemini.Execution;
using MyFinanceTracker.UseCases.Transaction.Create;
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
                amount: -100m,
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
        result.Transactions[0].Amount.Should().Be(-100m);

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
}