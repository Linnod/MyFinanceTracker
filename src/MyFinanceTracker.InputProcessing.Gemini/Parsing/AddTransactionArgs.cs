using MyFinanceTracker.Domain.Entities;

namespace MyFinanceTracker.InputProcessing.Gemini.Parsing;

internal sealed record AddTransactionArgs(
    TransactionType Type,
    IReadOnlyList<decimal>? Amounts,
    string? CategoryId,
    DateTime? Date,
    string? Note
)
{
    public IReadOnlyList<decimal> Amounts { get; init; } = Amounts ?? [];
}
