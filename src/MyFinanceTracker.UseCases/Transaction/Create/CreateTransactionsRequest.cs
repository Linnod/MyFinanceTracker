using MediatR;

namespace MyFinanceTracker.UseCases.Transaction.Create;

public record CreateTransactionsRequest(
    IReadOnlyList<CreateTransactionItem> Items
) : IRequest<CreateTransactionsResponse>
{
    public override string ToString() =>
        $"CreateTransactionsBatch | {Items.Count} items";
}