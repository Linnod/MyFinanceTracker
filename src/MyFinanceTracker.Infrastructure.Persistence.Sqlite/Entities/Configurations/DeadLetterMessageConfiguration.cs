using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyFinanceTracker.Infrastructure.Persistence.Sqlite.Entities;

namespace MyFinanceTracker.Infrastructure.Persistence.Sqlite.Entities.Configurations;

internal sealed class DeadLetterMessageConfiguration : IEntityTypeConfiguration<DeadLetterMessageEntity>
{
    public void Configure(EntityTypeBuilder<DeadLetterMessageEntity> builder)
    {
        builder.ToTable("dead_letter_messages");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .ValueGeneratedOnAdd();

        builder.Property(e => e.OriginalOutboxMessageId)
            .IsRequired();

        builder.Property(e => e.OperationType)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(e => e.PayloadJson)
            .IsRequired();

        builder.Property(e => e.CreatedAtUtc)
            .IsRequired();

        builder.Property(e => e.FailedAtUtc)
            .IsRequired();

        builder.Property(e => e.RetryCount)
            .IsRequired();

        builder.Property(e => e.ErrorMessage)
            .IsRequired();
    }
}
