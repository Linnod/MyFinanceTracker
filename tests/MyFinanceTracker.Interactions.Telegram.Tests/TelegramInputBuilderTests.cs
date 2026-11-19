using FluentAssertions;
using MyFinanceTracker.InputProcessing;
using NSubstitute;
using Telegram.Bot;
using Telegram.Bot.Requests.Abstractions;
using Telegram.Bot.Types;

namespace MyFinanceTracker.Interactions.Telegram.Tests;

public sealed class TelegramInputBuilderTests
{
    private static readonly byte[] downloadedBytes = [10, 20, 30];

    private readonly ITelegramBotClient botClient = Substitute.For<ITelegramBotClient>();

    public TelegramInputBuilderTests()
    {
        botClient.SendRequest(Arg.Any<IRequest<TGFile>>(), Arg.Any<CancellationToken>())
            .Returns(new TGFile { FilePath = "voice/file.oga" });

        botClient.When(x => x.DownloadFile(Arg.Any<string>(), Arg.Any<Stream>(), Arg.Any<CancellationToken>()))
            .Do(call => call.ArgAt<Stream>(1).Write(downloadedBytes));
    }

    [Fact]
    public async Task BuildInput_TextWithoutReply_ReturnsTextWithNullContext()
    {
        // arrange
        var message = new Message { Text = "coffee 5" };

        // act
        var result = await message.BuildInput(botClient, CancellationToken.None);

        // assert
        result.Should().Be(new Input.Text("coffee 5"));
    }

    [Fact]
    public async Task BuildInput_TextWithReplyText_SetsRawReplyTextAsContext()
    {
        // arrange
        var message = new Message
        {
            Text = "change amount to 10",
            ReplyToMessage = new Message { Text = "Created transaction\n11111111-1111-1111-1111-111111111111" }
        };

        // act
        var result = await message.BuildInput(botClient, CancellationToken.None);

        // assert
        result.Should().Be(new Input.Text(
            "change amount to 10",
            "Created transaction\n11111111-1111-1111-1111-111111111111"));
    }

    [Fact]
    public async Task BuildInput_TextWithReplyCaption_SetsRawReplyCaptionAsContext()
    {
        // arrange
        var message = new Message
        {
            Text = "delete it",
            ReplyToMessage = new Message { Caption = "Receipt 42.00" }
        };

        // act
        var result = await message.BuildInput(botClient, CancellationToken.None);

        // assert
        result.Should().Be(new Input.Text("delete it", "Receipt 42.00"));
    }

    [Fact]
    public async Task BuildInput_TextWithQuoteAndReplyText_PrioritizesQuoteTextAsContext()
    {
        // arrange
        var message = new Message
        {
            Text = "update amount",
            Quote = new TextQuote { Text = "Selected transaction 123" },
            ReplyToMessage = new Message { Text = "Full message with multiple transactions" }
        };

        // act
        var result = await message.BuildInput(botClient, CancellationToken.None);

        // assert
        result.Should().Be(new Input.Text("update amount", "Selected transaction 123"));
    }

    [Fact]
    public async Task BuildInput_TextWithWhitespaceQuote_FallsBackToReplyText()
    {
        // arrange
        var message = new Message
        {
            Text = "update amount",
            Quote = new TextQuote { Text = "   " },
            ReplyToMessage = new Message { Text = "Reply text" }
        };

        // act
        var result = await message.BuildInput(botClient, CancellationToken.None);

        // assert
        result.Should().Be(new Input.Text("update amount", "Reply text"));
    }

    [Fact]
    public async Task BuildInput_TextWithWhitespaceContextCandidates_SetsNullContext()
    {
        // arrange
        var message = new Message
        {
            Text = "coffee 5",
            Quote = new TextQuote { Text = " " },
            ReplyToMessage = new Message { Text = "\t", Caption = "   " }
        };

        // act
        var result = await message.BuildInput(botClient, CancellationToken.None);

        // assert
        result.Should().Be(new Input.Text("coffee 5", null));
    }

    [Fact]
    public async Task BuildInput_VoiceWithReply_SetsRawContextAndNullCaption()
    {
        // arrange
        var message = new Message
        {
            Voice = new Voice { FileId = "voice-id", MimeType = "audio/ogg" },
            ReplyToMessage = new Message { Text = "22222222-2222-2222-2222-222222222222" }
        };

        // act
        var result = await message.BuildInput(botClient, CancellationToken.None);

        // assert
        var audioInput = result.Should().BeOfType<Input.Audio>().Which;
        audioInput.Data.Should().Equal(downloadedBytes);
        audioInput.ContentType.Should().Be("audio/ogg");
        audioInput.Caption.Should().BeNull();
        audioInput.Context.Should().Be("22222222-2222-2222-2222-222222222222");
    }

    [Fact]
    public async Task BuildInput_VoiceWithCaptionAndQuote_SetsRawCaptionAndContext()
    {
        // arrange
        var message = new Message
        {
            Voice = new Voice { FileId = "voice-id", MimeType = "audio/ogg" },
            Caption = "Voice caption",
            Quote = new TextQuote { Text = "Quoted text" }
        };

        // act
        var result = await message.BuildInput(botClient, CancellationToken.None);

        // assert
        var audioInput = result.Should().BeOfType<Input.Audio>().Which;
        audioInput.Data.Should().Equal(downloadedBytes);
        audioInput.ContentType.Should().Be("audio/ogg");
        audioInput.Caption.Should().Be("Voice caption");
        audioInput.Context.Should().Be("Quoted text");
    }

    [Fact]
    public async Task BuildInput_PhotoWithCaptionAndReply_SetsRawCaptionAndContext()
    {
        // arrange
        var message = new Message
        {
            Photo = [new PhotoSize { FileId = "thumb" }, new PhotoSize { FileId = "full" }],
            Caption = "Groceries yesterday",
            ReplyToMessage = new Message { Caption = "Previous total: 100" }
        };

        // act
        var result = await message.BuildInput(botClient, CancellationToken.None);

        // assert
        var imageInput = result.Should().BeOfType<Input.Image>().Which;
        imageInput.Data.Should().Equal(downloadedBytes);
        imageInput.ContentType.Should().Be("image/jpeg");
        imageInput.Caption.Should().Be("Groceries yesterday");
        imageInput.Context.Should().Be("Previous total: 100");
    }

    [Fact]
    public async Task BuildInput_PhotoWithWhitespaceCaptionAndNoReply_SetsNullCaptionAndNullContext()
    {
        // arrange
        var message = new Message
        {
            Photo = [new PhotoSize { FileId = "full" }],
            Caption = "   "
        };

        // act
        var result = await message.BuildInput(botClient, CancellationToken.None);

        // assert
        var imageInput = result.Should().BeOfType<Input.Image>().Which;
        imageInput.Caption.Should().BeNull();
        imageInput.Context.Should().BeNull();
    }

    [Fact]
    public async Task BuildInput_UnsupportedMessage_ReturnsNull()
    {
        // arrange
        var message = new Message();

        // act
        var result = await message.BuildInput(botClient, CancellationToken.None);

        // assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task BuildInput_PdfDocumentWithoutCaptionOrContext_ReturnsDocumentInput()
    {
        // arrange
        var message = new Message
        {
            Document = new Document { FileId = "doc-id", MimeType = "application/pdf", FileName = "statement.pdf" }
        };

        // act
        var result = await message.BuildInput(botClient, CancellationToken.None);

        // assert
        var documentInput = result.Should().BeOfType<Input.Document>().Which;
        documentInput.Data.Should().Equal(downloadedBytes);
        documentInput.ContentType.Should().Be("application/pdf");
        documentInput.Caption.Should().BeNull();
        documentInput.Context.Should().BeNull();
    }

    [Theory]
    [InlineData("Quoted line", "Reply text", "Quoted line")]
    [InlineData(null, "Reply text", "Reply text")]
    public async Task BuildInput_PdfDocumentWithCaptionAndContext_SetsCaptionAndContext(
        string? quoteText,
        string replyText,
        string expectedContext)
    {
        // arrange
        var message = new Message
        {
            Document = new Document { FileId = "doc-id", MimeType = "application/pdf" },
            Caption = "Monthly statement",
            Quote = quoteText is null ? null : new TextQuote { Text = quoteText },
            ReplyToMessage = new Message { Text = replyText }
        };

        // act
        var result = await message.BuildInput(botClient, CancellationToken.None);

        // assert
        var documentInput = result.Should().BeOfType<Input.Document>().Which;
        documentInput.Data.Should().Equal(downloadedBytes);
        documentInput.ContentType.Should().Be("application/pdf");
        documentInput.Caption.Should().Be("Monthly statement");
        documentInput.Context.Should().Be(expectedContext);
    }

    [Fact]
    public async Task BuildInput_PdfDocumentWithNonCanonicalMimeType_ReturnsCanonicalApplicationPdf()
    {
        // arrange
        var message = new Message
        {
            Document = new Document { FileId = "doc-id", MimeType = "APPLICATION/PDF; charset=binary" }
        };

        // act
        var result = await message.BuildInput(botClient, CancellationToken.None);

        // assert
        var documentInput = result.Should().BeOfType<Input.Document>().Which;
        documentInput.Data.Should().Equal(downloadedBytes);
        documentInput.ContentType.Should().Be("application/pdf");
    }

    [Theory]
    [InlineData(null, "statement.pdf")]
    [InlineData("application/octet-stream", "statement.pdf")]
    [InlineData(null, " statement.PDF ")]
    [InlineData("application/octet-stream", " statement.PDF ")]
    public async Task BuildInput_PdfDocumentWithFallbackExtension_ReturnsApplicationPdf(
        string? mimeType,
        string fileName)
    {
        // arrange
        var message = new Message
        {
            Document = new Document { FileId = "doc-id", MimeType = mimeType, FileName = fileName }
        };

        // act
        var result = await message.BuildInput(botClient, CancellationToken.None);

        // assert
        var documentInput = result.Should().BeOfType<Input.Document>().Which;
        documentInput.Data.Should().Equal(downloadedBytes);
        documentInput.ContentType.Should().Be("application/pdf");
    }

    [Theory]
    [InlineData("application/zip", "archive.zip")]
    [InlineData("image/png", "receipt.png")]
    [InlineData(null, "notes.txt")]
    [InlineData("application/octet-stream", null)]
    public async Task BuildInput_UnsupportedDocumentFormat_ReturnsNullWithoutDownloading(
        string? mimeType,
        string? fileName)
    {
        // arrange
        var message = new Message
        {
            Document = new Document { FileId = "doc-id", MimeType = mimeType, FileName = fileName }
        };

        // act
        var result = await message.BuildInput(botClient, CancellationToken.None);

        // assert
        result.Should().BeNull();
        botClient.ReceivedCalls().Should().BeEmpty();
    }

    [Theory]
    [InlineData("video/mp4", "animation.mp4")]
    [InlineData("application/pdf", "animation.pdf")]
    public async Task BuildInput_AnimationWithDocument_ReturnsNullWithoutDownloading(
        string mimeType,
        string fileName)
    {
        // arrange
        var message = new Message
        {
            Animation = new Animation { FileId = "anim-id" },
            Document = new Document { FileId = "doc-id", MimeType = mimeType, FileName = fileName }
        };

        // act
        var result = await message.BuildInput(botClient, CancellationToken.None);

        // assert
        result.Should().BeNull();
        botClient.ReceivedCalls().Should().BeEmpty();
    }

    [Fact]
    public async Task BuildInput_DocumentAtMaxFileSize_DownloadsAndReturnsDocumentInput()
    {
        // arrange
        var message = new Message
        {
            Document = new Document
            {
                FileId = "doc-id",
                MimeType = "application/pdf",
                FileSize = TelegramInputBuilder.MaxFileSizeBytes
            }
        };

        // act
        var result = await message.BuildInput(botClient, CancellationToken.None);

        // assert
        var documentInput = result.Should().BeOfType<Input.Document>().Which;
        documentInput.Data.Should().Equal(downloadedBytes);
        documentInput.ContentType.Should().Be("application/pdf");
    }

    [Theory]
    [InlineData("document")]
    [InlineData("voice")]
    [InlineData("photo")]
    public async Task BuildInput_MediaExceedingMaxFileSize_ReturnsNullWithoutDownloading(string mediaType)
    {
        // arrange
        const long oversized = TelegramInputBuilder.MaxFileSizeBytes + 1;
        var message = mediaType switch
        {
            "document" => new Message
            {
                Document = new Document { FileId = "doc-id", MimeType = "application/pdf", FileSize = oversized }
            },
            "voice" => new Message
            {
                Voice = new Voice { FileId = "voice-id", MimeType = "audio/ogg", FileSize = oversized }
            },
            "photo" => new Message
            {
                Photo = [new PhotoSize { FileId = "thumb", FileSize = 100 }, new PhotoSize { FileId = "full", FileSize = oversized }]
            },
            _ => throw new ArgumentOutOfRangeException(nameof(mediaType), mediaType, null)
        };

        // act
        var result = await message.BuildInput(botClient, CancellationToken.None);

        // assert
        result.Should().BeNull();
        botClient.ReceivedCalls().Should().BeEmpty();
    }
}
