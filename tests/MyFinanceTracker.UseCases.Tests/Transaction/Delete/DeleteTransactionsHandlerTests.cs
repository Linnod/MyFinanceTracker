using FluentAssertions;
using MyFinanceTracker.Domain.Repositories;
using MyFinanceTracker.UseCases;
using MyFinanceTracker.UseCases.Transaction.Delete;
using NSubstitute;
using Xunit;

namespace MyFinanceTracker.UseCases.Tests.Transaction.Delete;

public class DeleteTransactionsHandlerTests
{
    private readonly ITransactionRepository transactionRepository;
    private readonly DeleteTransactionsHandler sut;

    public DeleteTransactionsHandlerTests()
    {
        transactionRepository = Substitute.For<ITransactionRepository>();
        sut = new DeleteTransactionsHandler(transactionRepository);
    }

    [Fact]
    public async Task Handle_WhenAllRequestedIdsFound_ShouldReturnSuccess()
    {
        // arrange
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();

        transactionRepository
            .DeleteRange(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new DeleteTransactionsResult.Success(2));

        var request = new DeleteTransactionsRequest([id1, id2]);

        // act
        var result = await sut.Handle(request, CancellationToken.None);

        // assert
        var success = result.Should().BeOfType<DeleteTransactionsResponse.Success>().Subject;
        success.DeletedCount.Should().Be(2);

        await transactionRepository.Received(1).DeleteRange(
            Arg.Is<IReadOnlyCollection<Guid>>(ids => ids.Count == 2 && ids.Contains(id1) && ids.Contains(id2)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenRequestContainsDuplicateIds_ShouldDeduplicateBeforeCallingDeleteRange()
    {
        // arrange
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();

        transactionRepository
            .DeleteRange(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new DeleteTransactionsResult.Success(2));

        var request = new DeleteTransactionsRequest([id1, id1, id2, id2]);

        // act
        var result = await sut.Handle(request, CancellationToken.None);

        // assert
        var success = result.Should().BeOfType<DeleteTransactionsResponse.Success>().Subject;
        success.DeletedCount.Should().Be(2);

        await transactionRepository.Received(1).DeleteRange(
            Arg.Is<IReadOnlyCollection<Guid>>(ids => ids.Count == 2 && ids.Contains(id1) && ids.Contains(id2)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenRepositoryReturnsNotFound_ShouldReturnValidationError()
    {
        // arrange
        var foundId = Guid.NewGuid();
        var missingId1 = Guid.NewGuid();
        var missingId2 = Guid.NewGuid();

        transactionRepository
            .DeleteRange(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new DeleteTransactionsResult.NotFound([missingId1, missingId2]));

        var request = new DeleteTransactionsRequest([foundId, missingId1, missingId2]);

        // act
        var result = await sut.Handle(request, CancellationToken.None);

        // assert
        var validationError = result.Should().BeOfType<DeleteTransactionsResponse.ValidationError>().Subject;
        validationError.Errors.Should().HaveCount(2);

        validationError.Errors.Should().Contain(e =>
            e.ErrorCode == ValidationErrorCode.Transaction.TransactionNotFound &&
            e.AttemptedValue == missingId1.ToString());

        validationError.Errors.Should().Contain(e =>
            e.ErrorCode == ValidationErrorCode.Transaction.TransactionNotFound &&
            e.AttemptedValue == missingId2.ToString());
    }
}
