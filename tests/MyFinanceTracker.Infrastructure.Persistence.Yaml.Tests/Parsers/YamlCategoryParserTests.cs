using FluentAssertions;
using MyFinanceTracker.Domain.Entities;
using MyFinanceTracker.Infrastructure.Persistence.Yaml.Parsers;
using MyFinanceTracker.Infrastructure.Persistence.Yaml.Parsers.Exceptions;

namespace MyFinanceTracker.Infrastructure.Persistence.Yaml.Tests.Parsers;

public class YamlCategoryParserTests
{
    private readonly YamlCategoryParser sut = new();

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    void Parse_ShouldThrowEmptyContent_WhenContentIsEmptyOrWhitespace(string yaml)
    {
        // act
        var act = () => sut.Parse(yaml);

        // assert
        act.Should().Throw<CategoryParserException>()
            .WithMessage("*content is empty*");
    }

    [Theory]
    [InlineData("categories: []")]
    [InlineData("categories:")]
    [InlineData("# only comments")]
    void Parse_ShouldThrowNoCategoriesFound_WhenContentHasNoCategories(string yaml)
    {
        // act
        var act = () => sut.Parse(yaml);

        // assert
        act.Should().Throw<CategoryParserException>()
            .WithMessage("*contains no categories*");
    }

    [Fact]
    void Parse_ShouldThrowDuplicateId_WhenIdsAreNotUnique()
    {
        // arrange
        const string yaml = """
            categories:
              - id: income
                name: Income
              - id: A
                name: First
              - id: A
                name: Second
            """;

        // act
        var act = () => sut.Parse(yaml);

        // assert
        act.Should().Throw<CategoryParserException>()
            .WithMessage("*Duplicate Category ID*A*");
    }

    [Fact]
    void Parse_ShouldThrowDuplicateName_WhenNamesAreNotUnique()
    {
        // arrange
        const string yaml = """
            categories:
              - id: income
                name: Income
              - id: food_1
                name: Products
              - id: food_2
                name: products
            """;

        // act
        var act = () => sut.Parse(yaml);

        // assert
        act.Should().Throw<CategoryParserException>()
            .WithMessage("*Duplicate Category Name*Products*");
    }

    [Fact]
    void Parse_ShouldThrowInvalidCategory_WhenDomainValidationFails()
    {
        // arrange
        const string yaml = """
            categories:
              - id: ""
                name: Products
            """;

        // act
        var act = () => sut.Parse(yaml);

        // assert
        act.Should().Throw<CategoryParserException>()
            .WithMessage("*Invalid category*Category ID cannot be empty or whitespace*");
    }

    [Fact]
    void Parse_ShouldReturnCategories_WhenYamlIsValid()
    {
        // arrange
        const string yaml = """
            categories:
              - id: INCOME
                name: Salary
              - id: FOOD
                name: Products
            """;

        Category[] expected =
        [
            new("INCOME", "Salary"),
            new("FOOD", "Products")
        ];

        // act
        var result = sut.Parse(yaml);

        // assert
        result.Should().BeEquivalentTo(expected);
    }

    [Fact]
    void Parse_ShouldSetIsArchived_WhenArchivedFlagIsSpecifiedInYaml()
    {
        // arrange
        const string yaml = """
            categories:
              - id: ACTIVE
                name: Active Category
              - id: ARCHIVED
                name: Archived Category
                archived: true
            """;

        Category[] expected =
        [
            new("ACTIVE", "Active Category", isArchived: false),
            new("ARCHIVED", "Archived Category", isArchived: true)
        ];

        // act
        var result = sut.Parse(yaml);

        // assert
        result.Should().BeEquivalentTo(expected);
    }

    [Fact]
    void Parse_ShouldThrowDeserializationFailed_WhenYamlIsInvalid()
    {
        // arrange
        const string yaml = "invalid: [ [ [ yaml structure";

        // act
        var act = () => sut.Parse(yaml);

        // assert
        act.Should().Throw<CategoryParserException>()
            .WithMessage("*Failed to deserialize*");
    }
}
