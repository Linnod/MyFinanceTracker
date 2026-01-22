using FluentAssertions;
using MyFinanceTracker.Domain.Entities;
using MyFinanceTracker.Interactions.Parsing.Parser;
using MyFinanceTracker.Interactions.Parsing.Parser.Exceptions;


namespace MyFinanceTracker.Interactions.Tests.Parser;

public class FinancialOperationParserTests
{
    private readonly FinancialOperationParser parser = new();

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Parse_ShouldThrowEmptyInputException_WhenInputIsNullOrWhiteSpace(string? input)
    {
        // arrange
        var expectedMessage = "Input string cannot be empty or whitespace.";

        // act
        Action act = () => { parser.Parse(input!); };

        // assert
        act.Should().Throw<ParsingException>()
           .WithMessage(expectedMessage);
    }

    [Theory]
    [InlineData("just some random text")]
    [InlineData("expense food")]
    [InlineData("only_category_no_numbers")]
    [InlineData("!!!")]
    public void Parse_ShouldReturnEmptyOperation_WhenInputDoesNotMatchRegex(string input)
    {
        // arrange
        // act
        var result = parser.Parse(input);

        // assert
        result.Type.Should().BeNull();
        result.CategoryAlias.Should().BeNull();
        result.Amounts.Should().BeEmpty();
        result.Date.Should().BeNull();
        result.Notes.Should().BeEmpty();
    }

    [Theory]
    [InlineData("expense food 100", FinancialOperationType.Expense, "food", new[] { 100.0 })]
    [InlineData("income salary 5000", FinancialOperationType.Income, "salary", new[] { 5000.0 })]
    [InlineData("return rent 150.50", FinancialOperationType.Return, "rent", new[] { 150.50 })]
    [InlineData("adjust balance 1000", FinancialOperationType.Adjustment, "balance", new[] { 1000.0 })]
    // Multiple sums
    [InlineData("expense food 100 200 50.25", FinancialOperationType.Expense, "food", new[] { 100.0, 200.0, 50.25 })]
    [InlineData("income gift 50 50", FinancialOperationType.Income, "gift", new[] { 50.0, 50.0 })]
    [InlineData("return refund 100 20.50", FinancialOperationType.Return, "refund", new[] { 100.0, 20.50 })]
    [InlineData("adjust diff 500 100 50", FinancialOperationType.Adjustment, "diff", new[] { 500.0, 100.0, 50.0 })]
    public void Parse_ShouldReturnCorrectData_WhenBasicInputIsValid(
        string input,
        FinancialOperationType expectedType,
        string expectedCategory,
        double[] expectedAmounts)
    {
        // arrange
        var expectedDecimals = expectedAmounts.Select(a => (decimal)a).ToArray();

        // act
        var result = parser.Parse(input);

        // assert
        result.Type.Should().Be(expectedType);
        result.CategoryAlias.Should().Be(expectedCategory);
        result.Amounts.Should().BeEquivalentTo(expectedDecimals);
    }

    [Theory]
    [InlineData("expense 100")]
    [InlineData("100")]
    [InlineData("income 50 50")]
    public void Parse_ShouldReturnNullCategory_WhenCategoryIsMissing(string input)
    {
        // arrange
        // act
        var result = parser.Parse(input);

        // assert
        result.CategoryAlias.Should().BeNull();
    }

    [Theory]
    [InlineData("expense food 100.50", 100.50)]
    [InlineData("income salary 1500,75", 1500.75)]
    public void Parse_ShouldKeepDecimalPart_WhenInputHasFloatingPoint(string input, double expectedAmount)
    {
        // arrange
        var parser = new FinancialOperationParser();

        // act
        var result = parser.Parse(input);

        // assert
        result.Amounts.Should().ContainSingle()
              .Which.Should().Be((decimal)expectedAmount);
    }

    [Theory]
    [InlineData("food 100")]
    [InlineData("100")]
    [InlineData("01.01.2024 notes")]
    public void Parse_ShouldReturnNullType_WhenTypeIsMissing(string input)
    {
        // arrange
        // act
        var result = parser.Parse(input);

        // assert
        result.Type.Should().BeNull();
    }

    [Theory]
    [InlineData("expense food 99999999999999999999999999999")]
    public void Parse_ShouldThrowInvalidAmountException_WhenAmountIsTooLarge(string input)
    {
        // arrange
        var invalidToken = "99999999999999999999999999999";
        var expectedMessage = $"'{invalidToken}' is not a valid number.";

        // act
        Action act = () => { parser.Parse(input); };

        // assert
        act.Should().Throw<ParsingException>()
           .WithMessage(expectedMessage);
    }

    [Theory]
    [InlineData("expense food 100 01.01.2024", 2024, 1, 1)] // dd.MM.yyyy
    [InlineData("expense food 100 01.01.24", 2024, 1, 1)]   // dd.MM.yy
    [InlineData("expense food 100 1.1.2024", 2024, 1, 1)]   // d.M.yyyy
    [InlineData("expense food 100 1.1.24", 2024, 1, 1)]     // d.M.yy
    public void Parse_ShouldCorrectlyParseDifferentDateFormats(
        string input,
        int expectedYear,
        int expectedMonth,
        int expectedDay)
    {
        // arrange
        var expectedDate = new DateOnly(expectedYear, expectedMonth, expectedDay);

        // act
        var result = parser.Parse(input);

        // assert
        result.Date.Should().Be(expectedDate);
    }

    [Theory]
    [InlineData("expense food 100 32.01.2024", "32.01.2024")]
    [InlineData("expense food 100 12.13.2024", "12.13.2024")]
    public void Parse_ShouldThrowInvalidDateException_WhenDateFormatIsWrong(string input, string invalidToken)
    {
        // arrange
        var expectedMessage = $"'{invalidToken}' is not a valid date. Use dd.MM.yyyy, dd.MM.yy, d.M.yyyy, d.M.yy.";

        // act
        Action act = () => { parser.Parse(input); };

        // assert
        act.Should().Throw<ParsingException>()
           .WithMessage(expectedMessage);
    }

    [Fact]
    public void Parse_ShouldThrowInvalidDateRangeException_WhenYearIsTooSmall()
    {
        // arrange
        var dateStr = "01.01.1000";
        var input = $"expense food 100 {dateStr}";

        var date = DateOnly.ParseExact(dateStr, "dd.MM.yyyy");
        var expectedMessage = $"Date {date:dd.MM.yyyy} is out of range ({FinancialRules.MinAllowedYear}-{FinancialRules.MaxAllowedYear}).";

        // act
        Action act = () => { parser.Parse(input); };

        // assert
        act.Should().Throw<ParsingException>()
           .WithMessage(expectedMessage);
    }

    [Fact]
    public void Parse_ShouldThrowInvalidDateRangeException_WhenYearIsTooHigh()
    {
        // arrange
        var dateStr = "01.01.3000";
        var input = $"expense food 100 {dateStr}";

        var date = DateOnly.ParseExact(dateStr, "dd.MM.yyyy");
        var expectedMessage = $"Date {date:dd.MM.yyyy} is out of range ({FinancialRules.MinAllowedYear}-{FinancialRules.MaxAllowedYear}).";

        // act
        Action act = () => { parser.Parse(input); };

        // assert
        act.Should().Throw<ParsingException>()
           .WithMessage(expectedMessage);
    }
}
