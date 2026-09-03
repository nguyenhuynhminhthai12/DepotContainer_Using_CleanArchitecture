using TechSpherex.CleanArchitecture.Application.Abstractions.Messaging;
using TechSpherex.CleanArchitecture.Domain.Common;

namespace TechSpherex.CleanArchitecture.Application.Features.DeliveryOrders;

/// <summary>
/// DTO dòng chi tiết của DeliveryOrder.
/// </summary>
/// <param name="ContainerTypeId">Mã loại container.</param>
/// <param name="RequestedQuantity">Số lượng yêu cầu.</param>
/// <param name="DeliveredQuantity">Số lượng đã giao.</param>
public sealed record DeliveryOrderLineDto(Guid ContainerTypeId, int RequestedQuantity, int DeliveredQuantity);

/// <summary>
/// DTO phản hồi thông tin đơn giao hàng.
/// </summary>
/// <param name="Id">Mã đơn hàng.</param>
/// <param name="OrderNumber">Số đơn hàng.</param>
/// <param name="CustomerId">Mã khách hàng.</param>
/// <param name="CustomerName">Tên khách hàng.</param>
/// <param name="LineOperatorId">Mã hành đường.</param>
/// <param name="LineOperatorName">Tên hành đường.</param>
/// <param name="ExpiryDate">Ngày hết hạn.</param>
/// <param name="VesselVoyage">Chuyến tàu/voyage.</param>
/// <param name="IsClosed">Đã đóng chưa.</param>
/// <param name="Lines">Danh sách dòng chi tiết.</param>
public sealed record DeliveryOrderResponse(
    Guid Id,
    string OrderNumber,
    Guid CustomerId,
    string CustomerName,
    Guid LineOperatorId,
    string LineOperatorName,
    DateTimeOffset ExpiryDate,
    string? VesselVoyage,
    bool IsClosed,
    IReadOnlyList<DeliveryOrderLineDto> Lines);

/// <summary>
/// Lệnh tạo đơn giao hàng mới. Trả về <see cref="DeliveryOrderResponse"/>.
/// </summary>
/// <param name="OrderNumber">Số đơn hàng.</param>
/// <param name="CustomerId">Mã khách hàng.</param>
/// <param name="LineOperatorId">Mã hành đường.</param>
/// <param name="ExpiryDate">Ngày hết hạn.</param>
/// <param name="VesselVoyage">Chuyến tàu/voyage (tùy chọn).</param>
/// <param name="Notes">Ghi chú (tùy chọn).</param>
/// <param name="Lines">Danh sách dòng chi tiết container type.</param>
public sealed record CreateDeliveryOrderCommand(
    string OrderNumber,
    Guid CustomerId,
    Guid LineOperatorId,
    DateTimeOffset ExpiryDate,
    string? VesselVoyage,
    string? Notes,
    IReadOnlyList<DeliveryOrderLineDto> Lines) : ICommand<Result<DeliveryOrderResponse>>;

/// <summary>Truy vấn lấy đơn giao hàng theo ID.</summary>
/// <param name="Id">Mã đơn hàng.</param>
public sealed record GetDeliveryOrderByIdQuery(Guid Id) : IQuery<Result<DeliveryOrderResponse>>;

/// <summary>Truy vấn lấy danh sách đơn giao hàng đang hoạt động (chưa đóng, chưa hết hạn).</summary>
public sealed record GetActiveDeliveryOrdersQuery() : IQuery<Result<IReadOnlyList<DeliveryOrderResponse>>>;

/// <summary>
/// Lệnh đóng một đơn giao hàng (đánh dấu IsClosed = true).
/// </summary>
/// <param name="Id">Mã đơn hàng cần đóng.</param>
public sealed record CloseDeliveryOrderCommand(Guid Id) : ICommand<Result>;

/// <summary>
/// Lệnh cập nhật thông tin đơn giao hàng.
/// </summary>
/// <param name="Id">Mã đơn hàng.</param>
/// <param name="ExpiryDate">Ngày hết hạn mới.</param>
/// <param name="VesselVoyage">Chuyến tàu/voyage mới (tùy chọn).</param>
/// <param name="Notes">Ghi chú mới (tùy chọn).</param>
/// <param name="Lines">Danh sách dòng cập nhật (tùy chọn).</param>
public sealed record UpdateDeliveryOrderCommand(
    Guid Id,
    DateTimeOffset ExpiryDate,
    string? VesselVoyage,
    string? Notes,
    IReadOnlyList<DeliveryOrderLineDto>? Lines) : ICommand<Result<DeliveryOrderResponse>>;

/// <summary>
/// Lệnh xóa đơn giao hàng — chỉ cho phép khi chưa có container nào được giao.
/// </summary>
/// <param name="Id">Mã đơn hàng cần xóa.</param>
public sealed record DeleteDeliveryOrderCommand(Guid Id) : ICommand<Result>;
