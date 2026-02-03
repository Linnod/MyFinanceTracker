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
    [InlineData("add expense food 100", TextCommandType.AddTransaction, "expense food 100")]
    [InlineData("rem 12345", TextCommandType.DeleteTransaction, "12345")]
    [InlineData("  ADD  payload  ", TextCommandType.AddTransaction, "payload")]
    [InlineData("rem", TextCommandType.DeleteTransaction, "")]
    async Task Interpret_ValidCommands_ReturnsIdentified(string input, TextCommandType expectedType, string expectedPayload)
    {
        // act
        var result = await interpreter.Interpret(input);

        // assert
        var identified = result.Should().BeOfType<InterpretationResult.Identified>().Subject;
        identified.Type.Should().Be(expectedType);
        identified.Payload.Should().Be(expectedPayload);
    }

    [Theory]
    [InlineData("unknown_command some data")]
    async Task Interpret_InvalidOrUnknownInput_ReturnsUnrecognized(string? input)
    {
        // act
        var result = await interpreter.Interpret(input!);

        // assert
        result.Should().BeOfType<InterpretationResult.Unrecognized>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    async Task Interpret_InvalidOrUnknownInput_ReturnsEmptyInput(string? input)
    {
        // act
        var result = await interpreter.Interpret(input!);

        // assert
        result.Should().BeOfType<InterpretationResult.EmptyInput>();
    }
}
