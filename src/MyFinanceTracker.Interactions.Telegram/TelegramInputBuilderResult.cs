using MyFinanceTracker.InputProcessing;

namespace MyFinanceTracker.Interactions.Telegram;

internal abstract record TelegramInputBuilderResult
{
    private TelegramInputBuilderResult() { }

    public sealed record Success(Input Input) : TelegramInputBuilderResult;
    public sealed record Unsupported : TelegramInputBuilderResult;
    public sealed record Failed(Exception Exception) : TelegramInputBuilderResult;
}
