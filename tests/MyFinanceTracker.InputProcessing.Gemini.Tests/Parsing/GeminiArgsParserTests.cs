using System.Text.Json;
using FluentAssertions;
using MyFinanceTracker.Domain.Entities;
using MyFinanceTracker.InputProcessing.Gemini.Parsing;
using Xunit;

namespace MyFinanceTracker.InputProcessing.Gemini.Tests.Parsing;

public class GeminiArgsParserTests
{
    private record TestArgs(
        string? CategoryAlias,
        DateTime? Date,
        IReadOnlyList<decimal>? Amounts,
        TransactionType? Type,
        string? RecognizedInput);

    [Fact]
    void BindArgs_WhenArgsIsNull_ShouldReturnNull()
    {
        // arrange
        IDictionary<string, object>? args = null;

        // act
        var result = args.BindArgs<TestArgs>();

        // assert
        result.Should().BeNull();
    }

    [Fact]
    void BindArgs_WhenArgsIsEmpty_ShouldReturnNull()
    {
        // arrange
        var args = new Dictionary<string, object>();

        // act
        var result = args.BindArgs<TestArgs>();

        // assert
        result.Should().BeNull();
    }

    [Fact]
    public void BindArgs_WhenJsonElementsProvided_ShouldBindToDtoCorrectly()
    {
        // arrange
        using var jsonDoc = JsonDocument.Parse("""
        {
            "categoryAlias": "groceries",
            "date": "2026-08-08T14:30:00",
            "amounts": [100.5, 50],
            "type": "expense",
            "recognizedInput": "Расход 150.5 € в категорию Еда"
        }
        """);

        var args = new Dictionary<string, object>
        {
            ["categoryAlias"] = jsonDoc.RootElement.GetProperty("categoryAlias"),
            ["date"] = jsonDoc.RootElement.GetProperty("date"),
            ["amounts"] = jsonDoc.RootElement.GetProperty("amounts"),
            ["type"] = jsonDoc.RootElement.GetProperty("type"),
            ["recognizedInput"] = jsonDoc.RootElement.GetProperty("recognizedInput")
        };

        // act
        var result = args.BindArgs<TestArgs>();

        // assert
        result.Should().NotBeNull();
        result!.CategoryAlias.Should().Be("groceries");
        result.Date.Should().Be(new DateTime(2026, 8, 8, 14, 30, 0));
        result.Amounts.Should().Equal(100.5m, 50m);
        result.Type.Should().Be(TransactionType.Expense);
        result.RecognizedInput.Should().Be("Расход 150.5 € в категорию Еда");
    }

    [Theory]
    [InlineData("2026-08-08T14:30:00", 2026, 8, 8, 14, 30, 0)]
    [InlineData("2026-08-08 14:30:00", 2026, 8, 8, 14, 30, 0)]
    [InlineData("2026-08-08T14:30", 2026, 8, 8, 14, 30, 0)]
    [InlineData("2026-08-08", 2026, 8, 8, 0, 0, 0)]
    void BindArgs_FlexibleDateTimeFormats_ShouldBindCorrectly(string dateStr, int year, int month, int day, int hour, int minute, int second)
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

        // act
        var result = args.BindArgs<TestArgs>();

        // assert
        result.Should().NotBeNull();
        result!.Date.Should().Be(new DateTime(year, month, day, hour, minute, second));
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-a-date")]
    [InlineData("08/11/2026")]
    void BindArgs_WhenInvalidDateFormatProvided_ShouldReturnNull(string invalidDate)
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

        // act
        var result = args.BindArgs<TestArgs>();

        // assert
        result.Should().BeNull();
    }

    [Fact]
    void BindArgs_WhenNumbersArePassedAsStrings_ShouldBindNumbersCorrectly()
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

        // act
        var result = args.BindArgs<TestArgs>();

        // assert
        result.Should().NotBeNull();
        result!.Amounts.Should().Equal(100.5m, 50m);
    }

    [Fact]
    void BindArgs_WhenAddTransactionArgsHasMissingOrNullAmounts_ShouldDefaultAmountsToEmptyList()
    {
        // arrange
        using var jsonDoc = JsonDocument.Parse("""
        {
            "type": "expense",
            "amounts": null,
            "recognizedInput": "Test input"
        }
        """);

        var args = new Dictionary<string, object>
        {
            ["type"] = jsonDoc.RootElement.GetProperty("type"),
            ["amounts"] = jsonDoc.RootElement.GetProperty("amounts"),
            ["recognizedInput"] = jsonDoc.RootElement.GetProperty("recognizedInput")
        };

        // act
        var result = args.BindArgs<AddTransactionArgs>();

        // assert
        result.Should().NotBeNull();
        result!.Amounts.Should().NotBeNull();
        result.Amounts.Should().BeEmpty();
    }
}
