using MyFinanceTracker.Domain.Entities;
using MyFinanceTracker.Infrastructure.Persistence.GoogleSheets.Models;

namespace MyFinanceTracker.Infrastructure.Persistence.GoogleSheets.Mapping;

internal abstract record CategoryRowMappingResult
{
    private CategoryRowMappingResult() { }

    public sealed record Success(GoogleSheetRow Row, Category Category) : CategoryRowMappingResult;
    public sealed record Empty : CategoryRowMappingResult;
    public sealed record Failure(int RowNumber, string Reason) : CategoryRowMappingResult;
}
