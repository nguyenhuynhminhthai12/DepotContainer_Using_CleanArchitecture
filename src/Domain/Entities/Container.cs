using TechSpherex.CleanArchitecture.Domain.Common;

namespace TechSpherex.CleanArchitecture.Domain.Entities;

    /// <summary>
    /// Dữ liệu thực thể Container. ContainerNumber tuân thủ ISO 6346 (chữ số kiểm tra Modulo-11, được xác thực bởi domain rule).
    /// </summary>
    public sealed class Container : AuditableEntity, ITenantEntity
    {
        /// <summary>Số thùng hàng BIC/ISO 6346 11 ký tự (mã chủ sở hữu + mã loại + số seri + chữ số kiểm tra).</summary>
        public string ContainerNumberRaw { get; internal set; } = default!;

        /// <summary>Mã tham chiếu đến <see cref="ContainerType"/>.</summary>
        public Guid ContainerTypeId { get; set; }

        /// <summary>Mã ISO (ví dụ: "22G1").</summary>
        public string IsoCode { get; set; } = default!;

        /// <summary>Kích thước thùng hàng tính bằng feet (20 hoặc 40).</summary>
        public int SizeFeet { get; set; }

        /// <summary>Trọng lượng tối đa (kg) — tải thành.</summary>
        public decimal MaxWeightKg { get; set; }

        /// <summary>Trọng lượng tắm (kg) — trọng lượng của thùng hàng rỗng.</summary>
        public decimal TareWeightKg { get; set; }

        /// <summary>Ngày sản xuất thùng hàng.</summary>
        public DateTimeOffset ManufactureDate { get; set; }

        /// <summary>Chủ sở hữu (line operator) của thùng hàng.</summary>
        public string Owner { get; set; } = default!;

        /// <summary>Tình trạng hiện tại của thùng hàng.</summary>
        public ContainerCondition Condition { get; set; } = ContainerCondition.Normal;

        /// <summary>Mã tenant mà container này thuộc về.</summary>
        public string TenantId { get; set; } = "default";

        /// <summary>Thông tin chi tiết về loại container (điều hướng quan hệ).</summary>
        public ContainerType? ContainerType { get; set; }

        /// <summary>Biểu diễn mạnh kiểu của <see cref="ContainerNumberRaw"/> dưới dạng <see cref="ContainerNumber"/>.</summary>
        public ContainerNumber ContainerNumber => new(ContainerNumberRaw);

#pragma warning disable S107 // Phương thức factory yêu cầu nhiều tham số
        /// <summary>
        /// Tạo một Container mới và kiểm tra chữ số kiểm tra ISO 6346.
        /// </summary>
        /// <param name="containerNumber">Số thùng hàng (chuỗi 11 ký tự).</param>
        /// <param name="containerTypeId">Mã loại container.</param>
        /// <param name="isoCode">Mã ISO.</param>
        /// <param name="sizeFeet">Kích thước (feet).</param>
        /// <param name="maxWeightKg">Trọng lượng tối đa.</param>
        /// <param name="tareWeightKg">Trọng lượng tắm.</param>
        /// <param name="manufactureDate">Ngày sản xuất.</param>
        /// <param name="owner">Chủ sở hữu.</param>
        /// <param name="condition">Tình trạng — mặc định <see cref="ContainerCondition.Normal"/>.</param>
        /// <returns>Đối tượng <see cref="Container"/> mới đã được khởi tạo.</returns>
        public static Container Create(string containerNumber, Guid containerTypeId, string isoCode,
            int sizeFeet, decimal maxWeightKg, decimal tareWeightKg,
            DateTimeOffset manufactureDate, string owner, ContainerCondition condition = ContainerCondition.Normal)
#pragma warning restore S107 // Phương thức factory yêu cầu nhiều tham số
        {
            var normalized = (containerNumber ?? string.Empty).Trim().ToUpperInvariant();
            Domain.Common.Rules.BusinessRuleValidator.CheckRule(
                new Domain.Common.Rules.ContainerNumberCheckDigitRule(normalized));

            return new Container
            {
                ContainerNumberRaw = normalized,
                ContainerTypeId = containerTypeId,
                IsoCode = isoCode,
                SizeFeet = sizeFeet,
                MaxWeightKg = maxWeightKg,
                TareWeightKg = tareWeightKg,
                ManufactureDate = manufactureDate,
                Owner = owner,
                Condition = condition
            };
        }
    }

    /// <summary>
    /// Tình trạng vật lý của thùng hàng.
    /// </summary>
    public enum ContainerCondition
    {
        /// <summary>Thùng hàng ở trạng thái bình thường.</summary>
        Normal,

        /// <summary>Thùng hàng hư hỏng nghiêm trọng.</summary>
        Damaged,

        /// <summary>Thùng hàng bị lồm (dent).</summary>
        Dented,

        /// <summary>Thùng hàng bị xoẹn (twist).</summary>
        Twisted,

        /// <summary>Thùng hàng bị nứt (crack).</summary>
        Cracked,

        /// <summary>Thùng hàng bị rò rỉ (leaking).</summary>
        Leaking,

        /// <summary>Trạng thái khác (không thuộc các trạng thái trên).</summary>
        Other
    }