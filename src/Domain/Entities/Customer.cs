using TechSpherex.CleanArchitecture.Domain.Common;

namespace TechSpherex.CleanArchitecture.Domain.Entities;

    /// <summary>
    /// Khách hàng (shipper / consignee / công ty vận chuyển) được phép nhận container.
    /// </summary>
    public sealed class Customer : AuditableEntity, ITenantEntity
    {
        /// <summary>Mã số thuế Việt Nam (MST) của khách hàng.</summary>
        public string TaxCode { get; set; } = default!;

        /// <summary>Tên khách hàng.</summary>
        public string Name { get; set; } = default!;

        /// <summary>Địa chỉ khách hàng.</summary>
        public string? Address { get; set; }

        /// <summary>Số điện thoại liên hệ.</summary>
        public string? Phone { get; set; }

        /// <summary>Email liên hệ.</summary>
        public string? Email { get; set; }

        /// <summary>Cho biết khách hàng còn đang hoạt động không.</summary>
        public bool IsActive { get; set; } = true;

        /// <summary>Mã tenant mà khách hàng này thuộc về.</summary>
        public string TenantId { get; set; } = "default";
    }