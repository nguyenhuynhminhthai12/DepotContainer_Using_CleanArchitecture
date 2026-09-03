using TechSpherex.CleanArchitecture.Application.Abstractions.Messaging;
using TechSpherex.CleanArchitecture.Domain.Common;

namespace TechSpherex.CleanArchitecture.Application.Features.Yard;

/// <summary>
/// Lệnh tạo một Block thực (có lưới vị trí Bay/Row/Tier) trong Depot.
/// </summary>
/// <param name="DepotId">Mã depot chứa block.</param>
/// <param name="Code">Mã code của block.</param>
/// <param name="Name">Tên block.</param>
/// <param name="IsVirtual">Cho biết block có phải ảo không (luôn false với command này).</param>
/// <param name="MaxBay">Số Bay tối đa.</param>
/// <param name="MaxRow">Số Row tối đa.</param>
/// <param name="MaxTier">Số Tier tối đa.</param>
/// <param name="DisplayOrder">Thứ tự hiển thị (mặc định 0).</param>
public sealed record CreateBlockCommand(
    Guid DepotId,
    string Code,
    string Name,
    bool IsVirtual,
    int? MaxBay,
    int? MaxRow,
    int? MaxTier,
    int DisplayOrder = 0) : ICommand<Result<CreateBlockResponse>>;

/// <summary>
/// DTO trả về thông tin block vừa được tạo.
/// </summary>
/// <param name="Id">Mã block.</param>
/// <param name="Code">Mã code.</param>
/// <param name="Name">Tên block.</param>
/// <param name="IsVirtual">Block ảo hay thực.</param>
/// <param name="MaxBay">Số Bay tối đa.</param>
/// <param name="MaxRow">Số Row tối đa.</param>
/// <param name="MaxTier">Số Tier tối đa.</param>
public sealed record CreateBlockResponse(
    Guid Id,
    string Code,
    string Name,
    bool IsVirtual,
    int? MaxBay,
    int? MaxRow,
    int? MaxTier);

/// <summary>
/// Lệnh tạo một Block ảo (không có lưới vị trí) trong Depot.
/// </summary>
/// <param name="DepotId">Mã depot chứa block.</param>
/// <param name="Code">Mã code của block.</param>
/// <param name="Name">Tên block.</param>
/// <param name="DisplayOrder">Thứ tự hiển thị (mặc định 0).</param>
public sealed record CreateVirtualBlockCommand(
    Guid DepotId,
    string Code,
    string Name,
    int DisplayOrder = 0) : ICommand<Result<CreateBlockResponse>>;

/// <summary>
/// Lệnh thay đổi kích thước (MaxBay/MaxRow/MaxTier) của một Block thực.
/// </summary>
/// <param name="BlockId">Mã block cần thay đổi.</param>
/// <param name="MaxBay">Số Bay mới.</param>
/// <param name="MaxRow">Số Row mới.</param>
/// <param name="MaxTier">Số Tier mới.</param>
public sealed record ResizeBlockCommand(
    Guid BlockId,
    int MaxBay,
    int MaxRow,
    int MaxTier) : ICommand<Result>;

/// <summary>
/// Lệnh cập nhật mã code và tên của một Block.
/// </summary>
/// <param name="BlockId">Mã block cần cập nhật.</param>
/// <param name="Code">Mã code mới.</param>
/// <param name="Name">Tên mới.</param>
public sealed record UpdateBlockCommand(
    Guid BlockId,
    string Code,
    string Name) : ICommand<Result<CreateBlockResponse>>;

/// <summary>
/// Lệnh xóa một Block (phải đảm bảo không có container đang chiếm slot).
/// </summary>
/// <param name="BlockId">Mã block cần xóa.</param>
public sealed record DeleteBlockCommand(Guid BlockId) : ICommand<Result>;

/// <summary>
/// DTO mô tả một YardSlot trong bản đồ yard.
/// </summary>
/// <param name="Id">Mã slot.</param>
/// <param name="Bay">Số Bay.</param>
/// <param name="Row">Số Row.</param>
/// <param name="Tier">Số Tier.</param>
/// <param name="IsOccupied">Trạng thái chiếm slot.</param>
/// <param name="CurrentContainerId">Mã container đang chiếm (nếu có).</param>
public sealed record YardSlotDto(
    Guid Id,
    int Bay,
    int Row,
    int Tier,
    bool IsOccupied,
    Guid? CurrentContainerId);

/// <summary>
/// DTO mô tả một Block cùng danh sách các slot của nó trong bản đồ yard.
/// </summary>
/// <param name="Id">Mã block.</param>
/// <param name="Code">Mã code.</param>
/// <param name="Name">Tên block.</param>
/// <param name="IsVirtual">Block ảo hay thực.</param>
/// <param name="MaxBay">Số Bay tối đa.</param>
/// <param name="MaxRow">Số Row tối đa.</param>
/// <param name="MaxTier">Số Tier tối đa.</param>
/// <param name="Slots">Danh sách slot trong block.</param>
public sealed record BlockMapDto(
    Guid Id,
    string Code,
    string Name,
    bool IsVirtual,
    int? MaxBay,
    int? MaxRow,
    int? MaxTier,
    IReadOnlyList<YardSlotDto> Slots);

/// <summary>
/// DTO mô tả toàn bộ bản đồ yard của một Depot.
/// </summary>
/// <param name="DepotId">Mã depot.</param>
/// <param name="DepotName">Tên depot.</param>
/// <param name="Blocks">Danh sách block trong depot.</param>
public sealed record YardMapDto(Guid DepotId, string DepotName, IReadOnlyList<BlockMapDto> Blocks);

/// <summary>
/// Truy vấn lấy bản đồ yard (blocks + slots) của một Depot.
/// </summary>
/// <param name="DepotId">Mã depot cần lấy bản đồ.</param>
public sealed record GetYardMapQuery(Guid DepotId) : IQuery<Result<YardMapDto>>;

/// <summary>
/// Truy vấn lấy danh sách tất cả các Depot.
/// </summary>
public sealed record GetDepotsQuery() : IQuery<Result<IReadOnlyList<DepotDto>>>;
