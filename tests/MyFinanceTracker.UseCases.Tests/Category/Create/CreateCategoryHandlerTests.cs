using FluentAssertions;
using MyFinanceTracker.Domain.Entities;
using MyFinanceTracker.Domain.Repositories;
using MyFinanceTracker.UseCases.Category.Create;
using NSubstitute;

namespace MyFinanceTracker.UseCases.Tests.Category.Create;

public class CreateCategoryHandlerTests
{
    private readonly ICategoryRepository categoryRepository = Substitute.For<ICategoryRepository>();
    private readonly CreateCategoryHandler sut;

    public CreateCategoryHandlerTests()
    {
        sut = new CreateCategoryHandler(categoryRepository);
    }

    [Fact]
    public async Task Handle_CategoryAlreadyExists_ReturnsValidationErrorAndDoesNotCallAdd()
    {
        // arrange
        var existing = new Domain.Entities.Category("food", "Food");
        categoryRepository.GetById("food", Arg.Any<CancellationToken>())
            .Returns(existing);

        var request = new CreateCategoryRequest("food", "Food");

        // act
        var result = await sut.Handle(request, CancellationToken.None);

        // assert
        var error = result.Should().BeOfType<CreateCategoryResponse.ValidationError>().Subject;
        error.Errors.Should().ContainSingle(e =>
            e.ErrorCode == ValidationErrorCode.Category.CategoryAlreadyExists &&
            e.AttemptedValue == "food");

        await categoryRepository.DidNotReceiveWithAnyArgs().Add(default!, default);
    }

    [Fact]
    public async Task Handle_CategoryDoesNotExist_TrimsAndNormalizes_CallsAddAndReturnsSuccess()
    {
        // arrange
        categoryRepository.GetById(" Food_1 ", Arg.Any<CancellationToken>())
            .Returns((Domain.Entities.Category?)null);

        var request = new CreateCategoryRequest(" Food_1 ", "  Groceries  ");

        // act
        var result = await sut.Handle(request, CancellationToken.None);

        // assert
        var success = result.Should().BeOfType<CreateCategoryResponse.Success>().Subject;
        success.Category.Id.Should().Be("food_1");
        success.Category.Name.Should().Be("Groceries");
        success.Category.IsArchived.Should().BeFalse();

        await categoryRepository.Received(1).Add(
            Arg.Is<Domain.Entities.Category>(c =>
                c.Id == "food_1" &&
                c.Name == "Groceries" &&
                !c.IsArchived),
            Arg.Any<CancellationToken>());
    }
}
