namespace MyFinanceTracker.Interactions.Telegram;

internal interface ITelegramMessageSender
{
    Task Send(string text, CancellationToken ct);
}
