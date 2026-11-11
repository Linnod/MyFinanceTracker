using FluentAssertions;
using MyFinanceTracker.Domain.Entities;
using MyFinanceTracker.Domain.Repositories;
using MyFinanceTracker.UseCases.Transaction.Update;
using NSubstitute;

namespace MyFinanceTracker.UseCases.Tests.Transaction.Update;

public class UpdateTransactionHandlerTests
{
    private readonly ICategoryRepository categoryRepository = Substitute.For<ICategoryRepository>();
    private readonly ITransactionRepository transactionRepository = Substitute.For<ITransactionRepository>();
    private readonly UpdateTransactionHandler sut;

    private readonly Domain.Entities.Category foodCategory = new("food", "Food");
    private readonly Domain.Entities.Category transportCategory = new("transport", "Transport");

    public UpdateTransactionHandlerTests()
    {
        sut = new UpdateTransactionHandler(categoryRepository, transactionRepository);
    }

    [Fact]
    public async Task Handle_TransactionNotFound_ReturnsValidationError()
    {
        // arrange
        var id = Guid.NewGuid();
        transactionRepository.GetById(id, Arg.Any<CancellationToken>())
            .Returns((Domain.Entities.Transaction?)null);

        var request = new UpdateTransactionRequest(id, Amount: 100m);

        // act
        var result = await sut.Handle(request, CancellationToken.None);

        // assert
        var error = result.Should().BeOfType<UpdateTransactionResponse.ValidationError>().Subject;
        error.Errors.Should().ContainSingle(e =>
            e.ErrorCode == ValidationErrorCode.Transaction.TransactionNotFound &&
            e.AttemptedValue == id.ToString());

        await transactionRepository.DidNotReceiveWithAnyArgs().Update(default!, default);
    }

    [Fact]
    public async Task Handle_PartialUpdate_PreservesUnchangedFieldsAndCallsRepositoryUpdate()
    {
        // arrange
        var id = Guid.NewGuid();
        var existing = new Domain.Entities.Transaction(
            id,
            TransactionType.Expense,
            foodCategory,
            50m,
            new DateTime(2026, 11, 1),
            "Lunch");

        transactionRepository.GetById(id, Arg.Any<CancellationToken>())
            .Returns(existing);

        var request = new UpdateTransactionRequest(id, Amount: 125.75m);

        // act
        var result = await sut.Handle(request, CancellationToken.None);

        // assert
        var success = result.Should().BeOfType<UpdateTransactionResponse.Success>().Subject;
        success.Transaction.Id.Should().Be(id);
        success.Transaction.Type.Should().Be(TransactionType.Expense);
        success.Transaction.Category.Should().Be(foodCategory);
        success.Transaction.Amount.Should().Be(125.75m);
        success.Transaction.Date.Should().Be(new DateTime(2026, 11, 1));
        success.Transaction.Note.Should().Be("Lunch");

        await transactionRepository.Received(1).Update(
            Arg.Is<Domain.Entities.Transaction>(t =>
                t.Id == id &&
                t.Type == TransactionType.Expense &&
                t.Category == foodCategory &&
                t.Amount == 125.75m &&
                t.Date == new DateTime(2026, 11, 1) &&
                t.Note == "Lunch"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ChangeCategory_CategoryNotFound_ReturnsValidationError()
    {
        // arrange
        var id = Guid.NewGuid();
        var existing = new Domain.Entities.Transaction(
            id,
            TransactionType.Expense,
            foodCategory,
            50m,
            new DateTime(2026, 11, 1),
            null);

        transactionRepository.GetById(id, Arg.Any<CancellationToken>())
            .Returns(existing);

        categoryRepository.GetById("unknown", Arg.Any<CancellationToken>())
            .Returns((Domain.Entities.Category?)null);

        var request = new UpdateTransactionRequest(id, CategoryId: "unknown");

        // act
        var result = await sut.Handle(request, CancellationToken.None);

        // assert
        var error = result.Should().BeOfType<UpdateTransactionResponse.ValidationError>().Subject;
        error.Errors.Should().ContainSingle(e =>
            e.ErrorCode == ValidationErrorCode.Transaction.CategoryNotFound &&
            e.AttemptedValue == "unknown");

        await transactionRepository.DidNotReceiveWithAnyArgs().Update(default!, default);
    }

    [Fact]
    public async Task Handle_ChangeCategory_CategoryArchived_ReturnsValidationError()
    {
        // arrange
        var id = Guid.NewGuid();
        var existing = new Domain.Entities.Transaction(
            id,
            TransactionType.Expense,
            foodCategory,
            50m,
            new DateTime(2026, 11, 1),
            null);

        var archivedCategory = new Domain.Entities.Category("archived", "Archived Category", isArchived: true);

        transactionRepository.GetById(id, Arg.Any<CancellationToken>())
            .Returns(existing);

        categoryRepository.GetById("archived", Arg.Any<CancellationToken>())
            .Returns(archivedCategory);

        var request = new UpdateTransactionRequest(id, CategoryId: "archived");

        // act
        var result = await sut.Handle(request, CancellationToken.None);

        // assert
        var error = result.Should().BeOfType<UpdateTransactionResponse.ValidationError>().Subject;
        error.Errors.Should().ContainSingle(e =>
            e.ErrorCode == ValidationErrorCode.Transaction.CategoryArchived &&
            e.AttemptedValue == "archived");

        await transactionRepository.DidNotReceiveWithAnyArgs().Update(default!, default);
    }

    [Fact]
    public async Task Handle_ChangeCategory_Successful_UpdatesCategory()
    {
        // arrange
        var id = Guid.NewGuid();
        var existing = new Domain.Entities.Transaction(
            id,
            TransactionType.Expense,
            foodCategory,
            50m,
            new DateTime(2026, 11, 1),
            null);

        transactionRepository.GetById(id, Arg.Any<CancellationToken>())
            .Returns(existing);

        categoryRepository.GetById("transport", Arg.Any<CancellationToken>())
            .Returns(transportCategory);

        var request = new UpdateTransactionRequest(id, CategoryId: "transport");

        // act
        var result = await sut.Handle(request, CancellationToken.None);

        // assert
        var success = result.Should().BeOfType<UpdateTransactionResponse.Success>().Subject;
        success.Transaction.Category.Should().Be(transportCategory);

        await transactionRepository.Received(1).Update(
            Arg.Is<Domain.Entities.Transaction>(t => t.Category == transportCategory),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ClearNoteIsTrue_ClearsExistingNoteToNull()
    {
        // arrange
        var id = Guid.NewGuid();
        var existing = new Domain.Entities.Transaction(
            id,
            TransactionType.Expense,
            foodCategory,
            50m,
            new DateTime(2026, 11, 1),
            "Old Note");

        transactionRepository.GetById(id, Arg.Any<CancellationToken>())
            .Returns(existing);

        var request = new UpdateTransactionRequest(id, ClearNote: true);

        // act
        var result = await sut.Handle(request, CancellationToken.None);

        // assert
        var success = result.Should().BeOfType<UpdateTransactionResponse.Success>().Subject;
        success.Transaction.Note.Should().BeNull();

        await transactionRepository.Received(1).Update(
            Arg.Is<Domain.Entities.Transaction>(t => t.Note == null),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NewNonEmptyNote_UpdatesNote()
    {
        // arrange
        var id = Guid.NewGuid();
        var existing = new Domain.Entities.Transaction(
            id,
            TransactionType.Expense,
            foodCategory,
            50m,
            new DateTime(2026, 11, 1),
            "Old Note");

        transactionRepository.GetById(id, Arg.Any<CancellationToken>())
            .Returns(existing);

        var request = new UpdateTransactionRequest(id, Note: "Brand New Note");

        // act
        var result = await sut.Handle(request, CancellationToken.None);

        // assert
        var success = result.Should().BeOfType<UpdateTransactionResponse.Success>().Subject;
        success.Transaction.Note.Should().Be("Brand New Note");

        await transactionRepository.Received(1).Update(
            Arg.Is<Domain.Entities.Transaction>(t => t.Note == "Brand New Note"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NullNoteWithClearNoteFalse_PreservesExistingNote()
    {
        // arrange
        var id = Guid.NewGuid();
        var existing = new Domain.Entities.Transaction(
            id,
            TransactionType.Expense,
            foodCategory,
            50m,
            new DateTime(2026, 11, 1),
            "Keep This Note");

        transactionRepository.GetById(id, Arg.Any<CancellationToken>())
            .Returns(existing);

        var request = new UpdateTransactionRequest(id, Amount: 99m, Note: null, ClearNote: false);

        // act
        var result = await sut.Handle(request, CancellationToken.None);

        // assert
        var success = result.Should().BeOfType<UpdateTransactionResponse.Success>().Subject;
        success.Transaction.Note.Should().Be("Keep This Note");

        await transactionRepository.Received(1).Update(
            Arg.Is<Domain.Entities.Transaction>(t => t.Note == "Keep This Note"),
            Arg.Any<CancellationToken>());
    }
}
