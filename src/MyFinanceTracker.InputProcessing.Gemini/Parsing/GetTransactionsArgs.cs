using MyFinanceTracker.Domain.Entities;

namespace MyFinanceTracker.InputProcessing.Gemini.Parsing;

internal sealed record GetTransactionsArgs(
    DateOnly? FromDate,
    DateOnly? ToDate,
    IReadOnlyList<string>? CategoryAliases,
    TransactionType? Type,
    string RecognizedInput
);
