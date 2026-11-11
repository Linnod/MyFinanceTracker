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
                aliases: [income]
              - id: A
                name: First
                aliases: [first]
              - id: A
                name: Second
                aliases: [second]
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
                aliases: [income]
              - id: food_1
                name: Products
                aliases: [first]
              - id: food_2
                name: products
                aliases: [second]
            """;

        // act
        var act = () => sut.Parse(yaml);

        // assert
        act.Should().Throw<CategoryParserException>()
            .WithMessage("*Duplicate Category Name*Products*");
    }

    [Fact]
    void Parse_ShouldThrowDuplicateAlias_WhenAliasesAreNotUnique()
    {
        // arrange
        const string yaml = """
            categories:
              - id: A
                name: Income
                aliases: [income]
              - id: B
                name: Food
                aliases: [test]
              - id: C
                name: Drinks
                aliases: [test]
            """;

        // act
        var act = () => sut.Parse(yaml);

        // assert
        act.Should().Throw<CategoryParserException>()
            .WithMessage("*Duplicate alias*test*");
    }

    [Fact]
    void Parse_ShouldThrowMissingAliases_WhenCategoryHasNoAliases()
    {
        // arrange
        const string yaml = """
            categories:
              - id: A
                name: Income
                aliases: []
            """;

        // act
        var act = () => sut.Parse(yaml);

        // assert
        act.Should().Throw<CategoryParserException>()
            .WithMessage("*must have at least one alias*");
    }

    [Fact]
    void Parse_ShouldThrowInvalidCategory_WhenDomainValidationFails()
    {
        // arrange
        const string yaml = """
            categories:
              - id: ""
                name: Products
                aliases: [products]
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
                aliases: [income]
              - id: FOOD
                name: Products
                aliases: [eat, meal]
            """;

        Category[] expected =
        [
            new("INCOME", "Salary", ["income"]),
            new("FOOD", "Products", ["eat", "meal"])
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
                aliases: [active]
              - id: ARCHIVED
                name: Archived Category
                aliases: [archived]
                archived: true
            """;

        Category[] expected =
        [
            new("ACTIVE", "Active Category", ["active"], isArchived: false),
            new("ARCHIVED", "Archived Category", ["archived"], isArchived: true)
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
