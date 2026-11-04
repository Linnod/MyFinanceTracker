using MyFinanceTracker.UseCases;

namespace MyFinanceTracker.InputProcessing;

public abstract record ActionResult
{
    private ActionResult() { }

    public required string RawInput { get; init; }

    public static class Transaction
    {
        public sealed record Added(IReadOnlyList<Domain.Entities.Transaction> Transactions) : ActionResult;
        public sealed record Deleted(int DeletedCount) : ActionResult;
        public sealed record Listed(
            DateOnly FromDate,
            DateOnly ToDate,
            IReadOnlyList<string> CategoryNames,
            IReadOnlyList<Domain.Entities.Transaction> Transactions) : ActionResult;
    }

    public static class Category
    {
        public sealed record Listed(IReadOnlyList<Domain.Entities.Category> Categories) : ActionResult;
    }

    public sealed record InvalidInput(
        IReadOnlyCollection<ValidationErrorItem> Errors
    ) : ActionResult;

    public sealed record Failure(string? Message = null) : ActionResult;
}
