using FluentAssertions;
using MyFinanceTracker.Infrastructure.Persistence.GoogleSheets.Mapping;
using MyFinanceTracker.Infrastructure.Persistence.GoogleSheets.Models;
using MyFinanceTracker.Infrastructure.Persistence.Models;

namespace MyFinanceTracker.Infrastructure.Persistence.Tests.GoogleSheets.Mapping;

public class GoogleSheetCategoryMapperTests
{
    private readonly GoogleSheetCategoryMapper sut = new();

    [Fact]
    public void MapToRow_ActiveCategory_FormatsRowCorrectly()
    {
        // arrange
        var category = new CategorySyncRecord("food_id", "Food", IsArchived: false);

        // act
        var result = sut.MapToRow(category);

        // assert
        result.Values.Should().Equal("food_id", "Food", "FALSE");
    }

    [Fact]
    public void MapToRow_ArchivedCategory_FormatsRowCorrectly()
    {
        // arrange
        var category = new CategorySyncRecord("travel_id", "Travel", IsArchived: true);

        // act
        var result = sut.MapToRow(category);

        // assert
        result.Values.Should().Equal("travel_id", "Travel", "TRUE");
    }

    [Fact]
    public void MapFromRows_EmptyOrWhitespaceRow_ReturnsEmpty()
    {
        // arrange
        var emptyRow = new GoogleSheetRow(1, new GoogleSheetRowValues([]));
        var whitespaceRow = new GoogleSheetRow(2, new GoogleSheetRowValues([" ", "\t", ""]));

        // act
        var emptyResult = sut.MapFromRows([emptyRow]).Single();
        var whitespaceResult = sut.MapFromRows([whitespaceRow]).Single();

        // assert
        emptyResult.Should().BeOfType<CategoryRowMappingResult.Empty>();
        whitespaceResult.Should().BeOfType<CategoryRowMappingResult.Empty>();
    }

    [Fact]
    public void MapFromRows_MissingId_ReturnsFailure()
    {
        // arrange
        var row = new GoogleSheetRow(2, new GoogleSheetRowValues(["", "Food", "FALSE"]));

        // act
        var result = sut.MapFromRows([row]).Single();

        // assert
        var failure = result.Should().BeOfType<CategoryRowMappingResult.Failure>().Subject;
        failure.RowNumber.Should().Be(3);
        failure.Reason.Should().Contain("Id");
    }

    [Fact]
    public void MapFromRows_MissingName_ReturnsFailure()
    {
        // arrange
        var row = new GoogleSheetRow(4, new GoogleSheetRowValues(["food_id", "   ", "FALSE"]));

        // act
        var result = sut.MapFromRows([row]).Single();

        // assert
        var failure = result.Should().BeOfType<CategoryRowMappingResult.Failure>().Subject;
        failure.RowNumber.Should().Be(5);
        failure.Reason.Should().Contain("Name");
    }

    [Fact]
    public void MapFromRows_MissingIsArchivedColumn_DefaultsToFalse()
    {
        // arrange
        var row = new GoogleSheetRow(1, new GoogleSheetRowValues(["food_id", "Food"]));

        // act
        var result = sut.MapFromRows([row]).Single();

        // assert
        var success = result.Should().BeOfType<CategoryRowMappingResult.Success>().Subject;
        success.Category.Id.Should().Be("food_id");
        success.Category.Name.Should().Be("Food");
        success.Category.IsArchived.Should().BeFalse();
    }

    [Fact]
    public void MapFromRows_EmptyIsArchivedCell_DefaultsToFalse()
    {
        // arrange
        var row = new GoogleSheetRow(1, new GoogleSheetRowValues(["food_id", "Food", "  "]));

        // act
        var result = sut.MapFromRows([row]).Single();

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
    public void MapFromRows_ValidIsArchived_ParsesBooleanCorrectly(string isArchivedString, bool expected)
    {
        // arrange
        var row = new GoogleSheetRow(1, new GoogleSheetRowValues(["food_id", "Food", isArchivedString]));

        // act
        var result = sut.MapFromRows([row]).Single();

        // assert
        var success = result.Should().BeOfType<CategoryRowMappingResult.Success>().Subject;
        success.Category.IsArchived.Should().Be(expected);
    }

    [Fact]
    public void MapFromRows_InvalidIsArchived_ReturnsFailure()
    {
        // arrange
        var row = new GoogleSheetRow(6, new GoogleSheetRowValues(["food_id", "Food", "not_a_boolean"]));

        // act
        var result = sut.MapFromRows([row]).Single();

        // assert
        var failure = result.Should().BeOfType<CategoryRowMappingResult.Failure>().Subject;
        failure.RowNumber.Should().Be(7);
        failure.Reason.Should().Contain("IsArchived");
    }

    [Fact]
    public void MapFromRows_ValidRows_MapsAllCorrectly()
    {
        // arrange
        GoogleSheetRow[] rows =
        [
            new(1, new GoogleSheetRowValues(["c1", "Cat 1", "TRUE"])),
            new(2, new GoogleSheetRowValues([])),
            new(3, new GoogleSheetRowValues(["c2", "Cat 2", "FALSE"]))
        ];

        // act
        var results = sut.MapFromRows(rows);

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
        var category = new CategorySyncRecord("001", "Special", IsArchived: false);

        // act
        var result = sut.MapToRow(category);

        // assert
        result.Values[0].Should().Be("001");
    }

    [Fact]
    public void MapFromRows_IdWithLeadingZeros_PreservesStringFormat()
    {
        // arrange
        var row = new GoogleSheetRow(1, new GoogleSheetRowValues(["001", "Special", "FALSE"]));

        // act
        var result = sut.MapFromRows([row]).Single();

        // assert
        var success = result.Should().BeOfType<CategoryRowMappingResult.Success>().Subject;
        success.Category.Id.Should().Be("001");
    }
}
