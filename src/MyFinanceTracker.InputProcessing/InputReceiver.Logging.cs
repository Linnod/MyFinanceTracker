using Microsoft.Extensions.Logging;

namespace MyFinanceTracker.InputProcessing;

using static Logging.LogEvents.Receiver;

public sealed partial class InputReceiver
{
    [LoggerMessage(
        EventId = Entry,
        Level = LogLevel.Information,
        Message = "Processing input: '{Input}'")]
    partial void LogReceiveEntry(IInput input);

    [LoggerMessage(
        EventId = Exit,
        Level = LogLevel.Information,
        Message = "Processing finished with result: {Response}")]
    partial void LogReceiveExit(ProcessingResult response);

    [LoggerMessage(
        EventId = ProcessorNotFound,
        Level = LogLevel.Warning,
        Message = "No processor found for input type '{InputType}'")]
    partial void LogProcessorNotFound(string inputType);

    [LoggerMessage(
        EventId = CriticalError,
        Level = LogLevel.Error,
        Message = "Critical error during input processing. Raw input: '{Input}'")]
    partial void LogCriticalSystemError(IInput input, Exception ex);
}