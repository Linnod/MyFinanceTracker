using MyFinanceTracker.InputProcessing;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace MyFinanceTracker.Interactions.Telegram;

internal static class TelegramInputBuilder
{
    public static async Task<TelegramInputBuilderResult> BuildInput(this Message message, ITelegramBotClient botClient, CancellationToken ct)
    {
        if (message.Text is { } text)
        {
            return new TelegramInputBuilderResult.Success(new Input.Text(text));
        }

        if (message.Voice is { } voice)
        {
            var mimeType = voice.MimeType ?? "audio/ogg";
            return await DownloadAsInput(botClient, voice.FileId, bytes => new Input.Audio(bytes, mimeType), ct);
        }

        if (message.Photo is { Length: > 0 } photos)
        {
            var photo = photos[^1];
            return await DownloadAsInput(botClient, photo.FileId, bytes => new Input.Image(bytes, "image/jpeg", message.Caption), ct);
        }

        return new TelegramInputBuilderResult.Unsupported();
    }

    private static async Task<TelegramInputBuilderResult> DownloadAsInput(
        ITelegramBotClient botClient,
        string fileId,
        Func<byte[], Input> createInput,
        CancellationToken ct)
    {
        using var stream = new MemoryStream();
        try
        {
            await botClient.GetInfoAndDownloadFile(
                fileId: fileId,
                destination: stream,
                cancellationToken: ct);

            return new TelegramInputBuilderResult.Success(createInput(stream.ToArray()));
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            return new TelegramInputBuilderResult.Failed(ex);
        }
    }
}
