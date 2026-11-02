using FluentAssertions;
using Microsoft.Extensions.Options;
using MyFinanceTracker.Domain.Entities;
using MyFinanceTracker.Infrastructure.Persistence.GoogleSheets.Configuration;
using MyFinanceTracker.Infrastructure.Persistence.GoogleSheets.Mapping;
using MyFinanceTracker.Infrastructure.Persistence.GoogleSheets.Models;

namespace MyFinanceTracker.Infrastructure.Persistence.GoogleSheets.Tests.Mapping;

public class GoogleSheetMapperTests
{
    private readonly GoogleSheetsOptions defaultOptions = new()
    {
        SpreadsheetId = "test-id",
        TransactionsSheetName = "Transactions",
        DecimalSeparator = "."
    };

    private readonly Category foodCategory = new("food_id", "Food", ["food"]);
    private readonly Category salaryCategory = new("salary_id", "Salary", ["salary"], isIncome: true);

    [Fact]
    public void MapToRow_ShouldFormatExpenseCorrectly()
    {
        // arrange
        var sut = CreateMapper(".");
        var id = Guid.NewGuid();
        var transaction = new Transaction(
            id,
            TransactionType.Expense,
            foodCategory,
            123.45m,
            new DateOnly(2026, 11, 2),
            "Dinner"
        );

        // act
        var result = sut.MapToRow(transaction);

        // assert
        result.Values.Should().Equal(
            id.ToString(),
            "2026-11-02",
            "Expense",
            "Food",
            "-123.45",
            "Dinner"
        );
    }

    [Fact]
    public void MapToRow_ShouldFormatIncomeAndRespectDecimalSeparator()
    {
        // arrange
        var sut = CreateMapper(",");
        var id = Guid.NewGuid();
        var transaction = new Transaction(
            id,
            TransactionType.Income,
            salaryCategory,
            5000m,
            new DateOnly(2026, 11, 1),
            null
        );

        // act
        var result = sut.MapToRow(transaction);

        // assert
        result.Values.Should().Equal(
            id.ToString(),
            "2026-11-01",
            "Income",
            "Salary",
            "5000",
            string.Empty
        );
    }

    [Fact]
    public void MapToDomain_ShouldReturnEmpty_WhenRowHasNoValuesOrOnlyWhitespace()
    {
        // arrange
        var sut = CreateMapper(".");
        var emptyRow = new GoogleSheetRow(1, new GoogleSheetRowValues([]));
        var whitespaceRow = new GoogleSheetRow(2, new GoogleSheetRowValues([" ", "\t"]));

        // act
        var emptyResult = sut.MapToDomain(emptyRow, [foodCategory]);
        var whitespaceResult = sut.MapToDomain(whitespaceRow, [foodCategory]);

        // assert
        emptyResult.Should().BeOfType<RowMappingResult.Empty>();
        whitespaceResult.Should().BeOfType<RowMappingResult.Empty>();
    }

    [Theory]
    [InlineData("2026-11-02")]
    [InlineData("02.11.2026")]
    [InlineData("2.11.2026")]
    [InlineData("02/11/2026")]
    [InlineData("2026.11.02")]
    public void MapToDomain_ShouldSupportVariousDateFormats(string dateString)
    {
        // arrange
        var sut = CreateMapper(".");
        var id = Guid.NewGuid();
        var row = new GoogleSheetRow(1, new GoogleSheetRowValues([
            id.ToString(),
            dateString,
            "Expense",
            "Food",
            "-50.25",
            "Lunch"
        ]));

        // act
        var result = sut.MapToDomain(row, [foodCategory]);

        // assert
        result.Should().BeOfType<RowMappingResult.Success>();
        var success = (RowMappingResult.Success)result;
        success.Transaction.Date.Should().Be(new DateOnly(2026, 11, 2));
        success.Transaction.Amount.Should().Be(50.25m);
        success.Transaction.Category.Should().Be(foodCategory);
        success.Transaction.Note.Should().Be("Lunch");
    }

    [Fact]
    public void MapToDomain_ShouldReturnSuccess_WithCaseInsensitiveCategoryMatch()
    {
        // arrange
        var sut = CreateMapper(".");
        var id = Guid.NewGuid();
        var row = new GoogleSheetRow(3, new GoogleSheetRowValues([
            id.ToString(),
            "2026-11-02",
            "Income",
            "salary", // lower case
            "2500",
            ""
        ]));

        // act
        var result = sut.MapToDomain(row, [salaryCategory]);

        // assert
        result.Should().BeOfType<RowMappingResult.Success>();
        var success = (RowMappingResult.Success)result;
        success.Transaction.Category.Name.Should().Be("Salary");
        success.Transaction.Note.Should().BeNull();
    }

    [Fact]
    public void MapToDomain_ShouldReturnFailure_WhenIdIsMissingOrInvalid()
    {
        // arrange
        var sut = CreateMapper(".");
        var missingIdRow = new GoogleSheetRow(0, new GoogleSheetRowValues(["", "2026-11-02", "Expense", "Food", "-10"]));
        var invalidIdRow = new GoogleSheetRow(5, new GoogleSheetRowValues(["not-a-guid", "2026-11-02", "Expense", "Food", "-10"]));

        // act
        var missingResult = sut.MapToDomain(missingIdRow, [foodCategory]);
        var invalidResult = sut.MapToDomain(invalidIdRow, [foodCategory]);

        // assert
        missingResult.Should().BeOfType<RowMappingResult.Failure>()
            .Which.RowNumber.Should().Be(1);
        invalidResult.Should().BeOfType<RowMappingResult.Failure>()
            .Which.RowNumber.Should().Be(6);
    }

    [Fact]
    public void MapToDomain_ShouldReturnFailure_WhenDateIsInvalid()
    {
        // arrange
        var sut = CreateMapper(".");
        var id = Guid.NewGuid();
        var row = new GoogleSheetRow(2, new GoogleSheetRowValues([id.ToString(), "invalid-date", "Expense", "Food", "-10"]));

        // act
        var result = sut.MapToDomain(row, [foodCategory]);

        // assert
        result.Should().BeOfType<RowMappingResult.Failure>()
            .Which.Reason.Should().Contain("Invalid Date");
    }

    [Fact]
    public void MapToDomain_ShouldReturnFailure_WhenTypeIsInvalid()
    {
        // arrange
        var sut = CreateMapper(".");
        var id = Guid.NewGuid();
        var row = new GoogleSheetRow(2, new GoogleSheetRowValues([id.ToString(), "2026-11-02", "UnknownType", "Food", "-10"]));

        // act
        var result = sut.MapToDomain(row, [foodCategory]);

        // assert
        result.Should().BeOfType<RowMappingResult.Failure>()
            .Which.Reason.Should().Contain("Invalid Transaction Type");
    }

    [Fact]
    public void MapToDomain_ShouldReturnFailure_WhenCategoryIsUnknown()
    {
        // arrange
        var sut = CreateMapper(".");
        var id = Guid.NewGuid();
        var row = new GoogleSheetRow(4, new GoogleSheetRowValues([id.ToString(), "2026-11-02", "Expense", "NonExistent", "-10"]));

        // act
        var result = sut.MapToDomain(row, [foodCategory]);

        // assert
        result.Should().BeOfType<RowMappingResult.Failure>()
            .Which.Reason.Should().Contain("Unknown Category: 'NonExistent'");
    }

    [Theory]
    [InlineData("not-a-number")]
    [InlineData("0")]
    [InlineData("0.00")]
    [InlineData("-1100,25")]
    public void MapToDomain_ShouldReturnFailure_WhenAmountIsInvalidOrZero(string amountStr)
    {
        // arrange
        var sut = CreateMapper(".");
        var id = Guid.NewGuid();
        var row = new GoogleSheetRow(1, new GoogleSheetRowValues([id.ToString(), "2026-11-02", "Expense", "Food", amountStr]));

        // act
        var result = sut.MapToDomain(row, [foodCategory]);

        // assert
        result.Should().BeOfType<RowMappingResult.Failure>()
            .Which.Reason.Should().Contain("Invalid Amount");
    }

    [Fact]
    public void MapToDomain_ShouldRespectCommaDecimalSeparator()
    {
        // arrange
        var sut = CreateMapper(",");
        var id = Guid.NewGuid();
        var validRow = new GoogleSheetRow(1, new GoogleSheetRowValues([
            id.ToString(),
            "2026-11-02",
            "Expense",
            "Food",
            "-1100,25"
        ]));
        var invalidRow = new GoogleSheetRow(2, new GoogleSheetRowValues([
            id.ToString(),
            "2026-11-02",
            "Expense",
            "Food",
            "-1100.25"
        ]));

        // act
        var validResult = sut.MapToDomain(validRow, [foodCategory]);
        var invalidResult = sut.MapToDomain(invalidRow, [foodCategory]);

        // assert
        validResult.Should().BeOfType<RowMappingResult.Success>()
            .Which.Transaction.Amount.Should().Be(1100.25m);

        invalidResult.Should().BeOfType<RowMappingResult.Failure>()
            .Which.Reason.Should().Contain("Invalid Amount");
    }

    private GoogleSheetMapper CreateMapper(string decimalSeparator)
    {
        return new GoogleSheetMapper(Options.Create(new GoogleSheetsOptions
        {
            SpreadsheetId = "test-id",
            TransactionsSheetName = "Transactions",
            DecimalSeparator = decimalSeparator
        }));
    }
}
