using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TechSpherex.CleanArchitecture.Domain.Entities;

namespace TechSpherex.CleanArchitecture.Infrastructure.Persistence.Configurations;

public sealed class ContainerMovementConfiguration : IEntityTypeConfiguration<ContainerMovement>
{
    public void Configure(EntityTypeBuilder<ContainerMovement> builder)
    {
        builder.HasKey(m => m.Id);

        builder.Property(m => m.Classification).IsRequired().HasMaxLength(10);
        builder.Property(m => m.ConditionAtGateIn).HasConversion<string>().HasMaxLength(20);
        builder.Property(m => m.ConditionAtGateOut).HasConversion<string>().HasMaxLength(20);
        builder.Property(m => m.Status).HasConversion<string>().HasMaxLength(20);

        builder.Property(m => m.VehicleInNumber).HasMaxLength(50);
        builder.Property(m => m.DriverInName).HasMaxLength(200);
        builder.Property(m => m.VehicleOutNumber).HasMaxLength(50);
        builder.Property(m => m.DriverOutName).HasMaxLength(200);

        builder.HasIndex(m => m.Status);
        builder.HasIndex(m => m.GateInAt);
        builder.HasIndex(m => m.GateOutAt);
        builder.HasIndex(m => new { m.TenantId, m.ContainerId, m.GateInAt });

        builder.HasOne(m => m.Container)
            .WithMany()
            .HasForeignKey(m => m.ContainerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(m => m.LineOperator)
            .WithMany()
            .HasForeignKey(m => m.LineOperatorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(m => m.YardSlot)
            .WithMany()
            .HasForeignKey(m => m.YardSlotId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(m => m.Block)
            .WithMany()
            .HasForeignKey(m => m.BlockId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(m => m.DeliveryOrder)
            .WithMany()
            .HasForeignKey(m => m.DeliveryOrderId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}