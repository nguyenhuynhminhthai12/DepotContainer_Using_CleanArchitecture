using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TechSpherex.CleanArchitecture.Domain.Entities;

namespace TechSpherex.CleanArchitecture.Infrastructure.Persistence.Configurations;

public sealed class DeliveryOrderConfiguration : IEntityTypeConfiguration<DeliveryOrder>
{
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

public sealed class DeliveryOrderLineConfiguration : IEntityTypeConfiguration<DeliveryOrderLine>
{
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