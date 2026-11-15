using MyFinanceTracker.Infrastructure.Persistence.GoogleSheets.Models;
using MyFinanceTracker.Infrastructure.Persistence.Models;

namespace MyFinanceTracker.Infrastructure.Persistence.GoogleSheets.Mapping;

internal sealed class GoogleSheetCategoryMapper
{
    private static class Columns
    {
        public const int Id = 0;
        public const int Name = 1;
        public const int IsArchived = 2;
    }

    public GoogleSheetRowValues MapToRow(CategorySyncRecord category)
    {
        return new GoogleSheetRowValues([
            category.Id,
            category.Name,
            category.IsArchived ? "TRUE" : "FALSE"
        ]);
    }

    public IReadOnlyList<CategoryRowMappingResult> MapFromRows(IEnumerable<GoogleSheetRow> rows)
        => [.. rows.Select(ParseRow)];

    private static CategoryRowMappingResult ParseRow(GoogleSheetRow row)
    {
        var rowNumber = GoogleSheetRowReader.ToSheetRowNumber(row.Index);
        var rowValues = row.Values;

        if (GoogleSheetRowReader.IsEmpty(rowValues))
        {
            return new CategoryRowMappingResult.Empty();
        }

        if (!GoogleSheetRowReader.TryGetRequiredValue(rowValues, Columns.Id, out var id))
        {
            return new CategoryRowMappingResult.Failure(rowNumber, "Missing required 'Id' value.");
        }

        if (!GoogleSheetRowReader.TryGetRequiredValue(rowValues, Columns.Name, out var name))
        {
            return new CategoryRowMappingResult.Failure(rowNumber, "Missing required 'Name' value.");
        }

        var isArchived = false;
        var isArchivedString = GoogleSheetRowReader.GetOptionalValue(rowValues, Columns.IsArchived);
        if (isArchivedString is not null && !bool.TryParse(isArchivedString, out isArchived))
        {
            return new CategoryRowMappingResult.Failure(rowNumber, $"Invalid IsArchived value: '{isArchivedString}'.");
        }

        return new CategoryRowMappingResult.Success(row, new CategorySyncRecord(id, name, isArchived));
    }
}
