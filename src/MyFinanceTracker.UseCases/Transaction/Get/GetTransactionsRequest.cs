using MediatR;
using MyFinanceTracker.Domain.Entities;

namespace MyFinanceTracker.UseCases.Transaction.Get;

public record GetTransactionsRequest(
    DateTime FromDate,
    DateTime ToDate,
    IReadOnlyList<string>? CategoryAliases = null,
    TransactionType? Type = null,
    int? Skip = null,
    int? Take = null
) : IRequest<GetTransactionsResponse>
{
    public override string ToString()
    {
        var parts = new List<string>
        {
            $"From: {FromDate:dd.MM.yyyy HH:mm:ss}",
            $"To: {ToDate:dd.MM.yyyy HH:mm:ss}"
        };

        if (CategoryAliases is { Count: > 0 })
        {
            parts.Add($"Categories: {string.Join(", ", CategoryAliases)}");
        }

        if (Type.HasValue)
        {
            parts.Add($"Type: {Type.Value}");
        }

        if (Skip.HasValue)
        {
            parts.Add($"Skip: {Skip.Value}");
        }

        if (Take.HasValue)
        {
            parts.Add($"Take: {Take.Value}");
        }

        return string.Join(" | ", parts);
    }
}
