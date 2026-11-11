using System.Net.Sockets;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace MyFinanceTracker.Interactions.Telegram;

internal sealed partial class TelegramInteractionWorker(
    ITelegramBotClient botClient,
    IServiceScopeFactory scopeFactory,
    IOptions<TelegramInteractionOptions> options,
    ITelegramMessageSender messageSender,
    ILogger<TelegramInteractionWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        LogStarted();

        var receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = [UpdateType.Message],
            DropPendingUpdates = true
        };

        botClient.StartReceiving(
            updateHandler: HandleUpdate,
            errorHandler: HandlePollingError,
            receiverOptions: receiverOptions,
            cancellationToken: stoppingToken
        );

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private async Task HandleUpdate(ITelegramBotClient bot, Update update, CancellationToken ct)
    {
        if (update.Message is not { } message)
        {
            return;
        }

        var userId = message.From?.Id;
        if (userId != options.Value.AllowedUserId)
        {
            LogUnauthorized(userId);
            return;
        }

        using (logger.BeginScope("CorrelationId: {CorrelationId}", Guid.NewGuid()))
        {
            try
            {
                LogIncomingMessage(message.Type, message.Id);

                var input = await message.BuildInput(bot, ct);
                if (input is null)
                {
                    await messageSender.Send("Unsupported input. Please send a text, voice, or photo message.", ct);
                    return;
                }

                LogUpdateReceived(input);

                await using var scope = scopeFactory.CreateAsyncScope();
                var sender = scope.ServiceProvider.GetRequiredService<ISender>();

                var result = await sender.Send(input, ct);
                var responseText = TelegramResponseFormatter.FormatResponse(result);

                await messageSender.Send(responseText, ct);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                LogUpdateProcessingFailed(ex, message.Id);
                await messageSender.Send("Something went wrong. Please try again.", ct);
            }
        }
    }

    private Task HandlePollingError(ITelegramBotClient bot, Exception ex, CancellationToken ct)
    {
        if (ex.GetBaseException() is SocketException
            {
                SocketErrorCode: SocketError.ConnectionReset
            })
        {
            return Task.CompletedTask;
        }

        LogPollingError(ex);
        return Task.CompletedTask;
    }
}
