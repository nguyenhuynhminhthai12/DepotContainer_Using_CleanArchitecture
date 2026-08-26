using TechSpherex.CleanArchitecture.Application.Abstractions.Caching;
using TechSpherex.CleanArchitecture.Application.Abstractions.Data;
using TechSpherex.CleanArchitecture.Application.Abstractions.Messaging;
using TechSpherex.CleanArchitecture.Application.Abstractions.Rules;
using TechSpherex.CleanArchitecture.Domain.Common;
using TechSpherex.CleanArchitecture.Domain.Common.Rules;
using TechSpherex.CleanArchitecture.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace TechSpherex.CleanArchitecture.Application.Features.Gate;

public sealed class GateInContainerCommandHandler(
    IAppDbContext dbContext,
    IRuleEngine ruleEngine,
    ICacheService cache) :
    ICommandHandler<GateInContainerCommand, Result<ContainerMovementResponse>>
{
    public async Task<Result<ContainerMovementResponse>> HandleAsync(GateInContainerCommand command, CancellationToken cancellationToken = default)
    {
        var normalizedNumber = command.ContainerNumber.Trim().ToUpperInvariant();

        var container = await dbContext.Containers
            .FirstOrDefaultAsync(c => c.ContainerNumberRaw == normalizedNumber, cancellationToken);
        if (container is null)
            return Result.Failure<ContainerMovementResponse>(Error.NotFound("Container.NotFound",
                $"Container '{normalizedNumber}' was not found."));

        // Reject if already in-yard (the latest movement is still open).
        var alreadyInYard = await dbContext.ContainerMovements
            .AnyAsync(m => m.ContainerId == container.Id && m.Status == MovementStatus.InYard, cancellationToken);
        if (alreadyInYard)
            return Result.Failure<ContainerMovementResponse>(Error.Conflict("Gate.AlreadyInYard",
                $"Container '{normalizedNumber}' is already in the yard. Move it before a new Gate-In."));

        var block = await dbContext.Blocks.FirstOrDefaultAsync(b => b.Id == command.BlockId, cancellationToken);
        if (block is null)
            return Result.Failure<ContainerMovementResponse>(Error.NotFound("Block.NotFound",
                $"Block '{command.BlockId}' was not found."));

        YardSlot? slot = null;

        if (!block.IsVirtual)
        {
            if (!command.Bay.HasValue || !command.Row.HasValue || !command.Tier.HasValue)
                return Result.Failure<ContainerMovementResponse>(Error.Validation("Gate.BayRowTierRequired",
                    "Bay/Row/Tier are required for non-virtual blocks."));

            slot = await dbContext.YardSlots
                .FirstOrDefaultAsync(s => s.BlockId == block.Id && s.Bay == command.Bay.Value
                    && s.Row == command.Row.Value && s.Tier == command.Tier.Value, cancellationToken);

            if (slot is null)
                return Result.Failure<ContainerMovementResponse>(Error.NotFound("YardSlot.NotFound",
                    "Yard slot not found for the given Block/Bay/Row/Tier."));

            // Rule: Bay parity matches container size.
            var bayRule = new BayParityMatchesContainerSizeRule(slot.Bay, container.SizeFeet);
            if (bayRule.IsBroken())
                return Result.Failure<ContainerMovementResponse>(Error.Validation(bayRule.RuleCode, bayRule.Message));

            // Rule: Slot not occupied.
            var slotRule = new YardSlotNotOccupiedRule(slot.IsOccupied);
            if (slotRule.IsBroken())
                return Result.Failure<ContainerMovementResponse>(Error.Validation(slotRule.RuleCode, slotRule.Message));

            // Config-driven Rule Engine (GateInValidation rule set)
            var ruleContext = new Dictionary<string, object?>
            {
                ["BlockId"] = block.Id,
                ["Bay"] = slot.Bay,
                ["Row"] = slot.Row,
                ["Tier"] = slot.Tier,
                ["SizeFeet"] = container.SizeFeet,
                ["IsOccupied"] = slot.IsOccupied
            };
            var ruleResult = ruleEngine.Evaluate("GateInValidation", ruleContext);
            if (!ruleResult.IsValid)
                return Result.Failure<ContainerMovementResponse>(Error.Validation(ruleResult.Violations[0].RuleCode, ruleResult.Violations[0].Message));
        }

        if (!Enum.TryParse<ContainerCondition>(command.ConditionAtGateIn, true, out var conditionAtGateIn))
            return Result.Failure<ContainerMovementResponse>(Error.Validation("Gate.InvalidCondition",
                "Invalid ConditionAtGateIn value."));

        var movement = new ContainerMovement
        {
            ContainerId = container.Id,
            LineOperatorId = command.LineOperatorId,
            BlockId = block.Id,
            YardSlotId = slot?.Id,
            Classification = command.Classification,
            ConditionAtGateIn = conditionAtGateIn,
            VehicleInNumber = command.VehicleInNumber,
            DriverInName = command.DriverInName,
            GateInAt = DateTimeOffset.UtcNow,
            Status = MovementStatus.InYard
        };

        if (slot is not null)
        {
            slot.IsOccupied = true;
            slot.CurrentContainerId = container.Id;
        }

        dbContext.ContainerMovements.Add(movement);
        await dbContext.SaveChangesAsync(cancellationToken);

        await cache.InvalidateByTagAsync("yard-map", cancellationToken);

        return Result.Success(Map(movement));
    }

    private static ContainerMovementResponse Map(ContainerMovement m) => new(
        m.Id, m.ContainerId, m.LineOperatorId, m.YardSlotId, m.BlockId,
        m.Classification,
        m.ConditionAtGateIn.ToString(),
        m.ConditionAtGateOut?.ToString(),
        m.VehicleInNumber, m.DriverInName, m.GateInAt,
        m.VehicleOutNumber, m.DriverOutName, m.GateOutAt,
        m.Status.ToString(), m.DeliveryOrderId);
}

public sealed class GateOutContainerCommandHandler(
    IAppDbContext dbContext,
    IRuleEngine ruleEngine,
    ICacheService cache) :
    ICommandHandler<GateOutContainerCommand, Result<ContainerMovementResponse>>
{
    public async Task<Result<ContainerMovementResponse>> HandleAsync(GateOutContainerCommand command, CancellationToken cancellationToken = default)
    {
        var normalizedNumber = command.ContainerNumber.Trim().ToUpperInvariant();

        var container = await dbContext.Containers
            .FirstOrDefaultAsync(c => c.ContainerNumberRaw == normalizedNumber, cancellationToken);
        if (container is null)
            return Result.Failure<ContainerMovementResponse>(Error.NotFound("Container.NotFound",
                $"Container '{normalizedNumber}' was not found."));

        var openMovement = await dbContext.ContainerMovements
            .FirstOrDefaultAsync(m => m.ContainerId == container.Id && m.Status == MovementStatus.InYard, cancellationToken);
        if (openMovement is null)
            return Result.Failure<ContainerMovementResponse>(Error.Conflict("Gate.NotInYard",
                $"Container '{normalizedNumber}' is not in the yard."));

        var deliveryOrder = await dbContext.DeliveryOrders
            .Include(d => d.Lines)
            .FirstOrDefaultAsync(d => d.Id == command.DeliveryOrderId, cancellationToken);
        if (deliveryOrder is null)
            return Result.Failure<ContainerMovementResponse>(Error.NotFound("DeliveryOrder.NotFound",
                $"Delivery order '{command.DeliveryOrderId}' was not found."));

        if (deliveryOrder.LineOperatorId != openMovement.LineOperatorId)
            return Result.Failure<ContainerMovementResponse>(Error.Conflict("Gate.LineOperatorMismatch",
                "Delivery order's Line Operator does not match the container's current Line Operator."));

        if (deliveryOrder.IsClosed)
            return Result.Failure<ContainerMovementResponse>(Error.Conflict("DeliveryOrder.Closed",
                "Delivery order has been closed."));

        var expiryRule = new DeliveryOrderNotExpiredRule(deliveryOrder.ExpiryDate, DateTimeOffset.UtcNow);
        if (expiryRule.IsBroken())
            return Result.Failure<ContainerMovementResponse>(Error.Validation(expiryRule.RuleCode, expiryRule.Message));

        var line = deliveryOrder.Lines.FirstOrDefault(l => l.ContainerTypeId == container.ContainerTypeId);
        if (line is null)
            return Result.Failure<ContainerMovementResponse>(Error.Validation("DeliveryOrder.NoLineForType",
                $"Delivery order has no line for container type '{container.ContainerTypeId}'."));

        var qtyRule = new DeliveryOrderQuantityAvailableRule(line.RequestedQuantity, line.DeliveredQuantity);
        if (qtyRule.IsBroken())
            return Result.Failure<ContainerMovementResponse>(Error.Validation(qtyRule.RuleCode, qtyRule.Message));

        // Config-driven Rule Engine (GateOutValidation rule set).
        var ruleContext = new Dictionary<string, object?>
        {
            ["OrderNumber"] = deliveryOrder.OrderNumber,
            ["CustomerId"] = deliveryOrder.CustomerId,
            ["LineOperatorId"] = deliveryOrder.LineOperatorId,
            ["ExpiryDate"] = deliveryOrder.ExpiryDate,
            ["IsClosed"] = deliveryOrder.IsClosed,
            ["ContainerTypeId"] = line.ContainerTypeId,
            ["RequestedQuantity"] = line.RequestedQuantity,
            ["DeliveredQuantity"] = line.DeliveredQuantity
        };
        var ruleResult = ruleEngine.Evaluate("GateOutValidation", ruleContext);
        if (!ruleResult.IsValid)
            return Result.Failure<ContainerMovementResponse>(Error.Validation(ruleResult.Violations[0].RuleCode, ruleResult.Violations[0].Message));

        if (!Enum.TryParse<ContainerCondition>(command.ConditionAtGateOut, true, out var conditionOut))
            return Result.Failure<ContainerMovementResponse>(Error.Validation("Gate.InvalidCondition",
                "Invalid ConditionAtGateOut value."));

        openMovement.Status = MovementStatus.GateOut;
        openMovement.GateOutAt = DateTimeOffset.UtcNow;
        openMovement.VehicleOutNumber = command.VehicleOutNumber;
        openMovement.DriverOutName = command.DriverOutName;
        openMovement.ConditionAtGateOut = conditionOut;
        openMovement.DeliveryOrderId = deliveryOrder.Id;

        if (openMovement.YardSlotId is not null)
        {
            var slot = await dbContext.YardSlots
                .FirstOrDefaultAsync(s => s.Id == openMovement.YardSlotId, cancellationToken);
            if (slot is not null)
            {
                slot.IsOccupied = false;
                slot.CurrentContainerId = null;
            }
        }

        line.DeliveredQuantity += 1;

        await dbContext.SaveChangesAsync(cancellationToken);

        await cache.InvalidateByTagAsync("yard-map", cancellationToken);

        return Result.Success(new ContainerMovementResponse(
            openMovement.Id, openMovement.ContainerId, openMovement.LineOperatorId,
            openMovement.YardSlotId, openMovement.BlockId,
            openMovement.Classification,
            openMovement.ConditionAtGateIn.ToString(),
            openMovement.ConditionAtGateOut?.ToString(),
            openMovement.VehicleInNumber, openMovement.DriverInName, openMovement.GateInAt,
            openMovement.VehicleOutNumber, openMovement.DriverOutName, openMovement.GateOutAt,
            openMovement.Status.ToString(), openMovement.DeliveryOrderId));
    }
}

public sealed class MoveContainerInYardCommandHandler(
    IAppDbContext dbContext,
    ICacheService cache) :
    ICommandHandler<MoveContainerInYardCommand, Result>
{
    public async Task<Result> HandleAsync(MoveContainerInYardCommand command, CancellationToken cancellationToken = default)
    {
        var normalizedNumber = command.ContainerNumber.Trim().ToUpperInvariant();
        var container = await dbContext.Containers
            .FirstOrDefaultAsync(c => c.ContainerNumberRaw == normalizedNumber, cancellationToken);
        if (container is null)
            return Result.Failure(Error.NotFound("Container.NotFound",
                $"Container '{normalizedNumber}' was not found."));

        var openMovement = await dbContext.ContainerMovements
            .FirstOrDefaultAsync(m => m.ContainerId == container.Id && m.Status == MovementStatus.InYard, cancellationToken);
        if (openMovement is null)
            return Result.Failure(Error.Conflict("Gate.NotInYard",
                "Container is not currently in the yard."));

        var block = await dbContext.Blocks.FirstOrDefaultAsync(b => b.Id == command.NewBlockId, cancellationToken);
        if (block is null)
            return Result.Failure(Error.NotFound("Block.NotFound",
                $"Block '{command.NewBlockId}' was not found."));
        if (block.IsVirtual)
            return Result.Failure(Error.Validation("Block.Virtual",
                "Cannot move a container into a virtual block using Bay/Row/Tier."));

        var targetSlot = await dbContext.YardSlots
            .FirstOrDefaultAsync(s => s.BlockId == command.NewBlockId && s.Bay == command.NewBay
                && s.Row == command.NewRow && s.Tier == command.NewTier, cancellationToken);
        if (targetSlot is null)
            return Result.Failure(Error.NotFound("YardSlot.NotFound",
                "Yard slot not found for the given Block/Bay/Row/Tier."));

        var bayRule = new BayParityMatchesContainerSizeRule(targetSlot.Bay, container.SizeFeet);
        if (bayRule.IsBroken())
            return Result.Failure(Error.Validation(bayRule.RuleCode, bayRule.Message));

        var occupyingContainerId = targetSlot.CurrentContainerId;
        if (targetSlot.IsOccupied && occupyingContainerId != container.Id)
            return Result.Failure(Error.Conflict("Yard.SlotOccupied",
                "Yard slot is occupied by another container."));

        // Release old slot
        if (openMovement.YardSlotId is not null)
        {
            var oldSlot = await dbContext.YardSlots
                .FirstOrDefaultAsync(s => s.Id == openMovement.YardSlotId, cancellationToken);
            if (oldSlot is not null)
            {
                oldSlot.IsOccupied = false;
                oldSlot.CurrentContainerId = null;
            }
        }

        targetSlot.IsOccupied = true;
        targetSlot.CurrentContainerId = container.Id;

        openMovement.YardSlotId = targetSlot.Id;
        openMovement.BlockId = block.Id;

        await dbContext.SaveChangesAsync(cancellationToken);
        await cache.InvalidateByTagAsync("yard-map", cancellationToken);

        return Result.Success();
    }
}

public sealed class GetContainerMovementHistoryQueryHandler(IAppDbContext dbContext) :
    IQueryHandler<GetContainerMovementHistoryQuery, Result<IReadOnlyList<ContainerMovementResponse>>>
{
    public async Task<Result<IReadOnlyList<ContainerMovementResponse>>> HandleAsync(GetContainerMovementHistoryQuery query, CancellationToken cancellationToken = default)
    {
        var normalizedNumber = query.ContainerNumber.Trim().ToUpperInvariant();
        var container = await dbContext.Containers
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.ContainerNumberRaw == normalizedNumber, cancellationToken);
        if (container is null)
            return Result.Failure<IReadOnlyList<ContainerMovementResponse>>(Error.NotFound("Container.NotFound",
                $"Container '{normalizedNumber}' was not found."));

        var items = await dbContext.ContainerMovements
            .AsNoTracking()
            .Where(m => m.ContainerId == container.Id)
            .OrderByDescending(m => m.GateInAt)
            .ToListAsync(cancellationToken);

        return Result.Success<IReadOnlyList<ContainerMovementResponse>>(items.Select(m => new ContainerMovementResponse(
            m.Id, m.ContainerId, m.LineOperatorId, m.YardSlotId, m.BlockId,
            m.Classification,
            m.ConditionAtGateIn.ToString(),
            m.ConditionAtGateOut?.ToString(),
            m.VehicleInNumber, m.DriverInName, m.GateInAt,
            m.VehicleOutNumber, m.DriverOutName, m.GateOutAt,
            m.Status.ToString(), m.DeliveryOrderId)).ToList());
    }
}