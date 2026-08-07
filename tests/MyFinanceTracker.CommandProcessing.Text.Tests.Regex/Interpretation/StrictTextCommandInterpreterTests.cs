using FluentAssertions;
using Microsoft.Extensions.Logging;
using MyFinanceTracker.CommandProcessing.Text.Regex.Dispatching.Commands.AddTransaction;
using MyFinanceTracker.CommandProcessing.Text.Regex.Dispatching.Commands.DeleteTransaction;
using MyFinanceTracker.CommandProcessing.Text.Regex.Dispatching.Commands.ListCategories;
using MyFinanceTracker.CommandProcessing.Text.Regex.Interpretation;
using NSubstitute;
using Xunit;

namespace MyFinanceTracker.CommandProcessing.Text.Regex.Tests.Interpretation;

public class StrictTextCommandInterpreterTests
{
    private readonly ILogger<StrictTextCommandInterpreter> loggerMock;
    private readonly ICommandRegistry commandRegistry;
    private readonly StrictTextCommandInterpreter interpreter;

    public StrictTextCommandInterpreterTests()
    {
        loggerMock = Substitute.For<ILogger<StrictTextCommandInterpreter>>();
        commandRegistry = new CommandRegistry();
        interpreter = new StrictTextCommandInterpreter(commandRegistry, loggerMock);
    }

    [Theory]
    [InlineData("t add food 100", "food 100")]
    [InlineData("  T  +  rent 500  ", "rent 500")]
    public async Task Interpret_AddTransactionCommand_ReturnsIdentifiedWithCorrectPayload(string input, string expectedPayload)
    {
        // act
        var result = await interpreter.Interpret(input);

        // assert
        var identified = result.Should().BeOfType<InterpretationResult.Identified>().Subject;
        var command = identified.Command.Should().BeOfType<AddTransactionCommand>().Subject;
        command.Payload.Should().Be(expectedPayload);
    }

    [Fact]
    public async Task Interpret_DeleteTransactionCommand_ReturnsIdentifiedWithCorrectPayload()
    {
        // arrange
        var input = "transaction rem 12345";

        // act
        var result = await interpreter.Interpret(input);

        // assert
        var identified = result.Should().BeOfType<InterpretationResult.Identified>().Subject;
        var command = identified.Command.Should().BeOfType<DeleteTransactionCommand>().Subject;
        command.Payload.Should().Be("12345");
    }

    [Fact]
    public async Task Interpret_ListCategoriesCommand_ReturnsIdentifiedWithoutPayload()
    {
        // arrange
        var input = "c all";

        // act
        var result = await interpreter.Interpret(input);

        // assert
        var identified = result.Should().BeOfType<InterpretationResult.Identified>().Subject;
        identified.Command.Should().BeOfType<ListCategoriesCommand>();
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