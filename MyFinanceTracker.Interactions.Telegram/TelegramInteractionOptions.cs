using System.ComponentModel.DataAnnotations;

namespace MyFinanceTracker.Interactions.Telegram;

internal sealed class TelegramInteractionOptions
{
    public const string SectionName = "Telegram";

    [Required(AllowEmptyStrings = false)]
    public string Token { get; init; } = string.Empty;

    [Range(1, long.MaxValue)]
    public long AllowedUserId { get; init; }
}