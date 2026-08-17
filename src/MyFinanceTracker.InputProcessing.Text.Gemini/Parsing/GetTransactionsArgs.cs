namespace MyFinanceTracker.InputProcessing.Text.Gemini.Parsing;

internal sealed record GetTransactionsArgs(
    string CategoryAlias,
    DateOnly Date,
    string RecognizedInput
);