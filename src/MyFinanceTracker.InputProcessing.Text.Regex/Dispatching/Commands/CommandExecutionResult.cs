using MyFinanceTracker.UseCases;

namespace MyFinanceTracker.InputProcessing.Text.Regex.Dispatching.Commands;

internal abstract record CommandExecutionResult
{
    private CommandExecutionResult() { }

    public static class Transaction
    {
        public sealed record Added(IReadOnlyList<Domain.Entities.Transaction> Transactions) : CommandExecutionResult
        {
            public override string ToString() => $"Added ({Transactions.Count} items)";
        }

        public sealed record Deleted(string CategoryName, DateOnly Date) : CommandExecutionResult
        {
            public override string ToString() => $"Deleted '{CategoryName}' on {Date:dd.MM.yyyy}";
        }
    }

    public static class Category
    {
        public sealed record Listed(IReadOnlyList<Domain.Entities.Category> Categories) : CommandExecutionResult
        {
            public override string ToString() => $"Listed ({Categories.Count} categories)";
        }
    }

    public sealed record InvalidSyntax(
        string ErrorCode,
        string? Suggestion = null,
        IReadOnlyCollection<string>? Examples = null
    ) : CommandExecutionResult
    {
        public override string ToString() => $"Invalid syntax: {ErrorCode}";
    }

    public sealed record InvalidInput(IReadOnlyCollection<ValidationErrorItem> Errors) : CommandExecutionResult
    {
        public override string ToString() =>
            $"Validation error: {string.Join(", ", Errors.Select(e => e.ErrorCode))}";
    }

    public sealed record Failure(string? Message = null) : CommandExecutionResult
    {
        public override string ToString() =>
            $"Failure{(Message is not null ? $": {Message}" : string.Empty)}";
    }
}