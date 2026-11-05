using System.ComponentModel.DataAnnotations;

namespace MyFinanceTracker.InputProcessing.Gemini.Configuration;

public sealed class GeminiOptions
{
    public const string SectionName = "Gemini";

    [Required(AllowEmptyStrings = false)]
    public string ApiKey { get; init; } = string.Empty;

    [Required(AllowEmptyStrings = false)]
    public string Model { get; init; } = string.Empty;
}