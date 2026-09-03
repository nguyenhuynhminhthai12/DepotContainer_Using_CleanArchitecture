namespace TechSpherex.CleanArchitecture.Domain.Common;

/// <summary>
/// Lớp cơ sở trừu tượng mở rộng <see cref="BaseEntity"/> với thông tin bản ghi thời gian.
/// Tự động cập nhật thời gian tạo/sửa và người thực hiện.
/// </summary>
public abstract class AuditableEntity : BaseEntity
{
    /// <summary>Thời điểm bản ghi được tạo.</summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>Tên người hoặc hệ thống tạo bản ghi.</summary>
    public string? CreatedBy { get; set; }

    /// <summary>Thời điểm bản ghi được sửa lần cuối.</summary>
    public DateTimeOffset? LastModifiedAt { get; set; }

    /// <summary>Tên người hoặc hệ thống sửa bản ghi lần cuối.</summary>
    public string? LastModifiedBy { get; set; }
}
