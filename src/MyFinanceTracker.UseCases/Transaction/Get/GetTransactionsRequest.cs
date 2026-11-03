using MediatR;
using MyFinanceTracker.Domain.Entities;

namespace MyFinanceTracker.UseCases.Transaction.Get;

public record GetTransactionsRequest(
    DateOnly FromDate,
    DateOnly ToDate,
    IReadOnlyList<string>? CategoryAliases = null,
    TransactionType? Type = null
) : IRequest<GetTransactionsResponse>
{
    public override string ToString()
    {
        var parts = new List<string>
        {
            $"From: {FromDate:dd.MM.yyyy}",
            $"To: {ToDate:dd.MM.yyyy}"
        };

        if (CategoryAliases is { Count: > 0 })
        {
            parts.Add($"Categories: {string.Join(", ", CategoryAliases)}");
        }

        if (Type.HasValue)
        {
            parts.Add($"Type: {Type.Value}");
        }

        return string.Join(" | ", parts);
    }
}
