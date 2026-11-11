using MyFinanceTracker.Domain.Entities;

namespace MyFinanceTracker.InputProcessing.Gemini.Parsing;

internal sealed record UpdateTransactionArgs(
    string? Id,
    TransactionType? Type,
    decimal? Amount,
    string? CategoryId,
    DateTime? Date,
    string? Note,
    bool? ClearNote);
