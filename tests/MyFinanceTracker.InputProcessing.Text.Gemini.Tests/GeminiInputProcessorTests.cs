using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Google.GenAI;
using Google.GenAI.Types;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MyFinanceTracker.InputProcessing.Inputs;
using MyFinanceTracker.InputProcessing.Text.Gemini.Configuration;
using MyFinanceTracker.InputProcessing.Text.Gemini.Declarations;
using MyFinanceTracker.InputProcessing.Text.Gemini.Execution;
using MyFinanceTracker.InputProcessing.Text.Gemini.Prompt;
using NSubstitute;
using Xunit;

namespace MyFinanceTracker.InputProcessing.Text.Gemini.Tests;

public class GeminiInputProcessorTests
{
    [Fact]
    public async Task Process_Text_SendsTextWithSystemInstructionAndTools()
    {
        using var fixture = new Fixture();

        await fixture.Processor.Process(new TextInput("coffee 5"), CancellationToken.None);

        using var request = JsonDocument.Parse(fixture.Handler.RequestBody!);
        var root = request.RootElement;
        var content = root.GetProperty("contents").EnumerateArray().Single();
        content.GetProperty("role").GetString().Should().Be("user");
        content.GetProperty("parts").EnumerateArray().Single().GetProperty("text")
            .GetString().Should().Be("coffee 5");
        root.GetProperty("systemInstruction").GetProperty("parts")[0].GetProperty("text")
            .GetString().Should().Be("System instruction");
        root.GetProperty("tools")[0].GetProperty("functionDeclarations")[0].GetProperty("name")
            .GetString().Should().Be("record_transaction");
        fixture.Handler.RequestUri!.AbsolutePath.Should().EndWith("/models/test-model:generateContent");
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Process_Audio_PreservesStreamReadingAndLeavesInputOpen(bool memoryStream)
    {
        using var fixture = new Fixture();
        byte[] bytes = [1, 2, 3, 4];
        using Stream stream = memoryStream
            ? new MemoryStream(bytes)
            : new BufferedStream(new MemoryStream(bytes));
        stream.Position = 1;

        await fixture.Processor.Process(new AudioInput(stream, "audio/ogg"), CancellationToken.None);

        using var request = JsonDocument.Parse(fixture.Handler.RequestBody!);
        var parts = request.RootElement.GetProperty("contents")[0].GetProperty("parts");
        parts.GetArrayLength().Should().Be(2);
        parts[0].GetProperty("inlineData").GetProperty("mimeType").GetString().Should().Be("audio/ogg");
        parts[0].GetProperty("inlineData").GetProperty("data").GetString()
            .Should().Be(Convert.ToBase64String(memoryStream ? bytes : bytes[1..]));
        parts[1].GetProperty("text").GetString().Should().Be(
            "Extract financial transaction details and execute the corresponding function call.");
        stream.CanRead.Should().BeTrue();
        stream.Position.Should().Be(memoryStream ? 1 : bytes.Length);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Process_FunctionCalls_ExecutesInOrderAndReturnsActions(bool audio)
    {
        using var fixture = new Fixture("""
            {"candidates":[{"content":{"parts":[
              {"text":"Tools take precedence over text"},
              {"functionCall":{"name":"first","args":{}}},
              {"functionCall":{"name":"second","args":{}}}
            ]}}]}
            """);
        using var cancellation = new CancellationTokenSource();
        var calls = new List<string>();
        ActionResult first = new ActionResult.Category.Listed([]) { RawInput = "first" };
        ActionResult second = new ActionResult.Failure("failed") { RawInput = "second" };
        fixture.Executor.ExecuteToolCall(Arg.Any<FunctionCall>(), cancellation.Token).Returns(call =>
        {
            var name = call.Arg<FunctionCall>().Name!;
            calls.Add(name);
            return Task.FromResult(name == "first" ? first : second);
        });

        var result = await fixture.Process(audio, cancellation.Token);

        result.Should().BeOfType<ProcessingResult.Completed>().Which.Actions
            .Should().Equal(first, second);
        calls.Should().Equal("first", "second");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Process_TextResponse_ReturnsInvalidInput(bool audio)
    {
        using var fixture = new Fixture("""
            {"candidates":[{"content":{"parts":[{"text":"Please specify a category."}]}}]}
            """);

        var result = await fixture.Process(audio);

        result.Should().Be(new ProcessingResult.InvalidInput("Please specify a category."));
        fixture.Executor.ReceivedCalls().Should().BeEmpty();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Process_EmptyResponse_ReturnsRequestFailureMessage(bool audio)
    {
        using var fixture = new Fixture();

        var result = await fixture.Process(audio);

        result.Should().Be(new ProcessingResult.SystemError(
            "Failed to process your request. Please try again later."));
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task Process_PromptFailureIncludingCancellation_ReturnsOriginalException(bool audio, bool cancelled)
    {
        using var fixture = new Fixture();
        using var cancellation = new CancellationTokenSource();
        if (cancelled)
            cancellation.Cancel();
        Exception exception = cancelled
            ? new OperationCanceledException(cancellation.Token)
            : new InvalidOperationException("Prompt failure");
        fixture.PromptBuilder.BuildSystemInstruction(cancellation.Token)
            .Returns(Task.FromException<string>(exception));

        var result = await fixture.Process(audio, cancellation.Token);

        var error = result.Should().BeOfType<ProcessingResult.SystemError>().Which;
        error.Message.Should().Be("Failed to process your request. Please try again later.");
        error.Exception.Should().BeSameAs(exception);
        fixture.Handler.RequestBody.Should().BeNull();
    }

    [Fact]
    public async Task Process_AudioReadFailure_ReturnsSystemErrorAndLeavesStreamOpen()
    {
        using var fixture = new Fixture();
        var exception = new IOException("Read failure");
        using var stream = new FailingStream(exception);

        var result = await fixture.Processor.Process(new AudioInput(stream, "audio/ogg"), CancellationToken.None);

        result.Should().Be(new ProcessingResult.SystemError(
            "Failed to process your request. Please try again later.", exception));
        stream.IsDisposed.Should().BeFalse();
        fixture.PromptBuilder.ReceivedCalls().Should().BeEmpty();
        fixture.Handler.RequestBody.Should().BeNull();
    }

    private sealed class Fixture : IDisposable
    {
        public IGeminiSystemPromptBuilder PromptBuilder { get; } = Substitute.For<IGeminiSystemPromptBuilder>();
        public IGeminiToolExecutor Executor { get; } = Substitute.For<IGeminiToolExecutor>();
        public StubHandler Handler { get; }
        public GeminiInputProcessor Processor { get; }
        private readonly Client client;

        public Fixture(string response = "{}")
        {
            Handler = new StubHandler(response);
            client = new Client(apiKey: "test-key", enterprise: false, clientOptions: new ClientOptions
            {
                HttpClientFactory = () => new HttpClient(Handler)
            });
            PromptBuilder.BuildSystemInstruction(Arg.Any<CancellationToken>()).Returns("System instruction");
            var declarations = Substitute.For<IGeminiToolDeclarationProvider>();
            declarations.GetToolDeclarations().Returns(new List<Tool>
            {
                new() { FunctionDeclarations = [new FunctionDeclaration { Name = "record_transaction" }] }
            });
            Processor = new GeminiInputProcessor(PromptBuilder, declarations, Executor,
                Options.Create(new GeminiOptions { Model = "test-model" }), client,
                NullLogger<GeminiInputProcessor>.Instance);
        }

        public async Task<ProcessingResult> Process(bool audio, CancellationToken ct = default)
        {
            if (!audio)
                return await Processor.Process(new TextInput("coffee 5"), ct);

            using var stream = new MemoryStream([1, 2, 3]);
            return await Processor.Process(new AudioInput(stream, "audio/ogg"), ct);
        }

        public void Dispose() => client.Dispose();
    }

    private sealed class StubHandler(string response) : HttpMessageHandler
    {
        public string? RequestBody { get; private set; }
        public Uri? RequestUri { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            RequestBody = await request.Content!.ReadAsStringAsync(ct);
            RequestUri = request.RequestUri;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(response, Encoding.UTF8, "application/json")
            };
        }
    }

    private sealed class FailingStream(Exception exception) : Stream
    {
        public bool IsDisposed { get; private set; }
        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override int Read(byte[] buffer, int offset, int count) => throw exception;
        public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
            => Task.FromException(exception);
        public override void Flush() => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        protected override void Dispose(bool disposing)
        {
            IsDisposed = true;
            base.Dispose(disposing);
        }
    }
}
