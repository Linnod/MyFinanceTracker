namespace MyFinanceTracker.UseCases;

public sealed record ValidationErrorItem(
    string PropertyName, 
    string Message, string?
    Suggestion = null);