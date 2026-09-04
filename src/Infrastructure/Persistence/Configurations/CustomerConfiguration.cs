using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TechSpherex.CleanArchitecture.Domain.Entities;

namespace TechSpherex.CleanArchitecture.Infrastructure.Persistence.Configurations;

public sealed class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
#pragma warning disable CA1822, S2325 // Configure must implement IEntityTypeConfiguration interface method
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property(c => c.TaxCode).IsRequired().HasMaxLength(20);
        builder.Property(c => c.Name).IsRequired().HasMaxLength(200);
        builder.Property(c => c.Address).HasMaxLength(500);
        builder.Property(c => c.Phone).HasMaxLength(30);
        builder.Property(c => c.Email).HasMaxLength(200);

        builder.HasIndex(c => new { c.TenantId, c.TaxCode }).IsUnique();
    }
}