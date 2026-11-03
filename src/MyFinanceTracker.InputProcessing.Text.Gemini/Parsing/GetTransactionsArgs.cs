using MyFinanceTracker.Domain.Entities;

namespace MyFinanceTracker.InputProcessing.Text.Gemini.Parsing;

internal sealed record GetTransactionsArgs(
    DateOnly? FromDate,
    DateOnly? ToDate,
    IReadOnlyList<string>? CategoryAliases,
    TransactionType? Type,
    string RecognizedInput
);
