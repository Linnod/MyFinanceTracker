using MyFinanceTracker.Domain.Entities;
using MyFinanceTracker.Domain.Exceptions;

namespace MyFinanceTracker.Domain.Tests.Entities;

public class CategoryTests
{
    [Fact]
    void Constructor_WithValidArguments_InitializesPropertiesCorrectly()
    {
        // arrange
        var id = "salary";
        var name = "Salary";
        var aliases = new[] { "salary", "income", "main" };
        var isIncome = true;

        // act
        var category = new Category(id, name, aliases, isIncome);

        // assert
        Assert.Equal(id, category.Id);
        Assert.Equal(name, category.Name);
        Assert.Equal(aliases, category.Aliases);
        Assert.True(category.IsIncome);
    }

    [Fact]
    void Constructor_WithoutIsIncome_DefaultsToFalse()
    {
        // arrange
        var id = "food";
        var name = "Food";
        var aliases = new[] { "food" };

        // act
        var category = new Category(id, name, aliases);

        // assert
        Assert.False(category.IsIncome);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    void Constructor_WithInvalidId_ThrowsDomainException(string? invalidId)
    {
        // arrange
        var name = "Food";
        var aliases = new[] { "food" };

        // act
        Category act() => new(invalidId!, name, aliases);

        // assert
        var exception = Assert.Throws<DomainException>((Func<Category>)act);
        Assert.Equal("Category ID cannot be empty or whitespace.", exception.Message);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    void Constructor_WithInvalidName_ThrowsDomainException(string? invalidName)
    {
        // arrange
        var id = "food";
        var aliases = new[] { "food" };

        // act
        Category act() => new(id, invalidName!, aliases);

        // assert
        var exception = Assert.Throws<DomainException>(act);
        Assert.Equal("Category Name cannot be empty or whitespace.", exception.Message);
    }

    [Fact]
    void Constructor_WithNullAliases_ThrowsDomainException()
    {
        // arrange
        var id = "food";
        var name = "Food";

        // Act
        Category act() => new(id, name, null!);

        // assert
        var exception = Assert.Throws<DomainException>((Func<Category>)act);
        Assert.Equal("Category must have at least one alias.", exception.Message);
    }

    [Fact]
    void Constructor_WithEmptyAliases_ThrowsDomainException()
    {
        // arrange
        var id = "food";
        var name = "Food";
        var emptyAliases = Array.Empty<string>();

        // act
        Category act() => new(id, name, emptyAliases);

        // assert
        var exception = Assert.Throws<DomainException>((Func<Category>)act);
        Assert.Equal("Category must have at least one alias.", exception.Message);
    }

    [Fact]
    public void ToString_ReturnsCategoryName()
    {
        // arrange
        var name = "Food";
        var category = new Category("food", name, ["food"]);

        // act
        var result = category.ToString();

        // assert
        Assert.Equal(name, result);
    }
}