using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TechSpherex.CleanArchitecture.Domain.Entities;

namespace TechSpherex.CleanArchitecture.Infrastructure.Persistence.Configurations;

/// <summary>
/// Cấu hình ánh xạ Entity Framework Core cho thực thể Vị trí ô bãi <see cref="YardSlot"/> (Tọa độ Bay, Row, Tier trong Block).
/// </summary>
public sealed class YardSlotConfiguration : IEntityTypeConfiguration<YardSlot>
{
    /// <inheritdoc/>
#pragma warning disable CA1822, S2325 // Configure must implement IEntityTypeConfiguration interface method
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
