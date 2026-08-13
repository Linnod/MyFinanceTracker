using Microsoft.Extensions.Logging;

namespace MyFinanceTracker.InputProcessing.Text.Structured.Dispatching.Commands.AddTransaction;

using static MyFinanceTracker.InputProcessing.Text.Structured.Logging.LogEvents.Commands.Add;

internal sealed partial class AddTransactionCommandHandler
{
    [LoggerMessage(
        EventId = Entry,
        Level = LogLevel.Debug,
        Message = "Handling add transaction command '{Command}'")]
    partial void LogHandlerEntry(AddTransactionCommand command);

    [LoggerMessage(
        EventId = Exit,
        Level = LogLevel.Debug,
        Message = "Handling finished with result: {Result}")]
    partial void LogHandlerExit(CommandExecutionResult result);
}