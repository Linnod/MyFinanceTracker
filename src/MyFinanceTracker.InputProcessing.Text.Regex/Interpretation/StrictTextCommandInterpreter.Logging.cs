using Microsoft.Extensions.Logging;

namespace MyFinanceTracker.InputProcessing.Text.Regex.Interpretation;

using static MyFinanceTracker.InputProcessing.Text.Regex.Logging.LogEvents.Interpreter;

internal sealed partial class StrictTextCommandInterpreter
{
    [LoggerMessage(
        EventId = Entry,
        Level = LogLevel.Debug,
        Message = "--> Interpreting: '{Input}'")]
    partial void LogInterpretationStarted(InterpretationInput input);

    [LoggerMessage(
        EventId = Success,
        Level = LogLevel.Debug,
        Message = "++ Success: {Result}")]
    partial void LogInterpretationSuccess(InterpretationResult.Identified result);

    [LoggerMessage(
        EventId = UnrecognizedCommand,
        Level = LogLevel.Information,
        Message = "!! Failed: {Result}")]
    partial void LogUnrecognizedCommand(InterpretationResult.Unrecognized result);
}