using FluentAssertions;
using MyFinanceTracker.Domain.Entities;
using MyFinanceTracker.Interactions.Commands.AddTransaction.Parsing;

namespace MyFinanceTracker.Interactions.Tests.Commands.AddTransaction.Parsing;

public class AddTransactionCommandParserTests
{
    private readonly AddTransactionCommandParser parser;

    public AddTransactionCommandParserTests()
    {
        // arrange
        parser = new AddTransactionCommandParser();
    }

    [Theory]
    [InlineData("income food 100", TransactionType.Income, "food", new[] { 100.0 })]
    [InlineData("expense taxi 50.5", TransactionType.Expense, "taxi", new[] { 50.5 })]
    [InlineData("expense shopping 10,25", TransactionType.Expense, "shopping", new[] { 10.25 })]
    [InlineData("income 1000", TransactionType.Income, null, new[] { 1000.0 })]
    void Parse_BasicValidCommands_ReturnsSuccess(
        string input,
        TransactionType expectedType,
        string? expectedCategory,
        double[] expectedAmounts)
    {
        // act
        var result = parser.Parse(input);

        // assert
        var success = result.Should().BeOfType<AddTransactionCommandParseResult.Success>().Subject;
        success.Data.Type.Should().Be(expectedType);
        success.Data.CategoryAlias.Should().Be(expectedCategory);
        success.Data.Amounts.Should().Equal(expectedAmounts.Select(x => (decimal)x));
    }

    [Fact]
    void Parse_MultipleAmounts_ParsedCorrectly()
    {
        // arrange
        var input = "expense food 10 20.5 30,25";

        // act
        var result = parser.Parse(input);

        // assert
        var success = result.Should().BeOfType<AddTransactionCommandParseResult.Success>().Subject;
        success.Data.Amounts.Should().Equal(10m, 20.5m, 30.25m);
    }

    [Theory]
    [InlineData("expense food 100 25.01.2026", 2026, 1, 25)]
    [InlineData("income 500 1.2.26", 2026, 2, 1)]
    void Parse_WithValidDate_ReturnsCorrectDate(string input, int year, int month, int day)
    {
        // act
        var result = parser.Parse(input);

        // assert
        var success = result.Should().BeOfType<AddTransactionCommandParseResult.Success>().Subject;
        success.Data.Date.Should().Be(new DateOnly(year, month, day));
    }

    [Fact]
    void Parse_WithNotes_ExtractsNoteCorrectly()
    {
        // arrange
        var input = "expense lunch 150 26.01.2026 Pizza with friends";

        // act
        var result = parser.Parse(input);

        // assert
        var success = result.Should().BeOfType<AddTransactionCommandParseResult.Success>().Subject;
        success.Data.Note.Should().Be("Pizza with friends");
    }

    [Theory]
    [InlineData("expense food 100 01.01.1899", typeof(AddTransactionCommandParseResult.DateBelowMinLimit))]
    [InlineData("expense food 100 01.01.2101", typeof(AddTransactionCommandParseResult.DateAboveMaxLimit))]
    [InlineData("expense food 100 32.01.2026", typeof(AddTransactionCommandParseResult.UnparseableDate))]
    void Parse_InvalidDates_ReturnsSpecificError(string input, Type expectedErrorType)
    {
        // act
        var result = parser.Parse(input);

        // assert
        result.Should().BeOfType(expectedErrorType);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    void Parse_EmptyInput_ReturnsEmptyInputResult(string input)
    {
        // act
        var result = parser.Parse(input);

        // assert
        result.Should().BeOfType<AddTransactionCommandParseResult.EmptyInput>();
    }

    [Theory]
    [InlineData("just some text")]
    [InlineData("100 expense food")]
    [InlineData("expense")]
    void Parse_InvalidFormat_ReturnsInvalidFormatResult(string input)
    {
        // act
        var result = parser.Parse(input);

        // assert
        result.Should().BeOfType<AddTransactionCommandParseResult.InvalidFormat>();
    }
}