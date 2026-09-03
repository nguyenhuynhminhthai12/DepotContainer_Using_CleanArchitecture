namespace TechSpherex.CleanArchitecture.Application.Abstractions.Agents;

/// <summary>
/// Kết quả thực thi một skill agent.
/// </summary>
public sealed record AgentResult
{
    /// <summary>Trạng thái kết quả của lần thực thi.</summary>
    public required AgentResultStatus Status { get; init; }

    /// <summary>Tin nhắn mô tả kết quả thực thi.</summary>
    public required string Message { get; init; }

    /// <summary>Dữ liệu trả về (có thể null).</summary>
    public object? Data { get; init; }

    /// <summary>Metadata bổ sung dưới dạng cặp khóa-giá trị.</summary>
    public Dictionary<string, object?> Metadata { get; init; } = [];

    /// <summary>Tạo một <see cref="AgentResult"/> thành công.</summary>
    /// <param name="message">Tin nhắn mô tả.</param>
    /// <param name="data">Dữ liệu trả về (tùy chọn).</param>
    public static AgentResult Success(string message, object? data = null) =>
        new() { Status = AgentResultStatus.Success, Message = message, Data = data };

    /// <summary>Tạo một <see cref="AgentResult"/> thất bại.</summary>
    /// <param name="message">Tin nhắn mô tả lỗi.</param>
    public static AgentResult Failure(string message) =>
        new() { Status = AgentResultStatus.Failure, Message = message };

    /// <summary>Tạo một <see cref="AgentResult"/> cần thêm thông tin để tiếp tục.</summary>
    /// <param name="message">Yêu cầu thông tin bổ sung từ người dùng.</param>
    public static AgentResult NeedsMoreInfo(string message) =>
        new() { Status = AgentResultStatus.NeedsMoreInfo, Message = message };

    /// <summary>Tạo một <see cref="AgentResult"/> thành công một phần (partial success).</summary>
    /// <param name="message">Tin nhắn mô tả.</param>
    /// <param name="data">Dữ liệu trả về (tùy chọn).</param>
    public static AgentResult PartialSuccess(string message, object? data = null) =>
        new() { Status = AgentResultStatus.PartialSuccess, Message = message, Data = data };
}

/// <summary>
/// Trạng thái của một <see cref="AgentResult"/>.
/// </summary>
public enum AgentResultStatus
{
    /// <summary>Lần thực thi thành công.</summary>
    Success,

    /// <summary>Lần thực thi thất bại.</summary>
    Failure,

    /// <summary>Cần thêm thông tin từ người dùng để tiếp tục.</summary>
    NeedsMoreInfo,

    /// <summary>Thành công một phần — một số công việc đã hoàn thành nhưng chưa toàn bộ.</summary>
    PartialSuccess
}
