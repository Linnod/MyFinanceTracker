using System.Net.Sockets;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MyFinanceTracker.InputProcessing.Text;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace MyFinanceTracker.Interactions.Telegram;

internal sealed partial class TelegramInteractionWorker(
    ITelegramBotClient botClient,
    ITextInputReceiver textCommandReceiver,
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
        if (update.Message is not { Text: { } messageText } message)
        {
            return;
        }

        var userId = message.From?.Id;
        var username = message.From?.Username ?? message.From?.FirstName ?? "Unknown";
        var correlationId = Guid.NewGuid();

        using (logger.BeginScope("TelegramUser: {TelegramUser}, UserId: {TelegramUserId}, CorrelationId: {CorrelationId}", 
                   username, userId ?? 0, correlationId))
        {
            LogUpdateReceived(messageText);

            if (userId != options.Value.AllowedUserId)
            {
                LogUnauthorized(userId);
                return;
            }

            var response = await textCommandReceiver.Receive(new TextInput(messageText), ct);
            var formattedText = TelegramResponseFormatter.FormatResponse(response);

            try
            {
                await bot.SendMessage(
                    chatId: message.Chat.Id,
                    text: formattedText,
                    parseMode: ParseMode.Html,
                    cancellationToken: ct);
            }
            catch (Exception ex)
            {
                LogMessageSendFailed(ex, messageText);
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