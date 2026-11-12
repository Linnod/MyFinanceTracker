using System.ComponentModel.DataAnnotations;

namespace MyFinanceTracker.Infrastructure.Persistence.GoogleSheets.Configuration;

internal sealed class GoogleSheetsOptions
{
    public const string SectionName = "GoogleSheets";

    [Required(AllowEmptyStrings = false)]
    public string SpreadsheetId { get; init; } = string.Empty;

    [Required]
    public string CredentialsFilePath { get; init; } = string.Empty;

    [Required(AllowEmptyStrings = false)]
    public string ApplicationName { get; init; } = "MyFinanceTracker";

    [Required(AllowEmptyStrings = false)]
    public string TransactionsSheetName { get; init; } = "Transactions";

    [Required(AllowEmptyStrings = false)]
    public string CategoriesSheetName { get; init; } = "Categories";

    [Required(AllowEmptyStrings = false)]
    public string DecimalSeparator { get; init; } = ",";
}
