using Microsoft.Extensions.Logging;
using NSubstitute;
using MyFinanceTracker.Interactions.Interpretation;
using FluentAssertions;

namespace MyFinanceTracker.Interactions.Tests.Interpretation;

public class StrictInteractionInterpreterTests
{
    private readonly ILogger<StrictInteractionInterpreter> loggerMock;
    private readonly StrictInteractionInterpreter interpreter;

    public StrictInteractionInterpreterTests()
    {
        loggerMock = Substitute.For<ILogger<StrictInteractionInterpreter>>();
        interpreter = new StrictInteractionInterpreter(loggerMock);
    }

    [Theory]
    [InlineData("add expense food 100", InteractionType.AddTransaction, "expense food 100")]
    [InlineData("rem 12345", InteractionType.DeleteTransaction, "12345")]
    [InlineData("  ADD  payload  ", InteractionType.AddTransaction, "payload")]
    [InlineData("rem", InteractionType.DeleteTransaction, "")]
    void Interpret_ValidCommands_ReturnsIdentified(string input, InteractionType expectedType, string expectedPayload)
    {
        // act
        var result = interpreter.Interpret(input);

        // assert
        var identified = result.Should().BeOfType<InterpretationResult.Identified>().Subject;
        identified.Type.Should().Be(expectedType);
        identified.Payload.Should().Be(expectedPayload);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    [InlineData("unknown_command some data")]
    [InlineData("delete 123")]
    void Interpret_InvalidOrUnknownInput_ReturnsUnrecognized(string? input)
    {
        // act
        var result = interpreter.Interpret(input!);

        // assert
        result.Should().BeOfType<InterpretationResult.Unrecognized>();
    }
}