using FluentAssertions;
using MyFinanceTracker.Domain.Entities;
using MyFinanceTracker.Infrastructure.Persistence.GoogleSheets.Configuration;
using MyFinanceTracker.Infrastructure.Persistence.GoogleSheets.Mapping;

namespace MyFinanceTracker.Infrastructure.Persistence.GoogleSheets.Tests.Mapping;

public class GoogleSheetMapperTests
{
    private readonly GoogleSheetsOptions defaultOptions = new()
    {
        SpreadsheetId = "test-id",
        HeaderRowsCount = 2,
        DecimalSeparator = "."
    };

    [Fact]
    void Map_ShouldGroupMultipleTransactionsIntoOneUpdate()
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
        result[0].Address.Column.Should().Be("A");
        result[0].Address.Row.Should().Be(26);
        result[0].Address.SheetName.Should().Be("2026.01");
        result[0].Content.Should().Be("-100.5-50.25");
    }

    [Theory]
    [InlineData(TransactionType.Income, 100, "+100")]
    [InlineData(TransactionType.Expense, 100, "-100")]
    [InlineData(TransactionType.Expense, 10.55, "-10.55")]
    void Map_ShouldApplyCorrectSignsAndFormat(TransactionType type, decimal amount, string expectedDelta)
    {
        // arrange
        var options = Microsoft.Extensions.Options.Options.Create(defaultOptions);
        var sut = new GoogleSheetMapper(options);
        var category = new Category("B", "Test", ["test"], isIncome: type == TransactionType.Income);
        var transaction = new Transaction(Guid.NewGuid(), type, category, amount, new DateOnly(2026, 1, 1), "");

        // act
        var result = sut.MapForAddition([transaction]);

        // assert
        result[0].Content.Should().Be(expectedDelta);
    }

    [Fact]
    void Map_ShouldRespectDecimalSeparatorFromOptions()
    {
        // arrange
        var optionsWithComma = Microsoft.Extensions.Options.Options.Create(new GoogleSheetsOptions
        {
            HeaderRowsCount = 0,
            DecimalSeparator = ","
        });
        var sut = new GoogleSheetMapper(optionsWithComma);
        var category = new Category("C", "Food", ["food"]);
        var transaction = new Transaction(Guid.NewGuid(), TransactionType.Expense, category, 10.5m, new DateOnly(2026, 1, 1), "");

        // act
        var result = sut.MapForAddition([transaction]);

        // assert
        result[0].Content.Should().Contain("-10,5");
    }

    [Theory]
    [InlineData(1, 2, "A", 3)]
    [InlineData(31, 0, "A", 31)]
    [InlineData(15, 5, "A", 20)]
    void Map_ShouldCalculateCellAddressCorrectly(int day, int headerRows, string expectedCellColumn, int expectedCellRow)
    {
        // arrange
        var customOptions = Microsoft.Extensions.Options.Options.Create(new GoogleSheetsOptions
        {
            HeaderRowsCount = headerRows,
            DecimalSeparator = "."
        });
        var sut = new GoogleSheetMapper(customOptions);
        var category = new Category("A", "Food", ["food"]);
        var transaction = new Transaction(Guid.NewGuid(), TransactionType.Expense, category, 10, new DateOnly(2026, 1, day), "");

        // act
        var result = sut.MapForAddition([transaction]);

        // assert
        result[0].Address.Column.Should().Be(expectedCellColumn);
        result[0].Address.Row.Should().Be(expectedCellRow);
    }

    [Fact]
    void MapForClearance_ShouldReturnZeroAndCorrectAddress()
    {
        // arrange
        var options = Microsoft.Extensions.Options.Options.Create(defaultOptions);
        var sut = new GoogleSheetMapper(options);
        var date = new DateOnly(2026, 01, 27);
        var category = new Category("B", "Food", ["food"]);

        // act
        var result = sut.MapForClearance(category, date);

        // assert
        result.Address.SheetName.Should().Be("2026.01");
        result.Address.Column.Should().Be("B");
        result.Address.Row.Should().Be(29);
        result.Content.Should().Be("0");
    }
}