using System.Text.Json;
using System.Text.Json.Serialization;

namespace MyFinanceTracker.InputProcessing.Text.Gemini.Parsing;

internal static class GeminiArgsParser
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        NumberHandling = JsonNumberHandling.AllowReadingFromString,
        Converters = 
        { 
            new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) 
        }
    };

    public static T? BindArgs<T>(this IDictionary<string, object>? args) where T : class
    {
        if (args == null || args.Count == 0)
        {
            return null;
        }

        try
        {
            var jsonNode = JsonSerializer.SerializeToNode(args);
            return jsonNode?.Deserialize<T>(Options);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}