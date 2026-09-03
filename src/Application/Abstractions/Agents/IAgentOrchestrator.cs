namespace TechSpherex.CleanArchitecture.Application.Abstractions.Agents;

/// <summary>
/// Đi phối việc lựa chọn và thực thi skill agent.
/// Định tuyến (route) các lời nhắn của người dùng đến skill agent phù hợp.
/// </summary>
public interface IAgentOrchestrator
{
    /// <summary>
    /// Thực thi một lời nhắn bằng cách lựa chọn và gọi skill agent thích hợp.
    /// </summary>
    /// <param name="context">Bối cảnh thực thi chứa prompt và tham số.</param>
    /// <param name="cancellationToken">Token hủy.</param>
    /// <returns>Kết quả thực thi dưới dạng <see cref="AgentResult"/>.</returns>
    Task<AgentResult> ExecuteAsync(AgentContext context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Thực thi một skill cụ thể theo ID.
    /// </summary>
    /// <param name="skillId">Mã định danh của skill cần thực thi.</param>
    /// <param name="context">Bối cảnh thực thi.</param>
    /// <param name="cancellationToken">Token hủy.</param>
    /// <returns>Kết quả thực thi dưới dạng <see cref="AgentResult"/>.</returns>
    Task<AgentResult> ExecuteSkillAsync(string skillId, AgentContext context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Liệt kê tất cả các skill khả dụng.
    /// </summary>
    /// <returns>Danh sách <see cref="SkillInfo"/> chứa thông tin các skill.</returns>
    IReadOnlyList<SkillInfo> GetAvailableSkills();
}

/// <summary>
/// Thông tin tóm tắt về một skill khả dụng.
/// </summary>
/// <param name="Id">Mã định danh duy nhất của skill.</param>
/// <param name="Name">Tên hiển thị của skill.</param>
/// <param name="Description">Mô tả chức năng của skill.</param>
/// <param name="ExamplePrompts">Danh sách ví dụ prompt kích hoạt skill này.</param>
public sealed record SkillInfo(string Id, string Name, string Description, IReadOnlyList<string> ExamplePrompts);
