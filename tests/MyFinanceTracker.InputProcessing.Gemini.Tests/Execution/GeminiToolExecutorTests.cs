using FluentAssertions;
using Google.GenAI.Types;
using MediatR;
using Microsoft.Extensions.Logging;
using MyFinanceTracker.Domain.Entities;
using MyFinanceTracker.InputProcessing.Gemini.Declarations;
using MyFinanceTracker.InputProcessing.Gemini.Execution;
using MyFinanceTracker.UseCases;
using MyFinanceTracker.UseCases.Transaction.Create;
using MyFinanceTracker.UseCases.Transaction.Delete;
using MyFinanceTracker.UseCases.Transaction.Get;
using MyFinanceTracker.UseCases.Transaction.Update;
using NSubstitute;
using Xunit;

namespace MyFinanceTracker.InputProcessing.Gemini.Tests.Execution;

public class GeminiToolExecutorTests
{
    private readonly ILogger<GeminiToolExecutor> loggerMock;
    private readonly ISender sender = Substitute.For<ISender>();
    private readonly TimeProvider timeProvider = TimeProvider.System;
    private readonly GeminiToolExecutor executor;

    public GeminiToolExecutorTests()
    {
        loggerMock = Substitute.For<ILogger<GeminiToolExecutor>>();
        executor = new GeminiToolExecutor(sender, timeProvider, loggerMock);
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

        var category = new Category(Guid.NewGuid().ToString(), "Groceries", ["food"]);
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

        sender.Send(Arg.Any<CreateTransactionsRequest>(), Arg.Any<CancellationToken>())
            .Returns(new CreateTransactionsResponse.Success(createdTransactions));

        // Act
        var response = await executor.ExecuteToolCall(functionCall, CancellationToken.None);

        // Assert
        response.Response.Should().BeOfType<CreateTransactionsResponse.Success>();

        var result = (CreateTransactionsResponse.Success)response.Response;
        result.Transactions.Should().HaveCount(1);
        result.Transactions[0].Amount.Should().Be(100m);

        await sender.Received(1).Send(
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
    async Task ExecuteToolCall_WhenAddTransactionCallWithoutDate_ShouldDefaultToCurrentDate()
    {
        // Arrange
        var today = DateOnly.FromDateTime(timeProvider.GetLocalNow().DateTime);
        var functionCall = new FunctionCall
        {
            Name = GeminiToolDeclarationProvider.ToolNames.AddTransaction,
            Args = new Dictionary<string, object>
            {
                ["type"] = "expense",
                ["amounts"] = new[] { 50m },
                ["categoryAlias"] = "food",
                ["recognizedInput"] = "Coffee 50"
            }
        };

        var category = new Category(Guid.NewGuid().ToString(), "Food", ["food"]);
        var createdTransactions = new List<Transaction>
        {
            new(
                id: Guid.NewGuid(),
                type: TransactionType.Expense,
                category: category,
                amount: 50m,
                date: today,
                note: null
            )
        };

        sender.Send(Arg.Any<CreateTransactionsRequest>(), Arg.Any<CancellationToken>())
            .Returns(new CreateTransactionsResponse.Success(createdTransactions));

        // Act
        var response = await executor.ExecuteToolCall(functionCall, CancellationToken.None);

        // Assert
        response.Response.Should().BeOfType<CreateTransactionsResponse.Success>();

        await sender.Received(1).Send(
            Arg.Is<CreateTransactionsRequest>(r =>
                r.Items.Count == 1 &&
                r.Items[0].Date == today),
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

        sender.Send(Arg.Any<CreateTransactionsRequest>(), Arg.Any<CancellationToken>())
            .Returns(new CreateTransactionsResponse.ValidationError(errors));

        // Act
        var response = await executor.ExecuteToolCall(functionCall, CancellationToken.None);

        // Assert
        response.Response.Should().BeOfType<CreateTransactionsResponse.ValidationError>();
        var result = (CreateTransactionsResponse.ValidationError)response.Response;
        result.Errors.Should().BeEquivalentTo(errors);
        response.RawInput.Should().Be("Expense in food category");

        await sender.Received(1).Send(
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

        var category = new Category(Guid.NewGuid().ToString(), "Groceries", ["food"]);
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

        sender.Send(Arg.Any<GetTransactionsRequest>(), Arg.Any<CancellationToken>())
            .Returns(new GetTransactionsResponse.Success(
                new DateOnly(2026, 8, 1),
                new DateOnly(2026, 8, 10),
                ["Food", "Entertainment"],
                transactions));

        // Act
        var response = await executor.ExecuteToolCall(functionCall, CancellationToken.None);

        // Assert
        response.Response.Should().BeOfType<GetTransactionsResponse.Success>();
        var listed = (GetTransactionsResponse.Success)response.Response;
        listed.FromDate.Should().Be(new DateOnly(2026, 8, 1));
        listed.ToDate.Should().Be(new DateOnly(2026, 8, 10));
        listed.CategoryNames.Should().Equal("Food", "Entertainment");
        listed.Transactions.Should().BeEquivalentTo(transactions);
        response.RawInput.Should().Be("View expenses in food and entertainment");

        await sender.Received(1).Send(
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
    async Task ExecuteToolCall_WhenGetTransactionsCallHasMissingDates_ShouldReturnInvalidInput()
    {
        // Arrange
        var functionCall = new FunctionCall
        {
            Name = GeminiToolDeclarationProvider.ToolNames.GetTransactions,
            Args = new Dictionary<string, object>
            {
                ["recognizedInput"] = "Show transactions"
            }
        };

        // Act
        var response = await executor.ExecuteToolCall(functionCall, CancellationToken.None);

        // Assert
        response.Response.Should().BeOfType<GetTransactionsResponse.ValidationError>();
        var invalid = (GetTransactionsResponse.ValidationError)response.Response;
        invalid.Errors.Should().Contain(e => e.ErrorCode == ValidationErrorCode.Transaction.FromDateRequired);
        invalid.Errors.Should().Contain(e => e.ErrorCode == ValidationErrorCode.Transaction.ToDateRequired);
        response.RawInput.Should().Be("Show transactions");
        await sender.DidNotReceive().Send(Arg.Any<GetTransactionsRequest>(), Arg.Any<CancellationToken>());
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

        sender.Send(Arg.Any<DeleteTransactionsRequest>(), Arg.Any<CancellationToken>())
            .Returns(new DeleteTransactionsResponse.Success(2));

        // Act
        var response = await executor.ExecuteToolCall(functionCall, CancellationToken.None);

        // Assert
        response.Response.Should().BeOfType<DeleteTransactionsResponse.Success>();
        var deleted = (DeleteTransactionsResponse.Success)response.Response;
        deleted.DeletedCount.Should().Be(2);
        response.RawInput.Should().Be("Delete 2 transactions");

        await sender.Received(1).Send(
            Arg.Is<DeleteTransactionsRequest>(r =>
                r.Ids != null &&
                r.Ids.SequenceEqual(new[] { id1, id2 })),
            Arg.Any<CancellationToken>()
        );
    }

    [Fact]
    async Task ExecuteToolCall_WhenUpdateTransactionCall_ShouldSendUpdateTransactionRequest()
    {
        // arrange
        var txId = Guid.NewGuid();
        var functionCall = new FunctionCall
        {
            Name = GeminiToolDeclarationProvider.ToolNames.UpdateTransaction,
            Args = new Dictionary<string, object>
            {
                ["id"] = txId.ToString(),
                ["amount"] = 75.50m,
                ["type"] = "expense",
                ["categoryAlias"] = "food",
                ["recognizedInput"] = "update tx amount 75.50"
            }
        };

        var category = new Category("food", "Food", ["groceries", "food"]);
        var updatedTransaction = new Transaction(
            txId,
            TransactionType.Expense,
            category,
            75.50m,
            new DateOnly(2026, 11, 1),
            "Lunch");

        sender.Send(Arg.Any<UpdateTransactionRequest>(), Arg.Any<CancellationToken>())
            .Returns(new UpdateTransactionResponse.Success(updatedTransaction));

        // act
        var response = await executor.ExecuteToolCall(functionCall, CancellationToken.None);

        // assert
        response.Response.Should().BeOfType<UpdateTransactionResponse.Success>();
        var updated = (UpdateTransactionResponse.Success)response.Response;
        updated.Transaction.Should().Be(updatedTransaction);
        response.RawInput.Should().Be("update tx amount 75.50");

        await sender.Received(1).Send(
            Arg.Is<UpdateTransactionRequest>(r =>
                r.Id == txId &&
                r.Amount == 75.50m &&
                r.Type == TransactionType.Expense &&
                r.CategoryAlias == "food" &&
                r.Date == null &&
                r.Note == null &&
                !r.ClearNote),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    async Task ExecuteToolCall_WhenUpdateTransactionReturnsValidationError_ShouldReturnInvalidInput()
    {
        // arrange
        var txId = Guid.NewGuid();
        var functionCall = new FunctionCall
        {
            Name = GeminiToolDeclarationProvider.ToolNames.UpdateTransaction,
            Args = new Dictionary<string, object>
            {
                ["id"] = txId.ToString(),
                ["recognizedInput"] = "update without changes"
            }
        };

        ValidationErrorItem[] errors =
        [
            new(ValidationErrorCode.Transaction.NothingToUpdate)
        ];

        sender.Send(Arg.Any<UpdateTransactionRequest>(), Arg.Any<CancellationToken>())
            .Returns(new UpdateTransactionResponse.ValidationError(errors));

        // act
        var response = await executor.ExecuteToolCall(functionCall, CancellationToken.None);

        // assert
        response.Response.Should().BeOfType<UpdateTransactionResponse.ValidationError>();
        var invalid = (UpdateTransactionResponse.ValidationError)response.Response;
        invalid.Errors.Should().BeEquivalentTo(errors);
        response.RawInput.Should().Be("update without changes");

        await sender.Received(1).Send(
            Arg.Is<UpdateTransactionRequest>(r => r.Id == txId),
            Arg.Any<CancellationToken>());
    }
}
