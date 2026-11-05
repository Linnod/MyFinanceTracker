using System.Diagnostics;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MyFinanceTracker.InputProcessing;

namespace MyFinanceTracker.Interactions.Console;

internal sealed partial class ConsoleInteractionWorker(
    IInputReceiver inputReceiver,
    ILogger<ConsoleInteractionWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        System.Console.InputEncoding = System.Text.Encoding.UTF8;
        System.Console.OutputEncoding = System.Text.Encoding.UTF8;

        LogStarted();
        PrintWelcomeMessage();

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await ProcessNextCommand(stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            ConsoleCommands.WriteInfo("\n>>> Shutdown signal received. Closing...");
        }
        catch (Exception ex)
        {
            LogLoopCrashed(ex);
            ConsoleCommands.WriteError("The application crashed. See logs for details.");
        }
    }

    private async Task ProcessNextCommand(CancellationToken ct)
    {
        System.Console.WriteLine();
        System.Console.Write("> ");

        var rawLine = await ConsoleCommands.ReadLine(ct);
        if (string.IsNullOrWhiteSpace(rawLine))
        {
            return;
        }

        var correlationId = Guid.NewGuid();
        using (logger.BeginScope("CorrelationId: {CorrelationId}", correlationId))
        {
            LogCommandReceived(rawLine);

            try
            {
                var input = new Input.Text(rawLine);
                var response = await inputReceiver.Receive(input, ct);
                HandleResponse(response);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                LogCommandFailed(ex, rawLine);
                ConsoleCommands.WriteError("Internal system failure.");
            }
        }
    }

    private static void HandleResponse(ProcessingResult response)
    {
        switch (response)
        {
            case ProcessingResult.Completed completed:
                ConsoleCommands.WriteCompleted(completed);
                break;

            case ProcessingResult.InvalidInput invalid:
                ConsoleCommands.WriteError($"Input error: {invalid.Details}");
                break;

            case ProcessingResult.SystemError systemError:
                ConsoleCommands.WriteError($"System failure: {systemError.Message}");
                break;

            default:
                throw new UnreachableException($"Unknown response type: {response.GetType()}");
        }
    }

    private static void PrintWelcomeMessage()
    {
        ConsoleCommands.WriteInfo(">>> Finance Tracker 2026 is active.");
        ConsoleCommands.WriteInfo(">>> Example: Spent 15.50 on groceries yesterday");
    }
}
