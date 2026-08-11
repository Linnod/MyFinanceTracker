namespace MyFinanceTracker.InputProcessing.Text.Gemini.Parsing;

internal sealed record DeleteTransactionsArgs(
    string CategoryAlias,
    DateOnly Date,
    string RecognizedInput
);