using FluentAssertions;
using MyFinanceTracker.Domain.Entities;
using MyFinanceTracker.Infrastructure.GoogleSheets.Configuration;
using MyFinanceTracker.Infrastructure.GoogleSheets.Mapping;

namespace MyFinanceTracker.Infrastructure.GoogleSheets.Tests.Mapping;

public class GoogleSheetMapperTests
{
    private readonly GoogleSheetsOptions defaultOptions = new()
    {
        SpreadsheetId = "test-id",
        HeaderRowsCount = 2,
        DecimalSeparator = "."
    };

    [Fact]
    public void Map_ShouldGroupMultipleTransactionsIntoOneUpdate()
    {
        // arrange
        var options = Microsoft.Extensions.Options.Options.Create(defaultOptions);
        var sut = new GoogleSheetMapper(options);
        var date = new DateOnly(2026, 01, 24);
        var category = new Category("A", "Food", ["food"]);

        var transactions = new List<Transaction>
        {
            new(Guid.NewGuid(), TransactionType.Expense, category, 100.50m, date, "Lunch"),
            new(Guid.NewGuid(), TransactionType.Expense, category, 50.25m, date, "Coffee")
        };

        // act
        var result = sut.MapForAddition(transactions);

        // assert
        result.Should().HaveCount(1);
        result[0].CellAddress.Should().Be("A26");
        result[0].Content.Should().Be("-100.5-50.25");
        result[0].SheetName.Should().Be("2026.01");
    }

    [Theory]
    [InlineData(true, 100, "+100")]
    [InlineData(false, 100, "-100")]
    [InlineData(false, 10.55, "-10.55")]
    public void Map_ShouldApplyCorrectSignsAndFormat(bool isIncome, decimal amount, string expectedDelta)
    {
        // arrange
        var options = Microsoft.Extensions.Options.Options.Create(defaultOptions);
        var sut = new GoogleSheetMapper(options);
        var category = new Category("B", "Test", [], isIncome);
        var transaction = new Transaction(Guid.NewGuid(), TransactionType.Expense, category, amount, new DateOnly(2026, 1, 1), "");

        // act
        var result = sut.MapForAddition([transaction]);

        // assert
        result[0].Content.Should().Be(expectedDelta);
    }

    [Fact]
    public void Map_ShouldRespectDecimalSeparatorFromOptions()
    {
        // arrange
        var optionsWithComma = Microsoft.Extensions.Options.Options.Create(new GoogleSheetsOptions
        {
            HeaderRowsCount = 0,
            DecimalSeparator = ","
        });
        var sut = new GoogleSheetMapper(optionsWithComma);
        var category = new Category("C", "Food", []);
        var transaction = new Transaction(Guid.NewGuid(), TransactionType.Expense, category, 10.5m, new DateOnly(2026, 1, 1), "");

        // act
        var result = sut.MapForAddition([transaction]);

        // assert
        result[0].Content.Should().Contain("-10,5");
    }

    [Theory]
    [InlineData(1, 2, "A3")]
    [InlineData(31, 0, "A31")]
    [InlineData(15, 5, "A20")]
    public void Map_ShouldCalculateCellAddressCorrectly(int day, int headerRows, string expectedCell)
    {
        // arrange
        var customOptions = Microsoft.Extensions.Options.Options.Create(new GoogleSheetsOptions
        {
            HeaderRowsCount = headerRows,
            DecimalSeparator = "."
        });
        var sut = new GoogleSheetMapper(customOptions);
        var category = new Category("A", "Food", []);
        var transaction = new Transaction(Guid.NewGuid(), TransactionType.Expense, category, 10, new DateOnly(2026, 1, day), "");

        // act
        var result = sut.MapForAddition([transaction]);

        // assert
        result[0].CellAddress.Should().Be(expectedCell);
    }

    [Fact]
    public void MapForClearance_ShouldReturnZeroAndCorrectAddress()
    {
        // arrange
        var options = Microsoft.Extensions.Options.Options.Create(defaultOptions);
        var sut = new GoogleSheetMapper(options);
        var date = new DateOnly(2026, 01, 27);
        var categoryId = "B";

        // act
        var result = sut.MapForClearance(categoryId, date);

        // assert
        result.SheetName.Should().Be("2026.01");
        result.CellAddress.Should().Be("B29");
        result.Content.Should().Be("0");
    }
}