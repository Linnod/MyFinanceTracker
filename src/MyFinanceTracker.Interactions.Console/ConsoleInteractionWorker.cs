using System.Diagnostics;
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
        logger.LogInformation("Console worker started.");

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
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "Fatal error in worker loop.");
            ConsoleCommands.WriteError("The application crashed. See logs for details.");
        }
    }

    private async Task ProcessNextCommandAsync(CancellationToken ct)
    {
        System.Console.Write("\n> ");

        var input = System.Console.ReadLine();
        if (string.IsNullOrWhiteSpace(input)) return;

        try
        {
            var response = await mediator.Send(new InteractionRequest(input), ct);
            HandleResponse(response);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing interaction for input: {Input}", input);
            ConsoleCommands.WriteError("Internal system failure.");
        }
        finally
        {
            System.Console.ResetColor();
        }
    }

    private static void HandleResponse(InteractionResponse response)
    {
        switch (response)
        {
            case InteractionResponse.Success success:
                ConsoleCommands.WriteSuccess(success);
                break;

            case InteractionResponse.UnrecognizedInteraction unrecognized:
                ConsoleCommands.WriteError($"Unknown command: '{unrecognized.RawInput}'");
                ConsoleCommands.WriteInfo("Hint: Try starting with 'add expense ...'");
                break;

            case InteractionResponse.InvalidInput invalidInput:
                ConsoleCommands.WriteError($"Invalid input for '{invalidInput.InteractionDescription}'");
                ConsoleCommands.WriteInfo($"Details: {invalidInput.Details}");
                break;

            case InteractionResponse.LogicError logicError:
                ConsoleCommands.WriteError(logicError.Message);
                break;

            case InteractionResponse.SystemError systemError:
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