using FluentAssertions;
using MyFinanceTracker.Domain.Entities;
using MyFinanceTracker.Infrastructure.Persistence.Yaml.Loaders;

namespace MyFinanceTracker.Infrastructure.Persistence.Yaml.Tests.Repositories;

public class YamlCategoryRepositoryTests
{
    private readonly Category groceries = new(
        "groceries",
        "Продукты",
        ["продукты", "еда", "супермаркет"]);

    private readonly Category salary = new(
        "salary",
        "Зарплата",
        ["зарплата", "доход"]);

    private readonly YamlCategoryRepository sut;

    public YamlCategoryRepositoryTests()
    {
        var loader = new FakeCategoryLoader([groceries, salary]);
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
        var result = await sut.Resolve("groceries", CancellationToken.None);

        // assert
        result.Should().Be(groceries);
    }

    [Fact]
    public async Task Resolve_ByName_ReturnsCategory()
    {
        // act
        var result = await sut.Resolve("Продукты", CancellationToken.None);

        // assert
        result.Should().Be(groceries);
    }

    [Fact]
    public async Task Resolve_ByAlias_ReturnsCategory()
    {
        // act
        var result = await sut.Resolve("еда", CancellationToken.None);

        // assert
        result.Should().Be(groceries);
    }

    [Theory]
    [InlineData("GROCERIES")]
    [InlineData("Groceries")]
    [InlineData("ПРОДУКТЫ")]
    [InlineData("продукты")]
    [InlineData("ЕДА")]
    [InlineData("еда")]
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

    private sealed class FakeCategoryLoader(IReadOnlyList<Category> categories) : ICategoryLoader
    {
        public IReadOnlyList<Category> Load() => categories;
    }
}
