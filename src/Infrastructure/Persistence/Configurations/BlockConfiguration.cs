using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TechSpherex.CleanArchitecture.Domain.Entities;

namespace TechSpherex.CleanArchitecture.Infrastructure.Persistence.Configurations;

/// <summary>
/// Cấu hình ánh xạ Entity Framework Core cho thực thể Phân khu bãi <see cref="Block"/> trong Depot.
/// Thiết lập ràng buộc duy nhất theo mã Block trong từng Depot và quan hệ với Depot cha.
/// </summary>
public sealed class BlockConfiguration : IEntityTypeConfiguration<Block>
{
    /// <inheritdoc/>
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
