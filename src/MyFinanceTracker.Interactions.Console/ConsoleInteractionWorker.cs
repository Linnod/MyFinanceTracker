using System.Diagnostics;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MyFinanceTracker.InputProcessing.Text;

namespace MyFinanceTracker.Interactions.Console;

internal sealed partial class ConsoleInteractionWorker(
    ITextInputReceiver textCommandReceiver,
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

        var input = await ConsoleCommands.ReadLine(ct);
        if (string.IsNullOrWhiteSpace(input))
        {
            return;
        }

        var correlationId = Guid.NewGuid();
        using (logger.BeginScope("CorrelationId: {CorrelationId}", correlationId))
        {
            LogCommandReceived(input);

            try
            {
                var response = await textCommandReceiver.Receive(new TextInput(input), ct);
                HandleResponse(response);
            }
            catch (Exception ex)
            {
                LogCommandFailed(ex, input);
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

                if (invalid.Suggestion is not null)
                {
                    ConsoleCommands.WriteInfo($"💡 Did you mean: '{invalid.Suggestion}'?");
                }

                if (invalid.Examples is { Count: > 0 })
                {
                    ConsoleCommands.WriteInfo("💡 Examples:");
                    foreach (var example in invalid.Examples)
                    {
                        ConsoleCommands.WriteInfo($"  > {example}");
                    }
                }
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
        ConsoleCommands.WriteInfo(">>> Usage: t add <type> <category?> <amounts> <date?>");
        ConsoleCommands.WriteInfo(">>> Example: t add expense food 100 200");
    }
}