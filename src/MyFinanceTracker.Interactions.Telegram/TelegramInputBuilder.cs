using MyFinanceTracker.InputProcessing;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace MyFinanceTracker.Interactions.Telegram;

internal static class TelegramInputBuilder
{
    public const long MaxFileSizeBytes = 20L * 1024 * 1024;

    public static readonly IReadOnlyList<DocumentFormat> SupportedDocumentFormats =
    [
        new("application/pdf", "PDF", ".pdf")
    ];

    private const string DefaultAudioMimeType = "audio/ogg";
    private const string PhotoMimeType = "image/jpeg";

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
            { Voice: { } voice } => await BuildMediaInput(
                botClient,
                voice,
                data => new Input.Audio(data, voice.MimeType ?? DefaultAudioMimeType, caption, context),
                ct),
            { Photo: [.., var photo] } => await BuildMediaInput(
                botClient,
                photo,
                data => new Input.Image(data, PhotoMimeType, caption, context),
                ct),
            { Animation: null, Document: { } document } => await BuildDocumentInput(
                botClient,
                document,
                caption,
                context,
                ct),
            _ => null
        };
    }

    private static Task<Input?> BuildDocumentInput(
        ITelegramBotClient botClient,
        Document document,
        string? caption,
        string? context,
        CancellationToken ct)
    {
        var format = ResolveDocumentFormat(document);
        if (format is null)
        {
            return Task.FromResult<Input?>(null);
        }

        return BuildMediaInput(
            botClient,
            document,
            data => new Input.Document(data, format.ContentType, caption, context),
            ct);
    }

    private static async Task<Input?> BuildMediaInput(
        ITelegramBotClient botClient,
        FileBase file,
        Func<byte[], Input> createInput,
        CancellationToken ct)
    {
        if (file.FileSize > MaxFileSizeBytes)
        {
            return null;
        }

        var data = await DownloadFile(botClient, file.FileId, ct);
        return createInput(data);
    }

    private static DocumentFormat? ResolveDocumentFormat(Document document)
    {
        if (!string.IsNullOrWhiteSpace(document.MimeType))
        {
            var byMime = SupportedDocumentFormats.FirstOrDefault(
                f => document.MimeType.Contains(f.ContentType, StringComparison.OrdinalIgnoreCase));
            if (byMime is not null)
            {
                return byMime;
            }
        }

        var extension = Path.GetExtension(document.FileName?.Trim());
        return string.IsNullOrEmpty(extension)
            ? null
            : SupportedDocumentFormats.FirstOrDefault(
                f => string.Equals(f.Extension, extension, StringComparison.OrdinalIgnoreCase));
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
