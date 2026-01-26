using FluentAssertions;
using MyFinanceTracker.Interactions.Commands.DeleteTransaction.Parsing;

namespace MyFinanceTracker.Interactions.Tests.Commands.DeleteTransaction.Parsing;

public class DeleteTransactionCommandParserTests
{
    private readonly DeleteTransactionCommandParser parser = new();

    [Theory]
    [InlineData("food 27.01.2026", "food", 2026, 1, 27)]
    [InlineData("  taxi   01.01.26  ", "taxi", 2026, 1, 1)]
    [InlineData("salary_income 5.5.25", "salary_income", 2025, 5, 5)]
    void Parse_ValidInput_ReturnsSuccess(string input, string expectedCategory, int year, int month, int day)
    {
        // act
        var result = parser.Parse(input);

        // assert
        result.Should().BeOfType<DeleteTransactionCommandParseResult.Success>();
        var success = (DeleteTransactionCommandParseResult.Success)result;

        success.Command.CategoryAlias.Should().Be(expectedCategory);
        success.Command.Date.Should().Be(new DateOnly(year, month, day));
    }

    [Fact]
    void Parse_EmptyInput_ReturnsEmptyInput()
    {
        // act
        var result = parser.Parse("   ");

        // assert
        result.Should().BeOfType<DeleteTransactionCommandParseResult.EmptyInput>();
    }

    [Theory]
    [InlineData("justCategory")]
    [InlineData("27.01.2026")]
    [InlineData("category with spaces 27.01.2026")]
    void Parse_InvalidFormat_ReturnsInvalidFormat(string input)
    {
        // act
        var result = parser.Parse(input);

        // assert
        result.Should().BeOfType<DeleteTransactionCommandParseResult.InvalidFormat>();
    }

    [Theory]
    [InlineData("food 32.01.2026")]
    [InlineData("food 27.13.2026")]
    [InlineData("food 27.01.abcd")]
    [InlineData("food as.01.abcd")]
    [InlineData("food 27.sd.abcd")]
    [InlineData("food sd.01.abcd")]
    void Parse_InvalidDate_ReturnsUnparseableDate(string input)
    {
        // act
        var result = parser.Parse(input);

        // assert
        result.Should().BeOfType<DeleteTransactionCommandParseResult.UnparseableDate>();
    }
}
