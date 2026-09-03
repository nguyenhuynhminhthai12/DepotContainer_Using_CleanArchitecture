using TechSpherex.CleanArchitecture.Application.Abstractions.Messaging;
using TechSpherex.CleanArchitecture.Domain.Common;
using TechSpherex.CleanArchitecture.Domain.Entities;

namespace TechSpherex.CleanArchitecture.Application.Features.Gate;

/// <summary>
/// Lệnh nhập cửa (Gate-In) một container vào yard.
/// </summary>
/// <param name="ContainerNumber">Số thùng hàng (11 ký tự ISO 6346).</param>
/// <param name="LineOperatorId">Mã hành đường sở hữu container.</param>
/// <param name="BlockId">Mã block chứa container.</param>
/// <param name="Bay">Số Bay (cho block thực, có thể null).</param>
/// <param name="Row">Số Row (cho block thực, có thể null).</param>
/// <param name="Tier">Số Tier (cho block thực, có thể null).</param>
/// <param name="Classification">Phân loại (A / B / C).</param>
/// <param name="ConditionAtGateIn">Tình trạng container khi nhập cửa.</param>
/// <param name="VehicleInNumber">Số xe nhập cửa (tùy chọn).</param>
/// <param name="DriverInName">Tên tài xế nhập cửa (tùy chọn).</param>
public sealed record GateInContainerCommand(
    string ContainerNumber,
    Guid LineOperatorId,
    Guid BlockId,
    int? Bay,
    int? Row,
    int? Tier,
    string Classification,
    string ConditionAtGateIn,
    string? VehicleInNumber,
    string? DriverInName) : ICommand<Result<ContainerMovementResponse>>;

/// <summary>
/// Lệnh xuất cửa (Gate-Out) một container khỏi yard — yêu cầu DeliveryOrder hợp lệ.
/// </summary>
/// <param name="ContainerNumber">Số thùng hàng.</param>
/// <param name="DeliveryOrderId">Mã đơn giao hàng.</param>
/// <param name="VehicleOutNumber">Số xe xuất cửa (tùy chọn).</param>
/// <param name="DriverOutName">Tên tài xế xuất cửa (tùy chọn).</param>
/// <param name="ConditionAtGateOut">Tình trạng container khi xuất cửa.</param>
public sealed record GateOutContainerCommand(
    string ContainerNumber,
    Guid DeliveryOrderId,
    string? VehicleOutNumber,
    string? DriverOutName,
    string ConditionAtGateOut) : ICommand<Result<ContainerMovementResponse>>;

/// <summary>
/// Lệnh di chuyển container trong yard (thay đổi vị trí slot).
/// </summary>
/// <param name="ContainerNumber">Số thùng hàng.</param>
/// <param name="NewBlockId">Mã block đích.</param>
/// <param name="NewBay">Số Bay mới.</param>
/// <param name="NewRow">Số Row mới.</param>
/// <param name="NewTier">Số Tier mới.</param>
public sealed record MoveContainerInYardCommand(
    string ContainerNumber,
    Guid NewBlockId,
    int NewBay,
    int NewRow,
    int NewTier) : ICommand<Result>;

/// <summary>
/// DTO phản hồi thong tin movement (EIR) của container.
/// </summary>
/// <param name="Id">Mã movement.</param>
/// <param name="ContainerId">Mã container.</param>
/// <param name="LineOperatorId">Mã hành đường.</param>
/// <param name="YardSlotId">Mã yard slot (nếu có).</param>
/// <param name="BlockId">Mã block chứa container.</param>
/// <param name="Classification">Phân loại A/B/C.</param>
/// <param name="ConditionAtGateIn">Tình trạng nhập cửa.</param>
/// <param name="ConditionAtGateOut">Tình trạng xuất cửa (nếu có).</param>
/// <param name="VehicleInNumber">Số xe nhập cửa.</param>
/// <param name="DriverInName">Tên tài xế nhập cửa.</param>
/// <param name="GateInAt">Thời gian nhập cửa.</param>
/// <param name="VehicleOutNumber">Số xe xuất cửa.</param>
/// <param name="DriverOutName">Tên tài xế xuất cửa.</param>
/// <param name="GateOutAt">Thời gian xuất cửa (nếu có).</param>
/// <param name="Status">Trạng thái movement.</param>
/// <param name="DeliveryOrderId">Mã đơn giao hàng (dành cho Gate-Out).</param>
public sealed record ContainerMovementResponse(
    Guid Id,
    Guid ContainerId,
    Guid LineOperatorId,
    Guid? YardSlotId,
    Guid? BlockId,
    string Classification,
    string ConditionAtGateIn,
    string? ConditionAtGateOut,
    string? VehicleInNumber,
    string? DriverInName,
    DateTimeOffset GateInAt,
    string? VehicleOutNumber,
    string? DriverOutName,
    DateTimeOffset? GateOutAt,
    string Status,
    Guid? DeliveryOrderId);

/// <summary>
/// Truy vấn lấy lịch sử di chuyển của một container.
/// </summary>
/// <param name="ContainerNumber">Số thùng hàng cần tra cứu.</param>
public sealed record GetContainerMovementHistoryQuery(string ContainerNumber)
    : IQuery<Result<IReadOnlyList<ContainerMovementResponse>>>;
