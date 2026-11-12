using MyFinanceTracker.Domain.Entities;
using MyFinanceTracker.Infrastructure.Persistence.GoogleSheets.Models;

namespace MyFinanceTracker.Infrastructure.Persistence.GoogleSheets.Mapping;

internal sealed class GoogleSheetCategoryMapper
{
    private static class Columns
    {
        public const int Id = 0;
        public const int Name = 1;
        public const int IsArchived = 2;
    }

    public GoogleSheetRowValues MapToRow(Category category)
    {
        return new GoogleSheetRowValues([
            category.Id,
            category.Name,
            category.IsArchived ? "TRUE" : "FALSE"
        ]);
    }

    public IReadOnlyList<GoogleSheetRowValues> MapToRows(IEnumerable<Category> categories)
        => [.. categories.Select(MapToRow)];

    public IReadOnlyList<CategoryRowMappingResult> MapToDomain(IEnumerable<GoogleSheetRow> rows)
        => [.. rows.Select(MapToDomain)];

    private CategoryRowMappingResult MapToDomain(GoogleSheetRow row)
    {
        var rowNumber = ToSheetRowNumber(row.Index);
        var rowValues = row.Values;

        if (rowValues.Values.Count == 0 || rowValues.Values.All(string.IsNullOrWhiteSpace))
        {
            return new CategoryRowMappingResult.Empty();
        }

        if (!TryGetRequiredValue(rowValues.Values, Columns.Id, out var id))
        {
            return new CategoryRowMappingResult.Failure(rowNumber, "Missing required 'Id' value.");
        }

        if (!TryGetRequiredValue(rowValues.Values, Columns.Name, out var name))
        {
            return new CategoryRowMappingResult.Failure(rowNumber, "Missing required 'Name' value.");
        }

        var isArchived = false;
        if (TryGetOptionalValue(rowValues.Values, Columns.IsArchived, out var isArchivedString))
        {
            if (!bool.TryParse(isArchivedString, out isArchived))
            {
                return new CategoryRowMappingResult.Failure(rowNumber, $"Invalid IsArchived value: '{isArchivedString}'.");
            }
        }

        return new CategoryRowMappingResult.Success(row, new Category(id, name, isArchived));
    }

    private static bool TryGetRequiredValue(IReadOnlyList<string> values, int index, out string value)
    {
        if (index < values.Count && !string.IsNullOrWhiteSpace(values[index]))
        {
            value = values[index].Trim();
            return true;
        }

        value = string.Empty;
        return false;
    }

    private static bool TryGetOptionalValue(IReadOnlyList<string> values, int index, out string value)
    {
        if (index < values.Count && !string.IsNullOrWhiteSpace(values[index]))
        {
            value = values[index].Trim();
            return true;
        }

        value = string.Empty;
        return false;
    }

    private static int ToSheetRowNumber(int rowIndex) => rowIndex + 1;
}
