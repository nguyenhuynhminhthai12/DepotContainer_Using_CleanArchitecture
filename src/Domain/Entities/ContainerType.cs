using TechSpherex.CleanArchitecture.Domain.Common;

namespace TechSpherex.CleanArchitecture.Domain.Entities;

    /// <summary>
    /// Bảng tra cứu loại container theo ISO — Dry / Reefer / Open Top / Flat Rack / Bunker / Ventilated / Specialized.
    /// Được gieo (seed) theo Phụ lục II (ISO 6346 Type Codes).
    /// </summary>
    public sealed class ContainerType : AuditableEntity, ITenantEntity
    {
        /// <summary>Mã ISO 6346 (ví dụ: "22G1", "45G1").</summary>
        public string Code { get; set; } = default!;

        /// <summary>Tên loại container.</summary>
        public string Name { get; set; } = default!;

        /// <summary>Mô tả chi tiết (tùy chọn).</summary>
        public string? Description { get; set; }

        /// <summary>Nhóm loại container (Dry / Reefer / OpenTop / FlatRack / Bunker / Ventilated / Specialized).</summary>
        public string Family { get; set; } = default!;

        /// <summary>Cho biết loại container còn đang sử dụng không.</summary>
        public bool IsActive { get; set; } = true;

        /// <summary>Mã tenant mà loại container này thuộc về.</summary>
        public string TenantId { get; set; } = "default";
    }