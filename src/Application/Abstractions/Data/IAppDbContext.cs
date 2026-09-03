using TechSpherex.CleanArchitecture.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace TechSpherex.CleanArchitecture.Application.Abstractions.Data;

/// <summary>
/// Giao diện DbContext trừu tượng cho tầng Application.
/// Cung cấp truy cập đến các <see cref="DbSet{TEntity}"/> và phương thức lưu thay đổi.
/// </summary>
public interface IAppDbContext
{
    /// <summary>Tập hợp các công việc Todo.</summary>
    DbSet<TodoItem> Todos { get; }

    // Depot domain
    /// <summary>Tập hợp các depot.</summary>
    DbSet<Depot> Depots { get; }

    /// <summary>Tập hợp các block trong depot.</summary>
    DbSet<Block> Blocks { get; }

    /// <summary>Tập hợp các vị trí yard slot.</summary>
    DbSet<YardSlot> YardSlots { get; }

    /// <summary>Tập hợp các loại container.</summary>
    DbSet<ContainerType> ContainerTypes { get; }

    /// <summary>Tập hợp các container.</summary>
    DbSet<Container> Containers { get; }

    /// <summary>Tập hợp các hành đường (line operators).</summary>
    DbSet<LineOperator> LineOperators { get; }

    /// <summary>Tập hợp các bản ghi di chuyển container (EIR).</summary>
    DbSet<ContainerMovement> ContainerMovements { get; }

    /// <summary>Tập hợp các khách hàng.</summary>
    DbSet<Customer> Customers { get; }

    /// <summary>Tập hợp các đơn giao hàng.</summary>
    DbSet<DeliveryOrder> DeliveryOrders { get; }

    /// <summary>Tập hợp các dòng chi tiết đơn giao hàng.</summary>
    DbSet<DeliveryOrderLine> DeliveryOrderLines { get; }

    /// <summary>Lưu tất cả thay đổi vào cơ sở dữ liệu.</summary>
    /// <param name="cancellationToken">Token hủy.</param>
    /// <returns>Số bản ghi đã được lưu.</returns>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
