namespace MyFinanceTracker.Infrastructure.Persistence.Sqlite.Entities;

internal enum OutboxOperationType
{
    Add = 1,
    Delete = 2
}
