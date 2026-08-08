using System.Text.Json;
using FluentAssertions;
using MyFinanceTracker.CommandProcessing.Text.Gemini.Parsing;
using Xunit;

namespace MyFinanceTracker.CommandProcessing.Text.Gemini.Tests.Parsing;

public class GeminiArgsParserTests
{
    [Fact]
    public void GetString_WhenPresentAndJsonElement_ShouldReturnStringValue()
    {
        // Arrange
        using var jsonDoc = JsonDocument.Parse("""{"categoryAlias": "groceries"}""");
        var args = new Dictionary<string, object>
        {
            ["categoryAlias"] = jsonDoc.RootElement.GetProperty("categoryAlias")
        };

        // Act
        var result = args.GetString("categoryAlias");

        // Assert
        result.Should().Be("groceries");
    }

    [Fact]
    public void GetDateOnly_WhenValidDateString_ShouldParseCorrectly()
    {
        // Arrange
        var args = new Dictionary<string, object>
        {
            ["date"] = "2026-08-08"
        };

        // Act
        var result = args.GetDateOnly("date");

        // Assert
        result.Should().Be(new DateOnly(2026, 8, 8));
    }

    [Fact]
    public void GetDecimalArray_WhenJsonArray_ShouldReturnDecimalArray()
    {
        // Arrange
        using var jsonDoc = JsonDocument.Parse("""{"amounts": [100.5, 50]}""");
        var args = new Dictionary<string, object>
        {
            ["amounts"] = jsonDoc.RootElement.GetProperty("amounts")
        };

        // Act
        var result = args.GetDecimalArray("amounts");

        // Assert
        result.Should().Equal(100.5m, 50m);
    }

    [Fact]
    public void GetDecimalArray_WhenSingleNumber_ShouldReturnSingleElementArray()
    {
        // Arrange
        var args = new Dictionary<string, object>
        {
            ["amounts"] = 150.75m
        };

        // Act
        var result = args.GetDecimalArray("amounts");

        // Assert
        result.Should().Equal(150.75m);
    }
}