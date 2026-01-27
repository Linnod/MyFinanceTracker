using System.Diagnostics;
using System.Text;
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
    IInteractionGateway interactionGateway,
    IOptions<TelegramInteractionOptions> options,
    ILogger<TelegramInteractionWorker> logger) : BackgroundService
{
    private readonly TelegramInteractionOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Telegram Interaction Worker started.");

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

        if (message.From?.Id != _options.AllowedUserId)
        {
            logger.LogWarning("Unauthorized access attempt. UserID: {UserId}", message.From?.Id);
            return;
        }

        var response = await interactionGateway.Send(new InteractionRequest(messageText), ct);
        var formattedText = FormatResponse(response);
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
            logger.LogError(ex, "Failed to send Telegram message. Input was: {Input}", messageText);
        }
    }

    private static string FormatResponse(InteractionResponse response) => response switch
    {
        InteractionResponse.Success success => FormatSuccess(success),

        InteractionResponse.UnrecognizedInteraction unrecognized => $"""
        ❓ <b>I didn't quite get that...</b>
        Input: <code>{unrecognized.RawInput}</code>
        
        <i>Hint: Try starting with 'add', e.g., 'add expense food 100'</i>
        """,

        InteractionResponse.InvalidInput invalid => $"""
        ⚠️ <b>Input Error: {invalid.InteractionDescription}</b>
        {invalid.Details}
        """,

        InteractionResponse.LogicError logicError => $"""
        ❌ <b>Logic Error:</b>
        {logicError.Message}
        """,

        InteractionResponse.SystemError systemError => $"""
        🔌 <b>System Hiccup</b>
        Something went wrong on our side. We're already looking into it.
        <i>Error ref: {systemError.Message}</i>
        """,

        _ => throw new UnreachableException($"Unknown response type: {response.GetType()}")
    };

    private static string FormatSuccess(InteractionResponse.Success success)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"✅ <b>{success.InteractionDescription.ToUpper()}</b>");
        builder.AppendLine($"Result: <b>{success.PrimaryValue}</b>");
        builder.AppendLine();

        foreach (var detail in success.Details)
        {
            var icon = detail.Icon ?? "🔹";
            builder.AppendLine($"{icon} {detail.Name}: <code>{detail.Value}</code>");
        }

        return builder.ToString();
    }

    private Task HandlePollingErrorAsync(ITelegramBotClient bot, Exception ex, CancellationToken ct)
    {
        logger.LogError(ex, "Telegram Bot API polling error.");
        return Task.CompletedTask;
    }
}