namespace TechSpherex.CleanArchitecture.Domain.Common;

    /// <summary>
    /// Giao diện đánh dấu (marker interface) cho các thực thể thuộc về một tenant cụ thể.
    /// Các thực thể triển khai giao diện này sẽ tự động được lọc theo <see cref="TenantId"/>.
    /// </summary>
    public interface ITenantEntity
    {
        /// <summary>Mã tenant mà thực thể này thuộc về.</summary>
        string TenantId { get; set; }
    }
