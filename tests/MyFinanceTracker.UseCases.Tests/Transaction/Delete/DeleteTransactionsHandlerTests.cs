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
            .Returns(2);

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
            .Returns(2);

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
    public async Task Handle_WhenSomeOrNoIdsDeleted_ShouldReturnSuccessWithDeletedCount()
    {
        // arrange
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();

        transactionRepository
            .DeleteRange(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(1);

        var request = new DeleteTransactionsRequest([id1, id2]);

        // act
        var result = await sut.Handle(request, CancellationToken.None);

        // assert
        var success = result.Should().BeOfType<DeleteTransactionsResponse.Success>().Subject;
        success.DeletedCount.Should().Be(1);

        await transactionRepository.Received(1).DeleteRange(
            Arg.Is<IReadOnlyCollection<Guid>>(ids => ids.Count == 2 && ids.Contains(id1) && ids.Contains(id2)),
            Arg.Any<CancellationToken>());
    }
}
