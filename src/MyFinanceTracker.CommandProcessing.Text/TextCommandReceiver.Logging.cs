using Microsoft.Extensions.Logging;

namespace MyFinanceTracker.CommandProcessing.Text;

using static MyFinanceTracker.CommandProcessing.Text.Logging.LogEvents.Receiver;

public sealed partial class TextCommandReceiver
{
    [LoggerMessage(
        EventId = Entry,
        Level = LogLevel.Information,
        Message = "--> Processing command: '{Request}'")]
    partial void LogReceiveEntry(TextCommandRequest request);

    [LoggerMessage(
        EventId = Exit,
        Level = LogLevel.Information,
        Message = "<-- Result: {Response}")]
    partial void LogReceiveExit(TextCommandResponse response);

    [LoggerMessage(
        EventId = CriticalError,
        Level = LogLevel.Error,
        Message = "!!! Critical error during command processing. Raw input: '{Text}'")]
    partial void LogCriticalSystemError(string text, Exception ex);
}
