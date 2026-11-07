using System.Diagnostics;
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
        var username = message.From?.Username ?? message.From?.FirstName ?? "Unknown";
        var correlationId = Guid.NewGuid();

        using (logger.BeginScope("TelegramUser: {TelegramUser}, UserId: {TelegramUserId}, CorrelationId: {CorrelationId}",
                   username, userId ?? 0, correlationId))
        {
            if (userId != options.Value.AllowedUserId)
            {
                LogUnauthorized(userId);
                return;
            }

            await using var scope = scopeFactory.CreateAsyncScope();
            var sender = scope.ServiceProvider.GetRequiredService<ISender>();

            var responseText = await ProcessMessage(bot, message, sender, ct);

            try
            {
                await bot.SendMessage(
                    chatId: message.Chat.Id,
                    text: responseText,
                    parseMode: ParseMode.Html,
                    cancellationToken: ct);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                LogMessageSendFailed(ex, message.Chat.Id, message.Id);

                try
                {
                    await bot.SendMessage(
                        chatId: message.Chat.Id,
                        text: "⚠️ Failed to send response. The message may have been too long or an unexpected error occurred. Please try a narrower query.",
                        cancellationToken: ct);
                }
                catch
                {
                    // Ignore secondary failure
                }
            }
        }
    }

    private async Task<string> ProcessMessage(
        ITelegramBotClient bot,
        Message message,
        ISender sender,
        CancellationToken ct)
    {
        var buildResult = await message.BuildInput(bot, ct);

        switch (buildResult)
        {
            case TelegramInputBuilderResult.Success success:
                {
                    var input = success.Input;

                    LogUpdateReceived(input);

                    var response = await sender.Send(input, ct);
                    return TelegramResponseFormatter.FormatResponse(response);
                }

            case TelegramInputBuilderResult.Unsupported:
                return "Unsupported input. Please send a text, voice, or photo message.";

            case TelegramInputBuilderResult.Failed failed:
                LogInputBuildFailed(failed.Exception);
                return "Failed to prepare your input. Please try again.";

            default:
                throw new UnreachableException(
                    $"Unknown input builder result: {buildResult.GetType()}");
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
