namespace MyFinanceTracker.Infrastructure.Persistence.GoogleSheets.Mapping;

internal sealed record RowMappingFailure(int RowNumber, string Reason);
