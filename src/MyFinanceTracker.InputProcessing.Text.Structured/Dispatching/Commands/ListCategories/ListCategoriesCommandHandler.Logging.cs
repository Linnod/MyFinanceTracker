using Microsoft.Extensions.Logging;

namespace MyFinanceTracker.InputProcessing.Text.Structured.Dispatching.Commands.ListCategories;

using static MyFinanceTracker.InputProcessing.Text.Structured.Logging.LogEvents.Commands.ListCategories;

internal sealed partial class ListCategoriesCommandHandler
{
    [LoggerMessage(
        EventId = Entry,
        Level = LogLevel.Debug,
        Message = "Executing list categories command")]
    private partial void LogHandlerEntry();

    [LoggerMessage(
        EventId = Exit,
        Level = LogLevel.Debug,
        Message = "Finished with result: {Result}")]
    private partial void LogHandlerExit(CommandExecutionResult result);
}