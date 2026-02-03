using FluentAssertions;
using MyFinanceTracker.CommandProcessing.Text.Engine.Dispatching.Commands.DeleteTransaction.Parsing;
using Xunit;

namespace MyFinanceTracker.CommandProcessing.Text.Tests.Engine.Dispatching.Commands.DeleteTransaction.Parsing;

public class DeleteTransactionCommandRegexParserTests
{
    private readonly DeleteTransactionCommandRegexParser parser = new ();

    [Theory]
    [InlineData("food 27.01.2026", "food", 2026, 1, 27)]
    [InlineData("  taxi   01.01.26  ", "taxi", 2026, 1, 1)]
    [InlineData("salary_income 5.5.25", "salary_income", 2025, 5, 5)]
    async Task Parse_ValidInput_ReturnsSuccess(string input, string expectedCategory, int year, int month, int day)
    {
        // act
        var result = await parser.Parse(input);

        // assert
        var success = result.Should().BeOfType<DeleteTransactionCommandParseResult.Success>().Subject;
        success.Command.CategoryAlias.Should().Be(expectedCategory);
        success.Command.Date.Should().Be(new DateOnly(year, month, day));
    }

    [Fact]
    async Task Parse_EmptyInput_ReturnsFailure()
    {
        // act
        var result = await parser.Parse("   ");

        // assert
        var failure = result.Should().BeOfType<DeleteTransactionCommandParseResult.Failure>().Subject;
        failure.Message.Should().Be("The input string is empty.");
    }

    [Theory]
    [InlineData("justCategory")]
    [InlineData("27.01.2026")]
    [InlineData("category with spaces 27.01.2026")]
    async Task Parse_InvalidFormat_ReturnsFailure(string input)
    {
        // act
        var result = await parser.Parse(input);

        // assert
        var failure = result.Should().BeOfType<DeleteTransactionCommandParseResult.Failure>().Subject;
        failure.Message.Should().Contain("Format error");
    }

    [Theory]
    [InlineData("food 32.01.2026")]
    [InlineData("food 27.13.2026")]
    [InlineData("food 27.01.abcd")]
    [InlineData("food as.01.abcd")]
    [InlineData("food 27.sd.abcd")]
    [InlineData("food sd.01.abcd")]
    async Task Parse_InvalidDate_ReturnsFailure(string input)
    {
        // act
        var result = await parser.Parse(input);

        // assert
        var failure = result.Should().BeOfType<DeleteTransactionCommandParseResult.Failure>().Subject;
        failure.Message.Should().Contain("is not a valid date format");
    }
}
