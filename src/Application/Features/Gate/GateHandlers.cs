#pragma warning disable CA1873 // Structured logging arguments are intentionally evaluated
#pragma warning disable S3776 // Cognitive complexity is expected for gate workflow

using Microsoft.Extensions.Logging;
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
    ICacheService cache,
    ILogger<GateInContainerCommandHandler> logger) :
    ICommandHandler<GateInContainerCommand, Result<ContainerMovementResponse>>
{
#pragma warning disable S3776 // Cognitive complexity is expected for gate-in workflow
    public async Task<Result<ContainerMovementResponse>> HandleAsync(GateInContainerCommand command, CancellationToken cancellationToken = default)
    {
        var normalizedNumber = command.ContainerNumber.Trim().ToUpperInvariant();
        logger.LogInformation("Processing Gate-In for container {ContainerNumber}", normalizedNumber);

        var container = await dbContext.Containers
            .FirstOrDefaultAsync(c => c.ContainerNumberRaw == normalizedNumber, cancellationToken);
        if (container is null)
        {
            logger.LogWarning("Gate-In failed: Container {ContainerNumber} not found", normalizedNumber);
            return Result.Failure<ContainerMovementResponse>(Error.NotFound("Container.NotFound",
                $"Container '{normalizedNumber}' was not found."));
        }

        var alreadyInYard = await dbContext.ContainerMovements
            .AnyAsync(m => m.ContainerId == container.Id && m.Status == MovementStatus.InYard, cancellationToken);
        if (alreadyInYard)
        {
            logger.LogWarning("Gate-In rejected: Container {ContainerNumber} already in yard", normalizedNumber);
            return Result.Failure<ContainerMovementResponse>(Error.Conflict("Gate.AlreadyInYard",
                $"Container '{normalizedNumber}' is already in the yard. Move it before a new Gate-In."));
        }

        var block = await dbContext.Blocks.FirstOrDefaultAsync(b => b.Id == command.BlockId, cancellationToken);
        if (block is null)
        {
            logger.LogWarning("Gate-In failed: Block {BlockId} not found", command.BlockId);
            return Result.Failure<ContainerMovementResponse>(Error.NotFound("Block.NotFound",
                $"Block '{command.BlockId}' was not found."));
        }

        YardSlot? slot = null;

        if (!block.IsVirtual)
        {
            if (!command.Bay.HasValue || !command.Row.HasValue || !command.Tier.HasValue)
            {
                logger.LogWarning("Gate-In validation failed: Bay/Row/Tier required for non-virtual block {BlockId}", block.Id);
                return Result.Failure<ContainerMovementResponse>(Error.Validation("Gate.BayRowTierRequired",
                    "Bay/Row/Tier are required for non-virtual blocks."));
            }

            slot = await dbContext.YardSlots
                .FirstOrDefaultAsync(s => s.BlockId == block.Id && s.Bay == command.Bay.Value
                    && s.Row == command.Row.Value && s.Tier == command.Tier.Value, cancellationToken);

            if (slot is null)
            {
                logger.LogWarning("Gate-In failed: Yard slot not found at B{Bay}/R{Row}/T{Tier} in block {BlockId}",
                    command.Bay, command.Row, command.Tier, block.Id);
                return Result.Failure<ContainerMovementResponse>(Error.NotFound("YardSlot.NotFound",
                    "Yard slot not found for the given Block/Bay/Row/Tier."));
            }

            var bayRule = new BayParityMatchesContainerSizeRule(slot.Bay, container.SizeFeet);
            if (bayRule.IsBroken())
            {
                logger.LogWarning("Gate-In rule violation: {RuleCode} - {Message}", bayRule.RuleCode, bayRule.Message);
                return Result.Failure<ContainerMovementResponse>(Error.Validation(bayRule.RuleCode, bayRule.Message));
            }

            var slotRule = new YardSlotNotOccupiedRule(slot.IsOccupied);
            if (slotRule.IsBroken())
            {
                logger.LogWarning("Gate-In rule violation: {RuleCode} - {Message}", slotRule.RuleCode, slotRule.Message);
                return Result.Failure<ContainerMovementResponse>(Error.Validation(slotRule.RuleCode, slotRule.Message));
            }

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
            {
                logger.LogWarning("Gate-In config rule violation: {RuleCode} - {Message}",
                    ruleResult.Violations[0].RuleCode, ruleResult.Violations[0].Message);
                return Result.Failure<ContainerMovementResponse>(Error.Validation(ruleResult.Violations[0].RuleCode, ruleResult.Violations[0].Message));
            }
        }

        if (!Enum.TryParse<ContainerCondition>(command.ConditionAtGateIn, true, out var conditionAtGateIn))
        {
            logger.LogWarning("Gate-In failed: Invalid condition value '{Condition}'", command.ConditionAtGateIn);
            return Result.Failure<ContainerMovementResponse>(Error.Validation("Gate.InvalidCondition",
                "Invalid ConditionAtGateIn value."));
        }

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

        logger.LogInformation("Gate-In successful: Movement {MovementId} created for container {ContainerNumber} at B{Bay}/R{Row}/T{Tier}",
            movement.Id, normalizedNumber, command.Bay ?? 0, command.Row ?? 0, command.Tier ?? 0);

        return Result.Success(MapToResponse(movement));
    }

    private static ContainerMovementResponse MapToResponse(ContainerMovement m) => new(
        m.Id, m.ContainerId, m.LineOperatorId, m.YardSlotId, m.BlockId,
        m.Classification,
        m.ConditionAtGateIn.ToString(),
        m.ConditionAtGateOut?.ToString(),
        m.VehicleInNumber, m.DriverInName, m.GateInAt,
        m.VehicleOutNumber, m.DriverOutName, m.GateOutAt,
        m.Status.ToString(), m.DeliveryOrderId);
}
#pragma warning restore CA1873, S3776

public sealed class GateOutContainerCommandHandler(
    IAppDbContext dbContext,
    IRuleEngine ruleEngine,
    ICacheService cache,
    ILogger<GateOutContainerCommandHandler> logger) :
    ICommandHandler<GateOutContainerCommand, Result<ContainerMovementResponse>>
{
    public async Task<Result<ContainerMovementResponse>> HandleAsync(GateOutContainerCommand command, CancellationToken cancellationToken = default)
    {
        var normalizedNumber = command.ContainerNumber.Trim().ToUpperInvariant();
#pragma warning disable CA1873
        logger.LogInformation("Processing Gate-Out for container {ContainerNumber}, DeliveryOrder {OrderId}",
            normalizedNumber, command.DeliveryOrderId);
#pragma warning restore CA1873

        var container = await dbContext.Containers
            .FirstOrDefaultAsync(c => c.ContainerNumberRaw == normalizedNumber, cancellationToken);
        if (container is null)
        {
            logger.LogWarning("Gate-Out failed: Container {ContainerNumber} not found", normalizedNumber);
            return Result.Failure<ContainerMovementResponse>(Error.NotFound("Container.NotFound",
                $"Container '{normalizedNumber}' was not found."));
        }

        var openMovement = await dbContext.ContainerMovements
            .FirstOrDefaultAsync(m => m.ContainerId == container.Id && m.Status == MovementStatus.InYard, cancellationToken);
        if (openMovement is null)
        {
            logger.LogWarning("Gate-Out rejected: Container {ContainerNumber} not in yard", normalizedNumber);
            return Result.Failure<ContainerMovementResponse>(Error.Conflict("Gate.NotInYard",
                $"Container '{normalizedNumber}' is not in the yard."));
        }

        var deliveryOrder = await dbContext.DeliveryOrders
            .Include(d => d.Lines)
            .FirstOrDefaultAsync(d => d.Id == command.DeliveryOrderId, cancellationToken);
        if (deliveryOrder is null)
        {
            logger.LogWarning("Gate-Out failed: Delivery order {OrderId} not found", command.DeliveryOrderId);
            return Result.Failure<ContainerMovementResponse>(Error.NotFound("DeliveryOrder.NotFound",
                $"Delivery order '{command.DeliveryOrderId}' was not found."));
        }

        if (deliveryOrder.LineOperatorId != openMovement.LineOperatorId)
        {
            logger.LogWarning("Gate-Out rejected: LineOperator mismatch for container {ContainerNumber}", normalizedNumber);
            return Result.Failure<ContainerMovementResponse>(Error.Conflict("Gate.LineOperatorMismatch",
                "Delivery order's Line Operator does not match the container's current Line Operator."));
        }

        if (deliveryOrder.IsClosed)
        {
            logger.LogWarning("Gate-Out rejected: Delivery order {OrderId} is closed", command.DeliveryOrderId);
            return Result.Failure<ContainerMovementResponse>(Error.Conflict("DeliveryOrder.Closed",
                "Delivery order has been closed."));
        }

        var expiryRule = new DeliveryOrderNotExpiredRule(deliveryOrder.ExpiryDate, DateTimeOffset.UtcNow);
        if (expiryRule.IsBroken())
        {
            logger.LogWarning("Gate-Out rule violation: {RuleCode} - {Message}", expiryRule.RuleCode, expiryRule.Message);
            return Result.Failure<ContainerMovementResponse>(Error.Validation(expiryRule.RuleCode, expiryRule.Message));
        }

        var line = deliveryOrder.Lines.FirstOrDefault(l => l.ContainerTypeId == container.ContainerTypeId);
        if (line is null)
        {
            logger.LogWarning("Gate-Out failed: No line for container type {TypeId} in order {OrderId}",
                container.ContainerTypeId, command.DeliveryOrderId);
            return Result.Failure<ContainerMovementResponse>(Error.Validation("DeliveryOrder.NoLineForType",
                $"Delivery order has no line for container type '{container.ContainerTypeId}'."));
        }

        var qtyRule = new DeliveryOrderQuantityAvailableRule(line.RequestedQuantity, line.DeliveredQuantity);
        if (qtyRule.IsBroken())
        {
            logger.LogWarning("Gate-Out rule violation: {RuleCode} - {Message}", qtyRule.RuleCode, qtyRule.Message);
            return Result.Failure<ContainerMovementResponse>(Error.Validation(qtyRule.RuleCode, qtyRule.Message));
        }

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
        {
            logger.LogWarning("Gate-Out config rule violation: {RuleCode} - {Message}",
                ruleResult.Violations[0].RuleCode, ruleResult.Violations[0].Message);
            return Result.Failure<ContainerMovementResponse>(Error.Validation(ruleResult.Violations[0].RuleCode, ruleResult.Violations[0].Message));
        }

        if (!Enum.TryParse<ContainerCondition>(command.ConditionAtGateOut, true, out var conditionOut))
        {
            logger.LogWarning("Gate-Out failed: Invalid condition value '{Condition}'", command.ConditionAtGateOut);
            return Result.Failure<ContainerMovementResponse>(Error.Validation("Gate.InvalidCondition",
                "Invalid ConditionAtGateOut value."));
        }

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

        line.DeliveredQuantity++;

        await dbContext.SaveChangesAsync(cancellationToken);

        await cache.InvalidateByTagAsync("yard-map", cancellationToken);

#pragma warning disable CA1873
        logger.LogInformation("Gate-Out successful: Movement {MovementId} closed for container {ContainerNumber}, DO {OrderId}",
            openMovement.Id, normalizedNumber, command.DeliveryOrderId);
#pragma warning restore CA1873

        return Result.Success(MapToResponse(openMovement));
    }

    private static ContainerMovementResponse MapToResponse(ContainerMovement m) => new(
        m.Id, m.ContainerId, m.LineOperatorId, m.YardSlotId, m.BlockId,
        m.Classification,
        m.ConditionAtGateIn.ToString(),
        m.ConditionAtGateOut?.ToString(),
        m.VehicleInNumber, m.DriverInName, m.GateInAt,
        m.VehicleOutNumber, m.DriverOutName, m.GateOutAt,
        m.Status.ToString(), m.DeliveryOrderId);
}
#pragma warning restore CA1873, S3776

public sealed class MoveContainerInYardCommandHandler(
    IAppDbContext dbContext,
    ICacheService cache,
    ILogger<MoveContainerInYardCommandHandler> logger) :
    ICommandHandler<MoveContainerInYardCommand, Result>
{
    public async Task<Result> HandleAsync(MoveContainerInYardCommand command, CancellationToken cancellationToken = default)
    {
        var normalizedNumber = command.ContainerNumber.Trim().ToUpperInvariant();
#pragma warning disable CA1873
        logger.LogInformation("Processing Move for container {ContainerNumber} to B{NewBay}/R{NewRow}/T{NewTier}",
            normalizedNumber, command.NewBay, command.NewRow, command.NewTier);
#pragma warning restore CA1873

        var container = await dbContext.Containers
            .FirstOrDefaultAsync(c => c.ContainerNumberRaw == normalizedNumber, cancellationToken);
        if (container is null)
        {
            logger.LogWarning("Move failed: Container {ContainerNumber} not found", normalizedNumber);
            return Result.Failure(Error.NotFound("Container.NotFound",
                $"Container '{normalizedNumber}' was not found."));
        }

        var openMovement = await dbContext.ContainerMovements
            .FirstOrDefaultAsync(m => m.ContainerId == container.Id && m.Status == MovementStatus.InYard, cancellationToken);
        if (openMovement is null)
        {
            logger.LogWarning("Move rejected: Container {ContainerNumber} not in yard", normalizedNumber);
            return Result.Failure(Error.Conflict("Gate.NotInYard",
                "Container is not currently in the yard."));
        }

        var block = await dbContext.Blocks.FirstOrDefaultAsync(b => b.Id == command.NewBlockId, cancellationToken);
        if (block is null)
        {
            logger.LogWarning("Move failed: Block {BlockId} not found", command.NewBlockId);
            return Result.Failure(Error.NotFound("Block.NotFound",
                $"Block '{command.NewBlockId}' was not found."));
        }
        if (block.IsVirtual)
        {
            logger.LogWarning("Move rejected: Cannot move to virtual block {BlockId}", block.Id);
            return Result.Failure(Error.Validation("Block.Virtual",
                "Cannot move a container into a virtual block using Bay/Row/Tier."));
        }

        var targetSlot = await dbContext.YardSlots
            .FirstOrDefaultAsync(s => s.BlockId == command.NewBlockId && s.Bay == command.NewBay
                && s.Row == command.NewRow && s.Tier == command.NewTier, cancellationToken);
        if (targetSlot is null)
        {
            logger.LogWarning("Move failed: Yard slot not found at B{Bay}/R{Row}/T{Tier} in block {BlockId}",
                command.NewBay, command.NewRow, command.NewTier, block.Id);
            return Result.Failure(Error.NotFound("YardSlot.NotFound",
                "Yard slot not found for the given Block/Bay/Row/Tier."));
        }

        var bayRule = new BayParityMatchesContainerSizeRule(targetSlot.Bay, container.SizeFeet);
        if (bayRule.IsBroken())
        {
            logger.LogWarning("Move rule violation: {RuleCode} - {Message}", bayRule.RuleCode, bayRule.Message);
            return Result.Failure(Error.Validation(bayRule.RuleCode, bayRule.Message));
        }

        var occupyingContainerId = targetSlot.CurrentContainerId;
        if (targetSlot.IsOccupied && occupyingContainerId != container.Id)
        {
            logger.LogWarning("Move rejected: Slot B{Bay}/R{Row}/T{Tier} occupied by another container",
                command.NewBay, command.NewRow, command.NewTier);
            return Result.Failure(Error.Conflict("Yard.SlotOccupied",
                "Yard slot is occupied by another container."));
        }

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

#pragma warning disable CA1873
        logger.LogInformation("Move successful: Container {ContainerNumber} moved to B{NewBay}/R{NewRow}/T{NewTier}",
            normalizedNumber, command.NewBay, command.NewRow, command.NewTier);
#pragma warning restore CA1873

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
        {
            return Result.Failure<IReadOnlyList<ContainerMovementResponse>>(Error.NotFound("Container.NotFound",
                $"Container '{normalizedNumber}' was not found."));
        }

        var items = await dbContext.ContainerMovements
            .AsNoTracking()
            .Where(m => m.ContainerId == container.Id)
            .OrderByDescending(m => m.GateInAt)
            .ToListAsync(cancellationToken);

        return Result.Success<IReadOnlyList<ContainerMovementResponse>>([.. items.Select(MapToResponse)]);
    }

    private static ContainerMovementResponse MapToResponse(ContainerMovement m) => new(
        m.Id, m.ContainerId, m.LineOperatorId, m.YardSlotId, m.BlockId,
        m.Classification,
        m.ConditionAtGateIn.ToString(),
        m.ConditionAtGateOut?.ToString(),
        m.VehicleInNumber, m.DriverInName, m.GateInAt,
        m.VehicleOutNumber, m.DriverOutName, m.GateOutAt,
        m.Status.ToString(), m.DeliveryOrderId);
}
#pragma warning restore CA1873, S3776
