using System.Diagnostics;
using System.Net;
using System.Text;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MyFinanceTracker.CommandProcessing.Text;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace MyFinanceTracker.Interactions.Telegram;

internal sealed partial class TelegramInteractionWorker(
    ITelegramBotClient botClient,
    ITextCommandReceiver textCommandReceiver,
    IOptions<TelegramInteractionOptions> options,
    ILogger<TelegramInteractionWorker> logger) : BackgroundService
{
    private readonly TelegramInteractionOptions _options = options.Value;

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

        using (logger.BeginScope(new Dictionary<string, object>
        {
            ["TelegramUserId"] = userId ?? 0,
            ["TelegramUser"] = username
        }))
        {
            LogUpdateReceived(messageText);

            if (userId != _options.AllowedUserId)
            {
                LogUnauthorized(userId);
                return;
            }

            var response = await textCommandReceiver.Receive(new TextCommandRequest(messageText), ct);
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
                LogMessageSendFailed(ex, messageText);
            }
        }
    }

    private Task HandlePollingError(ITelegramBotClient bot, Exception ex, CancellationToken ct)
    {
        LogPollingError(ex);
        return Task.CompletedTask;
    }

    private static string FormatResponse(TextCommandResponse response) => response switch
    {
        TextCommandResponse.Success success => FormatSuccess(success),

        TextCommandResponse.InvalidInput invalid => BuildInvalidInputMessage(invalid),

        TextCommandResponse.LogicError logicError => $"""
            ❌ <b>Logic Error</b>
            {logicError.Message}
            """,

        TextCommandResponse.SystemError systemError => $"""
            🔌 <b>System Hiccup</b>
            Something went wrong. We're looking into it.
            <i>Ref: {systemError.Message}</i>
            """,

        _ => throw new UnreachableException($"Unknown response type: {response.GetType()}")
    };

    private static string BuildInvalidInputMessage(TextCommandResponse.InvalidInput invalid)
    {
        var sb = new StringBuilder();
        sb.AppendLine("⚠️ <b>Input Error</b>");

        sb.AppendLine(WebUtility.HtmlEncode(invalid.Details));
        sb.AppendLine();

        if (invalid.Suggestion is not null)
        {
            sb.AppendLine($"💡 Did you mean: <code>{WebUtility.HtmlEncode(invalid.Suggestion)}</code>?");
        }

        if (invalid.Examples is { Count: > 0 })
        {
            sb.AppendLine("<b>Try like this:</b>");
            foreach (var example in invalid.Examples)
            {
                sb.AppendLine($"• <code>{WebUtility.HtmlEncode(example)}</code>");
            }
        }

        return sb.ToString();
    }

    private static string FormatSuccess(TextCommandResponse.Success success)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"✅ <b>{success.CommandDescription.ToUpper()}</b>");
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