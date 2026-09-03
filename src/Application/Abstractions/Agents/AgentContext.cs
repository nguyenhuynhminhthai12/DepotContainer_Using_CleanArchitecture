namespace TechSpherex.CleanArchitecture.Application.Abstractions.Agents;

/// <summary>
/// Bối cảnh (context) được truyền cho một skill agent để thực thi.
/// </summary>
public sealed record AgentContext
{
    /// <summary>Lời nhắn ngôn ngữ tự nhiên của người dùng.</summary>
    public required string Prompt { get; init; }

    /// <summary>Các tham số có cấu trúc tùy chọn được trích xuất từ prompt.</summary>
    public Dictionary<string, object?> Parameters { get; init; } = [];

    /// <summary>ID người dùng đã xác thực (nếu có).</summary>
    public string? UserId { get; init; }

    /// <summary>ID tenant hiện tại (nếu đa tenant).</summary>
    public string? TenantId { get; init; }

    /// <summary>Lịch sử hội thoại cho tương tác đa lượt (multi-turn).</summary>
    public List<AgentMessage> ConversationHistory { get; init; } = [];
}

/// <summary>
/// Một tin nhắn trong lịch sử hội thoại của agent.
/// </summary>
/// <param name="Role">Vai trò gửi tin nhắn (ví dụ: "user", "assistant").</param>
/// <param name="Content">Nội dung tin nhắn.</param>
/// <param name="Timestamp">Thời điểm gửi tin nhắn.</param>
public sealed record AgentMessage(string Role, string Content, DateTimeOffset Timestamp);
