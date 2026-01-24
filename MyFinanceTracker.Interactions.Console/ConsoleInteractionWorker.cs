using MediatR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MyFinanceTracker.Interactions.Contracts;

namespace MyFinanceTracker.Interactions.Console;

internal sealed class ConsoleInteractionWorker(
    IMediator mediator,
    ILogger<ConsoleInteractionWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        PrintWelcomeMessage();
        logger.LogInformation("Worker started.");

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await ProcessNextCommandAsync(stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            ConsoleCommands.WriteInfo("\n>>> Shutdown signal received. Closing...");
            logger.LogInformation("Worker is stopping due to cancellation.");
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "Fatal error in worker loop.");
            ConsoleCommands.WriteError("[FATAL ERROR] The application crashed. See logs for details.");
            throw;
        }
        finally
        {
            logger.LogInformation("Worker stopped.");
        }
    }

    private async Task ProcessNextCommandAsync(CancellationToken ct)
    {
        System.Console.Write("\n> ");
        var input = System.Console.ReadLine();

        if (string.IsNullOrWhiteSpace(input))
        {
            return;
        }

        try
        {
            var result = await mediator.Send(new ProcessRawMessageCommand(input), ct);
            HandleResult(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing command: {Input}", input);
            ConsoleCommands.WriteError("[CRITICAL ERROR] Internal system failure.");
        }
        finally
        {
            System.Console.ResetColor();
        }
    }

    private void HandleResult(InteractionResult result)
    {
        result.Match(
            onSuccess: success =>
            {
                ConsoleCommands.WriteSuccess(success.Operation);
            },
            onParseError: parseError =>
            {
                ConsoleCommands.WriteError($"[PARSE ERROR] Could not understand: \"{parseError.RawInput}\"");
                ConsoleCommands.WriteInfo($"Hint: {parseError.Details}");
            },
            onLogicError: logicError =>
            {
                ConsoleCommands.WriteError($"[LOGIC ERROR] {logicError.Message}");
            },
            onSystemError: systemError =>
            {
                ConsoleCommands.WriteError($"[SYSTEM ERROR] {systemError.Message}");
                logger.LogDebug(systemError.Exception, "System failure details");
            }
        );
    }

    private static void PrintWelcomeMessage()
    {
        ConsoleCommands.WriteInfo(">>> Finance Tracker is active. Type your command (e.g., 'expense food 100')");
        ConsoleCommands.WriteInfo(">>> Press Ctrl+C to exit.");
    }
}