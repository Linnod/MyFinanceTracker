using System.ComponentModel.DataAnnotations;

namespace MyFinanceTracker.Infrastructure.Persistence.Yaml.Configuration;

internal sealed class YamlPersistenceOptions
{
    [Required(ErrorMessage = "FilePath is required.")]
    public string FilePath { get; init; } = string.Empty;
}
