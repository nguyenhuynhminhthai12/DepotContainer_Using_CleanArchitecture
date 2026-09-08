namespace TechSpherex.CleanArchitecture.Application.Abstractions.Agents;

/// <summary>
/// Dịch vụ AI tổng quát (Google Gemini / LLM) hỗ trợ giải đáp ngôn ngữ tự nhiên, phân tích dữ liệu và trợ lý depot.
/// </summary>
public interface IAIService
{
    /// <summary>
    /// Kiểm tra xem API Key hoặc cấu hình AI đã được kích hoạt hay chưa.
    /// </summary>
    bool IsConfigured { get; }

    /// <summary>
    /// Gửi câu hỏi kèm ngữ cảnh hệ thống đến LLM để nhận câu trả lời dạng văn bản.
    /// </summary>
    /// <param name="systemInstruction">Hướng dẫn hệ thống và dữ liệu ngữ cảnh (Live Depot Data Context).</param>
    /// <param name="userPrompt">Câu hỏi / yêu cầu từ người dùng.</param>
    /// <param name="cancellationToken">Token hủy tác vụ.</param>
    /// <returns>Câu trả lời định dạng văn bản từ AI hoặc null nếu thất bại.</returns>
    Task<string?> GenerateResponseAsync(string systemInstruction, string userPrompt, CancellationToken cancellationToken = default);
}
