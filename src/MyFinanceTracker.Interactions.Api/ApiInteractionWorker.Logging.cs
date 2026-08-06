namespace MyFinanceTracker.Interactions.Api;

using static MyFinanceTracker.Interactions.Api.Logging.LogEvents.Worker;

internal sealed partial class ApiInteractionWorker
{
    [LoggerMessage(
        EventId = Started,
        Level = LogLevel.Information,
        Message = "Starting REST Web API on port {Port}")]
    private partial void LogStarted(int port);

    [LoggerMessage(
        EventId = Stopped,
        Level = LogLevel.Information,
        Message = "REST Web API stopped")]
    private partial void LogStopped();
}