using MyFinanceTracker.InputProcessing;
using MyFinanceTracker.InputProcessing.Inputs;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace MyFinanceTracker.Interactions.Telegram;

internal static class TelegramInputBuilder
{
    public static async Task<TelegramInputBuilderResult> BuildInput(this Message message, ITelegramBotClient botClient, CancellationToken ct)
    {
        if (message.Text is { } text)
        {
            return new TelegramInputBuilderResult.Success(new TextInput(text));
        }

        if (message.Voice is { } voice)
        {
            var mimeType = voice.MimeType ?? "audio/ogg";
            return await DownloadAsInput(botClient, voice.FileId, stream => new AudioInput(stream, mimeType), ct);
        }

        if (message.Photo is { Length: > 0 } photos)
        {
            var photo = photos[^1];
            return await DownloadAsInput(botClient, photo.FileId, stream => new ImageInput(stream, "image/jpeg", message.Caption), ct);
        }

        return new TelegramInputBuilderResult.Unsupported();
    }

    private static async Task<TelegramInputBuilderResult> DownloadAsInput(
        ITelegramBotClient botClient,
        string fileId,
        Func<Stream, IInput> createInput,
        CancellationToken ct)
    {
        var stream = new MemoryStream();
        try
        {
            await botClient.GetInfoAndDownloadFile(
                fileId: fileId,
                destination: stream,
                cancellationToken: ct);

            stream.Position = 0;
            return new TelegramInputBuilderResult.Success(createInput(stream));
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            await stream.DisposeAsync();
            throw;
        }
        catch (Exception ex)
        {
            await stream.DisposeAsync();
            return new TelegramInputBuilderResult.Failed(ex);
        }
    }
}
