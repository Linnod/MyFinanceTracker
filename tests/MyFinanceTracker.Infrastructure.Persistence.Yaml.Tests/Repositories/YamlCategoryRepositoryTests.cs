using FluentAssertions;
using MyFinanceTracker.Domain.Entities;
using MyFinanceTracker.Infrastructure.Persistence.Yaml.Loaders;
using NSubstitute;

namespace MyFinanceTracker.Infrastructure.Persistence.Yaml.Tests.Repositories;

public class YamlCategoryRepositoryTests
{
    private readonly Category groceries = new(
        "cat_groceries",
        "Groceries");

    private readonly Category salary = new(
        "cat_salary",
        "Salary");

    private readonly YamlCategoryRepository sut;

    public YamlCategoryRepositoryTests()
    {
        var loader = Substitute.For<ICategoryLoader>();
        loader.Load().Returns([groceries, salary]);
        sut = new YamlCategoryRepository(loader);
    }

    [Fact]
    public async Task GetAll_ReturnsAllLoadedCategories()
    {
        // act
        var result = await sut.GetAll(CancellationToken.None);

        // assert
        result.Should().Equal(groceries, salary);
    }

    [Fact]
    public async Task GetById_ValidId_ReturnsCategory()
    {
        // act
        var result = await sut.GetById("cat_groceries", CancellationToken.None);

        // assert
        result.Should().Be(groceries);
    }

    [Theory]
    [InlineData("CAT_GROCERIES")]
    [InlineData("cat_groceries")]
    public async Task GetById_CaseInsensitive_ReturnsCategory(string query)
    {
        // act
        var result = await sut.GetById(query, CancellationToken.None);

        // assert
        result.Should().Be(groceries);
    }

    [Theory]
    [InlineData("unknown")]
    [InlineData("groceris")]
    [InlineData("")]
    public async Task GetById_NotFound_ReturnsNull(string query)
    {
        // act
        var result = await sut.GetById(query, CancellationToken.None);

        // assert
        result.Should().BeNull();
    }
}
