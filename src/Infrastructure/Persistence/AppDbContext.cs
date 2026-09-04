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
/// <summary>
/// DbContext chính kết hợp ASP.NET Core Identity và các bảng domain của depot.
/// Tự động cập nhật thời gian bản ghi (auditable) và áp dụng bộ lọc tenant.
/// </summary>
public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : IdentityDbContext<ApplicationUser>(options), IAppDbContext
{
    /// <summary>Tập hợp các mục công việc TodoItem.</summary>
    public DbSet<TodoItem> Todos => Set<TodoItem>();

    #region Depot domain DbSets
    /// <summary>Tập hợp các depot.</summary>
    public DbSet<Depot> Depots => Set<Depot>();

    /// <summary>Tập hợp các block trong depot.</summary>
    public DbSet<Block> Blocks => Set<Block>();

    /// <summary>Tập hợp các vị trí yard slot.</summary>
    public DbSet<YardSlot> YardSlots => Set<YardSlot>();

    /// <summary>Tập hợp các loại container.</summary>
    public DbSet<ContainerType> ContainerTypes => Set<ContainerType>();

    /// <summary>Tập hợp các container.</summary>
    public DbSet<Container> Containers => Set<Container>();

    /// <summary>Tập hợp các hành đường (line operators).</summary>
    public DbSet<LineOperator> LineOperators => Set<LineOperator>();

    /// <summary>Tập hợp các bản ghi di chuyển container (EIR).</summary>
    public DbSet<ContainerMovement> ContainerMovements => Set<ContainerMovement>();

    /// <summary>Tập hợp các khách hàng.</summary>
    public DbSet<Customer> Customers => Set<Customer>();

    /// <summary>Tập hợp các đơn giao hàng.</summary>
    public DbSet<DeliveryOrder> DeliveryOrders => Set<DeliveryOrder>();

    /// <summary>Tập hợp các dòng chi tiết đơn giao hàng.</summary>
    public DbSet<DeliveryOrderLine> DeliveryOrderLines => Set<DeliveryOrderLine>();
    #endregion

    /// <summary>
    /// Cấu hình model — áp dụng cấu hình từ assembly và thêm bộ lọc tenant cho các thực thể ITenantEntity.
    /// </summary>
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        foreach (var entityType in builder.Model.GetEntityTypes().Where(e => typeof(ITenantEntity).IsAssignableFrom(e.ClrType)))
        {
#pragma warning disable S3011 // Safe reflection use for EF Core tenant filter
            var method = typeof(AppDbContext)
                .GetMethod(nameof(ApplyTenantFilter),
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!
                .MakeGenericMethod(entityType.ClrType);

            method.Invoke(null, [builder]);
#pragma warning restore S3011 // Safe reflection use for EF Core tenant filter
        }
    }

    /// <summary>
    /// Áp dụng bộ lọc truy vấn và chỉ mục cho entity kiểu <typeparamref name="TEntity"/>.
    /// Hiện tại luôn lọc theo TenantId = "default".
    /// </summary>
    private static void ApplyTenantFilter<TEntity>(ModelBuilder builder)
        where TEntity : class, ITenantEntity
    {
        builder.Entity<TEntity>().HasQueryFilter(e => e.TenantId == "default");
        builder.Entity<TEntity>().HasIndex(e => e.TenantId);
    }

    /// <summary>
    /// Lưu thay đổi — tự động cập nhật thời gian auditable và đặt TenantId cho thực thể mới.
    /// </summary>
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateAuditableEntities();
        SetTenantId();
        return base.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Cập nhật CreatedAt / LastModifiedAt cho các thực thể AuditableEntity.
    /// </summary>
#pragma warning disable CA1822, S2325
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

    /// <summary>
    /// Đặt TenantId cho các thực thể ITenantEntity mới được thêm.
    /// </summary>
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
