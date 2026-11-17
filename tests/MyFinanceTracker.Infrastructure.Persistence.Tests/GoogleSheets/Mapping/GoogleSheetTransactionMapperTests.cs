using FluentAssertions;
using MyFinanceTracker.Domain.Entities;
using MyFinanceTracker.Infrastructure.Persistence.GoogleSheets.Mapping;
using MyFinanceTracker.Infrastructure.Persistence.GoogleSheets.Models;
using MyFinanceTracker.Infrastructure.Persistence.Models;

namespace MyFinanceTracker.Infrastructure.Persistence.Tests.GoogleSheets.Mapping;

public class GoogleSheetTransactionMapperTests
{
    [Fact]
    public void MapToRow_ShouldFormatExpenseCorrectly()
    {
        // arrange
        var sut = new GoogleSheetTransactionMapper();
        var id = Guid.NewGuid();
        var transaction = new TransactionSyncRecord(
            id,
            TransactionType.Expense,
            "food_id",
            123.45m,
            new DateTime(2026, 11, 2, 18, 30, 45),
            "Dinner"
        );

        // act
        var result = sut.MapToRow(transaction);

        // assert
        result.Values.Should().Equal(
            id.ToString(),
            "2026-11-02 18:30:45",
            "Expense",
            "food_id",
            "-123.45",
            "Dinner"
        );
    }

    [Fact]
    public void MapToRow_ShouldFormatIncomeCorrectly()
    {
        // arrange
        var sut = new GoogleSheetTransactionMapper();
        var id = Guid.NewGuid();
        var transaction = new TransactionSyncRecord(
            id,
            TransactionType.Income,
            "salary_id",
            5000m,
            new DateTime(2026, 11, 1, 9, 0, 0),
            null
        );

        // act
        var result = sut.MapToRow(transaction);

        // assert
        result.Values.Should().Equal(
            id.ToString(),
            "2026-11-01 09:00:00",
            "Income",
            "salary_id",
            "5000",
            string.Empty
        );
    }

    [Fact]
    public void MapFromRows_ShouldReturnEmpty_WhenRowHasNoValuesOrOnlyWhitespace()
    {
        // arrange
        var sut = new GoogleSheetTransactionMapper();
        var emptyRow = new GoogleSheetRow(1, new GoogleSheetRowValues([]));
        var whitespaceRow = new GoogleSheetRow(2, new GoogleSheetRowValues([" ", "\t"]));

        // act
        var emptyResult = sut.MapFromRows([emptyRow]).Single();
        var whitespaceResult = sut.MapFromRows([whitespaceRow]).Single();

        // assert
        emptyResult.Should().BeOfType<TransactionRowMappingResult.Empty>();
        whitespaceResult.Should().BeOfType<TransactionRowMappingResult.Empty>();
    }

    [Theory]
    [InlineData("2026-11-02 14:30:45", 2026, 11, 2, 14, 30, 45)]
    [InlineData("2026-11-09 00:05:12", 2026, 11, 9, 0, 5, 12)]
    public void MapFromRows_ShouldParseExpectedDateFormat(
        string dateString,
        int year,
        int month,
        int day,
        int hour,
        int minute,
        int second)
    {
        // arrange
        var sut = new GoogleSheetTransactionMapper();
        var id = Guid.NewGuid();
        var row = new GoogleSheetRow(1, new GoogleSheetRowValues([
            id.ToString(),
            dateString,
            "Expense",
            "food_id",
            "-50.25",
            "Lunch"
        ]));

        // act
        var result = sut.MapFromRows([row]).Single();

        // assert
        result.Should().BeOfType<TransactionRowMappingResult.Success>();
        var success = (TransactionRowMappingResult.Success)result;
        success.Transaction.Date.Should().Be(new DateTime(year, month, day, hour, minute, second));
        success.Transaction.Amount.Should().Be(50.25m);
        success.Transaction.CategoryId.Should().Be("food_id");
        success.Transaction.Note.Should().Be("Lunch");
    }

    [Fact]
    public void MapFromRows_ShouldReturnFailure_WhenIdIsMissingInvalidOrEmptyGuid()
    {
        // arrange
        var sut = new GoogleSheetTransactionMapper();
        var missingIdRow = new GoogleSheetRow(0, new GoogleSheetRowValues(["", "2026-11-02 12:00:00", "Expense", "food_id", "-10"]));
        var invalidIdRow = new GoogleSheetRow(5, new GoogleSheetRowValues(["not-a-guid", "2026-11-02 12:00:00", "Expense", "food_id", "-10"]));
        var emptyGuidRow = new GoogleSheetRow(6, new GoogleSheetRowValues([Guid.Empty.ToString(), "2026-11-02 12:00:00", "Expense", "food_id", "-10"]));

        // act
        var missingResult = sut.MapFromRows([missingIdRow]).Single();
        var invalidResult = sut.MapFromRows([invalidIdRow]).Single();
        var emptyGuidResult = sut.MapFromRows([emptyGuidRow]).Single();

        // assert
        missingResult.Should().BeOfType<TransactionRowMappingResult.Failure>()
            .Which.RowNumber.Should().Be(1);
        invalidResult.Should().BeOfType<TransactionRowMappingResult.Failure>()
            .Which.RowNumber.Should().Be(6);
        emptyGuidResult.Should().BeOfType<TransactionRowMappingResult.Failure>()
            .Which.RowNumber.Should().Be(7);
    }

    [Fact]
    public void MapFromRows_ShouldReturnFailure_WhenDateIsInvalid()
    {
        // arrange
        var sut = new GoogleSheetTransactionMapper();
        var id = Guid.NewGuid();
        var row = new GoogleSheetRow(2, new GoogleSheetRowValues([id.ToString(), "invalid-date", "Expense", "food_id", "-10"]));

        // act
        var result = sut.MapFromRows([row]).Single();

        // assert
        result.Should().BeOfType<TransactionRowMappingResult.Failure>()
            .Which.Reason.Should().Contain("Invalid Date");
    }

    [Theory]
    [InlineData("1899-12-31 23:59:59")]
    [InlineData("2101-01-01 00:00:00")]
    public void MapFromRows_ShouldReturnFailure_WhenDateYearIsOutOfAllowedRange(string dateString)
    {
        // arrange
        var sut = new GoogleSheetTransactionMapper();
        var id = Guid.NewGuid();
        var row = new GoogleSheetRow(2, new GoogleSheetRowValues([id.ToString(), dateString, "Expense", "food_id", "-10"]));

        // act
        var result = sut.MapFromRows([row]).Single();

        // assert
        result.Should().BeOfType<TransactionRowMappingResult.Failure>()
            .Which.Reason.Should().Contain($"{FinancialRules.MinAllowedYear}").And.Contain($"{FinancialRules.MaxAllowedYear}");
    }

    [Theory]
    [InlineData("UnknownType")]
    [InlineData("5")]
    [InlineData("-1")]
    [InlineData("0")]
    [InlineData("1")]
    [InlineData("Income, Expense")]
    public void MapFromRows_ShouldReturnFailure_WhenTypeIsInvalid(string typeString)
    {
        // arrange
        var sut = new GoogleSheetTransactionMapper();
        var id = Guid.NewGuid();
        var row = new GoogleSheetRow(2, new GoogleSheetRowValues([id.ToString(), "2026-11-02 12:00:00", typeString, "food_id", "-10"]));

        // act
        var result = sut.MapFromRows([row]).Single();

        // assert
        result.Should().BeOfType<TransactionRowMappingResult.Failure>()
            .Which.Reason.Should().Be($"Invalid Transaction Type: '{typeString}'. Expected 'Income' or 'Expense'.");
    }

    [Theory]
    [InlineData("not-a-number")]
    [InlineData("0")]
    [InlineData("0.00")]
    [InlineData("-1100,25")]
    [InlineData("-10.123")]
    [InlineData("10.125")]
    public void MapFromRows_ShouldReturnFailure_WhenAmountIsInvalid(string amountStr)
    {
        // arrange
        var sut = new GoogleSheetTransactionMapper();
        var id = Guid.NewGuid();
        var row = new GoogleSheetRow(1, new GoogleSheetRowValues([id.ToString(), "2026-11-02 12:00:00", "Expense", "food_id", amountStr]));

        // act
        var result = sut.MapFromRows([row]).Single();

        // assert
        result.Should().BeOfType<TransactionRowMappingResult.Failure>()
            .Which.Reason.Should().Contain("Invalid Amount");
    }
}
