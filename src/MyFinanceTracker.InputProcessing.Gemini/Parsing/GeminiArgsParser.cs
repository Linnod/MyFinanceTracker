using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MyFinanceTracker.InputProcessing.Gemini.Parsing;

internal static class GeminiArgsParser
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        NumberHandling = JsonNumberHandling.AllowReadingFromString,
        Converters =
        {
            new JsonStringEnumConverter(JsonNamingPolicy.CamelCase),
            new FlexibleDateTimeConverter()
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

    private sealed class FlexibleDateTimeConverter : JsonConverter<DateTime>
    {
        private static readonly string[] Formats =
        [
            "yyyy-MM-ddTHH:mm:ss",
            "yyyy-MM-dd HH:mm:ss",
            "yyyy-MM-ddTHH:mm",
            "yyyy-MM-dd HH:mm",
            "yyyy-MM-dd"
        ];

        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.String)
            {
                throw new JsonException($"Unexpected token parsing DateTime. Expected String, got {reader.TokenType}.");
            }

            var stringValue = reader.GetString();
            if (DateTime.TryParseExact(stringValue, Formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
            {
                return date;
            }

            throw new JsonException($"Unable to convert \"{stringValue}\" to DateTime.");
        }

        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture));
        }
    }
}
