using System.Net;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace MyFinanceTracker.Integration.Tests.Fixtures;

internal sealed class StubGeminiHttpHandler : HttpMessageHandler
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private readonly Lock syncLock = new();
    private readonly Queue<string> queuedResponses = new();

    public void EnqueueFunctionCall(string functionName, object args)
    {
        var payload = new
        {
            candidates = new[]
            {
                new
                {
                    content = new
                    {
                        parts = new[]
                        {
                            new
                            {
                                functionCall = new
                                {
                                    name = functionName,
                                    args
                                }
                            }
                        }
                    }
                }
            }
        };

        EnqueueRawResponse(JsonSerializer.Serialize(payload, SerializerOptions));
    }

    public void EnqueueRawResponse(string responseJson)
    {
        lock (syncLock)
        {
            queuedResponses.Enqueue(responseJson);
        }
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        string responseJson;
        lock (syncLock)
        {
            if (!queuedResponses.TryDequeue(out responseJson!))
            {
                throw new InvalidOperationException(
                    "No stubbed Gemini HTTP response is queued for the incoming request.");
            }
        }

        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
        };

        return Task.FromResult(response);
    }
}
