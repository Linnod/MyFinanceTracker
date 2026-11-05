using FluentAssertions;
using MyFinanceTracker.Domain.Repositories;
using MyFinanceTracker.UseCases.Category.List;
using NSubstitute;

namespace MyFinanceTracker.UseCases.Tests.Category.List;

public class ListCategoriesCommandHandlerTests
{
    private readonly ICategoryRepository categoryRepository = Substitute.For<ICategoryRepository>();
    private readonly ListCategoriesCommandHandler sut;

    public ListCategoriesCommandHandlerTests()
    {
        sut = new ListCategoriesCommandHandler(categoryRepository);
    }

    [Fact]
    public async Task Handle_DefaultRequest_ReturnsOnlyNonArchivedCategories()
    {
        // arrange
        var activeCategory = new Domain.Entities.Category("active", "Active", ["active"], isArchived: false);
        var archivedCategory = new Domain.Entities.Category("archived", "Archived", ["archived"], isArchived: true);

        categoryRepository.GetAll(Arg.Any<CancellationToken>())
            .Returns([activeCategory, archivedCategory]);

        var request = new ListCategoriesRequest();

        // act
        var response = await sut.Handle(request, CancellationToken.None);

        // assert
        var success = response.Should().BeOfType<ListCategoriesResponse.Success>().Subject;
        success.Categories.Should().ContainSingle()
            .Which.Should().Be(activeCategory);
    }

    [Fact]
    public async Task Handle_IncludeArchivedTrue_ReturnsAllCategories()
    {
        // arrange
        var activeCategory = new Domain.Entities.Category("active", "Active", ["active"], isArchived: false);
        var archivedCategory = new Domain.Entities.Category("archived", "Archived", ["archived"], isArchived: true);

        categoryRepository.GetAll(Arg.Any<CancellationToken>())
            .Returns([activeCategory, archivedCategory]);

        var request = new ListCategoriesRequest(IncludeArchived: true);

        // act
        var response = await sut.Handle(request, CancellationToken.None);

        // assert
        var success = response.Should().BeOfType<ListCategoriesResponse.Success>().Subject;
        success.Categories.Should().HaveCount(2)
            .And.Contain([activeCategory, archivedCategory]);
    }
}
