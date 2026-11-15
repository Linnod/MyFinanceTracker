using MyFinanceTracker.InputProcessing;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace MyFinanceTracker.Interactions.Telegram;

internal static class TelegramInputBuilder
{
    public static async Task<Input?> BuildInput(
        this Message message,
        ITelegramBotClient botClient,
        CancellationToken ct)
    {
        var context = ExtractContext(message);
        var caption = NullIfWhiteSpace(message.Caption);

        return message switch
        {
            { Text: { } text } => new Input.Text(text, context),
            { Voice: { } voice } => new Input.Audio(
                await DownloadFile(botClient, voice.FileId, ct),
                voice.MimeType ?? "audio/ogg",
                caption,
                context),
            { Photo: [.., var photo] } => new Input.Image(
                await DownloadFile(botClient, photo.FileId, ct),
                "image/jpeg",
                caption,
                context),
            _ => null
        };
    }

    private static string? ExtractContext(Message message)
    {
        string?[] candidates =
        [
            message.Quote?.Text,
            message.ReplyToMessage?.Text,
            message.ReplyToMessage?.Caption
        ];

        return candidates.FirstOrDefault(static value => !string.IsNullOrWhiteSpace(value));
    }

    private static string? NullIfWhiteSpace(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value;

    private static async Task<byte[]> DownloadFile(
        ITelegramBotClient botClient,
        string fileId,
        CancellationToken ct)
    {
        using var stream = new MemoryStream();
        await botClient.GetInfoAndDownloadFile(
            fileId: fileId,
            destination: stream,
            cancellationToken: ct);

        return stream.ToArray();
    }
}
