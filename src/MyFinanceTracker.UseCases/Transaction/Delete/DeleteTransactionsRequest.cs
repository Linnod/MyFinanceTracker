using MediatR;

namespace MyFinanceTracker.UseCases.Transaction.Delete;

public record DeleteTransactionsRequest(
    string? CategoryAlias = null,
    DateOnly? Date = null
) : IRequest<DeleteTransactionsResponse>
{
    public override string ToString()
    {
        var parts = new List<string>();

        if (!string.IsNullOrWhiteSpace(CategoryAlias))
        {
            parts.Add($"Category: {CategoryAlias}");
        }

        if (Date.HasValue)
        {
            parts.Add($"Date: {Date.Value:dd.MM.yyyy}");
        }

        return parts.Count > 0
            ? string.Join(" | ", parts)
            : "Empty";
    }
}