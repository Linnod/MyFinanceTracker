using FluentAssertions;
using MyFinanceTracker.Domain.Entities;
using MyFinanceTracker.Infrastructure.Persistence.Yaml.Loaders;
using NSubstitute;

namespace MyFinanceTracker.Infrastructure.Persistence.Yaml.Tests.Repositories;

public class YamlCategoryRepositoryTests
{
    private readonly Category groceries = new(
        "cat_groceries",
        "Groceries",
        ["food", "supermarket"]);

    private readonly Category salary = new(
        "cat_salary",
        "Salary",
        ["income", "paycheck"]);

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
    public async Task Resolve_ById_ReturnsCategory()
    {
        // act
        var result = await sut.Resolve("cat_groceries", CancellationToken.None);

        // assert
        result.Should().Be(groceries);
    }

    [Fact]
    public async Task Resolve_ByName_ReturnsCategory()
    {
        // act
        var result = await sut.Resolve("Groceries", CancellationToken.None);

        // assert
        result.Should().Be(groceries);
    }

    [Fact]
    public async Task Resolve_ByAlias_ReturnsCategory()
    {
        // act
        var result = await sut.Resolve("food", CancellationToken.None);

        // assert
        result.Should().Be(groceries);
    }

    [Theory]
    [InlineData("CAT_GROCERIES")]
    [InlineData("cat_groceries")]
    [InlineData("GROCERIES")]
    [InlineData("Groceries")]
    [InlineData("FOOD")]
    [InlineData("food")]
    [InlineData("SUPERMARKET")]
    [InlineData("supermarket")]
    public async Task Resolve_CaseInsensitive_ReturnsCategory(string query)
    {
        // act
        var result = await sut.Resolve(query, CancellationToken.None);

        // assert
        result.Should().Be(groceries);
    }

    [Theory]
    [InlineData("unknown")]
    [InlineData("groceris")]
    [InlineData("")]
    public async Task Resolve_NotFound_ReturnsNull(string query)
    {
        // act
        var result = await sut.Resolve(query, CancellationToken.None);

        // assert
        result.Should().BeNull();
    }
}
