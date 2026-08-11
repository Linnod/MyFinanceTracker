using System.Text.Json;
using FluentAssertions;
using MyFinanceTracker.Domain.Entities;
using MyFinanceTracker.InputProcessing.Text.Gemini.Parsing;
using Xunit;

namespace MyFinanceTracker.InputProcessing.Text.Gemini.Tests.Parsing;

public class GeminiArgsParserTests
{
    private record TestArgs(
        string? CategoryAlias,
        DateOnly? Date,
        IReadOnlyList<decimal>? Amounts,
        TransactionType? Type,
        string? RecognizedInput);

    [Fact]
    public void BindArgs_WhenArgsIsNull_ShouldReturnNull()
    {
        // Arrange
        IDictionary<string, object>? args = null;

        // Act
        var result = args.BindArgs<TestArgs>();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void BindArgs_WhenArgsIsEmpty_ShouldReturnNull()
    {
        // Arrange
        var args = new Dictionary<string, object>();

        // Act
        var result = args.BindArgs<TestArgs>();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void BindArgs_WhenJsonElementsProvided_ShouldBindToDtoCorrectly()
    {
        // Arrange
        using var jsonDoc = JsonDocument.Parse("""
        {
            "categoryAlias": "groceries",
            "date": "2026-08-08",
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

        // Act
        var result = args.BindArgs<TestArgs>();

        // Assert
        result.Should().NotBeNull();
        result!.CategoryAlias.Should().Be("groceries");
        result.Date.Should().Be(new DateOnly(2026, 8, 8));
        result.Amounts.Should().Equal(100.5m, 50m);
        result.Type.Should().Be(TransactionType.Expense);
        result.RecognizedInput.Should().Be("Расход 150.5 € в категорию Еда");
    }

    [Fact]
    public void BindArgs_WhenNumbersArePassedAsStrings_ShouldBindNumbersCorrectly()
    {
        // Arrange
        using var jsonDoc = JsonDocument.Parse("""
        {
            "amounts": ["100.5", "50"]
        }
        """);

        var args = new Dictionary<string, object>
        {
            ["amounts"] = jsonDoc.RootElement.GetProperty("amounts")
        };

        // Act
        var result = args.BindArgs<TestArgs>();

        // Assert
        result.Should().NotBeNull();
        result!.Amounts.Should().Equal(100.5m, 50m);
    }
}