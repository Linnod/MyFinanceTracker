using FluentAssertions;
using Microsoft.Extensions.Logging;
using MyFinanceTracker.Domain.Entities;
using MyFinanceTracker.InputProcessing.Text.Structured.Dispatching.Commands.AddTransaction.Parsing;
using NSubstitute;
using Xunit;

namespace MyFinanceTracker.InputProcessing.Text.Structured.Tests.Commands.AddTransaction.Parsing;

public class AddTransactionCommandPayloadRegexParserTests
{
    private readonly AddTransactionCommandPayloadRegexParser parser;

    public AddTransactionCommandPayloadRegexParserTests()
    {
        var loggerMock = Substitute.For<ILogger<AddTransactionCommandPayloadRegexParser>>();
        parser = new AddTransactionCommandPayloadRegexParser(loggerMock);
    }

    [Theory]
    [InlineData("income food 100", TransactionType.Income, "food", new[] { 100.0 })]
    [InlineData("expense taxi 50.5", TransactionType.Expense, "taxi", new[] { 50.5 })]
    [InlineData("expense shopping 10,25", TransactionType.Expense, "shopping", new[] { 10.25 })]
    [InlineData("income 1000", TransactionType.Income, null, new[] { 1000.0 })]
    async Task Parse_ValidCommands_ReturnsSuccess(
        string input,
        TransactionType expectedType,
        string? expectedCategory,
        double[] expectedAmounts)
    {
        // act
        var result = await parser.Parse(input);

        // assert
        var success = result.Should().BeOfType<AddTransactionCommandParseResult.Success>().Subject;
        success.Payload.Type.Should().Be(expectedType);
        success.Payload.CategoryAlias.Should().Be(expectedCategory);
        success.Payload.Amounts.Should().Equal(expectedAmounts.Select(x => (decimal)x));
    }

    [Fact]
    async Task Parse_MultipleAmounts_ParsedCorrectly()
    {
        // arrange
        var input = "expense food 10 20.5 30,25";

        // act
        var result = await parser.Parse(input);

        // assert
        var success = result.Should().BeOfType<AddTransactionCommandParseResult.Success>().Subject;
        success.Payload.Amounts.Should().Equal(10m, 20.5m, 30.25m);
    }

    [Theory]
    [InlineData("expense food 100 25.01.2026", 2026, 1, 25)]
    [InlineData("income 500 1.2.26", 2026, 2, 1)]
    async Task Parse_WithValidDate_ReturnsCorrectDate(string input, int year, int month, int day)
    {
        // act
        var result = await parser.Parse(input);

        // assert
        var success = result.Should().BeOfType<AddTransactionCommandParseResult.Success>().Subject;
        success.Payload.Date.Should().Be(new DateOnly(year, month, day));
    }

    [Fact]
    async Task Parse_WithNotes_ExtractsNoteCorrectly()
    {
        // arrange
        var input = "expense lunch 150 26.01.2026 Pizza with friends";

        // act
        var result = await parser.Parse(input);

        // assert
        var success = result.Should().BeOfType<AddTransactionCommandParseResult.Success>().Subject;
        success.Payload.Note.Should().Be("Pizza with friends");
    }

    [Theory]
    [InlineData("expense food 100 32.01.2026")]
    async Task Parse_InvalidDates_ReturnsFailureWithInvalidDateFormatErrorCode(string input)
    {
        // act
        var result = await parser.Parse(input);

        // assert
        var failure = result.Should().BeOfType<AddTransactionCommandParseResult.Failure>().Subject;
        failure.ErrorCode.Should().Contain(ErrorCode.Syntax.InvalidDateFormat);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    async Task Parse_EmptyInput_ReturnsFailureWithEmptyInputErrorCode(string input)
    {
        // act
        var result = await parser.Parse(input);

        // assert
        var failure = result.Should().BeOfType<AddTransactionCommandParseResult.Failure>().Subject;
        failure.ErrorCode.Should().Be(ErrorCode.Syntax.EmptyInput);
    }

    [Theory]
    [InlineData("just some text")]
    [InlineData("100 expense food")]
    [InlineData("expense")]
    async Task Parse_InvalidFormat_ReturnsFailureWithInvalidFormatErrorCode(string input)
    {
        // act
        var result = await parser.Parse(input);

        // assert
        var failure = result.Should().BeOfType<AddTransactionCommandParseResult.Failure>().Subject;
        failure.ErrorCode.Should().Contain(ErrorCode.Syntax.InvalidFormat);
    }
}