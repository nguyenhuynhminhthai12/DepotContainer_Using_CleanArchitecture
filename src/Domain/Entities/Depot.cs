using TechSpherex.CleanArchitecture.Domain.Common;

namespace TechSpherex.CleanArchitecture.Domain.Entities;

    /// <summary>
    /// Đại diện cho một khu depot (cảng) chứa container — một địa điểm vật lý nơi container được lưu trữ.
    /// Trong môi trường nhiều tenant, mỗi Depot được coi là một tenant.
    /// </summary>
    public sealed class Depot : AuditableEntity, ITenantEntity
    {
        /// <summary>Mã code ngắn gọn của depot.</summary>
        public string Code { get; set; } = default!;

        /// <summary>Tên đầy đủ của depot.</summary>
        public string Name { get; set; } = default!;

        /// <summary>Địa chỉ vật lý của depot.</summary>
        public string Address { get; set; } = default!;

        /// <summary>Mã múi giờ của depot (ví dụ: "Asia/Ho_Chi_Minh").</summary>
        public string? TimeZone { get; set; }

        /// <summary>Cho biết depot có đang hoạt động không.</summary>
        public bool IsActive { get; set; } = true;

        /// <summary>Mã tenant mà depot này thuộc về.</summary>
        public string TenantId { get; set; } = "default";
    }