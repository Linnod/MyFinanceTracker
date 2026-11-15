using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace MyFinanceTracker.Infrastructure.Persistence.Sqlite.Data;

/// <summary>
/// Provides a design-time <see cref="SqliteDbContext"/> instance for EF Core CLI tooling without bootstrapping the full host.
/// </summary>
internal sealed class SqliteDbContextDesignTimeFactory : IDesignTimeDbContextFactory<SqliteDbContext>
{
    public SqliteDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<SqliteDbContext>();
        optionsBuilder.UseSqlite();
        return new SqliteDbContext(optionsBuilder.Options);
    }
}
