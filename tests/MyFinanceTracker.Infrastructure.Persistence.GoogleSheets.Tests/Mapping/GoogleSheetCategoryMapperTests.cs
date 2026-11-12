using FluentAssertions;
using MyFinanceTracker.Domain.Entities;
using MyFinanceTracker.Infrastructure.Persistence.GoogleSheets.Mapping;
using MyFinanceTracker.Infrastructure.Persistence.GoogleSheets.Models;

namespace MyFinanceTracker.Infrastructure.Persistence.GoogleSheets.Tests.Mapping;

public class GoogleSheetCategoryMapperTests
{
    private readonly GoogleSheetCategoryMapper sut = new();

    [Fact]
    public void MapToRow_ActiveCategory_FormatsRowCorrectly()
    {
        // arrange
        var category = new Category("food_id", "Food", isArchived: false);

        // act
        var result = sut.MapToRow(category);

        // assert
        result.Values.Should().Equal("food_id", "Food", "FALSE");
    }

    [Fact]
    public void MapToRow_ArchivedCategory_FormatsRowCorrectly()
    {
        // arrange
        var category = new Category("travel_id", "Travel", isArchived: true);

        // act
        var result = sut.MapToRow(category);

        // assert
        result.Values.Should().Equal("travel_id", "Travel", "TRUE");
    }

    [Fact]
    public void MapToRows_ValidCategories_FormatsAllRowsCorrectly()
    {
        // arrange
        Category[] categories =
        [
            new("c1", "Cat1", false),
            new("c2", "Cat2", true)
        ];

        // act
        var results = sut.MapToRows(categories);

        // assert
        results.Should().HaveCount(2);
        results[0].Values.Should().Equal("c1", "Cat1", "FALSE");
        results[1].Values.Should().Equal("c2", "Cat2", "TRUE");
    }

    [Fact]
    public void MapToDomain_EmptyOrWhitespaceRow_ReturnsEmpty()
    {
        // arrange
        var emptyRow = new GoogleSheetRow(1, new GoogleSheetRowValues([]));
        var whitespaceRow = new GoogleSheetRow(2, new GoogleSheetRowValues([" ", "\t", ""]));

        // act
        var emptyResult = sut.MapToDomain([emptyRow]).Single();
        var whitespaceResult = sut.MapToDomain([whitespaceRow]).Single();

        // assert
        emptyResult.Should().BeOfType<CategoryRowMappingResult.Empty>();
        whitespaceResult.Should().BeOfType<CategoryRowMappingResult.Empty>();
    }

    [Fact]
    public void MapToDomain_MissingId_ReturnsFailure()
    {
        // arrange
        var row = new GoogleSheetRow(2, new GoogleSheetRowValues(["", "Food", "FALSE"]));

        // act
        var result = sut.MapToDomain([row]).Single();

        // assert
        var failure = result.Should().BeOfType<CategoryRowMappingResult.Failure>().Subject;
        failure.RowNumber.Should().Be(3);
        failure.Reason.Should().Contain("Id");
    }

    [Fact]
    public void MapToDomain_MissingName_ReturnsFailure()
    {
        // arrange
        var row = new GoogleSheetRow(4, new GoogleSheetRowValues(["food_id", "   ", "FALSE"]));

        // act
        var result = sut.MapToDomain([row]).Single();

        // assert
        var failure = result.Should().BeOfType<CategoryRowMappingResult.Failure>().Subject;
        failure.RowNumber.Should().Be(5);
        failure.Reason.Should().Contain("Name");
    }

    [Fact]
    public void MapToDomain_MissingIsArchivedColumn_DefaultsToFalse()
    {
        // arrange
        var row = new GoogleSheetRow(1, new GoogleSheetRowValues(["food_id", "Food"]));

        // act
        var result = sut.MapToDomain([row]).Single();

        // assert
        var success = result.Should().BeOfType<CategoryRowMappingResult.Success>().Subject;
        success.Category.Id.Should().Be("food_id");
        success.Category.Name.Should().Be("Food");
        success.Category.IsArchived.Should().BeFalse();
    }

    [Fact]
    public void MapToDomain_EmptyIsArchivedCell_DefaultsToFalse()
    {
        // arrange
        var row = new GoogleSheetRow(1, new GoogleSheetRowValues(["food_id", "Food", "  "]));

        // act
        var result = sut.MapToDomain([row]).Single();

        // assert
        var success = result.Should().BeOfType<CategoryRowMappingResult.Success>().Subject;
        success.Category.IsArchived.Should().BeFalse();
    }

    [Theory]
    [InlineData("TRUE", true)]
    [InlineData("true", true)]
    [InlineData("True", true)]
    [InlineData("FALSE", false)]
    [InlineData("false", false)]
    [InlineData("False", false)]
    public void MapToDomain_ValidIsArchived_ParsesBooleanCorrectly(string isArchivedString, bool expected)
    {
        // arrange
        var row = new GoogleSheetRow(1, new GoogleSheetRowValues(["food_id", "Food", isArchivedString]));

        // act
        var result = sut.MapToDomain([row]).Single();

        // assert
        var success = result.Should().BeOfType<CategoryRowMappingResult.Success>().Subject;
        success.Category.IsArchived.Should().Be(expected);
    }

    [Fact]
    public void MapToDomain_InvalidIsArchived_ReturnsFailure()
    {
        // arrange
        var row = new GoogleSheetRow(6, new GoogleSheetRowValues(["food_id", "Food", "not_a_boolean"]));

        // act
        var result = sut.MapToDomain([row]).Single();

        // assert
        var failure = result.Should().BeOfType<CategoryRowMappingResult.Failure>().Subject;
        failure.RowNumber.Should().Be(7);
        failure.Reason.Should().Contain("IsArchived");
    }

    [Fact]
    public void MapToDomain_ValidRows_MapsAllCorrectly()
    {
        // arrange
        GoogleSheetRow[] rows =
        [
            new(1, new GoogleSheetRowValues(["c1", "Cat 1", "TRUE"])),
            new(2, new GoogleSheetRowValues([])),
            new(3, new GoogleSheetRowValues(["c2", "Cat 2", "FALSE"]))
        ];

        // act
        var results = sut.MapToDomain(rows);

        // assert
        results.Should().HaveCount(3);
        results[0].Should().BeOfType<CategoryRowMappingResult.Success>();
        results[1].Should().BeOfType<CategoryRowMappingResult.Empty>();
        results[2].Should().BeOfType<CategoryRowMappingResult.Success>();
    }

    [Fact]
    public void MapToRow_IdWithLeadingZeros_PreservesStringFormat()
    {
        // arrange
        var category = new Category("001", "Special", isArchived: false);

        // act
        var result = sut.MapToRow(category);

        // assert
        result.Values[0].Should().Be("001");
    }

    [Fact]
    public void MapToDomain_IdWithLeadingZeros_PreservesStringFormat()
    {
        // arrange
        var row = new GoogleSheetRow(1, new GoogleSheetRowValues(["001", "Special", "FALSE"]));

        // act
        var result = sut.MapToDomain([row]).Single();

        // assert
        var success = result.Should().BeOfType<CategoryRowMappingResult.Success>().Subject;
        success.Category.Id.Should().Be("001");
    }
}
