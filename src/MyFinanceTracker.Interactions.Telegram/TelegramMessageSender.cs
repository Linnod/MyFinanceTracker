using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;

namespace MyFinanceTracker.Interactions.Telegram;

internal sealed partial class TelegramMessageSender(
    ITelegramBotClient botClient,
    IOptions<TelegramInteractionOptions> options,
    ILogger<TelegramMessageSender> logger) : ITelegramMessageSender
{
    public async Task Send(string text, CancellationToken ct)
    {
        try
        {
            await botClient.SendMessage(
                chatId: options.Value.AllowedUserId,
                text: text,
                parseMode: ParseMode.Html,
                cancellationToken: ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            LogSendFailed(ex);
        }
    }
}
