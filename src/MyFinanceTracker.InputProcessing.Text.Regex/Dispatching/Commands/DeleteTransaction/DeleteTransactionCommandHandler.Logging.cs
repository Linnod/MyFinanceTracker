using Microsoft.Extensions.Logging;

namespace MyFinanceTracker.InputProcessing.Text.Regex.Dispatching.Commands.DeleteTransaction;

using static MyFinanceTracker.InputProcessing.Text.Regex.Logging.LogEvents.Commands.Delete;

internal sealed partial class DeleteTransactionCommandHandler
{
    [LoggerMessage(
        EventId = Entry,
        Level = LogLevel.Debug,
        Message = "--> Handling: '{Command}'")]
    partial void LogHandlerEntry(DeleteTransactionCommand command);

    [LoggerMessage(
        EventId = Exit,
        Level = LogLevel.Debug,
        Message = "<-- Finished with result: {Action}")]
    partial void LogHandlerExit(ActionResult action);
}