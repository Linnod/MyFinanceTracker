using FluentAssertions;
using MyFinanceTracker.Domain.Repositories;
using MyFinanceTracker.UseCases.Category.Update;
using NSubstitute;

namespace MyFinanceTracker.UseCases.Tests.Category.Update;

public class UpdateCategoryHandlerTests
{
    private readonly ICategoryRepository categoryRepository = Substitute.For<ICategoryRepository>();
    private readonly UpdateCategoryHandler sut;

    public UpdateCategoryHandlerTests()
    {
        sut = new UpdateCategoryHandler(categoryRepository);
    }

    [Fact]
    public async Task Handle_CategoryNotFound_ReturnsValidationErrorAndDoesNotCallUpdate()
    {
        // arrange
        categoryRepository.GetById("food", Arg.Any<CancellationToken>())
            .Returns((Domain.Entities.Category?)null);

        var request = new UpdateCategoryRequest("food", Name: "Food");

        // act
        var result = await sut.Handle(request, CancellationToken.None);

        // assert
        var error = result.Should().BeOfType<UpdateCategoryResponse.ValidationError>().Subject;
        error.Errors.Should().ContainSingle(e =>
            e.ErrorCode == ValidationErrorCode.Category.CategoryNotFound &&
            e.AttemptedValue == "food");

        await categoryRepository.DidNotReceiveWithAnyArgs().Update(default!, default);
    }

    [Fact]
    public async Task Handle_UpdateNameOnly_UpdatesNamePreservesIsArchivedAndCallsRepository()
    {
        // arrange
        var existing = new Domain.Entities.Category("food", "Old Food", isArchived: false);
        categoryRepository.GetById("food", Arg.Any<CancellationToken>())
            .Returns(existing);

        var request = new UpdateCategoryRequest("food", Name: "  Delicious Food  ");

        // act
        var result = await sut.Handle(request, CancellationToken.None);

        // assert
        var success = result.Should().BeOfType<UpdateCategoryResponse.Success>().Subject;
        success.Category.Id.Should().Be("food");
        success.Category.Name.Should().Be("Delicious Food");
        success.Category.IsArchived.Should().BeFalse();

        await categoryRepository.Received(1).Update(
            Arg.Is<Domain.Entities.Category>(c =>
                c.Id == "food" &&
                c.Name == "Delicious Food" &&
                !c.IsArchived),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_UpdateIsArchivedOnly_UpdatesIsArchivedPreservesNameAndCallsRepository()
    {
        // arrange
        var existing = new Domain.Entities.Category("food", "Food", isArchived: false);
        categoryRepository.GetById("food", Arg.Any<CancellationToken>())
            .Returns(existing);

        var request = new UpdateCategoryRequest("food", IsArchived: true);

        // act
        var result = await sut.Handle(request, CancellationToken.None);

        // assert
        var success = result.Should().BeOfType<UpdateCategoryResponse.Success>().Subject;
        success.Category.Id.Should().Be("food");
        success.Category.Name.Should().Be("Food");
        success.Category.IsArchived.Should().BeTrue();

        await categoryRepository.Received(1).Update(
            Arg.Is<Domain.Entities.Category>(c =>
                c.Id == "food" &&
                c.Name == "Food" &&
                c.IsArchived),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_UpdateBothFields_UpdatesBothAndCallsRepository()
    {
        // arrange
        var existing = new Domain.Entities.Category("food", "Old Food", isArchived: true);
        categoryRepository.GetById("food", Arg.Any<CancellationToken>())
            .Returns(existing);

        var request = new UpdateCategoryRequest("food", Name: "Groceries", IsArchived: false);

        // act
        var result = await sut.Handle(request, CancellationToken.None);

        // assert
        var success = result.Should().BeOfType<UpdateCategoryResponse.Success>().Subject;
        success.Category.Id.Should().Be("food");
        success.Category.Name.Should().Be("Groceries");
        success.Category.IsArchived.Should().BeFalse();

        await categoryRepository.Received(1).Update(
            Arg.Is<Domain.Entities.Category>(c =>
                c.Id == "food" &&
                c.Name == "Groceries" &&
                !c.IsArchived),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhitespaceName_PreservesExistingName()
    {
        // arrange
        var existing = new Domain.Entities.Category("food", "Food", isArchived: false);
        categoryRepository.GetById("food", Arg.Any<CancellationToken>())
            .Returns(existing);

        var request = new UpdateCategoryRequest("food", Name: "   ", IsArchived: true);

        // act
        var result = await sut.Handle(request, CancellationToken.None);

        // assert
        var success = result.Should().BeOfType<UpdateCategoryResponse.Success>().Subject;
        success.Category.Id.Should().Be("food");
        success.Category.Name.Should().Be("Food");
        success.Category.IsArchived.Should().BeTrue();

        await categoryRepository.Received(1).Update(
            Arg.Is<Domain.Entities.Category>(c =>
                c.Id == "food" &&
                c.Name == "Food" &&
                c.IsArchived),
            Arg.Any<CancellationToken>());
    }
}
