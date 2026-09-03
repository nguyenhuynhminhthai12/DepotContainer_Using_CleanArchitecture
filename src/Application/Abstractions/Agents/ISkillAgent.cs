namespace TechSpherex.CleanArchitecture.Application.Abstractions.Agents;

/// <summary>
/// Đại diện cho một kỹ năng (skill) mà một AI agent có thể thực thi.
/// Mỗi skill tương ứng với một khả năng miền (domain capability) cụ thể (ví dụ: quản lý todo, tạo báo cáo).
/// </summary>
public interface ISkillAgent
{
    /// <summary>Mã định danh duy nhất cho skill này.</summary>
    string SkillId { get; }

    /// <summary>Tên có thể đọc được của skill.</summary>
    string Name { get; }

    /// <summary>Mô tả chức năng của skill này.</summary>
    string Description { get; }

    /// <summary>Danh sách các ví dụ prompt có thể kích hoạt skill này.</summary>
    IReadOnlyList<string> ExamplePrompts { get; }

    /// <summary>
    /// Thực thi skill với bối cảnh (context) được cung cấp.
    /// </summary>
    /// <param name="context">Bối cảnh thực thi chứa prompt và tham số.</param>
    /// <param name="cancellationToken">Token hủy.</param>
    /// <returns>Kết quả thực thi dưỗng <see cref="AgentResult"/>.</returns>
    Task<AgentResult> ExecuteAsync(AgentContext context, CancellationToken cancellationToken = default);
}
