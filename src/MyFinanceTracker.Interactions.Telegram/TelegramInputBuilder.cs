using MyFinanceTracker.InputProcessing;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace MyFinanceTracker.Interactions.Telegram;

internal static class TelegramInputBuilder
{
    public static async Task<Input?> BuildInput(
        this Message message,
        ITelegramBotClient botClient,
        CancellationToken ct) => message switch
        {
            { Text: { } text } => new Input.Text(text),
            { Voice: { } voice } => new Input.Audio(
                await DownloadFile(botClient, voice.FileId, ct),
                voice.MimeType ?? "audio/ogg"),
            { Photo: [.., var photo] } => new Input.Image(
                await DownloadFile(botClient, photo.FileId, ct),
                "image/jpeg",
                message.Caption),
            _ => null
        };

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
