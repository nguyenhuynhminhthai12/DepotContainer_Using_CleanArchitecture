using TechSpherex.CleanArchitecture.Application.Abstractions.Data;
using TechSpherex.CleanArchitecture.Application.Abstractions.Messaging;
using TechSpherex.CleanArchitecture.Domain.Common;
using TechSpherex.CleanArchitecture.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace TechSpherex.CleanArchitecture.Application.Features.DeliveryOrders;

public sealed class CreateDeliveryOrderCommandHandler(IAppDbContext dbContext) :
    ICommandHandler<CreateDeliveryOrderCommand, Result<DeliveryOrderResponse>>
{
    public async Task<Result<DeliveryOrderResponse>> HandleAsync(CreateDeliveryOrderCommand command, CancellationToken cancellationToken = default)
    {
        if (!await dbContext.Customers.AnyAsync(c => c.Id == command.CustomerId, cancellationToken))
        {
            return Result.Failure<DeliveryOrderResponse>(Error.NotFound("Customer.NotFound",
                $"Customer '{command.CustomerId}' was not found."));
        }

        if (!await dbContext.LineOperators.AnyAsync(l => l.Id == command.LineOperatorId, cancellationToken))
        {
            return Result.Failure<DeliveryOrderResponse>(Error.NotFound("LineOperator.NotFound",
                $"Line Operator '{command.LineOperatorId}' was not found."));
        }

        var exists = await dbContext.DeliveryOrders
            .AnyAsync(d => d.OrderNumber == command.OrderNumber, cancellationToken);
        if (exists)
        {
            return Result.Failure<DeliveryOrderResponse>(Error.Conflict("DeliveryOrder.Duplicate",
                $"Delivery order '{command.OrderNumber}' already exists."));
        }

        var order = new DeliveryOrder
        {
            OrderNumber = command.OrderNumber.Trim(),
            CustomerId = command.CustomerId,
            LineOperatorId = command.LineOperatorId,
            ExpiryDate = command.ExpiryDate,
            VesselVoyage = command.VesselVoyage,
            Notes = command.Notes
        };

        foreach (var line in command.Lines)
        {
            order.Lines.Add(new DeliveryOrderLine
            {
                ContainerTypeId = line.ContainerTypeId,
                RequestedQuantity = line.RequestedQuantity,
                DeliveredQuantity = line.DeliveredQuantity
            });
        }

        dbContext.DeliveryOrders.Add(order);
        await dbContext.SaveChangesAsync(cancellationToken);

        var customer = await dbContext.Customers.AsNoTracking().FirstAsync(c => c.Id == order.CustomerId, cancellationToken);
        var lineOp = await dbContext.LineOperators.AsNoTracking().FirstAsync(l => l.Id == order.LineOperatorId, cancellationToken);

        return Result.Success(Map(order, customer.Name, lineOp.Name));
    }

    internal static DeliveryOrderResponse Map(DeliveryOrder order, string customerName, string lineOperatorName) => new(
        order.Id, order.OrderNumber, order.CustomerId, customerName,
        order.LineOperatorId, lineOperatorName,
        order.ExpiryDate, order.VesselVoyage, order.IsClosed,
        [.. order.Lines.Select(l => new DeliveryOrderLineDto(l.ContainerTypeId, l.RequestedQuantity, l.DeliveredQuantity))]);
}

public sealed class GetDeliveryOrderByIdQueryHandler(IAppDbContext dbContext) :
    IQueryHandler<GetDeliveryOrderByIdQuery, Result<DeliveryOrderResponse>>
{
    public async Task<Result<DeliveryOrderResponse>> HandleAsync(GetDeliveryOrderByIdQuery query, CancellationToken cancellationToken = default)
    {
        var order = await dbContext.DeliveryOrders
            .Include(d => d.Lines)
            .Include(d => d.Customer)
            .Include(d => d.LineOperator)
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == query.Id, cancellationToken);

        if (order is null)
        {
            return Result.Failure<DeliveryOrderResponse>(Error.NotFound("DeliveryOrder.NotFound",
                $"Delivery order '{query.Id}' was not found."));
        }

        return Result.Success(new DeliveryOrderResponse(
            order.Id, order.OrderNumber,
            order.CustomerId, order.Customer?.Name ?? string.Empty,
            order.LineOperatorId, order.LineOperator?.Name ?? string.Empty,
            order.ExpiryDate, order.VesselVoyage, order.IsClosed,
            [.. order.Lines.Select(l => new DeliveryOrderLineDto(l.ContainerTypeId, l.RequestedQuantity, l.DeliveredQuantity))]));
    }
}

public sealed class GetActiveDeliveryOrdersQueryHandler(IAppDbContext dbContext) :
    IQueryHandler<GetActiveDeliveryOrdersQuery, Result<IReadOnlyList<DeliveryOrderResponse>>>
{
    public async Task<Result<IReadOnlyList<DeliveryOrderResponse>>> HandleAsync(GetActiveDeliveryOrdersQuery query, CancellationToken cancellationToken = default)
    {
        var orders = await dbContext.DeliveryOrders
            .Include(d => d.Lines)
            .Include(d => d.Customer)
            .Include(d => d.LineOperator)
            .AsNoTracking()
            .Where(d => !d.IsClosed && d.ExpiryDate >= DateTimeOffset.UtcNow)
            .OrderBy(d => d.ExpiryDate)
            .Select(o => new DeliveryOrderResponse(
                o.Id, o.OrderNumber,
                o.CustomerId, o.Customer != null ? o.Customer.Name : string.Empty,
                o.LineOperatorId, o.LineOperator != null ? o.LineOperator.Name : string.Empty,
                o.ExpiryDate, o.VesselVoyage, o.IsClosed,
                o.Lines.Select(l => new DeliveryOrderLineDto(l.ContainerTypeId, l.RequestedQuantity, l.DeliveredQuantity)).ToList()))
            .ToListAsync(cancellationToken);

        return Result.Success<IReadOnlyList<DeliveryOrderResponse>>(orders);
    }
}

public sealed class CloseDeliveryOrderCommandHandler(IAppDbContext dbContext) :
    ICommandHandler<CloseDeliveryOrderCommand, Result>
{
    public async Task<Result> HandleAsync(CloseDeliveryOrderCommand command, CancellationToken cancellationToken = default)
    {
        var order = await dbContext.DeliveryOrders
            .FirstOrDefaultAsync(d => d.Id == command.Id, cancellationToken);
        if (order is null)
        {
            return Result.Failure(Error.NotFound("DeliveryOrder.NotFound",
                $"Delivery order '{command.Id}' was not found."));
        }

        order.IsClosed = true;
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}

public sealed class UpdateDeliveryOrderCommandHandler(IAppDbContext dbContext) :
    ICommandHandler<UpdateDeliveryOrderCommand, Result<DeliveryOrderResponse>>
{
    public async Task<Result<DeliveryOrderResponse>> HandleAsync(UpdateDeliveryOrderCommand command, CancellationToken cancellationToken = default)
    {
        var order = await dbContext.DeliveryOrders
            .Include(d => d.Lines)
            .Include(d => d.Customer)
            .Include(d => d.LineOperator)
            .FirstOrDefaultAsync(d => d.Id == command.Id, cancellationToken);

        if (order is null)
        {
            return Result.Failure<DeliveryOrderResponse>(Error.NotFound("DeliveryOrder.NotFound",
                $"Delivery order '{command.Id}' was not found."));
        }

        order.ExpiryDate = command.ExpiryDate;
        order.VesselVoyage = command.VesselVoyage;
        order.Notes = command.Notes;

        if (command.Lines?.Count > 0)
        {
            foreach (var reqLine in command.Lines)
            {
                var existingLine = order.Lines.FirstOrDefault(l => l.ContainerTypeId == reqLine.ContainerTypeId);
                if (existingLine is not null)
                {
                    existingLine.RequestedQuantity = Math.Max(reqLine.RequestedQuantity, existingLine.DeliveredQuantity);
                }
                else if (order.Lines.Count == 1 && command.Lines.Count == 1)
                {
                    // Update single line container type and quantity
                    var firstLine = order.Lines.First();
                    firstLine.ContainerTypeId = reqLine.ContainerTypeId;
                    firstLine.RequestedQuantity = Math.Max(reqLine.RequestedQuantity, firstLine.DeliveredQuantity);
                }
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success(CreateDeliveryOrderCommandHandler.Map(
            order,
            order.Customer?.Name ?? string.Empty,
            order.LineOperator?.Name ?? string.Empty));
    }
}

public sealed class DeleteDeliveryOrderCommandHandler(IAppDbContext dbContext) :
    ICommandHandler<DeleteDeliveryOrderCommand, Result>
{
    public async Task<Result> HandleAsync(DeleteDeliveryOrderCommand command, CancellationToken cancellationToken = default)
    {
        var order = await dbContext.DeliveryOrders
            .Include(d => d.Lines)
            .FirstOrDefaultAsync(d => d.Id == command.Id, cancellationToken);

        if (order is null)
        {
            return Result.Failure(Error.NotFound("DeliveryOrder.NotFound",
                $"Delivery order '{command.Id}' was not found."));
        }

        if (order.Lines.Any(l => l.DeliveredQuantity > 0))
        {
            return Result.Failure(Error.Conflict("DeliveryOrder.HasDeliveredContainersCannotDelete",
                $"Delivery order '{order.OrderNumber}' has already discharged containers and cannot be deleted. Use Close instead."));
        }

        dbContext.DeliveryOrderLines.RemoveRange(order.Lines);
        dbContext.DeliveryOrders.Remove(order);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}