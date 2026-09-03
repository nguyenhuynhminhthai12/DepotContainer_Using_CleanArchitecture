using TechSpherex.CleanArchitecture.Domain.Common;

namespace TechSpherex.CleanArchitecture.Domain.Entities;

    /// <summary>
    /// Một Block là một khu vực (thực tế hoặc ảo) trong Depot nhóm các vị trí lưu trữ container.
    /// Block thực được sắp xếp thành lưới 3D: Bay (dài) → Row (rộng) → Tier (cao).
    /// Block ảo (IsVirtual=true) chỉ theo dõi container mà không có lưới vị trí slot.
    /// </summary>
    public sealed class Block : AuditableEntity, ITenantEntity
    {
        /// <summary>Mã tham chiếu đến <see cref="Depot"/> chứa block này.</summary>
        public Guid DepotId { get; set; }

        /// <summary>Mã code ngắn gọn của block.</summary>
        public string Code { get; set; } = default!;

        /// <summary>Tên đầy đủ của block.</summary>
        public string Name { get; set; } = default!;

        /// <summary>Cho biết block có phải block ảo (không có lưới vị trí) không.</summary>
        public bool IsVirtual { get; set; }

        /// <summary>Số Bay tối đa (trục dài) — chỉ áp dụng cho block thực.</summary>
        public int? MaxBay { get; set; }

        /// <summary>Số Row tối đa (trục rộng) — chỉ áp dụng cho block thực.</summary>
        public int? MaxRow { get; set; }

        /// <summary>Số Tier tối đa (trục cao) — chỉ áp dụng cho block thực.</summary>
        public int? MaxTier { get; set; }

        /// <summary>Mã tenant mà block này thuộc về.</summary>
        public string TenantId { get; set; } = "default";

        /// <summary>Vị trí thứ tự hiển thị trong Depot (bắt đầu từ 1).</summary>
        public int DisplayOrder { get; set; }

        ///summary>Depot chứa block này (điều hướng quan hệ).</summary>
        public Depot? Depot { get; set; }
    }