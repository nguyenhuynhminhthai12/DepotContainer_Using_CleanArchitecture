using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using TechSpherex.CleanArchitecture.Application.Abstractions.Data;
using TechSpherex.CleanArchitecture.Application.Abstractions.Tenancy;
using TechSpherex.CleanArchitecture.Domain.Common;
using TechSpherex.CleanArchitecture.Domain.Entities;

namespace TechSpherex.CleanArchitecture.Infrastructure.Persistence;

// Copyright (c) 2026 TechSpherex
public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : IdentityDbContext<ApplicationUser>(options), IAppDbContext
{
    public DbSet<TodoItem> Todos => Set<TodoItem>();

    public DbSet<Depot> Depots => Set<Depot>();
    public DbSet<Block> Blocks => Set<Block>();
    public DbSet<YardSlot> YardSlots => Set<YardSlot>();
    public DbSet<ContainerType> ContainerTypes => Set<ContainerType>();
    public DbSet<Container> Containers => Set<Container>();
    public DbSet<LineOperator> LineOperators => Set<LineOperator>();
    public DbSet<ContainerMovement> ContainerMovements => Set<ContainerMovement>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<DeliveryOrder> DeliveryOrders => Set<DeliveryOrder>();
    public DbSet<DeliveryOrderLine> DeliveryOrderLines => Set<DeliveryOrderLine>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        foreach (var entityType in builder.Model.GetEntityTypes().Where(e => typeof(ITenantEntity).IsAssignableFrom(e.ClrType)))
        {
            var method = typeof(AppDbContext)
                .GetMethod(nameof(ApplyTenantFilter),
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!
                .MakeGenericMethod(entityType.ClrType);

            method.Invoke(null, [builder]);
        }
    }

    private static void ApplyTenantFilter<TEntity>(ModelBuilder builder)
        where TEntity : class, ITenantEntity
    {
        builder.Entity<TEntity>().HasQueryFilter(e => e.TenantId == "default");
        builder.Entity<TEntity>().HasIndex(e => e.TenantId);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateAuditableEntities();
        SetTenantId();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void UpdateAuditableEntities()
    {
        foreach (var entry in ChangeTracker.Entries<AuditableEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = DateTimeOffset.UtcNow;
                    break;
                case EntityState.Modified:
                    entry.Entity.LastModifiedAt = DateTimeOffset.UtcNow;
                    break;
            }
        }
    }

    private void SetTenantId()
    {
        var serviceProvider = this.GetInfrastructure();
        var tenantProvider = serviceProvider.GetService<ITenantProvider>();

        if (tenantProvider?.TenantId is null) return;

        foreach (var entry in ChangeTracker.Entries<ITenantEntity>().Where(e => e.State == EntityState.Added))
        {
            entry.Entity.TenantId = tenantProvider.TenantId;
        }
    }
}
