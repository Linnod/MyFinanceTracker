using FluentAssertions;
using MyFinanceTracker.Domain.Entities;
using MyFinanceTracker.Domain.Repositories;
using MyFinanceTracker.UseCases.Transaction.Get;
using NSubstitute;

namespace MyFinanceTracker.UseCases.Tests.Transaction.Get;

public class GetTransactionsHandlerTests
{
    private readonly ICategoryRepository categoryRepository;
    private readonly ITransactionRepository transactionRepository;
    private readonly GetTransactionsHandler sut;

    public GetTransactionsHandlerTests()
    {
        categoryRepository = Substitute.For<ICategoryRepository>();
        transactionRepository = Substitute.For<ITransactionRepository>();
        sut = new GetTransactionsHandler(categoryRepository, transactionRepository);
    }

    [Fact]
    public async Task Handle_SingleCategory_ReturnsSuccess()
    {
        // arrange
        var category = new Domain.Entities.Category("A", "Food", ["food"]);
        var fromDate = new DateOnly(2026, 1, 20);
        var toDate = new DateOnly(2026, 1, 27);

        var transactions = new[]
        {
            new Domain.Entities.Transaction(
                Guid.NewGuid(),
                TransactionType.Expense,
                category,
                100m,
                fromDate,
                "Lunch")
        };

        categoryRepository
            .GetAll(Arg.Any<CancellationToken>())
            .Returns([category]);

        transactionRepository
            .Get(
                Arg.Is<TransactionFilter>(f =>
                    f.FromDate == fromDate &&
                    f.ToDate == toDate &&
                    f.Categories != null &&
                    f.Categories.Count == 1 &&
                    f.Categories.First() == category &&
                    f.Type == TransactionType.Expense),
                Arg.Any<CancellationToken>())
            .Returns(transactions);

        // act
        var result = await sut.Handle(
            new GetTransactionsRequest(
                FromDate: fromDate,
                ToDate: toDate,
                CategoryAliases: ["food"],
                Type: TransactionType.Expense),
            CancellationToken.None);

        // assert
        var success = result.Should()
            .BeOfType<GetTransactionsResponse.Success>()
            .Subject;

        success.FromDate.Should().Be(fromDate);
        success.ToDate.Should().Be(toDate);
        success.CategoryNames.Should().Equal("Food");
        success.Type.Should().Be(TransactionType.Expense);
        success.Transactions.Should().BeEquivalentTo(transactions);

        await categoryRepository.Received(1).GetAll(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_MultipleCategories_ReturnsSuccessWithDistinctCategories()
    {
        // arrange
        var food = new Domain.Entities.Category("A", "Food", ["food", "groceries"]);
        var ent = new Domain.Entities.Category("B", "Entertainment", ["ent", "cinema"]);
        var fromDate = new DateOnly(2026, 1, 20);
        var toDate = new DateOnly(2026, 1, 27);

        categoryRepository
            .GetAll(Arg.Any<CancellationToken>())
            .Returns([food, ent]);

        var transactions = new[]
        {
            new Domain.Entities.Transaction(Guid.NewGuid(), TransactionType.Expense, food, 100m, fromDate, "Lunch"),
            new Domain.Entities.Transaction(Guid.NewGuid(), TransactionType.Expense, ent, 50m, toDate, "Movie")
        };

        transactionRepository
            .Get(
                Arg.Is<TransactionFilter>(f =>
                    f.FromDate == fromDate &&
                    f.ToDate == toDate &&
                    f.Categories != null &&
                    f.Categories.Count == 2),
                Arg.Any<CancellationToken>())
            .Returns(transactions);

        // act - note "food" and "groceries" map to same category A
        var result = await sut.Handle(
            new GetTransactionsRequest(
                FromDate: fromDate,
                ToDate: toDate,
                CategoryAliases: ["food", "groceries", "ent"]),
            CancellationToken.None);

        // assert
        var success = result.Should()
            .BeOfType<GetTransactionsResponse.Success>()
            .Subject;

        success.CategoryNames.Should().BeEquivalentTo(["Food", "Entertainment"]);
        success.Transactions.Should().BeEquivalentTo(transactions);
    }

    [Fact]
    public async Task Handle_NoCategories_DoesNotCallCategoryRepositoryAndReturnsSuccess()
    {
        // arrange
        var fromDate = new DateOnly(2026, 1, 1);
        var toDate = new DateOnly(2026, 1, 31);

        var transactions = new[]
        {
            new Domain.Entities.Transaction(
                Guid.NewGuid(),
                TransactionType.Expense,
                new Domain.Entities.Category("A", "Food", ["food"]),
                100m,
                fromDate,
                "Lunch")
        };

        transactionRepository
            .Get(
                Arg.Is<TransactionFilter>(f =>
                    f.FromDate == fromDate &&
                    f.ToDate == toDate &&
                    f.Categories == null &&
                    f.Type == null),
                Arg.Any<CancellationToken>())
            .Returns(transactions);

        // act
        var result = await sut.Handle(
            new GetTransactionsRequest(
                FromDate: fromDate,
                ToDate: toDate,
                CategoryAliases: null),
            CancellationToken.None);

        // assert
        var success = result.Should()
            .BeOfType<GetTransactionsResponse.Success>()
            .Subject;

        success.FromDate.Should().Be(fromDate);
        success.ToDate.Should().Be(toDate);
        success.CategoryNames.Should().BeEmpty();
        success.Type.Should().BeNull();
        success.Transactions.Should().BeEquivalentTo(transactions);

        await categoryRepository.DidNotReceive().GetAll(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_CategoryDoesNotExist_ReturnsValidationError()
    {
        // arrange
        categoryRepository
            .GetAll(Arg.Any<CancellationToken>())
            .Returns(
            [
                new Domain.Entities.Category("A", "Food", ["food"])
            ]);

        // act
        var result = await sut.Handle(
            new GetTransactionsRequest(
                FromDate: new DateOnly(2026, 1, 1),
                ToDate: new DateOnly(2026, 1, 27),
                CategoryAliases: ["fod"]),
            CancellationToken.None);

        // assert
        var error = result.Should()
            .BeOfType<GetTransactionsResponse.ValidationError>()
            .Subject;

        error.Errors.Should().ContainSingle();

        error.Errors.Single().ErrorCode
            .Should().Be(ValidationErrorCode.Transaction.CategoryNotFound);

        await categoryRepository.Received(1).GetAll(Arg.Any<CancellationToken>());

        await transactionRepository.DidNotReceive()
            .Get(Arg.Any<TransactionFilter>(), Arg.Any<CancellationToken>());
    }
}
