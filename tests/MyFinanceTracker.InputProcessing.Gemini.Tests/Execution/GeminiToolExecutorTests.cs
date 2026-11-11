using FluentAssertions;
using Google.GenAI.Types;
using MediatR;
using Microsoft.Extensions.Logging;
using MyFinanceTracker.Domain.Common;
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
    private static readonly DateTimeOffset FixedNow = new(2026, 8, 8, 12, 0, 0, TimeSpan.Zero);
    private readonly ILogger<GeminiToolExecutor> loggerMock = Substitute.For<ILogger<GeminiToolExecutor>>();
    private readonly ISender sender = Substitute.For<ISender>();
    private readonly TimeProvider timeProvider = new FixedTimeProvider(FixedNow);
    private readonly GeminiToolExecutor executor;

    public GeminiToolExecutorTests()
    {
        executor = new GeminiToolExecutor(sender, timeProvider, loggerMock);
    }

    [Fact]
    async Task ExecuteToolCall_WhenAddTransactionCall_ShouldSendCreateTransactionRequest()
    {
        // arrange
        var transactionDate = new DateTime(2026, 8, 8, 14, 30, 0);
        var functionCall = new FunctionCall
        {
            Name = GeminiToolDeclarationProvider.ToolNames.AddTransaction,
            Args = new Dictionary<string, object>
            {
                ["type"] = "expense",
                ["amounts"] = new[] { 100m },
                ["categoryId"] = "food",
                ["date"] = "2026-08-08T14:30:00"
            }
        };

        var category = new Category(Guid.NewGuid().ToString(), "Groceries");
        var createdTransactions = new List<Transaction>
        {
            new(
                id: Guid.NewGuid(),
                type: TransactionType.Expense,
                category: category,
                amount: 100m,
                date: transactionDate,
                note: null
            )
        };

        sender.Send(Arg.Any<CreateTransactionsRequest>(), Arg.Any<CancellationToken>())
            .Returns(new CreateTransactionsResponse.Success(createdTransactions));

        // act
        var response = await executor.ExecuteToolCall(functionCall, CancellationToken.None);

        // assert
        response.Should().BeOfType<CreateTransactionsResponse.Success>();

        var result = (CreateTransactionsResponse.Success)response;
        result.Transactions.Should().HaveCount(1);
        result.Transactions[0].Amount.Should().Be(100m);

        await sender.Received(1).Send(
            Arg.Is<CreateTransactionsRequest>(r =>
                r.Items.Count == 1 &&
                r.Items[0].TransactionType == TransactionType.Expense &&
                r.Items[0].CategoryId == "food" &&
                r.Items[0].Amount == 100m &&
                r.Items[0].Date == transactionDate),
            Arg.Any<CancellationToken>()
        );
    }

    [Fact]
    async Task ExecuteToolCall_WhenAddTransactionCallWithoutDate_ShouldDefaultToCurrentDate()
    {
        // arrange
        var expectedNow = timeProvider.GetLocalNow().DateTime;
        var functionCall = new FunctionCall
        {
            Name = GeminiToolDeclarationProvider.ToolNames.AddTransaction,
            Args = new Dictionary<string, object>
            {
                ["type"] = "expense",
                ["amounts"] = new[] { 50m },
                ["categoryId"] = "food",
                ["recognizedInput"] = "Coffee 50"
            }
        };

        var category = new Category(Guid.NewGuid().ToString(), "Food");
        var createdTransactions = new List<Transaction>
        {
            new(
                id: Guid.NewGuid(),
                type: TransactionType.Expense,
                category: category,
                amount: 50m,
                date: expectedNow,
                note: null
            )
        };

        sender.Send(Arg.Any<CreateTransactionsRequest>(), Arg.Any<CancellationToken>())
            .Returns(new CreateTransactionsResponse.Success(createdTransactions));

        // act
        var response = await executor.ExecuteToolCall(functionCall, CancellationToken.None);

        // assert
        response.Should().BeOfType<CreateTransactionsResponse.Success>();

        await sender.Received(1).Send(
            Arg.Is<CreateTransactionsRequest>(r =>
                r.Items.Count == 1 &&
                r.Items[0].Date == expectedNow),
            Arg.Any<CancellationToken>()
        );
    }

    [Fact]
    async Task ExecuteToolCall_WhenAddTransactionCallWithoutAmounts_ShouldSendRequestWithEmptyItemsAndReturnInvalidInput()
    {
        // arrange
        var functionCall = new FunctionCall
        {
            Name = GeminiToolDeclarationProvider.ToolNames.AddTransaction,
            Args = new Dictionary<string, object>
            {
                ["type"] = "expense",
                ["categoryId"] = "food",
                ["recognizedInput"] = "Expense in food category"
            }
        };

        var errors = new[]
        {
            new ValidationErrorItem(ValidationErrorCode.Transaction.AmountRequired)
        };

        sender.Send(Arg.Any<CreateTransactionsRequest>(), Arg.Any<CancellationToken>())
            .Returns(new CreateTransactionsResponse.ValidationError(errors));

        // act
        var response = await executor.ExecuteToolCall(functionCall, CancellationToken.None);

        // assert
        response.Should().BeOfType<CreateTransactionsResponse.ValidationError>();
        var result = (CreateTransactionsResponse.ValidationError)response;
        result.Errors.Should().BeEquivalentTo(errors);

        await sender.Received(1).Send(
            Arg.Is<CreateTransactionsRequest>(r => r.Items.Count == 0),
            Arg.Any<CancellationToken>()
        );
    }

    [Fact]
    async Task ExecuteToolCall_WhenGetTransactionsCall_ShouldSendGetTransactionsRequest()
    {
        // arrange
        var fromDate = new DateTime(2026, 8, 1, 0, 0, 0);
        var toDate = new DateTime(2026, 8, 10, 23, 59, 59);

        var functionCall = new FunctionCall
        {
            Name = GeminiToolDeclarationProvider.ToolNames.GetTransactions,
            Args = new Dictionary<string, object>
            {
                ["fromDate"] = "2026-08-01T00:00:00",
                ["toDate"] = "2026-08-10T23:59:59",
                ["categoryIds"] = new[] { "food", "entertainment" },
                ["type"] = "expense",
                ["recognizedInput"] = "View expenses in food and entertainment"
            }
        };

        var category = new Category(Guid.NewGuid().ToString(), "Groceries");
        var transactions = new List<Transaction>
        {
            new(
                id: Guid.NewGuid(),
                type: TransactionType.Expense,
                category: category,
                amount: 50m,
                date: new DateTime(2026, 8, 5, 12, 0, 0),
                note: "Lunch"
            )
        };

        sender.Send(Arg.Any<GetTransactionsRequest>(), Arg.Any<CancellationToken>())
            .Returns(new GetTransactionsResponse.Success(
                fromDate,
                toDate,
                ["Food", "Entertainment"],
                new PagedResult<Transaction>(transactions, transactions.Count)));

        // act
        var response = await executor.ExecuteToolCall(functionCall, CancellationToken.None);

        // assert
        response.Should().BeOfType<GetTransactionsResponse.Success>();
        var listed = (GetTransactionsResponse.Success)response;
        listed.FromDate.Should().Be(fromDate);
        listed.ToDate.Should().Be(toDate);
        listed.CategoryNames.Should().Equal("Food", "Entertainment");
        listed.Transactions.Items.Should().BeEquivalentTo(transactions);

        await sender.Received(1).Send(
            Arg.Is<GetTransactionsRequest>(r =>
                r.FromDate == fromDate &&
                r.ToDate == toDate &&
                r.CategoryIds != null &&
                r.CategoryIds.SequenceEqual(new[] { "food", "entertainment" }) &&
                r.Type == TransactionType.Expense &&
                r.Take == null),
            Arg.Any<CancellationToken>()
        );
    }

    [Fact]
    async Task ExecuteToolCall_WhenGetTransactionsCallWithTimeWindow_ShouldPreserveExactHoursAndMinutes()
    {
        // arrange
        var fromDate = new DateTime(2026, 8, 8, 10, 15, 0);
        var toDate = new DateTime(2026, 8, 8, 11, 15, 0);

        var functionCall = new FunctionCall
        {
            Name = GeminiToolDeclarationProvider.ToolNames.GetTransactions,
            Args = new Dictionary<string, object>
            {
                ["fromDate"] = "2026-08-08T10:15:00",
                ["toDate"] = "2026-08-08T11:15:00",
                ["recognizedInput"] = "Show transactions in the last hour"
            }
        };

        sender.Send(Arg.Any<GetTransactionsRequest>(), Arg.Any<CancellationToken>())
            .Returns(new GetTransactionsResponse.Success(
                fromDate,
                toDate,
                [],
                new PagedResult<Transaction>([], 0)));

        // act
        var response = await executor.ExecuteToolCall(functionCall, CancellationToken.None);

        // assert
        response.Should().BeOfType<GetTransactionsResponse.Success>();

        await sender.Received(1).Send(
            Arg.Is<GetTransactionsRequest>(r =>
                r.FromDate == fromDate &&
                r.ToDate == toDate),
            Arg.Any<CancellationToken>()
        );
    }

    [Fact]
    async Task ExecuteToolCall_WhenGetTransactionsCallWithLimit_ShouldSendGetTransactionsRequestWithTake()
    {
        // arrange
        var fromDate = new DateTime(2026, 8, 1, 0, 0, 0);
        var toDate = new DateTime(2026, 8, 8, 23, 59, 59);

        var functionCall = new FunctionCall
        {
            Name = GeminiToolDeclarationProvider.ToolNames.GetTransactions,
            Args = new Dictionary<string, object>
            {
                ["fromDate"] = "2026-08-01T00:00:00",
                ["toDate"] = "2026-08-08T23:59:59",
                ["limit"] = 5,
                ["recognizedInput"] = "Show last 5 transactions"
            }
        };

        sender.Send(Arg.Any<GetTransactionsRequest>(), Arg.Any<CancellationToken>())
            .Returns(new GetTransactionsResponse.Success(
                fromDate,
                toDate,
                [],
                new PagedResult<Transaction>([], 0)));

        // act
        var response = await executor.ExecuteToolCall(functionCall, CancellationToken.None);

        // assert
        response.Should().BeOfType<GetTransactionsResponse.Success>();

        await sender.Received(1).Send(
            Arg.Is<GetTransactionsRequest>(r =>
                r.FromDate == fromDate &&
                r.ToDate == toDate &&
                r.Take == 5),
            Arg.Any<CancellationToken>()
        );
    }

    [Fact]
    async Task ExecuteToolCall_WhenGetTransactionsCallHasMissingDates_ShouldReturnInvalidInput()
    {
        // arrange
        var functionCall = new FunctionCall
        {
            Name = GeminiToolDeclarationProvider.ToolNames.GetTransactions,
            Args = new Dictionary<string, object>
            {
                ["recognizedInput"] = "Show transactions"
            }
        };

        // act
        var response = await executor.ExecuteToolCall(functionCall, CancellationToken.None);

        // assert
        response.Should().BeOfType<GetTransactionsResponse.ValidationError>();
        var invalid = (GetTransactionsResponse.ValidationError)response;
        invalid.Errors.Should().Contain(e => e.ErrorCode == ValidationErrorCode.Transaction.FromDateRequired);
        invalid.Errors.Should().Contain(e => e.ErrorCode == ValidationErrorCode.Transaction.ToDateRequired);
        await sender.DidNotReceive().Send(Arg.Any<GetTransactionsRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    async Task ExecuteToolCall_WhenDeleteTransactionsCall_ShouldSendDeleteTransactionsRequest()
    {
        // arrange
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

        // act
        var response = await executor.ExecuteToolCall(functionCall, CancellationToken.None);

        // assert
        response.Should().BeOfType<DeleteTransactionsResponse.Success>();
        var deleted = (DeleteTransactionsResponse.Success)response;
        deleted.DeletedCount.Should().Be(2);

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
                ["categoryId"] = "food",
                ["recognizedInput"] = "update tx amount 75.50"
            }
        };

        var category = new Category("food", "Food");
        var updatedTransaction = new Transaction(
            txId,
            TransactionType.Expense,
            category,
            75.50m,
            new DateTime(2026, 11, 1, 12, 0, 0),
            "Lunch");

        sender.Send(Arg.Any<UpdateTransactionRequest>(), Arg.Any<CancellationToken>())
            .Returns(new UpdateTransactionResponse.Success(updatedTransaction));

        // act
        var response = await executor.ExecuteToolCall(functionCall, CancellationToken.None);

        // assert
        response.Should().BeOfType<UpdateTransactionResponse.Success>();
        var updated = (UpdateTransactionResponse.Success)response;
        updated.Transaction.Should().Be(updatedTransaction);

        await sender.Received(1).Send(
            Arg.Is<UpdateTransactionRequest>(r =>
                r.Id == txId &&
                r.Amount == 75.50m &&
                r.Type == TransactionType.Expense &&
                r.CategoryId == "food" &&
                r.Date == null &&
                r.Note == null &&
                !r.ClearNote),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    async Task ExecuteToolCall_WhenUpdateTransactionCallWithDate_ShouldSendUpdateTransactionRequestWithParsedDateTime()
    {
        // arrange
        var txId = Guid.NewGuid();
        var newDate = new DateTime(2026, 11, 2, 15, 45, 0);
        var functionCall = new FunctionCall
        {
            Name = GeminiToolDeclarationProvider.ToolNames.UpdateTransaction,
            Args = new Dictionary<string, object>
            {
                ["id"] = txId.ToString(),
                ["date"] = "2026-11-02T15:45:00",
                ["recognizedInput"] = "change date"
            }
        };

        var category = new Category("food", "Food");
        var updatedTransaction = new Transaction(
            txId,
            TransactionType.Expense,
            category,
            50m,
            newDate,
            null);

        sender.Send(Arg.Any<UpdateTransactionRequest>(), Arg.Any<CancellationToken>())
            .Returns(new UpdateTransactionResponse.Success(updatedTransaction));

        // act
        var response = await executor.ExecuteToolCall(functionCall, CancellationToken.None);

        // assert
        response.Should().BeOfType<UpdateTransactionResponse.Success>();

        await sender.Received(1).Send(
            Arg.Is<UpdateTransactionRequest>(r =>
                r.Id == txId &&
                r.Date == newDate),
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
        response.Should().BeOfType<UpdateTransactionResponse.ValidationError>();
        var invalid = (UpdateTransactionResponse.ValidationError)response;
        invalid.Errors.Should().BeEquivalentTo(errors);

        await sender.Received(1).Send(
            Arg.Is<UpdateTransactionRequest>(r => r.Id == txId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    async Task ExecuteToolCall_WhenDeleteTransactionsCallWithInvalidId_ShouldReturnValidationErrorWithoutSendingRequest()
    {
        // arrange
        var functionCall = new FunctionCall
        {
            Name = GeminiToolDeclarationProvider.ToolNames.DeleteTransactions,
            Args = new Dictionary<string, object>
            {
                ["ids"] = new[] { "abc", "def" },
                ["recognizedInput"] = "Delete transactions abc, def"
            }
        };

        // act
        var response = await executor.ExecuteToolCall(functionCall, CancellationToken.None);

        // assert
        response.Should().BeOfType<DeleteTransactionsResponse.ValidationError>();
        var validation = (DeleteTransactionsResponse.ValidationError)response;
        validation.Errors.Should().HaveCount(2);
        validation.Errors.Should().AllSatisfy(e => e.ErrorCode.Should().Be(ValidationErrorCode.Transaction.InvalidId));
        validation.Errors.Select(e => e.AttemptedValue).Should().Equal("abc", "def");

        await sender.DidNotReceive().Send(Arg.Any<DeleteTransactionsRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    async Task ExecuteToolCall_WhenUpdateTransactionCallWithInvalidId_ShouldReturnValidationErrorWithoutSendingRequest()
    {
        // arrange
        var functionCall = new FunctionCall
        {
            Name = GeminiToolDeclarationProvider.ToolNames.UpdateTransaction,
            Args = new Dictionary<string, object>
            {
                ["id"] = "invalid-guid",
                ["amount"] = 50m,
                ["recognizedInput"] = "update invalid id"
            }
        };

        // act
        var response = await executor.ExecuteToolCall(functionCall, CancellationToken.None);

        // assert
        response.Should().BeOfType<UpdateTransactionResponse.ValidationError>();
        var validation = (UpdateTransactionResponse.ValidationError)response;
        validation.Errors.Should().ContainSingle();
        var error = validation.Errors.Single();
        error.ErrorCode.Should().Be(ValidationErrorCode.Transaction.InvalidId);
        error.AttemptedValue.Should().Be("invalid-guid");

        await sender.DidNotReceive().Send(Arg.Any<UpdateTransactionRequest>(), Arg.Any<CancellationToken>());
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
