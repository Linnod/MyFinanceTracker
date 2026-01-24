using MediatR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MyFinanceTracker.Interactions.Contracts;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace MyFinanceTracker.Interactions.Telegram;

internal sealed class TelegramInteractionWorker(
    ITelegramBotClient botClient,
    IMediator mediator,
    IOptions<TelegramInteractionOptions> options,
    ILogger<TelegramInteractionWorker> logger) : BackgroundService
{
    private readonly TelegramInteractionOptions options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Telegram Interaction Worker started using Long Polling.");

        var receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = [UpdateType.Message],
            DropPendingUpdates = true
        };

        botClient.StartReceiving(
            updateHandler: HandleUpdateAsync,
            errorHandler: HandlePollingErrorAsync,
            receiverOptions: receiverOptions,
            cancellationToken: stoppingToken
        );

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private async Task HandleUpdateAsync(ITelegramBotClient bot, Update update, CancellationToken ct)
    {
        if (update.Message is not { Text: { } messageText } message)
            return;

        if (message.From?.Id != options.AllowedUserId)
        {
            logger.LogWarning("Unauthorized access attempt. UserID: {UserId}", message.From?.Id);
            return;
        }

        logger.LogInformation("Message received from {UserId}: '{Text}'", message.From?.Id, messageText);

        var result = await mediator.Send(new ProcessRawMessageCommand(messageText), ct);
        var response = FormatResponseMessage(result);
        try
        {
            await bot.SendMessage(
                chatId: message.Chat.Id,
                text: response,
                parseMode: ParseMode.Html,
                cancellationToken: ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send message to Telegram. Response was: {Response}", response);
        }
    }

    private static string FormatResponseMessage(InteractionResult result)
    {
        return result.Match(
            onSuccess: s =>
            {
                var op = s.Operation;
                var amountsLine = op.Amounts.Length > 1
                    ? $"{string.Join(" + ", op.Amounts)} = "
                    : "";

                return $"""
                ✅ **Recorded!**
                💰 {amountsLine}{op.Amounts.Sum()}€
                📂 Category: `{op.CategoryAlias}`
                📅 Date: {op.Date:dd/MM/yyyy}
                """;
            },

            onParseError: e => $"""
            ❓ **I didn't get that**
            Input: `{e.RawInput}`
            Hint: {e.Details}
            """,

            onLogicError: e => $"""
            ⚠️ **Logic Error**
            {e.Message}
            """,

            onSystemError: _ => $"""
            🔌 **System hiccup**
            Something went wrong on my side. 
            I've logged the details. Please try again in a bit.
            """
        );
    }

    private Task HandlePollingErrorAsync(ITelegramBotClient bot, Exception ex, CancellationToken ct)
    {
        logger.LogError(ex, "Telegram Bot API polling error.");
        return Task.CompletedTask;
    }
}