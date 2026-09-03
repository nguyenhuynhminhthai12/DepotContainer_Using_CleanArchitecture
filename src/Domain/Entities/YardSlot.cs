using TechSpherex.CleanArchitecture.Domain.Common;

namespace TechSpherex.CleanArchitecture.Domain.Entities;

    /// <summary>
    /// Một vị trí lưu trữ trong một Block không ảo, được định danh bởi Bay/Row/Tier.
    /// Các Bay lẻ (1, 3, 5…) chứa container 20 feet; các Bay chẵn (2, 4, 6…) chứa container 40 feet
    /// (hai Bay lẻ liền kề bằng một Bay chẵn).
    /// </summary>
    public sealed class YardSlot : AuditableEntity, ITenantEntity
    {
        /// <summary>Mã tham chiếu đến <see cref="Block"/> chứa slot này.</summary>
        public Guid BlockId { get; set; }

        /// <summary>Số Bay (trục dài).</summary>
        public int Bay { get; set; }

        /// <summary>Số Row (trục rộng).</summary>
        public int Row { get; set; }

        /// <summary>Số Tier (trục cao).</summary>
        public int Tier { get; set; }

        /// <summary>Cho biết slot có đang được chiếm bởi container nào không.</summary>
        public bool IsOccupied { get; set; }

        /// <summary>Mã container đang chiếm slot (null nếu trống).</summary>
        public Guid? CurrentContainerId { get; set; }

        /// <summary>Mã tenant mà slot này thuộc về.</summary>
        public string TenantId { get; set; } = "default";

        /// <summary>Block chứa slot này (điều hướng quan hệ).</summary>
        public Block? Block { get; set; }
    }