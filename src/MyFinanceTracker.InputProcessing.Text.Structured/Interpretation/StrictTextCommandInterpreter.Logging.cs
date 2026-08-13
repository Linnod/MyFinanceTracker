using Microsoft.Extensions.Logging;

namespace MyFinanceTracker.InputProcessing.Text.Structured.Interpretation;

using static MyFinanceTracker.InputProcessing.Text.Structured.Logging.LogEvents.Interpreter;

internal sealed partial class StrictTextCommandInterpreter
{
    [LoggerMessage(
        EventId = Entry,
        Level = LogLevel.Debug,
        Message = "Interpreting input '{Input}'")]
    partial void LogStarted(InterpretationInput input);

    [LoggerMessage(
        EventId = Success,
        Level = LogLevel.Debug,
        Message = "Successfully identified command: {Result}")]
    partial void LogIdentified(InterpretationResult.Identified result);

    [LoggerMessage(
        EventId = Unrecognized,
        Level = LogLevel.Information,
        Message = "Failed to identify command: {Result}")]
    partial void LogUnrecognized(InterpretationResult.Unrecognized result);
}