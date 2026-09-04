using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TechSpherex.CleanArchitecture.Domain.Entities;

namespace TechSpherex.CleanArchitecture.Infrastructure.Persistence.Configurations;

public sealed class DepotConfiguration : IEntityTypeConfiguration<Depot>
{
#pragma warning disable CA1822, S2325 // Configure must implement IEntityTypeConfiguration interface method
    public void Configure(EntityTypeBuilder<Depot> builder)
    {
        builder.HasKey(d => d.Id);

        builder.Property(d => d.Code)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(d => d.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(d => d.Address)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(d => d.TimeZone).HasMaxLength(50);

        builder.HasIndex(d => d.Code).IsUnique();
    }
}