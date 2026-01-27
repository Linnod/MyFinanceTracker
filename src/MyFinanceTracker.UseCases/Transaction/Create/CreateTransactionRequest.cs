using MediatR;
using MyFinanceTracker.Common.Contracts;
using MyFinanceTracker.Domain.Entities;

namespace MyFinanceTracker.UseCases.Transaction.Create;

public record CreateTransactionRequest(
    TransactionType TransactionType,
    decimal[] Amounts,
    string? CategoryAlias = null,
    DateOnly? Date = null,
    string? Note = null
) : IRequest<CreateTransactionResponse>, ILoggableRequest
{
    string ILoggableRequest.GetLogPayload()
    {
        var parts = new List<string>
        {
            TransactionType.ToString(),
            $"Amounts: [{string.Join(", ", Amounts)}]"
        };

        if (!string.IsNullOrWhiteSpace(CategoryAlias))
        {
            parts.Add($"Cat: {CategoryAlias}");
        }

        if (Date.HasValue)
        {
            parts.Add($"Date: {Date.Value:dd.MM.yyyy}");
        }

        if (!string.IsNullOrWhiteSpace(Note))
        {
            parts.Add($"Note: {Note}");
        }

        return string.Join(" | ", parts);
    }
}