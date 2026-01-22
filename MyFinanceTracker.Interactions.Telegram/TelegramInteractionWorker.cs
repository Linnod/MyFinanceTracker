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

        try
        {
            var result = await mediator.Send(new ProcessRawMessageCommand(messageText), ct);
            var response = result.Match(
                op => $"✅ Recorded in **{op.CategoryAlias}**: {string.Join(", ", op.Amounts)} on {op.Date:dd/MM/yyyy}",
                error => $"❌ Error: {error}"
            );
            await bot.SendMessage(
                chatId: message.Chat.Id,
                text: response,
                cancellationToken: ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while processing message: '{Text}'", messageText);

            await bot.SendMessage(
                chatId: message.Chat.Id,
                text: "⚠️ Inner error. Check logs.",
                cancellationToken: ct);
        }
    }

    private Task HandlePollingErrorAsync(ITelegramBotClient bot, Exception ex, CancellationToken ct)
    {
        logger.LogError(ex, "Telegram Bot API polling error.");
        return Task.CompletedTask;
    }
}