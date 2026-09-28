using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TechSpherex.CleanArchitecture.Domain.Entities;

namespace TechSpherex.CleanArchitecture.Infrastructure.Persistence.Configurations;

/// <summary>
/// Cấu hình ánh xạ Entity Framework Core cho thực thể Lệnh giao nhận / Đơn giao hàng <see cref="DeliveryOrder"/>.
/// </summary>
public sealed class DeliveryOrderConfiguration : IEntityTypeConfiguration<DeliveryOrder>
{
    /// <inheritdoc/>
#pragma warning disable S2325 // Configure must implement IEntityTypeConfiguration interface method
    public void Configure(EntityTypeBuilder<DeliveryOrder> builder)
    {
        builder.HasKey(d => d.Id);

        builder.Property(d => d.OrderNumber).IsRequired().HasMaxLength(50);
        builder.Property(d => d.VesselVoyage).HasMaxLength(100);
        builder.Property(d => d.Notes).HasMaxLength(1000);

        builder.HasIndex(d => new { d.TenantId, d.OrderNumber }).IsUnique();

        builder.HasOne(d => d.Customer)
            .WithMany()
            .HasForeignKey(d => d.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(d => d.LineOperator)
            .WithMany()
            .HasForeignKey(d => d.LineOperatorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(d => d.Lines)
            .WithOne()
            .HasForeignKey(l => l.DeliveryOrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(d => d.ExpiryDate);
        builder.HasIndex(d => d.IsClosed);
    }
}

/// <summary>
/// Cấu hình ánh xạ Entity Framework Core cho thực thể Dòng chi tiết loại container trong Đơn giao hàng <see cref="DeliveryOrderLine"/>.
/// </summary>
public sealed class DeliveryOrderLineConfiguration : IEntityTypeConfiguration<DeliveryOrderLine>
{
    /// <inheritdoc/>
#pragma warning disable S2325 // Configure must implement IEntityTypeConfiguration interface method
    public void Configure(EntityTypeBuilder<DeliveryOrderLine> builder)
    {
        builder.HasKey(l => l.Id);

        builder.HasOne(l => l.ContainerType)
            .WithMany()
            .HasForeignKey(l => l.ContainerTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(l => new { l.DeliveryOrderId, l.ContainerTypeId }).IsUnique();
    }
}
