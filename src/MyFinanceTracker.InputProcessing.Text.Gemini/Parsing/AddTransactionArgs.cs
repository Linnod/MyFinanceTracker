using MyFinanceTracker.Domain.Entities;

namespace MyFinanceTracker.InputProcessing.Text.Gemini.Parsing;

internal sealed record AddTransactionArgs(
    TransactionType Type,
    IReadOnlyList<decimal> Amounts,
    string? CategoryAlias,
    DateOnly? Date,
    string? Note,
    string RecognizedInput
);