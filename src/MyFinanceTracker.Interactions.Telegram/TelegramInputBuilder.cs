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
            var stream = new MemoryStream();
            try
            {
                await botClient.GetInfoAndDownloadFile(
                    fileId: voice.FileId,
                    destination: stream,
                    cancellationToken: ct);

                stream.Position = 0;
                var mimeType = voice.MimeType ?? "audio/ogg";

                return new TelegramInputBuilderResult.Success(new AudioInput(stream, mimeType));
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

        return new TelegramInputBuilderResult.Unsupported();
    }
}
