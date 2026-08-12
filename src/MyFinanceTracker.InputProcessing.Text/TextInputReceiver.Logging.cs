using Microsoft.Extensions.Logging;

namespace MyFinanceTracker.InputProcessing.Text;

using static MyFinanceTracker.InputProcessing.Text.Logging.LogEvents.Receiver;

public sealed partial class TextInputReceiver
{
    [LoggerMessage(
        EventId = Entry,
        Level = LogLevel.Information,
        Message = "Processing input '{Input}'")]
    partial void LogReceiveEntry(TextInput input);

    [LoggerMessage(
        EventId = Exit,
        Level = LogLevel.Information,
        Message = "Processing finished with result: {Response}")]
    partial void LogReceiveExit(ProcessingResult response);

    [LoggerMessage(
        EventId = CriticalError,
        Level = LogLevel.Error,
        Message = "Critical error during command processing. Raw input: '{Input}'")]
    partial void LogCriticalSystemError(TextInput input, Exception ex);
}