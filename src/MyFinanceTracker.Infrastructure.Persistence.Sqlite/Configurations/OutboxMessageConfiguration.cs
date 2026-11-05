using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyFinanceTracker.Infrastructure.Persistence.Sqlite.Entities;

namespace MyFinanceTracker.Infrastructure.Persistence.Sqlite.Configurations;

internal sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessageEntity>
{
    public void Configure(EntityTypeBuilder<OutboxMessageEntity> builder)
    {
        builder.ToTable("outbox_messages");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .ValueGeneratedOnAdd();

        builder.Property(e => e.OperationType)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(e => e.PayloadJson)
            .IsRequired();

        builder.Property(e => e.CreatedAtUtc)
            .IsRequired();

        builder.Property(e => e.RetryCount)
            .IsRequired()
            .HasDefaultValue(0);
    }
}
