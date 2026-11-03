using MediatR;

namespace MyFinanceTracker.UseCases.Transaction.Delete;

public record DeleteTransactionsRequest(
    IReadOnlyList<Guid> Ids
) : IRequest<DeleteTransactionsResponse>
{
    public override string ToString() =>
        Ids is { Count: > 0 } ? $"Ids: [{string.Join(", ", Ids)}]" : "Empty";
}
