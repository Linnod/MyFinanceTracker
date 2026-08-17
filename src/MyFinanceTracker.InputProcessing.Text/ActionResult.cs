using MyFinanceTracker.UseCases;

namespace MyFinanceTracker.InputProcessing.Text;

public abstract record ActionResult
{
    private ActionResult() { }

    public required string RawInput { get; init; }

    public static class Transaction
    {
        public sealed record Added(IReadOnlyList<Domain.Entities.Transaction> Transactions) : ActionResult;
        public sealed record Deleted(string CategoryName, DateOnly Date) : ActionResult;
        public sealed record Listed(
            string CategoryName,
            DateOnly Date,
            IReadOnlyList<Domain.Entities.Transaction> Transactions) : ActionResult;
    }

    public static class Category
    {
        public sealed record Listed(IReadOnlyList<Domain.Entities.Category> Categories) : ActionResult;
    }


    public sealed record InvalidSyntax(
        string ErrorCode,
        string? Suggestion = null,
        IReadOnlyCollection<string>? Examples = null
    ) : ActionResult;

    public sealed record DomainError(
        string ErrorCode,
        string? Suggestion = null,
        IReadOnlyCollection<string>? Examples = null
    ) : ActionResult;

    public sealed record InvalidInput(
        IReadOnlyCollection<ValidationErrorItem> Errors
    ) : ActionResult;

    public sealed record Failure(string? Message = null) : ActionResult;
}