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
    async Task Handle_CategoryExists_ReturnsSuccess()
    {
        // arrange
        var category = new Domain.Entities.Category("A", "Food", ["food"]);
        var date = new DateOnly(2026, 1, 27);

        var transactions = new[]
        {
            new Domain.Entities.Transaction(
                Guid.NewGuid(),
                TransactionType.Expense,
                category,
                100m,
                date,
                "Lunch")
        };

        categoryRepository
            .GetByAlias("food", Arg.Any<CancellationToken>())
            .Returns(category);

        transactionRepository
            .Get(category, date, Arg.Any<CancellationToken>())
            .Returns(transactions);

        // act
        var result = await sut.Handle(
            new GetTransactionsRequest("food", date),
            CancellationToken.None);

        // assert
        var success = result.Should()
            .BeOfType<GetTransactionsResponse.Success>()
            .Subject;

        success.CategoryName.Should().Be("Food");
        success.Date.Should().Be(date);
        success.Transactions.Should().BeEquivalentTo(transactions);

        await categoryRepository.Received(1)
            .GetByAlias("food", Arg.Any<CancellationToken>());

        await transactionRepository.Received(1)
            .Get(category, date, Arg.Any<CancellationToken>());
    }

    [Fact]
    async Task Handle_CategoryDoesNotExist_ReturnsValidationError()
    {
        // arrange
        categoryRepository
            .GetByAlias("fod", Arg.Any<CancellationToken>())
            .Returns((Domain.Entities.Category?)null);

        categoryRepository
            .GetAll(Arg.Any<CancellationToken>())
            .Returns(
            [
                new Domain.Entities.Category("A", "Food", ["food"])
            ]);

        // act
        var result = await sut.Handle(
            new GetTransactionsRequest(
                CategoryAlias: "fod",
                Date: new DateOnly(2026, 1, 27)),
            CancellationToken.None);

        // assert
        var error = result.Should()
            .BeOfType<GetTransactionsResponse.ValidationError>()
            .Subject;

        error.Errors.Should().ContainSingle();

        error.Errors.Single().ErrorCode
            .Should().Be(ValidationErrorCode.Transaction.CategoryNotFound);

        await categoryRepository.Received(1)
            .GetAll(Arg.Any<CancellationToken>());

        await transactionRepository.DidNotReceive()
            .Get(
                Arg.Any<Domain.Entities.Category>(),
                Arg.Any<DateOnly>(),
                Arg.Any<CancellationToken>());
    }
}
