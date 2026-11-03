using MediatR;
using MyFinanceTracker.Domain.Entities;

namespace MyFinanceTracker.UseCases.Transaction.Get;

public record GetTransactionsRequest(
    DateOnly? FromDate = null,
    DateOnly? ToDate = null,
    IReadOnlyList<string>? CategoryAliases = null,
    TransactionType? Type = null
) : IRequest<GetTransactionsResponse>
{
    public override string ToString()
    {
        var parts = new List<string>();

        if (FromDate.HasValue)
        {
            parts.Add($"From: {FromDate.Value:dd.MM.yyyy}");
        }

        if (ToDate.HasValue)
        {
            parts.Add($"To: {ToDate.Value:dd.MM.yyyy}");
        }

        if (CategoryAliases is { Count: > 0 })
        {
            parts.Add($"Categories: {string.Join(", ", CategoryAliases)}");
        }

        if (Type.HasValue)
        {
            parts.Add($"Type: {Type.Value}");
        }

        return parts.Count > 0
            ? string.Join(" | ", parts)
            : "Empty";
    }
}
