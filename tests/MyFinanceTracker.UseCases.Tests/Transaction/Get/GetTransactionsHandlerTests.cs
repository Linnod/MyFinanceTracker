using FluentAssertions;
using MyFinanceTracker.Domain.Common;
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
        var category = new Domain.Entities.Category("food", "Food");
        var fromDate = new DateTime(2026, 1, 20);
        var toDate = new DateTime(2026, 1, 27);

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
            .GetByIds(Arg.Is<IReadOnlyCollection<string>>(ids => ids.Contains("food")), Arg.Any<CancellationToken>())
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
            .Returns(new PagedResult<Domain.Entities.Transaction>(transactions, transactions.Length));

        // act
        var result = await sut.Handle(
            new GetTransactionsRequest(
                FromDate: fromDate,
                ToDate: toDate,
                CategoryIds: ["food"],
                Type: TransactionType.Expense),
            CancellationToken.None);

        // assert
        var success = result.Should()
            .BeOfType<GetTransactionsResponse.Success>()
            .Subject;

        success.FromDate.Should().Be(fromDate);
        success.ToDate.Should().Be(toDate);
        success.CategoryNames.Should().Equal("Food");
        success.Transactions.Items.Should().BeEquivalentTo(transactions);

        await categoryRepository.Received(1).GetByIds(Arg.Any<IReadOnlyCollection<string>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_MultipleCategories_ReturnsSuccessWithDistinctCategories()
    {
        // arrange
        var food = new Domain.Entities.Category("food", "Food");
        var ent = new Domain.Entities.Category("ent", "Entertainment");
        var fromDate = new DateTime(2026, 1, 20);
        var toDate = new DateTime(2026, 1, 27);

        categoryRepository
            .GetByIds(Arg.Any<IReadOnlyCollection<string>>(), Arg.Any<CancellationToken>())
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
            .Returns(new PagedResult<Domain.Entities.Transaction>(transactions, transactions.Length));

        // act - duplicate "food" ID should be deduplicated in resolvedCategories
        var result = await sut.Handle(
            new GetTransactionsRequest(
                FromDate: fromDate,
                ToDate: toDate,
                CategoryIds: ["food", "food", "ent"]),
            CancellationToken.None);

        // assert
        var success = result.Should()
            .BeOfType<GetTransactionsResponse.Success>()
            .Subject;

        success.CategoryNames.Should().BeEquivalentTo(["Food", "Entertainment"]);
        success.Transactions.Items.Should().BeEquivalentTo(transactions);

        await categoryRepository.Received(1).GetByIds(Arg.Any<IReadOnlyCollection<string>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NoCategories_DoesNotCallCategoryRepositoryAndReturnsSuccess()
    {
        // arrange
        var fromDate = new DateTime(2026, 1, 1);
        var toDate = new DateTime(2026, 1, 31);

        var transactions = new[]
        {
            new Domain.Entities.Transaction(
                Guid.NewGuid(),
                TransactionType.Expense,
                new Domain.Entities.Category("food", "Food"),
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
            .Returns(new PagedResult<Domain.Entities.Transaction>(transactions, transactions.Length));

        // act
        var result = await sut.Handle(
            new GetTransactionsRequest(
                FromDate: fromDate,
                ToDate: toDate,
                CategoryIds: null),
            CancellationToken.None);

        // assert
        var success = result.Should()
            .BeOfType<GetTransactionsResponse.Success>()
            .Subject;

        success.FromDate.Should().Be(fromDate);
        success.ToDate.Should().Be(toDate);
        success.CategoryNames.Should().BeEmpty();
        success.Transactions.Items.Should().BeEquivalentTo(transactions);

        await categoryRepository.DidNotReceive().GetByIds(Arg.Any<IReadOnlyCollection<string>>(), Arg.Any<CancellationToken>());
        await categoryRepository.DidNotReceive().GetById(Arg.Any<string>(), Arg.Any<CancellationToken>());
        await categoryRepository.DidNotReceive().GetAll(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_CategoryDoesNotExist_ReturnsValidationError()
    {
        // arrange
        categoryRepository
            .GetByIds(Arg.Any<IReadOnlyCollection<string>>(), Arg.Any<CancellationToken>())
            .Returns([]);

        // act
        var result = await sut.Handle(
            new GetTransactionsRequest(
                FromDate: new DateTime(2026, 1, 1),
                ToDate: new DateTime(2026, 1, 27),
                CategoryIds: ["fod"]),
            CancellationToken.None);

        // assert
        var error = result.Should()
            .BeOfType<GetTransactionsResponse.ValidationError>()
            .Subject;

        error.Errors.Should().ContainSingle();

        error.Errors.Single().ErrorCode
            .Should().Be(ValidationErrorCode.Transaction.CategoryNotFound);

        await categoryRepository.Received(1).GetByIds(Arg.Any<IReadOnlyCollection<string>>(), Arg.Any<CancellationToken>());

        await transactionRepository.DidNotReceive()
            .Get(Arg.Any<TransactionFilter>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithPagination_PassesSkipAndTakeToFilterAndReturnsPagedResult()
    {
        // arrange
        var fromDate = new DateTime(2026, 1, 1);
        var toDate = new DateTime(2026, 1, 31);
        var category = new Domain.Entities.Category("food", "Food");

        categoryRepository
            .GetByIds(Arg.Any<IReadOnlyCollection<string>>(), Arg.Any<CancellationToken>())
            .Returns([category]);

        var transactions = new[]
        {
            new Domain.Entities.Transaction(Guid.NewGuid(), TransactionType.Expense, category, 100m, fromDate, "Lunch")
        };

        transactionRepository
            .Get(
                Arg.Is<TransactionFilter>(f =>
                    f.Skip == 5 &&
                    f.Take == 10),
                Arg.Any<CancellationToken>())
            .Returns(new PagedResult<Domain.Entities.Transaction>(transactions, 15));

        // act
        var result = await sut.Handle(
            new GetTransactionsRequest(
                FromDate: fromDate,
                ToDate: toDate,
                CategoryIds: ["food"],
                Skip: 5,
                Take: 10),
            CancellationToken.None);

        // assert
        var success = result.Should()
            .BeOfType<GetTransactionsResponse.Success>()
            .Subject;

        success.Transactions.Items.Should().BeEquivalentTo(transactions);
        success.Transactions.TotalCount.Should().Be(15);
    }
}
