using TechSpherex.CleanArchitecture.Domain.Common;

namespace TechSpherex.CleanArchitecture.Domain.Entities;

    /// <summary>
    /// Equipment Interchange Receipt (EIR) — một bản ghi vòng đời cho mỗi lần di chuyển container qua cửa xưởng.
    /// </summary>
    public sealed class ContainerMovement : AuditableEntity, ITenantEntity
    {
        /// <summary>Mã tham chiếu đến <see cref="Container"/> di chuyển.</summary>
        public Guid ContainerId { get; set; }

        /// <summary>Mã tham chiếu đến <see cref="LineOperator"/> sở hữu container.</summary>
        public Guid LineOperatorId { get; set; }

        /// <summary>Vị trí lưu trữ trong kho (null nếu là block ảo / ngoài kho).</summary>
        public Guid? YardSlotId { get; set; }

        /// <summary>Mã block chứa container (có thể null).</summary>
        public Guid? BlockId { get; set; }

        /// <summary>Phân loại gán ở cửa xưởng vào lúc nhập (A / B / C).</summary>
        public string Classification { get; set; } = "A";

        /// <summary>Tình trạng container khi nhập cửa.</summary>
        public ContainerCondition ConditionAtGateIn { get; set; } = ContainerCondition.Normal;

        /// <summary>Tình trạng container khi xuất cửa (null nếu chưa xuất).</summary>
        public ContainerCondition? ConditionAtGateOut { get; set; }

        /// <summary>Số xe vào kho.</summary>
        public string? VehicleInNumber { get; set; }

        /// <summary>Tên tài xế vào kho.</summary>
        public string? DriverInName { get; set; }

        /// <summary>Thời điểm nhập cửa.</summary>
        public DateTimeOffset GateInAt { get; set; }

        /// <summary>Số xe xuất kho.</summary>
        public string? VehicleOutNumber { get; set; }

        /// <summary>Tên tài xế xuất kho.</summary>
        public string? DriverOutName { get; set; }

        /// <summary>Thời điểm xuất cửa (null nếu chưa xuất).</summary>
        public DateTimeOffset? GateOutAt { get; set; }

        /// <summary>Trạng thái di chuyển hiện tại của container.</summary>
        public MovementStatus Status { get; set; } = MovementStatus.InYard;

        /// <summary>Mã tham chiếu đến <see cref="DeliveryOrder"/> liên quan (dành cho Gate Out).</summary>
        public Guid? DeliveryOrderId { get; set; }

        /// <summary>Mã tenant mà bản ghi này thuộc về.</summary>
        public string TenantId { get; set; } = "default";

        /// <summary>Container (điều hướng quan hệ).</summary>
        public Container? Container { get; set; }

        /// <summary>Hành đường sở hữu container (điều hướng quan hệ).</summary>
        public LineOperator? LineOperator { get; set; }

        /// <summary>Vị trí yard slot chứa container (điều hướng quan hệ).</summary>
        public YardSlot? YardSlot { get; set; }

        /// <summary>Block chứa container (điều hướng quan hệ).</summary>
        public Block? Block { get; set; }

        /// <summary>Đơn giao hàng liên quan (điều hướng quan hệ).</summary>
        public DeliveryOrder? DeliveryOrder { get; set; }
    }

    /// <summary>
    /// Trạng thái của một lần di chuyển container.
    /// </summary>
    public enum MovementStatus
    {
        /// <summary>Container đang trong kho (chưa xuất cửa).</summary>
        InYard,

        /// <summary>Container đã xuất cửa khỏi kho.</summary>
        GateOut
    }