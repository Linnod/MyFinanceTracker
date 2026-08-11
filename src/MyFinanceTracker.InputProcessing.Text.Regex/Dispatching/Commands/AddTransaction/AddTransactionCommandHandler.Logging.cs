using Microsoft.Extensions.Logging;

namespace MyFinanceTracker.InputProcessing.Text.Regex.Dispatching.Commands.AddTransaction;

using static MyFinanceTracker.InputProcessing.Text.Regex.Logging.LogEvents.Commands.Add;

internal sealed partial class AddTransactionCommandHandler
{
    [LoggerMessage(
        EventId = Entry,
        Level = LogLevel.Debug,
        Message = "--> Handling: '{Command}'")]
    partial void LogHandlerEntry(AddTransactionCommand command);

    [LoggerMessage(
        EventId = Exit,
        Level = LogLevel.Debug,
        Message = "<-- Finished with result: {Action}")]
    partial void LogHandlerExit(ActionResult action);
}