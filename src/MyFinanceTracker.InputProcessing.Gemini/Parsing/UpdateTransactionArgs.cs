using MyFinanceTracker.Domain.Entities;

namespace MyFinanceTracker.InputProcessing.Gemini.Parsing;

internal sealed record UpdateTransactionArgs(
    Guid? Id,
    TransactionType? Type,
    decimal? Amount,
    string? CategoryAlias,
    DateOnly? Date,
    string? Note,
    bool? ClearNote,
    string RecognizedInput);
