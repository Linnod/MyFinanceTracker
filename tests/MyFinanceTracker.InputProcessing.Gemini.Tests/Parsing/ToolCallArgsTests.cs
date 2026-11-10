using System.Text.Json;
using FluentAssertions;
using MyFinanceTracker.Domain.Entities;
using MyFinanceTracker.InputProcessing.Gemini.Parsing;
using Xunit;

namespace MyFinanceTracker.InputProcessing.Gemini.Tests.Parsing;

public class ToolCallArgsTests
{
    private record TestArgs(
        string? CategoryAlias,
        DateTime? Date,
        IReadOnlyList<decimal>? Amounts,
        TransactionType? Type);

    [Fact]
    void To_WhenArgsIsNull_ShouldReturnNull()
    {
        // arrange
        var sut = new ToolCallArgs(null);

        // act
        var result = sut.To<TestArgs>();

        // assert
        result.Should().BeNull();
    }

    [Fact]
    void To_WhenArgsIsEmpty_ShouldReturnNull()
    {
        // arrange
        var sut = new ToolCallArgs(new Dictionary<string, object>());

        // act
        var result = sut.To<TestArgs>();

        // assert
        result.Should().BeNull();
    }

    [Fact]
    public void To_WhenJsonElementsProvided_ShouldBindToDtoCorrectly()
    {
        // arrange
        using var jsonDoc = JsonDocument.Parse("""
        {
            "categoryAlias": "groceries",
            "date": "2026-08-08T14:30:00",
            "amounts": [100.5, 50],
            "type": "expense"
        }
        """);

        var args = new Dictionary<string, object>
        {
            ["categoryAlias"] = jsonDoc.RootElement.GetProperty("categoryAlias"),
            ["date"] = jsonDoc.RootElement.GetProperty("date"),
            ["amounts"] = jsonDoc.RootElement.GetProperty("amounts"),
            ["type"] = jsonDoc.RootElement.GetProperty("type")
        };
        var sut = new ToolCallArgs(args);

        // act
        var result = sut.To<TestArgs>();

        // assert
        result.Should().NotBeNull();
        result!.CategoryAlias.Should().Be("groceries");
        result.Date.Should().Be(new DateTime(2026, 8, 8, 14, 30, 0));
        result.Amounts.Should().Equal(100.5m, 50m);
        result.Type.Should().Be(TransactionType.Expense);
    }

    [Theory]
    [InlineData("2026-08-08T14:30:00", 2026, 8, 8, 14, 30, 0)]
    [InlineData("2026-08-08 14:30:00", 2026, 8, 8, 14, 30, 0)]
    [InlineData("2026-08-08T14:30", 2026, 8, 8, 14, 30, 0)]
    [InlineData("2026-08-08", 2026, 8, 8, 0, 0, 0)]
    void To_FlexibleDateTimeFormats_ShouldBindCorrectly(string dateStr, int year, int month, int day, int hour, int minute, int second)
    {
        // arrange
        using var jsonDoc = JsonDocument.Parse($$"""
        {
            "date": "{{dateStr}}"
        }
        """);

        var args = new Dictionary<string, object>
        {
            ["date"] = jsonDoc.RootElement.GetProperty("date")
        };
        var sut = new ToolCallArgs(args);

        // act
        var result = sut.To<TestArgs>();

        // assert
        result.Should().NotBeNull();
        result!.Date.Should().Be(new DateTime(year, month, day, hour, minute, second));
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-a-date")]
    [InlineData("08/11/2026")]
    void To_WhenInvalidDateFormatProvided_ShouldReturnNull(string invalidDate)
    {
        // arrange
        using var jsonDoc = JsonDocument.Parse($$"""
        {
            "date": "{{invalidDate}}"
        }
        """);

        var args = new Dictionary<string, object>
        {
            ["date"] = jsonDoc.RootElement.GetProperty("date")
        };
        var sut = new ToolCallArgs(args);

        // act
        var result = sut.To<TestArgs>();

        // assert
        result.Should().BeNull();
    }

    [Fact]
    void To_WhenNumbersArePassedAsStrings_ShouldBindNumbersCorrectly()
    {
        // arrange
        using var jsonDoc = JsonDocument.Parse("""
        {
            "amounts": ["100.5", "50"]
        }
        """);

        var args = new Dictionary<string, object>
        {
            ["amounts"] = jsonDoc.RootElement.GetProperty("amounts")
        };
        var sut = new ToolCallArgs(args);

        // act
        var result = sut.To<TestArgs>();

        // assert
        result.Should().NotBeNull();
        result!.Amounts.Should().Equal(100.5m, 50m);
    }

    [Fact]
    void To_WhenAddTransactionArgsHasMissingOrNullAmounts_ShouldDefaultAmountsToEmptyList()
    {
        // arrange
        using var jsonDoc = JsonDocument.Parse("""
        {
            "type": "expense",
            "amounts": null
        }
        """);

        var args = new Dictionary<string, object>
        {
            ["type"] = jsonDoc.RootElement.GetProperty("type"),
            ["amounts"] = jsonDoc.RootElement.GetProperty("amounts")
        };
        var sut = new ToolCallArgs(args);

        // act
        var result = sut.To<AddTransactionArgs>();

        // assert
        result.Should().NotBeNull();
        result!.Amounts.Should().NotBeNull();
        result.Amounts.Should().BeEmpty();
    }

    [Fact]
    void To_WhenGetTransactionsArgsHasLimit_ShouldBindLimitCorrectly()
    {
        // arrange
        using var jsonDoc = JsonDocument.Parse("""
        {
            "fromDate": "2026-08-01T00:00:00",
            "toDate": "2026-08-10T23:59:59",
            "limit": 5
        }
        """);

        var args = new Dictionary<string, object>
        {
            ["fromDate"] = jsonDoc.RootElement.GetProperty("fromDate"),
            ["toDate"] = jsonDoc.RootElement.GetProperty("toDate"),
            ["limit"] = jsonDoc.RootElement.GetProperty("limit")
        };
        var sut = new ToolCallArgs(args);

        // act
        var result = sut.To<GetTransactionsArgs>();

        // assert
        result.Should().NotBeNull();
        result!.Limit.Should().Be(5);
        result.FromDate.Should().Be(new DateTime(2026, 8, 1, 0, 0, 0));
        result.ToDate.Should().Be(new DateTime(2026, 8, 10, 23, 59, 59));
    }

    [Fact]
    void To_WhenGetTransactionsArgsHasNoLimit_ShouldDefaultLimitToNull()
    {
        // arrange
        using var jsonDoc = JsonDocument.Parse("""
        {
            "fromDate": "2026-08-01T00:00:00",
            "toDate": "2026-08-10T23:59:59"
        }
        """);

        var args = new Dictionary<string, object>
        {
            ["fromDate"] = jsonDoc.RootElement.GetProperty("fromDate"),
            ["toDate"] = jsonDoc.RootElement.GetProperty("toDate")
        };
        var sut = new ToolCallArgs(args);

        // act
        var result = sut.To<GetTransactionsArgs>();

        // assert
        result.Should().NotBeNull();
        result!.Limit.Should().BeNull();
    }
}
