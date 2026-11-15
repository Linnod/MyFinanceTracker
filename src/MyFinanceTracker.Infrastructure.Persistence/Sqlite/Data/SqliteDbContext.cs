using Microsoft.EntityFrameworkCore;
using MyFinanceTracker.Infrastructure.Persistence.Sqlite.Entities;

namespace MyFinanceTracker.Infrastructure.Persistence.Sqlite.Data;

internal sealed class SqliteDbContext(DbContextOptions<SqliteDbContext> options) : DbContext(options)
{
    public DbSet<CategoryEntity> Categories => Set<CategoryEntity>();
    public DbSet<TransactionEntity> Transactions => Set<TransactionEntity>();
    public DbSet<OutboxMessageEntity> OutboxMessages => Set<OutboxMessageEntity>();
    public DbSet<DeadLetterMessageEntity> DeadLetterMessages => Set<DeadLetterMessageEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SqliteDbContext).Assembly);
    }
}
