using MyFinanceTracker.Infrastructure.Persistence.GoogleSheets.Models;
using MyFinanceTracker.Infrastructure.Persistence.Models;

namespace MyFinanceTracker.Infrastructure.Persistence.GoogleSheets.Mapping;

internal abstract record CategoryRowMappingResult
{
    private CategoryRowMappingResult() { }

    public sealed record Success(GoogleSheetRow Row, CategorySyncRecord Category) : CategoryRowMappingResult;
    public sealed record Empty : CategoryRowMappingResult;
    public sealed record Failure(int RowNumber, string Reason) : CategoryRowMappingResult;
}
