namespace MyFinanceTracker.Infrastructure.Persistence.Sqlite.Entities;

internal sealed class CategoryEntity
{
    public required string Id { get; set; }

    public required string Name { get; set; }

    public bool IsArchived { get; set; }
}
