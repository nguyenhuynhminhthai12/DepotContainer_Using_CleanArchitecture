using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TechSpherex.CleanArchitecture.Domain.Entities;

namespace TechSpherex.CleanArchitecture.Infrastructure.Persistence.Configurations;

public sealed class YardSlotConfiguration : IEntityTypeConfiguration<YardSlot>
{
    public void Configure(EntityTypeBuilder<YardSlot> builder)
    {
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Bay).IsRequired();
        builder.Property(s => s.Row).IsRequired();
        builder.Property(s => s.Tier).IsRequired();

        builder.HasIndex(s => new { s.BlockId, s.Bay, s.Row, s.Tier }).IsUnique();

        builder.HasOne(s => s.Block)
            .WithMany()
            .HasForeignKey(s => s.BlockId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(s => s.IsOccupied);
    }
}