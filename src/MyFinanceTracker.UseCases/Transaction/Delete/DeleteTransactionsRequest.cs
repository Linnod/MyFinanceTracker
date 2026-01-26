using MediatR;
using MyFinanceTracker.Common.Contracts;

namespace MyFinanceTracker.UseCases.Transaction.Delete;

public record DeleteTransactionsRequest(
    string? CategoryAlias = null, 
    DateOnly? Date = null
) : IRequest<DeleteTransactionsResponse>, ILoggableRequest
{
    string ILoggableRequest.GetLogPayload()
    {
        var parts = new List<string>();

        if (!string.IsNullOrWhiteSpace(CategoryAlias))
        {
            parts.Add($"Cat: {CategoryAlias}");
        }

        if (Date.HasValue)
        {
            parts.Add($"Date: {Date.Value:dd.MM.yyyy}");
        }

        return parts.Count > 0 
            ? $"Delete -> {string.Join(" | ", parts)}" 
            : "Delete -> EMPTY REQUEST";
    }
}