using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyFinanceTracker.Infrastructure.Persistence.Sqlite.Entities;

namespace MyFinanceTracker.Infrastructure.Persistence.Sqlite.Entities.Configurations;

internal sealed class TransactionConfiguration : IEntityTypeConfiguration<TransactionEntity>
{
    public void Configure(EntityTypeBuilder<TransactionEntity> builder)
    {
        builder.ToTable("transactions");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Type)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(e => e.CategoryId)
            .IsRequired();

        builder.Property(e => e.Amount)
            .IsRequired();

        builder.Property(e => e.Date)
            .IsRequired();

        builder.Property(e => e.Note);

        builder.HasIndex(e => e.Date);
        builder.HasIndex(e => e.CategoryId);
    }
}
