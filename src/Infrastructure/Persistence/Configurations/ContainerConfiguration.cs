using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TechSpherex.CleanArchitecture.Domain.Entities;

namespace TechSpherex.CleanArchitecture.Infrastructure.Persistence.Configurations;

public sealed class ContainerConfiguration : IEntityTypeConfiguration<Container>
{
#pragma warning disable S2325 // Configure must implement IEntityTypeConfiguration interface method
    public void Configure(EntityTypeBuilder<Container> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property(c => c.ContainerNumberRaw)
            .IsRequired()
            .HasMaxLength(11);

        builder.Property(c => c.IsoCode).IsRequired().HasMaxLength(20);
        builder.Property(c => c.SizeFeet).IsRequired();
        builder.Property(c => c.MaxWeightKg).HasPrecision(10, 2);
        builder.Property(c => c.TareWeightKg).HasPrecision(10, 2);
        builder.Property(c => c.Owner).IsRequired().HasMaxLength(100);

        builder.Property(c => c.Condition)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.HasIndex(c => new { c.TenantId, c.ContainerNumberRaw }).IsUnique();
        builder.HasIndex(c => c.Condition);

        builder.HasOne(c => c.ContainerType)
            .WithMany()
            .HasForeignKey(c => c.ContainerTypeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}