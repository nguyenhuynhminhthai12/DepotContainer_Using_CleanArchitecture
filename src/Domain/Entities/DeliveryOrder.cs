using TechSpherex.CleanArchitecture.Domain.Common;

namespace TechSpherex.CleanArchitecture.Domain.Entities;

    /// <summary>
    /// Đơn giao hàng / Đơn xuất kho — cho phép depot giải phóng (release) các thùng hàng ròng
    /// cho một khách hàng cụ thể. Bắt buộc phải có để thực hiện thao tác Gate Out.
    /// </summary>
    public sealed class DeliveryOrder : AuditableEntity, ITenantEntity
    {
        /// <summary>Số đơn hàng (mã định danh Đơn giao hàng).</summary>
        public string OrderNumber { get; set; } = default!;

        /// <summary>Mã khách hàng (tham chiếu đến <see cref="Customer"/>).</summary>
        public Guid CustomerId { get; set; }

        /// <summary>Mã hành đường (line operator) sở hữu các thùng hàng trong đơn này.</summary>
        public Guid LineOperatorId { get; set; }

        /// <summary>Ngày hết hạn — depot không được giải phóng vượa quá ngày này.</summary>
        public DateTimeOffset ExpiryDate { get; set; }

        /// <summary>Chuyến hàng (vessel voyage) sẽ chở các thùng hàng ra khỏi Việt Nam.</summary>
        public string? VesselVoyage { get; set; }

        /// <summary>Ghi chú bổ sung cho đơn hàng.</summary>
        public string? Notes { get; set; }

        /// <summary>Cho biết đơn hàng đã bị đóng (closed) hay chưa.</summary>
        public bool IsClosed { get; set; }

        /// <summary>Mã tenant mà đơn hàng này thuộc về.</summary>
        public string TenantId { get; set; } = "default";

        /// <summary>Khách hàng (điều hướng quan hệ).</summary>
        public Customer? Customer { get; set; }

        /// <summary>Hành đường (line operator) sở hữu thùng hàng (điều hướng quan hệ).</summary>
        public LineOperator? LineOperator { get; set; }

        /// <summary>Dòng chi tiết các loại container trong đơn hàng.</summary>
        public ICollection<DeliveryOrderLine> Lines { get; set; } = [];

        /// <summary>Kiểm tra xem đơn hàng đã hết hạn so với thời điểm <paramref name="now"/> chưa.</summary>
        /// <param name="now">Thời điểm hiện tại để so sánh.</param>
        /// <returns>True nếu đã hết hạn, False nếu còn hiệu lực.</returns>
        public bool IsExpiredAt(DateTimeOffset now) => ExpiryDate < now;
    }

    /// <summary>
    /// Dòng chi tiết của <see cref="DeliveryOrder"/> — mô tả số lượng container theo loại.
    /// </summary>
    public sealed class DeliveryOrderLine : AuditableEntity
    {
        /// <summary>Mã tham chiếu đến <see cref="DeliveryOrder"/> cha.</summary>
        public Guid DeliveryOrderId { get; set; }

        /// <summary>Mã loại container (tham chiếu đến <see cref="ContainerType"/>).</summary>
        public Guid ContainerTypeId { get; set; }

        /// <summary>Số lượng container yêu cầu trong dòng này.</summary>
        public int RequestedQuantity { get; set; }

        /// <summary>Số lượng container đã giao thực tế trong dòng này.</summary>
        public int DeliveredQuantity { get; set; }

        /// <summary>Đơn hàng cha (điều hướng quan hệ).</summary>
        public DeliveryOrder? DeliveryOrder { get; set; }

        /// <summary>Loại container (điều hướng quan hệ).</summary>
        public ContainerType? ContainerType { get; set; }
    }