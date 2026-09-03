namespace TechSpherex.CleanArchitecture.Application.Abstractions.Identity;

/// <summary>
/// Cung cấp thông tin người dùng hiện đang xác thực trong request hiện tại.
/// </summary>
public interface ICurrentUser
{
    /// <summary>ID người dùng (có thể null nếu chưa xác thực).</summary>
    string? UserId { get; }

    /// <summary>Email của người dùng (có thể null nếu chưa xác thực).</summary>
    string? Email { get; }

    /// <summary>Cho biết người dùng đã được xác thực chưa.</summary>
    bool IsAuthenticated { get; }
}
