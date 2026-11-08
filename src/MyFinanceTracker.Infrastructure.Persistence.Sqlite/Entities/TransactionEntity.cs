using MyFinanceTracker.Domain.Entities;

namespace MyFinanceTracker.Infrastructure.Persistence.Sqlite.Entities;

internal sealed class TransactionEntity
{
    public Guid Id { get; set; }

    public TransactionType Type { get; set; }

    public required string CategoryId { get; set; }

    public decimal Amount { get; set; }

    public DateTime Date { get; set; }

    public string? Note { get; set; }
}
