using System.ComponentModel.DataAnnotations;

namespace MyFinanceTracker.Infrastructure.GoogleSheets.Configuration;

internal sealed class GoogleSheetsOptions
{
    public const string SectionName = "GoogleSheets";

    [Required(AllowEmptyStrings = false)]
    public string SpreadsheetId { get; init; } = string.Empty;

    [Required(AllowEmptyStrings = false)]
    public string CredentialsPath { get; init; } = string.Empty;

    [Required(AllowEmptyStrings = false)]
    public string ApplicationName { get; init; } = "MyFinanceTracker";

    [Range(0, 10)]
    public int HeaderRowsCount { get; init; }

    [Required(AllowEmptyStrings = false)]
    public string DecimalSeparator { get; init; } = ",";
}