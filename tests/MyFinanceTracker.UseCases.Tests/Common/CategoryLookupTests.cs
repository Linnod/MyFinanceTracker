using FluentAssertions;
using MyFinanceTracker.UseCases.Common;
using DomainCategory = MyFinanceTracker.Domain.Entities.Category;

namespace MyFinanceTracker.UseCases.Tests.Common;

public class CategoryLookupTests
{
    private readonly DomainCategory groceriesCategory = new(
        "groceries",
        "Продукты",
        ["продукты", "еда", "супермаркет"]);

    private readonly DomainCategory salaryCategory = new(
        "salary",
        "Зарплата",
        ["зарплата", "доход"],
        isIncome: true);

    private readonly CategoryLookup sut;

    public CategoryLookupTests()
    {
        sut = new CategoryLookup([groceriesCategory, salaryCategory]);
    }

    [Fact]
    public void Resolve_ById_ReturnsFoundCategory()
    {
        // act
        var result = sut.Resolve("groceries");

        // assert
        var found = result.Should().BeOfType<CategoryResolution.Found>().Subject;
        found.Category.Should().Be(groceriesCategory);
    }

    [Fact]
    public void Resolve_ByName_ReturnsFoundCategory()
    {
        // act
        var result = sut.Resolve("Продукты");

        // assert
        var found = result.Should().BeOfType<CategoryResolution.Found>().Subject;
        found.Category.Should().Be(groceriesCategory);
    }

    [Fact]
    public void Resolve_ByAlias_ReturnsFoundCategory()
    {
        // act
        var result = sut.Resolve("еда");

        // assert
        var found = result.Should().BeOfType<CategoryResolution.Found>().Subject;
        found.Category.Should().Be(groceriesCategory);
    }

    [Theory]
    [InlineData("GROCERIES")]
    [InlineData("Groceries")]
    [InlineData("ПРОДУКТЫ")]
    [InlineData("продукты")]
    [InlineData("ЕДА")]
    [InlineData("еда")]
    public void Resolve_CaseInsensitive_ReturnsFoundCategory(string query)
    {
        // act
        var result = sut.Resolve(query);

        // assert
        var found = result.Should().BeOfType<CategoryResolution.Found>().Subject;
        found.Category.Should().Be(groceriesCategory);
    }

    [Fact]
    public void Resolve_NotFound_WithCloseTypo_ReturnsNotFoundWithSuggestion()
    {
        // act
        var result = sut.Resolve("groceris");

        // assert
        var notFound = result.Should().BeOfType<CategoryResolution.NotFound>().Subject;
        notFound.Suggestion.Should().Be("groceries");
    }

    [Fact]
    public void Resolve_NotFound_WithoutCloseMatch_ReturnsNotFoundWithNullSuggestion()
    {
        // act
        var result = sut.Resolve("completely_unrelated_text_12345");

        // assert
        var notFound = result.Should().BeOfType<CategoryResolution.NotFound>().Subject;
        notFound.Suggestion.Should().BeNull();
    }
}
