using TechSpherex.CleanArchitecture.Domain.Common;

namespace TechSpherex.CleanArchitecture.Domain.Entities;

    /// <summary>
    /// Hành đường (Line Operator) — công ty tàu biển sở hữu/quản lý các container (ví dụ: CMA CGM, MSC, HMM, Maersk).
    /// </summary>
    public sealed class LineOperator : AuditableEntity, ITenantEntity
    {
        /// <summary>Mã BIC 3 chữ cái đứng đầu số thùng hàng (ví dụ: "CMA", "MSK").</summary>
        public string Code { get; set; } = default!;

        /// <summary>Tên đầy đủ hành đường.</summary>
        public string Name { get; set; } = default!;

        /// <summary>Quốc gia của hành đường.</summary>
        public string? Country { get; set; }

        /// <summary>Cho biết hành đường còn đang hoạt động không.</summary>
        public bool IsActive { get; set; } = true;

        /// <summary>Mã tenant mà hành đường này thuộc về.</summary>
        public string TenantId { get; set; } = "default";
    }