using FluentAssertions;
using Microsoft.Extensions.Options;
using MyFinanceTracker.Infrastructure.Persistence.GoogleSheets.Configuration;
using MyFinanceTracker.Infrastructure.Persistence.GoogleSheets.Services;

namespace MyFinanceTracker.Infrastructure.Persistence.GoogleSheets.Tests.Services;

public class FormulaBuilderTests
{
    private readonly FormulaBuilder formulaBuilder = new(
        Options.Create(new GoogleSheetsOptions { DecimalSeparator = "." })
    );

    [Theory]
    [InlineData(null, "+100", "=100")]
    [InlineData("", "100", "=100")]
    [InlineData("0", "50", "=50")]
    [InlineData("0.00", "+50", "=50")]
    [InlineData("100", "+50", "=100+50")]
    [InlineData("=100", "+50", "=100+50")]
    [InlineData("-10", "+5", "=-10+5")]
    [InlineData("=SUM(A1:A5)", "+100", "=SUM(A1:A5)+100")]
    [InlineData("  100  ", " +50 ", "=100+50")]
    void Merge_ShouldProduceValidGoogleSheetsFormula(string? current, string delta, string expected)
    {
        // arrange
        // act
        var result = formulaBuilder.Merge(current, delta);

        // assert
        result.Should().Be(expected);
    }
}
