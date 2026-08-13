using Microsoft.Extensions.Logging;

namespace MyFinanceTracker.InputProcessing.Text.Structured.Dispatching.Commands.DeleteTransaction;

using static MyFinanceTracker.InputProcessing.Text.Structured.Logging.LogEvents.Commands.Delete;

internal sealed partial class DeleteTransactionCommandHandler
{
    [LoggerMessage(
        EventId = Entry,
        Level = LogLevel.Debug,
        Message = "Handling delete transaction command '{Command}'")]
    partial void LogHandlerEntry(DeleteTransactionCommand command);

    [LoggerMessage(
        EventId = Exit,
        Level = LogLevel.Debug,
        Message = "Handling finished with result: {Result}")]
    partial void LogHandlerExit(CommandExecutionResult result);
}