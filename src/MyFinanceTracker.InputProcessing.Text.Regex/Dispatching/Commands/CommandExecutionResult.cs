using MyFinanceTracker.UseCases;

namespace MyFinanceTracker.InputProcessing.Text.Regex.Dispatching.Commands;

internal abstract record CommandExecutionResult
{
    private CommandExecutionResult() { }

    public static class Transaction
    {
        public sealed record Added(IReadOnlyList<Domain.Entities.Transaction> Transactions) : CommandExecutionResult
        {
            public override string ToString() => $"ADDED ({Transactions.Count} items)";
        }

        public sealed record Deleted(string CategoryName, DateOnly Date) : CommandExecutionResult
        {
            public override string ToString() => $"DELETED '{CategoryName}' on {Date:dd.MM.yyyy}";
        }
    }

    public static class Category
    {
        public sealed record Listed(IReadOnlyList<Domain.Entities.Category> Categories) : CommandExecutionResult
        {
            public override string ToString() => $"LISTED ({Categories.Count} categories)";
        }
    }

    public sealed record InvalidSyntax(
        string ErrorCode,
        string? Suggestion = null,
        IReadOnlyCollection<string>? Examples = null
    ) : CommandExecutionResult
    {
        public override string ToString() => $"INVALID SYNTAX [{ErrorCode}]";
    }

    public sealed record InvalidInput(IReadOnlyCollection<ValidationErrorItem> Errors) : CommandExecutionResult
    {
        public override string ToString() =>
            $"VALIDATION ERROR [{string.Join(", ", Errors.Select(e => e.ErrorCode))}]";
    }

    public sealed record Failure(string? Message = null) : CommandExecutionResult
    {
        public override string ToString() =>
            $"FAILURE{(Message is not null ? $": {Message}" : string.Empty)}";
    }
}