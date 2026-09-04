using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TechSpherex.CleanArchitecture.Domain.Entities;

namespace TechSpherex.CleanArchitecture.Infrastructure.Persistence.Configurations;

public sealed class BlockConfiguration : IEntityTypeConfiguration<Block>
{
#pragma warning disable CA1822, S2325 // Configure must implement IEntityTypeConfiguration interface method
    public void Configure(EntityTypeBuilder<Block> builder)
    {
        builder.HasKey(b => b.Id);

        builder.Property(b => b.Code)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(b => b.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasIndex(b => new { b.DepotId, b.Code }).IsUnique();

        builder.HasOne(b => b.Depot)
            .WithMany()
            .HasForeignKey(b => b.DepotId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}