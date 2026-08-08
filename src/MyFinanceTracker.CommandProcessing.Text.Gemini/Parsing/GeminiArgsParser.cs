using System.Globalization;
using System.Text.Json;

namespace MyFinanceTracker.CommandProcessing.Text.Gemini.Parsing;

internal static class GeminiArgsParser
{
    public static string? GetString(this IDictionary<string, object>? args, string key)
    {
        if (args == null || !args.TryGetValue(key, out var raw) || raw == null)
        {
            return null;
        }

        return raw switch
        {
            JsonElement elem => elem.ValueKind == JsonValueKind.String ? elem.GetString() : elem.ToString(),
            _ => raw.ToString()
        };
    }

    public static DateOnly? GetDateOnly(this IDictionary<string, object>? args, string key)
    {
        var str = args.GetString(key);
        return DateOnly.TryParse(str, CultureInfo.InvariantCulture, out var date) ? date : null;
    }

    public static decimal[] GetDecimalArray(this IDictionary<string, object>? args, string key)
    {
        if (args == null || !args.TryGetValue(key, out var raw) || raw == null)
        {
            return [];
        }

        return raw switch
        {
            JsonElement elem => ParseJsonElement(elem),
            System.Collections.IEnumerable enumerable when raw is not string => ParseEnumerable(enumerable),
            _ => ParseSingleDecimal(raw)
        };
    }

    private static decimal[] ParseJsonElement(JsonElement elem)
    {
        if (elem.ValueKind == JsonValueKind.Array)
        {
            var list = new List<decimal>();
            foreach (var item in elem.EnumerateArray())
            {
                if (TryExtractDecimal(item, out var val))
                {
                    list.Add(val);
                }
            }
            return list.ToArray();
        }

        return TryExtractDecimal(elem, out var single) ? [single] : [];
    }

    private static decimal[] ParseEnumerable(System.Collections.IEnumerable enumerable)
    {
        var list = new List<decimal>();
        foreach (var item in enumerable)
        {
            if (item != null && TryParseDecimal(item.ToString(), out var val))
            {
                list.Add(val);
            }
        }
        return list.ToArray();
    }

    private static decimal[] ParseSingleDecimal(object raw)
    {
        return TryParseDecimal(raw.ToString(), out var single) ? [single] : [];
    }

    private static bool TryExtractDecimal(JsonElement item, out decimal result)
    {
        if (item.ValueKind == JsonValueKind.Number && item.TryGetDecimal(out result))
        {
            return true;
        }
        return TryParseDecimal(item.ToString(), out result);
    }

    private static bool TryParseDecimal(string? str, out decimal result)
    {
        return decimal.TryParse(str, NumberStyles.Any, CultureInfo.InvariantCulture, out result);
    }
}