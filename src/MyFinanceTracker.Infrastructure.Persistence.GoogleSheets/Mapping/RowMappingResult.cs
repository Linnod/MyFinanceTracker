using MyFinanceTracker.Domain.Entities;
using MyFinanceTracker.Infrastructure.Persistence.GoogleSheets.Models;

namespace MyFinanceTracker.Infrastructure.Persistence.GoogleSheets.Mapping;

internal abstract record RowMappingResult
{
    private RowMappingResult() { }

    public sealed record Success(GoogleSheetRow Row, Transaction Transaction) : RowMappingResult;
    public sealed record Empty : RowMappingResult;
    public sealed record Failure(int RowNumber, string Reason) : RowMappingResult;
}
