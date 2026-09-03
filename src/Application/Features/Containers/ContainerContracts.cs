using TechSpherex.CleanArchitecture.Application.Abstractions.Messaging;
using TechSpherex.CleanArchitecture.Domain.Common;

namespace TechSpherex.CleanArchitecture.Application.Features.Containers;

    /// <summary>
    /// DTO trả về thông tin container.
    /// </summary>
    /// <param name="Id">Mã định danh container.</param>
    /// <param name="ContainerNumber">Số thùng hàng (11 ký tự ISO 6346).</param>
    /// <param name="ContainerTypeId">Mã loại container.</param>
    /// <param name="IsoCode">Mã ISO.</param>
    /// <param name="SizeFeet">Kích thước (feet).</param>
    /// <param name="MaxWeightKg">Trọng lượng tối đa (kg).</param>
    /// <param name="TareWeightKg">Trọng lượng tắm (kg).</param>
    /// <param name="ManufactureDate">Ngày sản xuất.</param>
    /// <param name="Owner">Tên chủ sở hữu.</param>
    /// <param name="Condition">Tình trạng container.</param>
    public sealed record ContainerResponse(
        Guid Id,
        string ContainerNumber,
        Guid ContainerTypeId,
        string IsoCode,
        int SizeFeet,
        decimal MaxWeightKg,
        decimal TareWeightKg,
        DateTimeOffset ManufactureDate,
        string Owner,
        string Condition);

    /// <summary>
    /// Lệnh tạo container mới (Command). Trả về <see cref="Result{ContainerResponse}"/>.
    /// </summary>
    public sealed record CreateContainerCommand(
        string ContainerNumber,
        Guid ContainerTypeId,
        string IsoCode,
        int SizeFeet,
        decimal MaxWeightKg,
        decimal TareWeightKg,
        DateTimeOffset ManufactureDate,
        string Owner,
        string Condition) : ICommand<Result<ContainerResponse>>;

    /// <summary>Truy vấn lấy container theo số thùng hàng.</summary>
    public sealed record GetContainerByNumberQuery(string ContainerNumber)
        : IQuery<Result<ContainerResponse>>;

    /// <summary>
    /// Truy vấn lấy danh sách container có phân trang và bộ lọc.
    /// </summary>
    /// <param name="Page">Số trang (mặc định 1).</param>
    /// <param name="PageSize">Kích thước trang (mặc định 20).</param>
    /// <param name="LineOperatorId">Lọc theo hành đường (tùy chọn).</param>
    /// <param name="Condition">Lọc theo tình trạng container.</param>
    /// <param name="Search">Từ khóa tìm kiếm trong số thùng hàng hoặc tên chủ sở hữu.</param>
    public sealed record GetContainersQuery(
        int Page = 1,
        int PageSize = 20,
        Guid? LineOperatorId = null,
        string? Condition = null,
        string? Search = null) : IQuery<Result<PagedResult<ContainerResponse>>>;

    /// <summary>Lệnh cập nhật thông tin container.</summary>
    public sealed record UpdateContainerCommand(
        Guid Id,
        Guid ContainerTypeId,
        string IsoCode,
        int SizeFeet,
        decimal MaxWeightKg,
        decimal TareWeightKg,
        DateTimeOffset ManufactureDate,
        string Owner,
        string Condition) : ICommand<Result<ContainerResponse>>;

    /// <summary>Lệnh xóa container theo ID.</summary>
    public sealed record DeleteContainerCommand(Guid Id) : ICommand<Result>;