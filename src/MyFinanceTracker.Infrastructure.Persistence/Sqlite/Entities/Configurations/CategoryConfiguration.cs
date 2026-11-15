using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MyFinanceTracker.Infrastructure.Persistence.Sqlite.Entities.Configurations;

internal sealed class CategoryConfiguration : IEntityTypeConfiguration<CategoryEntity>
{
    public void Configure(EntityTypeBuilder<CategoryEntity> builder)
    {
        builder.ToTable("categories");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .UseCollation("NOCASE")
            .IsRequired();

        builder.Property(e => e.Name)
            .IsRequired();

        builder.Property(e => e.IsArchived)
            .IsRequired();
    }
}
