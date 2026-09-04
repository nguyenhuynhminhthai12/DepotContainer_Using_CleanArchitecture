using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TechSpherex.CleanArchitecture.Domain.Entities;

namespace TechSpherex.CleanArchitecture.Infrastructure.Persistence.Configurations;

public sealed class ContainerTypeConfiguration : IEntityTypeConfiguration<ContainerType>
{
#pragma warning disable CA1822, S2325 // Configure must implement IEntityTypeConfiguration interface method
    public void Configure(EntityTypeBuilder<ContainerType> builder)
    {
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Code).IsRequired().HasMaxLength(20);
        builder.Property(t => t.Name).IsRequired().HasMaxLength(100);
        builder.Property(t => t.Family).IsRequired().HasMaxLength(50);
        builder.Property(t => t.Description).HasMaxLength(500);

        builder.HasIndex(t => new { t.TenantId, t.Code }).IsUnique();
    }
}