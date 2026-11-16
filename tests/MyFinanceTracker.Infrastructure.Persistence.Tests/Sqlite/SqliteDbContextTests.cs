using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using MyFinanceTracker.Infrastructure.Persistence.Sqlite.Data;

namespace MyFinanceTracker.Infrastructure.Persistence.Tests.Sqlite;

public sealed class SqliteDbContextTests
{
    [Fact]
    public void HasPendingModelChanges_CurrentModel_ReturnsFalse()
    {
        // arrange
        var options = new DbContextOptionsBuilder<SqliteDbContext>()
            .UseSqlite("DataSource=:memory:")
            .Options;

        using var context = new SqliteDbContext(options);

        // act
        var hasPendingChanges = context.Database.HasPendingModelChanges();

        // assert
        hasPendingChanges.Should().BeFalse();
    }
}
