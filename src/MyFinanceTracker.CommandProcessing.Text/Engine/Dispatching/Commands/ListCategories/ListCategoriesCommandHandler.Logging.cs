using Microsoft.Extensions.Logging;

namespace MyFinanceTracker.CommandProcessing.Text.Engine.Dispatching.Commands.ListCategories;

using static MyFinanceTracker.CommandProcessing.Text.Logging.LogEvents.ListCategoriesCommandHandler;

internal sealed partial class ListCategoriesCommandHandler
{
    [LoggerMessage(
        EventId = Entry,
        Level = LogLevel.Debug,
        Message = "--> Handling payload: '{Payload}'")]
    partial void LogCommandHandlerEntry(string payload);

    [LoggerMessage(
        EventId = SystemError,
        Level = LogLevel.Error,
        Message = "!! System failure for input: {Input}")]
    partial void LogSystemError(string input, Exception ex);

    [LoggerMessage(
        EventId = Exit,
        Level = LogLevel.Debug,
        Message = "<-- Finished with result: {Response}")]
    partial void LogCommandHandlerExit(TextCommandResponse response);
}
