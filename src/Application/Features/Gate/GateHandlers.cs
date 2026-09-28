using TechSpherex.CleanArchitecture.Application.Abstractions.Caching;
using TechSpherex.CleanArchitecture.Application.Abstractions.Data;
using TechSpherex.CleanArchitecture.Application.Abstractions.Messaging;
using TechSpherex.CleanArchitecture.Application.Abstractions.Rules;
using TechSpherex.CleanArchitecture.Domain.Common;
using TechSpherex.CleanArchitecture.Domain.Common.Rules;
using TechSpherex.CleanArchitecture.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace TechSpherex.CleanArchitecture.Application.Features.Gate;

/// <summary>
/// Handler xử lý nghiệp vụ Nhập Cổng (Gate-In) cho container vào bãi Depot.
/// Thực hiện các bước:
/// 1. Kiểm tra tồn tại và trạng thái container (tránh nhập 2 lần khi đang ở trong bãi).
/// 2. Xác thực Block và Slot hạ bãi (kiểm tra Slot trống, tính chẵn lẻ của Bay tương ứng với kích thước 20ft/40ft).
/// 3. Đánh giá bộ quy tắc Rule Engine cấu hình động.
/// 4. Tạo bản ghi luân chuyển ContainerMovement (EIR) và cập nhật trạng thái ô YardSlot thành chiếm dụng.
/// 5. Xóa cache sơ đồ bãi (yard-map).
/// </summary>
public sealed class GateInContainerCommandHandler(
    IAppDbContext dbContext,
    IRuleEngine ruleEngine,
    ICacheService cache) :
    ICommandHandler<GateInContainerCommand, Result<ContainerMovementResponse>>
{
#pragma warning disable S3776 // Cognitive Complexity: handler methods contain necessary validation logic
    /// <summary>
    /// Thực thi lệnh Gate-In container.
    /// </summary>
    /// <param name="command">Dữ liệu lệnh Gate-In chứa số container, mã Block, tọa độ ô Slot, thông tin xe và tài xế.</param>
    /// <param name="cancellationToken">Token hủy tác vụ bất đồng bộ.</param>
    /// <returns>Đối tượng Result chứa DTO ContainerMovementResponse nếu thành công, hoặc Error nếu vi phạm nghiệp vụ.</returns>
    public async Task<Result<ContainerMovementResponse>> HandleAsync(GateInContainerCommand command, CancellationToken cancellationToken = default)
    {
        // 1. Chuẩn hóa số hiệu container
        var normalizedNumber = command.ContainerNumber.Trim().ToUpperInvariant();

        // 2. Tìm container trong cơ sở dữ liệu
        var container = await dbContext.Containers
            .FirstOrDefaultAsync(c => c.ContainerNumberRaw == normalizedNumber, cancellationToken);
        if (container is null)
        {
            return Result.Failure<ContainerMovementResponse>(Error.NotFound("Container.NotFound",
                $"Container '{normalizedNumber}' was not found."));
        }

        // 3. Từ chối nếu container đang ở trong bãi (lượt di chuyển trước đó vẫn đang mở)
        var alreadyInYard = await dbContext.ContainerMovements
            .AnyAsync(m => m.ContainerId == container.Id && m.Status == MovementStatus.InYard, cancellationToken);
        if (alreadyInYard)
        {
            return Result.Failure<ContainerMovementResponse>(Error.Conflict("Gate.AlreadyInYard",
                $"Container '{normalizedNumber}' is already in the yard. Move it before a new Gate-In."));
        }

        // 4. Kiểm tra Block chỉ định
        var block = await dbContext.Blocks.FirstOrDefaultAsync(b => b.Id == command.BlockId, cancellationToken);
        if (block is null)
        {
            return Result.Failure<ContainerMovementResponse>(Error.NotFound("Block.NotFound",
                $"Block '{command.BlockId}' was not found."));
        }

        YardSlot? slot = null;

        // 5. Nếu là Block vật lý (không phải Block ảo), kiểm tra tọa độ Bay/Row/Tier và quy tắc xếp bãi
        if (!block.IsVirtual)
        {
            if (!command.Bay.HasValue || !command.Row.HasValue || !command.Tier.HasValue)
            {
                return Result.Failure<ContainerMovementResponse>(Error.Validation("Gate.BayRowTierRequired",
                    "Bay/Row/Tier are required for non-virtual blocks."));
            }

            slot = await dbContext.YardSlots
                .FirstOrDefaultAsync(s => s.BlockId == block.Id && s.Bay == command.Bay.Value
                    && s.Row == command.Row.Value && s.Tier == command.Tier.Value, cancellationToken);

            if (slot is null)
            {
                return Result.Failure<ContainerMovementResponse>(Error.NotFound("YardSlot.NotFound",
                    "Yard slot not found for the given Block/Bay/Row/Tier."));
            }

            // Quy tắc Domain: Bay chẵn/lẻ phải khớp với kích thước container (20ft vào Bay lẻ, 40ft vào Bay chẵn)
            var bayRule = new BayParityMatchesContainerSizeRule(slot.Bay, container.SizeFeet);
            if (bayRule.IsBroken())
                return Result.Failure<ContainerMovementResponse>(Error.Validation(bayRule.RuleCode, bayRule.Message));

            // Quy tắc Domain: Ô bãi phải đang trống
            var slotRule = new YardSlotNotOccupiedRule(slot.IsOccupied);
            if (slotRule.IsBroken())
                return Result.Failure<ContainerMovementResponse>(Error.Validation(slotRule.RuleCode, slotRule.Message));

            // Đánh giá Rule Engine cấu hình động (GateInValidation)
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

        // 6. Kiểm tra tính hợp lệ của tình trạng ngoại quan lúc vào cổng
        if (!Enum.TryParse<ContainerCondition>(command.ConditionAtGateIn, true, out var conditionAtGateIn))
        {
            return Result.Failure<ContainerMovementResponse>(Error.Validation("Gate.InvalidCondition",
                "Invalid ConditionAtGateIn value."));
        }

        // 7. Tạo bản ghi lượt di chuyển ContainerMovement mới
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

        // 8. Đánh dấu ô Slot đã bị chiếm dụng
        if (slot is not null)
        {
            slot.IsOccupied = true;
            slot.CurrentContainerId = container.Id;
        }

        dbContext.ContainerMovements.Add(movement);
        await dbContext.SaveChangesAsync(cancellationToken);

        // 9. Xóa cache sơ đồ bãi để cập nhật giao diện thời gian thực
        await cache.InvalidateByTagAsync("yard-map", cancellationToken);

        return Result.Success(Map(movement));
#pragma warning restore S3776 // Cognitive Complexity: handler methods contain necessary validation logic
    }

    /// <summary>
    /// Chuyển đổi từ Entity ContainerMovement sang DTO ContainerMovementResponse.
    /// </summary>
    internal static ContainerMovementResponse Map(ContainerMovement m) => new(
        m.Id, m.ContainerId, m.LineOperatorId, m.YardSlotId, m.BlockId,
        m.Classification,
        m.ConditionAtGateIn.ToString(),
        m.ConditionAtGateOut?.ToString(),
        m.VehicleInNumber, m.DriverInName, m.GateInAt,
        m.VehicleOutNumber, m.DriverOutName, m.GateOutAt,
        m.Status.ToString(), m.DeliveryOrderId);
}

/// <summary>
/// Handler xử lý nghiệp vụ Xuất Cổng (Gate-Out) cho container ra khỏi bãi Depot.
/// Thực hiện các bước:
/// 1. Kiểm tra container có đang ở trong bãi (InYard) hay không.
/// 2. Kiểm tra tính hợp lệ của Đơn giao hàng (Delivery Order): khớp hãng tàu, chưa bị đóng, chưa hết hạn, chưa giao đủ số lượng.
/// 3. Đánh giá bộ quy tắc Rule Engine cấu hình động (GateOutValidation).
/// 4. Khấu trừ số lượng vào DeliveryOrderLine.
/// 5. Đóng lượt di chuyển ContainerMovement (chuyển trạng thái thành GateOut, giải phóng YardSlot).
/// 6. Xóa cache sơ đồ bãi.
/// </summary>
public sealed class GateOutContainerCommandHandler(
    IAppDbContext dbContext,
    IRuleEngine ruleEngine,
    ICacheService cache) :
    ICommandHandler<GateOutContainerCommand, Result<ContainerMovementResponse>>
{
    /// <summary>
    /// Thực thi lệnh Gate-Out container.
    /// </summary>
    /// <param name="command">Dữ liệu lệnh Gate-Out chứa số container, mã đơn giao hàng D/O, thông tin xe nhận và tài xế.</param>
    /// <param name="cancellationToken">Token hủy tác vụ bất đồng bộ.</param>
    /// <returns>Đối tượng Result chứa DTO ContainerMovementResponse cập nhật nếu thành công.</returns>
    public async Task<Result<ContainerMovementResponse>> HandleAsync(GateOutContainerCommand command, CancellationToken cancellationToken = default)
    {
        var normalizedNumber = command.ContainerNumber.Trim().ToUpperInvariant();

        var container = await dbContext.Containers
            .FirstOrDefaultAsync(c => c.ContainerNumberRaw == normalizedNumber, cancellationToken);
        if (container is null)
        {
            return Result.Failure<ContainerMovementResponse>(Error.NotFound("Container.NotFound",
                $"Container '{normalizedNumber}' was not found."));
        }

        var openMovement = await dbContext.ContainerMovements
            .FirstOrDefaultAsync(m => m.ContainerId == container.Id && m.Status == MovementStatus.InYard, cancellationToken);
        if (openMovement is null)
        {
            return Result.Failure<ContainerMovementResponse>(Error.Conflict("Gate.NotInYard",
                $"Container '{normalizedNumber}' is not in the yard."));
        }

        var deliveryOrder = await dbContext.DeliveryOrders
            .Include(d => d.Lines)
            .FirstOrDefaultAsync(d => d.Id == command.DeliveryOrderId, cancellationToken);
        if (deliveryOrder is null)
        {
            return Result.Failure<ContainerMovementResponse>(Error.NotFound("DeliveryOrder.NotFound",
                $"Delivery order '{command.DeliveryOrderId}' was not found."));
        }

        if (deliveryOrder.LineOperatorId != openMovement.LineOperatorId)
        {
            return Result.Failure<ContainerMovementResponse>(Error.Conflict("Gate.LineOperatorMismatch",
                "Delivery order's Line Operator does not match the container's current Line Operator."));
        }

        if (deliveryOrder.IsClosed)
        {
            return Result.Failure<ContainerMovementResponse>(Error.Conflict("DeliveryOrder.Closed",
                $"Delivery order '{deliveryOrder.OrderNumber}' is already closed."));
        }

        // Kiểm tra hạn sử dụng của Đơn giao hàng
        var expiryRule = new DeliveryOrderNotExpiredRule(deliveryOrder.ExpiryDate, DateTimeOffset.UtcNow);
        if (expiryRule.IsBroken())
        {
            return Result.Failure<ContainerMovementResponse>(Error.Validation(expiryRule.RuleCode, expiryRule.Message));
        }

        // Đánh giá Rule Engine cấu hình động cho Gate-Out
        var ruleContext = new Dictionary<string, object?>
        {
            ["ContainerNumber"] = container.ContainerNumberRaw,
            ["DeliveryOrderId"] = deliveryOrder.Id,
            ["LineOperatorId"] = deliveryOrder.LineOperatorId,
            ["IsExpired"] = deliveryOrder.ExpiryDate < DateTimeOffset.UtcNow
        };
        var ruleResult = ruleEngine.Evaluate("GateOutValidation", ruleContext);
        if (!ruleResult.IsValid)
        {
            return Result.Failure<ContainerMovementResponse>(Error.Validation(ruleResult.Violations[0].RuleCode, ruleResult.Violations[0].Message));
        }

        var line = deliveryOrder.Lines.FirstOrDefault(l => l.ContainerTypeId == container.ContainerTypeId);
        if (line is null)
        {
            return Result.Failure<ContainerMovementResponse>(Error.Conflict("DeliveryOrder.TypeMismatch",
                "Delivery order does not have an open line for this container type."));
        }

        if (line.DeliveredQuantity >= line.RequestedQuantity)
        {
            return Result.Failure<ContainerMovementResponse>(Error.Conflict("DeliveryOrder.FullyDelivered",
                "Requested quantity for this container type has already been fulfilled."));
        }

        if (!Enum.TryParse<ContainerCondition>(command.ConditionAtGateOut, true, out var conditionAtGateOut))
        {
            return Result.Failure<ContainerMovementResponse>(Error.Validation("Gate.InvalidCondition",
                "Invalid ConditionAtGateOut value."));
        }

        // Tăng số lượng đã giao
        line.DeliveredQuantity++;
        if (deliveryOrder.Lines.All(l => l.DeliveredQuantity >= l.RequestedQuantity))
        {
            deliveryOrder.IsClosed = true;
        }

        // Cập nhật lượt di chuyển thành GateOut
        openMovement.Status = MovementStatus.GateOut;
        openMovement.ConditionAtGateOut = conditionAtGateOut;
        openMovement.VehicleOutNumber = command.VehicleOutNumber;
        openMovement.DriverOutName = command.DriverOutName;
        openMovement.GateOutAt = DateTimeOffset.UtcNow;
        openMovement.DeliveryOrderId = deliveryOrder.Id;

        // Giải phóng ô YardSlot
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

        await dbContext.SaveChangesAsync(cancellationToken);
        await cache.InvalidateByTagAsync("yard-map", cancellationToken);

        return Result.Success(GateInContainerCommandHandler.Map(openMovement));
    }
}

/// <summary>
/// Handler xử lý nghiệp vụ Di chuyển Container trong nội bộ bãi (Move Container).
/// Cập nhật vị trí từ ô Slot cũ sang ô Slot mới và giải phóng ô cũ.
/// </summary>
public sealed class MoveContainerInYardCommandHandler(
    IAppDbContext dbContext,
    ICacheService cache) :
    ICommandHandler<MoveContainerInYardCommand, Result>
{
    /// <summary>
    /// Thực thi lệnh di chuyển container trong bãi.
    /// </summary>
    /// <param name="command">Thông tin container và tọa độ ô Slot đích mới.</param>
    /// <param name="cancellationToken">Token hủy tác vụ bất đồng bộ.</param>
    /// <returns>Result thành công nếu di chuyển hoàn tất.</returns>
    public async Task<Result> HandleAsync(MoveContainerInYardCommand command, CancellationToken cancellationToken = default)
    {
        var normalizedNumber = command.ContainerNumber.Trim().ToUpperInvariant();

        var container = await dbContext.Containers
            .FirstOrDefaultAsync(c => c.ContainerNumberRaw == normalizedNumber, cancellationToken);
        if (container is null)
        {
            return Result.Failure(Error.NotFound("Container.NotFound",
                $"Container '{normalizedNumber}' was not found."));
        }

        var openMovement = await dbContext.ContainerMovements
            .FirstOrDefaultAsync(m => m.ContainerId == container.Id && m.Status == MovementStatus.InYard, cancellationToken);
        if (openMovement is null)
        {
            return Result.Failure(Error.Conflict("Gate.NotInYard",
                $"Container '{normalizedNumber}' is not currently in the yard."));
        }

        var block = await dbContext.Blocks.FirstOrDefaultAsync(b => b.Id == command.NewBlockId, cancellationToken);
        if (block is null)
        {
            return Result.Failure(Error.NotFound("Block.NotFound",
                $"Block '{command.NewBlockId}' was not found."));
        }

        if (block.IsVirtual)
        {
            return Result.Failure(Error.Validation("Block.Virtual",
                "Cannot move container to a virtual block."));
        }

        var targetSlot = await dbContext.YardSlots
            .FirstOrDefaultAsync(s => s.BlockId == block.Id
                && s.Bay == command.NewBay
                && s.Row == command.NewRow
                && s.Tier == command.NewTier, cancellationToken);
        if (targetSlot is null)
        {
            return Result.Failure(Error.NotFound("YardSlot.NotFound",
                "Target yard slot was not found."));
        }

        // Quy tắc Domain: Bay chẵn/lẻ phải khớp với kích thước container
        var bayRule = new BayParityMatchesContainerSizeRule(targetSlot.Bay, container.SizeFeet);
        if (bayRule.IsBroken())
            return Result.Failure(Error.Validation(bayRule.RuleCode, bayRule.Message));

        if (targetSlot.IsOccupied && targetSlot.CurrentContainerId != container.Id)
        {
            return Result.Failure(Error.Conflict("Yard.SlotOccupied",
                "Yard slot is occupied by another container."));
        }

        // Giải phóng ô Slot cũ
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

        // Gán ô Slot mới
        targetSlot.IsOccupied = true;
        targetSlot.CurrentContainerId = container.Id;

        openMovement.YardSlotId = targetSlot.Id;
        openMovement.BlockId = block.Id;

        await dbContext.SaveChangesAsync(cancellationToken);
        await cache.InvalidateByTagAsync("yard-map", cancellationToken);

        return Result.Success();
    }
}

/// <summary>
/// Query Handler tra cứu toàn bộ lịch sử các lượt di chuyển (EIR) của một Container.
/// </summary>
public sealed class GetContainerMovementHistoryQueryHandler(IAppDbContext dbContext) :
    IQueryHandler<GetContainerMovementHistoryQuery, Result<IReadOnlyList<ContainerMovementResponse>>>
{
    /// <summary>
    /// Thực thi truy vấn lấy lịch sử di chuyển container.
    /// </summary>
    /// <param name="query">Đối tượng query chứa số hiệu container.</param>
    /// <param name="cancellationToken">Token hủy tác vụ bất đồng bộ.</param>
    /// <returns>Result chứa danh sách các lượt di chuyển xếp theo thời gian mới nhất.</returns>
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

        return Result.Success<IReadOnlyList<ContainerMovementResponse>>([.. items.Select(GateInContainerCommandHandler.Map)]);
    }
}