using System.ComponentModel.DataAnnotations;

namespace MyFinanceTracker.Infrastructure.Persistence.Yaml;

internal class YamlPersistenceOptions
{
    [Required(ErrorMessage = "YAML FilePath is required.")]
    [MinLength(5, ErrorMessage = "FilePath is too short.")]
    public string FilePath { get; init; } = string.Empty;
}
