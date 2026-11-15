using MyFinanceTracker.Infrastructure.Persistence.GoogleSheets.Models;
using MyFinanceTracker.Infrastructure.Persistence.Models;

namespace MyFinanceTracker.Infrastructure.Persistence.GoogleSheets.Mapping;

internal closed record TransactionRowMappingResult
{
    public sealed record Success(GoogleSheetRow Row, TransactionSyncRecord Transaction) : TransactionRowMappingResult;
    public sealed record Empty : TransactionRowMappingResult;
    public sealed record Failure(int RowNumber, string Reason) : TransactionRowMappingResult;
}
