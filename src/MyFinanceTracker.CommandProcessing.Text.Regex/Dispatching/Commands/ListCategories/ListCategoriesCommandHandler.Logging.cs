using Microsoft.Extensions.Logging;

namespace MyFinanceTracker.CommandProcessing.Text.Regex.Dispatching.Commands.ListCategories;

using static MyFinanceTracker.CommandProcessing.Text.Regex.Logging.LogEvents.ListCategoriesCommandHandler;

internal sealed partial class ListCategoriesCommandHandler
{
    [LoggerMessage(
        EventId = Entry,
        Level = LogLevel.Debug,
        Message = "--> Executing list categories command")]
    private partial void LogCommandHandlerEntry();

    [LoggerMessage(
        EventId = Exit,
        Level = LogLevel.Debug,
        Message = "<-- Finished with result: {Response}")]
    private partial void LogCommandHandlerExit(TextCommandResponse response);
}
