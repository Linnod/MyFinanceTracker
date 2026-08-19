using FluentAssertions;
using MyFinanceTracker.Domain.Repositories;
using MyFinanceTracker.UseCases.Transaction.Delete;
using NSubstitute;

namespace MyFinanceTracker.UseCases.Tests.Transaction.Delete;

public class DeleteTransactionsHandlerTests
{
    private readonly ICategoryRepository categoryRepository;
    private readonly ITransactionRepository transactionRepository;
    private readonly DeleteTransactionsHandler sut;

    public DeleteTransactionsHandlerTests()
    {
        categoryRepository = Substitute.For<ICategoryRepository>();
        transactionRepository = Substitute.For<ITransactionRepository>();
        sut = new DeleteTransactionsHandler(
            categoryRepository,
            transactionRepository);
    }

    [Fact]
    async Task Handle_CategoryExists_DeletesTransactionsAndReturnsSuccess()
    {
        // arrange
        var category = new Domain.Entities.Category("A", "Food", ["food"]);
        var date = new DateOnly(2026, 1, 27);

        categoryRepository
            .GetAll(Arg.Any<CancellationToken>())
            .Returns([category]);

        // act
        var result = await sut.Handle(
            new DeleteTransactionsRequest("food", date),
            CancellationToken.None);

        // assert
        var success = result.Should()
            .BeOfType<DeleteTransactionsResponse.Success>()
            .Subject;

        success.CategoryName.Should().Be("Food");
        success.Date.Should().Be(date);

        await transactionRepository.Received(1)
            .DeleteRange(category, date, Arg.Any<CancellationToken>());
        await categoryRepository.Received(1)
            .GetAll(Arg.Any<CancellationToken>());
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

        // act
        var result = await sut.Handle(
            new DeleteTransactionsRequest(
                CategoryAlias: "fod",
                Date: new DateOnly(2026, 1, 27)),
            CancellationToken.None);

        // assert
        var error = result.Should()
            .BeOfType<DeleteTransactionsResponse.ValidationError>()
            .Subject;

        error.Errors.Should().ContainSingle();

        error.Errors.Single().ErrorCode
            .Should().Be(ValidationErrorCode.Transaction.CategoryNotFound);

        await categoryRepository.Received(1)
            .GetAll(Arg.Any<CancellationToken>());
        await transactionRepository.DidNotReceive()
            .DeleteRange(
                Arg.Any<Domain.Entities.Category>(),
                Arg.Any<DateOnly>(),
                Arg.Any<CancellationToken>());
    }
}