using FluentAssertions;
using MyFinanceTracker.Domain.Entities;
using MyFinanceTracker.Domain.Repositories;
using MyFinanceTracker.UseCases.Transaction.GetById;
using NSubstitute;

namespace MyFinanceTracker.UseCases.Tests.Transaction.GetById;

public sealed class GetTransactionByIdHandlerTests
{
    private readonly ITransactionRepository transactionRepository = Substitute.For<ITransactionRepository>();
    private readonly GetTransactionByIdHandler sut;

    private readonly Domain.Entities.Category foodCategory = new("food", "Food");

    public GetTransactionByIdHandlerTests()
    {
        sut = new GetTransactionByIdHandler(transactionRepository);
    }

    [Fact]
    public async Task Handle_TransactionExists_ReturnsSuccessWithTransaction()
    {
        // arrange
        var id = Guid.NewGuid();
        var transaction = new Domain.Entities.Transaction(
            id,
            TransactionType.Expense,
            foodCategory,
            42.50m,
            new DateTime(2026, 11, 16, 12, 30, 0),
            "Lunch");

        transactionRepository.GetById(id, Arg.Any<CancellationToken>())
            .Returns(transaction);

        var request = new GetTransactionByIdRequest(id);

        // act
        var result = await sut.Handle(request, CancellationToken.None);

        // assert
        var success = result.Should().BeOfType<GetTransactionByIdResponse.Success>().Subject;
        success.Transaction.Should().Be(transaction);
    }

    [Fact]
    public async Task Handle_TransactionNotFound_ReturnsValidationError()
    {
        // arrange
        var id = Guid.NewGuid();
        transactionRepository.GetById(id, Arg.Any<CancellationToken>())
            .Returns((Domain.Entities.Transaction?)null);

        var request = new GetTransactionByIdRequest(id);

        // act
        var result = await sut.Handle(request, CancellationToken.None);

        // assert
        var error = result.Should().BeOfType<GetTransactionByIdResponse.ValidationError>().Subject;
        error.Errors.Should().ContainSingle(e =>
            e.ErrorCode == ValidationErrorCode.Transaction.TransactionNotFound &&
            e.AttemptedValue == id.ToString());
    }
}
