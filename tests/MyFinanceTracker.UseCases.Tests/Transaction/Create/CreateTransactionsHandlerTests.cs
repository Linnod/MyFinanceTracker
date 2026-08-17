using FluentAssertions;
using Microsoft.Extensions.Time.Testing;
using MyFinanceTracker.Domain.Entities;
using MyFinanceTracker.Domain.Repositories;
using MyFinanceTracker.UseCases.Transaction.Create;
using NSubstitute;

namespace MyFinanceTracker.UseCases.Tests.Transaction.Create;

public class CreateTransactionsHandlerTests
{
    private readonly ICategoryRepository categoryRepository;
    private readonly ITransactionRepository transactionRepository;
    private readonly TimeProvider timeProvider;
    private readonly CreateTransactionsHandler sut;

    public CreateTransactionsHandlerTests()
    {
        categoryRepository = Substitute.For<ICategoryRepository>();
        transactionRepository = Substitute.For<ITransactionRepository>();
        timeProvider = Substitute.For<TimeProvider>();
        timeProvider = new FakeTimeProvider(
            new DateTimeOffset(
                2026, 8, 17,
                10, 0, 0,
                TimeSpan.Zero));
        sut = new CreateTransactionsHandler(categoryRepository, transactionRepository, timeProvider);
    }

    [Fact]
    async Task Handle_CategoryExists_CreatesTransactionAndReturnsSuccess()
    {
        // arrange
        var category = new Domain.Entities.Category("A", "Food", ["food"]);

        categoryRepository
            .GetAll(Arg.Any<CancellationToken>())
            .Returns([category]);

        var request = new CreateTransactionsRequest(
        [
            new CreateTransactionItem(
            TransactionType.Expense,
            100m,
            "food",
            new DateOnly(2026, 1, 27),
            "Lunch")
        ]);

        // act
        var result = await sut.Handle(
            request,
            CancellationToken.None);

        // assert
        var success = result.Should()
            .BeOfType<CreateTransactionsResponse.Success>()
            .Subject;

        success.Transactions.Should().ContainSingle();

        var transaction = success.Transactions.Single();

        transaction.Category.Should().Be(category);
        transaction.Type.Should().Be(TransactionType.Expense);
        transaction.Amount.Should().Be(-100m);
        transaction.Date.Should().Be(new DateOnly(2026, 1, 27));
        transaction.Note.Should().Be("Lunch");

        await categoryRepository.Received(1)
            .GetAll(Arg.Any<CancellationToken>());

        await transactionRepository.Received(1)
            .AddRange(
                Arg.Is<IReadOnlyCollection<Domain.Entities.Transaction>>(items =>
                    items.Count == 1 &&
                    items.Single().Category == category &&
                    items.Single().Amount == -100m),
                Arg.Any<CancellationToken>());
    }

    [Fact]
    async Task Handle_ExpenseWithoutCategory_ReturnsValidationError()
    {
        // arrange
        categoryRepository
            .GetAll(Arg.Any<CancellationToken>())
            .Returns([]);

        var request = new CreateTransactionsRequest(
        [
            new CreateTransactionItem(
                TransactionType.Expense,
                100m,
                null,
                new DateOnly(2026, 1, 27),
            "Lunch")
        ]);

        // act
        var result = await sut.Handle(
            request,
            CancellationToken.None);

        // assert
        var error = result.Should()
            .BeOfType<CreateTransactionsResponse.ValidationError>()
            .Subject;

        error.Errors.Should().ContainSingle();

        error.Errors.Single().ErrorCode
            .Should().Be(ValidationErrorCode.Transaction.CategoryRequired);

        await categoryRepository.Received(1)
            .GetAll(Arg.Any<CancellationToken>());

        await transactionRepository.DidNotReceive()
            .AddRange(
                Arg.Any<IReadOnlyCollection<Domain.Entities.Transaction>>(),
                Arg.Any<CancellationToken>());
    }

    [Fact]
    async Task Handle_IncomeWithoutCategory_UsesDefaultIncomeCategory()
    {
        // arrange
        var incomeCategory = new Domain.Entities.Category(
            "A",
            "Income",
            [FinancialRules.DefaultIncomeCategoryAlias]);

        categoryRepository
            .GetAll(Arg.Any<CancellationToken>())
            .Returns([incomeCategory]);

        var request = new CreateTransactionsRequest(
        [
            new CreateTransactionItem(
            TransactionType.Income,
            100m,
            null,
            new DateOnly(2026, 1, 27),
            "Salary")
        ]);

        // act
        var result = await sut.Handle(
            request,
            CancellationToken.None);

        // assert
        var success = result.Should()
            .BeOfType<CreateTransactionsResponse.Success>()
            .Subject;

        success.Transactions.Should().ContainSingle();

        var transaction = success.Transactions.Single();

        transaction.Category.Should().Be(incomeCategory);
        transaction.Type.Should().Be(TransactionType.Income);
        transaction.Amount.Should().Be(100m);

        await transactionRepository.Received(1)
            .AddRange(
                Arg.Is<IReadOnlyCollection<Domain.Entities.Transaction>>(items =>
                    items.Count == 1 &&
                    items.Single().Category == incomeCategory),
                Arg.Any<CancellationToken>());
    }

    [Fact]
    async Task Handle_CategoryDoesNotExist_ReturnsValidationError()
    {
        // arrange
        categoryRepository
            .GetAll(Arg.Any<CancellationToken>())
            .Returns(
            [
                new Domain.Entities.Category("A", "Food", ["food"])
            ]);

        var request = new CreateTransactionsRequest(
        [
            new CreateTransactionItem(
            TransactionType.Expense,
            100m,
            "fod",
            new DateOnly(2026, 1, 27),
            "Lunch")
        ]);

        // act
        var result = await sut.Handle(
            request,
            CancellationToken.None);

        // assert
        var error = result.Should()
            .BeOfType<CreateTransactionsResponse.ValidationError>()
            .Subject;

        error.Errors.Should().ContainSingle();

        error.Errors.Single().ErrorCode
            .Should().Be(ValidationErrorCode.Transaction.CategoryNotFound);

        await categoryRepository.Received(1)
            .GetAll(Arg.Any<CancellationToken>());

        await transactionRepository.DidNotReceive()
            .AddRange(
                Arg.Any<IReadOnlyCollection<Domain.Entities.Transaction>>(),
                Arg.Any<CancellationToken>());
    }

    [Fact]
    async Task Handle_DateIsNotSpecified_UsesCurrentDate()
    {
        // arrange
        var category = new Domain.Entities.Category(
            "A",
            "Food",
            ["food"]);

        categoryRepository
            .GetAll(Arg.Any<CancellationToken>())
            .Returns([category]);

        var request = new CreateTransactionsRequest(
        [
            new CreateTransactionItem(
            TransactionType.Expense,
            100m,
            "food",
            null,
            "Lunch")
        ]);

        // act
        var result = await sut.Handle(
            request,
            CancellationToken.None);

        // assert
        var success = result.Should()
            .BeOfType<CreateTransactionsResponse.Success>()
            .Subject;

        success.Transactions.Should().ContainSingle();

        success.Transactions.Single().Date
            .Should().Be(new DateOnly(2026, 8, 17));

        await transactionRepository.Received(1)
            .AddRange(
                Arg.Is<IReadOnlyCollection<Domain.Entities.Transaction>>(items =>
                    items.Single().Date == new DateOnly(2026, 8, 17)),
                Arg.Any<CancellationToken>());
    }

    [Fact]
    async Task Handle_OneItemIsInvalid_DoesNotCreateAnyTransactions()
    {
        // arrange
        var category = new Domain.Entities.Category(
            "A",
            "Food",
            ["food"]);

        categoryRepository
            .GetAll(Arg.Any<CancellationToken>())
            .Returns([category]);

        var request = new CreateTransactionsRequest(
        [
            new CreateTransactionItem(
                TransactionType.Expense,
                100m,
                "food",
                new DateOnly(2026, 1, 27),
                "Valid"),

            new CreateTransactionItem(
                TransactionType.Expense,
                200m,
                "unknown-category",
                new DateOnly(2026, 1, 27),
                "Invalid")
        ]);

        // act
        var result = await sut.Handle(
            request,
            CancellationToken.None);

        // assert
        var error = result.Should()
            .BeOfType<CreateTransactionsResponse.ValidationError>()
            .Subject;

        error.Errors.Should().ContainSingle();

        error.Errors.Single().ErrorCode
            .Should().Be(ValidationErrorCode.Transaction.CategoryNotFound);

        await categoryRepository.Received(1)
            .GetAll(Arg.Any<CancellationToken>());

        await transactionRepository.DidNotReceive()
            .AddRange(
                Arg.Any<IReadOnlyCollection<Domain.Entities.Transaction>>(),
                Arg.Any<CancellationToken>());
    }
}