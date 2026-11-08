using MyFinanceTracker.Domain.Entities;

namespace MyFinanceTracker.InputProcessing.Gemini.Parsing;

internal sealed record GetTransactionsArgs(
    DateTime? FromDate,
    DateTime? ToDate,
    IReadOnlyList<string>? CategoryAliases,
    TransactionType? Type,
    string RecognizedInput
);
