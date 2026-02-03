using System.Diagnostics;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MyFinanceTracker.CommandProcessing.Text;

namespace MyFinanceTracker.Interactions.Console;

internal sealed partial class ConsoleInteractionWorker(
    ITextCommandReceiver textCommandReceiver,
    ILogger<ConsoleInteractionWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
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
        using (logger.BeginScope("ID: {CorrelationId}", correlationId))
        {
            LogCommandReceived(input);

            try
            {
                var response = await textCommandReceiver.Receive(new TextCommandRequest(input), ct);
                HandleResponse(response);
            }
            catch (Exception ex)
            {
                LogCommandFailed(ex, input);
                ConsoleCommands.WriteError("Internal system failure.");
            }
        }
    }

    private static void HandleResponse(TextCommandResponse response)
    {
        switch (response)
        {
            case TextCommandResponse.Success success:
                ConsoleCommands.WriteSuccess(success);
                break;

            case TextCommandResponse.InvalidInput invalid:
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

            case TextCommandResponse.LogicError logicError:
                ConsoleCommands.WriteError(logicError.Message);
                break;

            case TextCommandResponse.SystemError systemError:
                ConsoleCommands.WriteError($"System failure: {systemError.Message}");
                break;

            default:
                throw new UnreachableException($"Unknown response type: {response.GetType()}");
        }
    }

    private static void PrintWelcomeMessage()
    {
        ConsoleCommands.WriteInfo(">>> Finance Tracker 2026 is active.");
        ConsoleCommands.WriteInfo(">>> Usage: add <type> <category?> <amounts> <date?>");
        ConsoleCommands.WriteInfo(">>> Example: add expense food 100 200");
    }
}