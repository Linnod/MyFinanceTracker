using FluentAssertions;
using Microsoft.Extensions.Logging;
using MyFinanceTracker.InputProcessing.Text.Regex.Dispatching.Commands.DeleteTransaction.Parsing;
using NSubstitute;
using Xunit;

namespace MyFinanceTracker.InputProcessing.Text.Regex.Tests.Commands.DeleteTransaction.Parsing;

public class DeleteTransactionCommandPayloadRegexParserTests
{
    private readonly DeleteTransactionCommandPayloadRegexParser parser;

    public DeleteTransactionCommandPayloadRegexParserTests()
    {
        var loggerMock = Substitute.For<ILogger<DeleteTransactionCommandPayloadRegexParser>>();
        parser = new DeleteTransactionCommandPayloadRegexParser(loggerMock);
    }

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
        success.Payload.CategoryAlias.Should().Be(expectedCategory);
        success.Payload.Date.Should().Be(new DateOnly(year, month, day));
    }

    [Fact]
    async Task Parse_EmptyInput_ReturnsEmptyInputErrorCode()
    {
        // act
        var result = await parser.Parse("   ");

        // assert
        var failure = result.Should().BeOfType<DeleteTransactionCommandParseResult.Failure>().Subject;
        failure.ErrorCode.Should().Be(ErrorCode.Syntax.EmptyInput);
    }

    [Theory]
    [InlineData("justCategory")]
    [InlineData("27.01.2026")]
    [InlineData("category with spaces 27.01.2026")]
    async Task Parse_InvalidFormat_ReturnsInvalidFormatErrorCode(string input)
    {
        // act
        var result = await parser.Parse(input);

        // assert
        var failure = result.Should().BeOfType<DeleteTransactionCommandParseResult.Failure>().Subject;
        failure.ErrorCode.Should().Be(ErrorCode.Syntax.InvalidFormat);
    }

    [Theory]
    [InlineData("food 32.01.2026")]
    [InlineData("food 27.13.2026")]
    [InlineData("food 27.01.abcd")]
    [InlineData("food as.01.abcd")]
    async Task Parse_InvalidDate_ReturnsInvalidFormatDateErrorCode(string input)
    {
        // act
        var result = await parser.Parse(input);

        // assert
        var failure = result.Should().BeOfType<DeleteTransactionCommandParseResult.Failure>().Subject;
        failure.ErrorCode.Should().Contain(ErrorCode.Syntax.InvalidDateFormat);
    }
}