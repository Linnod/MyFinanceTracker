namespace MyFinanceTracker.Interactions.Api;

using static MyFinanceTracker.Interactions.Api.Logging.LogEvents.Worker;

internal static partial class ApiServerBuilder
{
    [LoggerMessage(
        EventId = RequestReceived,
        Level = LogLevel.Information,
        Message = "Processing HTTP {Method} request for '{Path}'")]
    public static partial void LogRequestReceived(ILogger logger, string method, PathString path);

    [LoggerMessage(
        EventId = RequestFinished,
        Level = LogLevel.Information,
        Message = "HTTP {Method} request for '{Path}' finished with status {StatusCode}")]
    public static partial void LogRequestFinished(ILogger logger, string method, PathString path, int statusCode);

    [LoggerMessage(
        EventId = UnauthorizedAccess,
        Level = LogLevel.Warning,
        Message = "Unauthorized access attempt to '{Path}'")]
    public static partial void LogUnauthorized(ILogger logger, PathString path);
}