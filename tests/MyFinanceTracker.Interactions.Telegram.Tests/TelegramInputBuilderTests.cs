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
}
