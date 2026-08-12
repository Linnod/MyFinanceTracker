using Microsoft.Extensions.Logging;

namespace MyFinanceTracker.Interactions.Console;

using static MyFinanceTracker.Interactions.Console.Logging.LogEvents.Worker;

internal sealed partial class ConsoleInteractionWorker
{
    [LoggerMessage(
        EventId = Started,
        Level = LogLevel.Information,
        Message = "Console interaction worker started")]
    partial void LogStarted();

    [LoggerMessage(
        EventId = CommandReceived,
        Level = LogLevel.Information,
        Message = "Received command '{Input}'")]
    partial void LogCommandReceived(string input);

    [LoggerMessage(
        EventId = ProcessingError,
        Level = LogLevel.Error,
        Message = "Error processing command '{Input}'")]
    partial void LogCommandFailed(Exception ex, string input);

    [LoggerMessage(
        EventId = FatalError,
        Level = LogLevel.Critical,
        Message = "Worker loop crashed unexpectedly")]
    partial void LogLoopCrashed(Exception ex);
}