using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TechSpherex.CleanArchitecture.Domain.Entities;

namespace TechSpherex.CleanArchitecture.Infrastructure.Persistence.Configurations;

public sealed class LineOperatorConfiguration : IEntityTypeConfiguration<LineOperator>
{
#pragma warning disable CA1822, S2325 // Configure must implement IEntityTypeConfiguration interface method
    public void Configure(EntityTypeBuilder<LineOperator> builder)
    {
        builder.HasKey(l => l.Id);

        builder.Property(l => l.Code).IsRequired().HasMaxLength(10);
        builder.Property(l => l.Name).IsRequired().HasMaxLength(200);
        builder.Property(l => l.Country).HasMaxLength(100);

        builder.HasIndex(l => new { l.TenantId, l.Code }).IsUnique();
    }
}