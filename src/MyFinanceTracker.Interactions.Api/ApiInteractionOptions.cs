using System.ComponentModel.DataAnnotations;

namespace MyFinanceTracker.Interactions.Api;

internal sealed class ApiInteractionOptions
{
    public const string SectionName = "Api";

    [Range(1024, 65535)]
    public int Port { get; init; } = 5000;
    
    [Required(AllowEmptyStrings = false)]
    public string ApiKey { get; init; } = string.Empty;
}