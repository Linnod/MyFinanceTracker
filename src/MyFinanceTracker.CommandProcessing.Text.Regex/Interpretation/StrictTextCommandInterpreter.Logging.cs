using Microsoft.Extensions.Logging;

namespace MyFinanceTracker.CommandProcessing.Text.Regex.Interpretation;

using static MyFinanceTracker.CommandProcessing.Text.Regex.Logging.LogEvents.Interpreter;

internal sealed partial class StrictTextCommandInterpreter
{
    [LoggerMessage(
        EventId = Entry,
        Level = LogLevel.Debug,
        Message = "--> Interpreting: '{Input}'")]
    partial void LogInterpretationStarted(string input);

    [LoggerMessage(
        EventId = Success,
        Level = LogLevel.Debug,
        Message = "++ Success: {Result}")]
    partial void LogInterpretationSuccess(InterpretationResult.Identified result);

    [LoggerMessage(
        EventId = EmptyInput,
        Level = LogLevel.Information,
        Message = "!! Failed: {Result}")]
    partial void LogEmptyInput(InterpretationResult.EmptyInput result);

    [LoggerMessage(
        EventId = UnrecognizedCommand,
        Level = LogLevel.Information,
        Message = "!! Failed: {Result}")]
    partial void LogUnrecognizedCommand(InterpretationResult.Unrecognized result);
}