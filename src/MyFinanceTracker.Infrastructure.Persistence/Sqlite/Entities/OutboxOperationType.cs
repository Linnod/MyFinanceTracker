namespace MyFinanceTracker.Infrastructure.Persistence.Sqlite.Entities;

internal enum OutboxOperationType
{
    AddTransaction = 1,
    DeleteTransaction = 2,
    UpdateTransaction = 3,
    AddCategory = 4,
    UpdateCategory = 5
}
