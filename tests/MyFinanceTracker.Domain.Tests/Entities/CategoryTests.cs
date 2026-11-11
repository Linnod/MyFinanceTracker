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

        // act
        var category = new Category(id, name);

        // assert
        Assert.Equal(id, category.Id);
        Assert.Equal(name, category.Name);
        Assert.False(category.IsArchived);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    void Constructor_WithArchivedFlag_InitializesIsArchivedCorrectly(bool isArchived)
    {
        // arrange & act
        var category = new Category("salary", "Salary", isArchived);

        // assert
        Assert.Equal(isArchived, category.IsArchived);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    void Constructor_WithInvalidId_ThrowsDomainException(string? invalidId)
    {
        // arrange
        var name = "Food";

        // act
        Category act() => new(invalidId!, name);

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

        // act
        Category act() => new(id, invalidName!);

        // assert
        var exception = Assert.Throws<DomainException>(act);
        Assert.Equal("Category Name cannot be empty or whitespace.", exception.Message);
    }

    [Fact]
    public void ToString_ReturnsCategoryName()
    {
        // arrange
        var name = "Food";
        var category = new Category("food", name);

        // act
        var result = category.ToString();

        // assert
        Assert.Equal(name, result);
    }
}
