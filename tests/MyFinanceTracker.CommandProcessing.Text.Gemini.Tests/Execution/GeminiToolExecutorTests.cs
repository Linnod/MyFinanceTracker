using FluentAssertions;
using Google.GenAI.Types;
using MediatR;
using Microsoft.Extensions.Logging;
using MyFinanceTracker.CommandProcessing.Text.Gemini.Execution;
using MyFinanceTracker.Domain.Entities;
using MyFinanceTracker.UseCases.Transaction.Create;
using NSubstitute;
using Xunit;

namespace MyFinanceTracker.CommandProcessing.Text.Gemini.Tests.Services;

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
    public async Task ExecuteToolCallAsync_WhenAddTransactionCall_ShouldSendCreateTransactionRequest()
    {
        // Arrange
        var functionCall = new FunctionCall
        {
            Name = "add_transaction",
            Args = new Dictionary<string, object>
            {
                ["type"] = "expense",
                ["amounts"] = 100m,
                ["categoryAlias"] = "food",
                ["date"] = "2026-08-08"
            }
        };

        mediator.Send(Arg.Any<CreateTransactionRequest>(), Arg.Any<CancellationToken>())
            .Returns(new CreateTransactionResponse.Success(
                CategoryName: "Groceries",
                Amounts: [100m],
                Date: new DateOnly(2026, 8, 8),
                Note: null
            ));

        // Act
        var response = await executor.ExecuteToolCallAsync(functionCall);

        // Assert
        response.Should().BeOfType<TextCommandResponse.Success>();
        await mediator.Received(1).Send(
            Arg.Is<CreateTransactionRequest>(r =>
                r.TransactionType == TransactionType.Expense &&
                r.CategoryAlias == "food" &&
                r.Amounts.SequenceEqual(new[] { 100m })),
            Arg.Any<CancellationToken>()
        );
    }
}