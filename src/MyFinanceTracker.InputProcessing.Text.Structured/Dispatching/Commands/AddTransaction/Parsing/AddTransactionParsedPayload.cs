using MyFinanceTracker.Domain.Entities;

namespace MyFinanceTracker.InputProcessing.Text.Structured.Dispatching.Commands.AddTransaction.Parsing;

internal record AddTransactionParsedPayload(
    TransactionType Type,
    decimal[] Amounts,
    string? CategoryAlias = null,
    DateOnly? Date = null,
    string? Note = null
)
{
    public override string ToString()
    {
        var amountsStr = string.Join(" + ", Amounts);
        var dateStr = Date?.ToString("dd.MM.yyyy") ?? "today";
        var categoryStr = string.IsNullOrWhiteSpace(CategoryAlias)
            ? "none"
            : $"'{CategoryAlias}'";

        return $"{Type}: {amountsStr} ({categoryStr}) for {dateStr}";
    }
}