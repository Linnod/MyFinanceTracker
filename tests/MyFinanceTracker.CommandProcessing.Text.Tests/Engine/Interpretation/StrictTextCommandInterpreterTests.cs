using FluentAssertions;
using Microsoft.Extensions.Logging;
using MyFinanceTracker.CommandProcessing.Text.Engine;
using MyFinanceTracker.CommandProcessing.Text.Engine.Interpretation;
using NSubstitute;
using Xunit;

namespace MyFinanceTracker.CommandProcessing.Text.Tests.Engine.Interpretation;

public class StrictTextCommandInterpreterTests
{
    private readonly ILogger<StrictTextCommandInterpreter> loggerMock;
    private readonly StrictTextCommandInterpreter interpreter;

    public StrictTextCommandInterpreterTests()
    {
        loggerMock = Substitute.For<ILogger<StrictTextCommandInterpreter>>();
        interpreter = new StrictTextCommandInterpreter(loggerMock);
    }

    [Theory]
    [InlineData("t add food 100", TextCommandType.AddTransaction, "food 100")]
    [InlineData("transaction rem 12345", TextCommandType.DeleteTransaction, "12345")]
    [InlineData("c all", TextCommandType.ListCategories, "")]
    [InlineData("  T  +  rent 500  ", TextCommandType.AddTransaction, "rent 500")]
    async Task Interpret_ValidHierarchicalCommands_ReturnsIdentified(
        string input, 
        TextCommandType expectedType, 
        string expectedPayload)
    {
        // act
        var result = await interpreter.Interpret(input);

        // assert
        var identified = result.Should().BeOfType<InterpretationResult.Identified>().Subject;
        identified.Type.Should().Be(expectedType);
        identified.Payload.Should().Be(expectedPayload);
    }

    [Fact]
    async Task Interpret_DomainError_ReturnsUnrecognizedWithDomainSuggestion()
    {
        // arrange
        var input = "tranz add food 100";

        // act
        var result = await interpreter.Interpret(input);

        // assert
        var unrecognized = result.Should().BeOfType<InterpretationResult.Unrecognized>().Subject;
        unrecognized.Command.Should().Be("tranz");
        unrecognized.Suggestion.Should().Be("tran");
        unrecognized.Examples.Should().Contain(e => e.Contains("t ..."));
    }

    [Fact]
    async Task Interpret_ActionError_ReturnsUnrecognizedWithActionSuggestion()
    {
        // arrange
        var input = "t ad food 100";

        // act
        var result = await interpreter.Interpret(input);

        // assert
        var unrecognized = result.Should().BeOfType<InterpretationResult.Unrecognized>().Subject;
        unrecognized.Command.Should().Be("ad");
        unrecognized.Suggestion.Should().Be("add");
        unrecognized.Examples.Should().Contain(e => e.StartsWith("t add"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    async Task Interpret_EmptyInput_ReturnsEmptyInput(string? input)
    {
        // act
        var result = await interpreter.Interpret(input!);

        // assert
        result.Should().BeOfType<InterpretationResult.EmptyInput>();
    }
}