using MediatR;

namespace MyFinanceTracker.UseCases.Transaction.Get;

public record GetTransactionsRequest(
    string? CategoryAlias = null,
    DateOnly? Date = null
) : IRequest<GetTransactionsResponse>
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