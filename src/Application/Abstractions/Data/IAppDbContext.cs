using TechSpherex.CleanArchitecture.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace TechSpherex.CleanArchitecture.Application.Abstractions.Data;

public interface IAppDbContext
{
    DbSet<TodoItem> Todos { get; }

    // Depot domain
    DbSet<Depot> Depots { get; }
    DbSet<Block> Blocks { get; }
    DbSet<YardSlot> YardSlots { get; }
    DbSet<ContainerType> ContainerTypes { get; }
    DbSet<Container> Containers { get; }
    DbSet<LineOperator> LineOperators { get; }
    DbSet<ContainerMovement> ContainerMovements { get; }
    DbSet<Customer> Customers { get; }
    DbSet<DeliveryOrder> DeliveryOrders { get; }
    DbSet<DeliveryOrderLine> DeliveryOrderLines { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}