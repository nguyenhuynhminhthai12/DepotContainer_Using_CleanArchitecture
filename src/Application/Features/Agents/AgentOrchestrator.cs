using System.Text;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TechSpherex.CleanArchitecture.Application.Abstractions.Agents;
using TechSpherex.CleanArchitecture.Application.Abstractions.Data;
using TechSpherex.CleanArchitecture.Domain.Entities;

namespace TechSpherex.CleanArchitecture.Application.Features.Agents;

/// <summary>
/// Agent orchestrator nâng cao tích hợp Google Gemini AI và Skill Agents Clean Architecture.
/// Tự động thu thập dữ liệu ngữ cảnh thực tế từ Depot Database và chuyển tiếp cho Gemini AI xử lý ngôn ngữ tự nhiên.
/// Tự động fallback về Local Skill Agents nếu Gemini API không khả dụng.
/// </summary>
public sealed partial class AgentOrchestrator(
    IEnumerable<ISkillAgent> skills,
    IAIService aiService,
    IAppDbContext dbContext,
    ILogger<AgentOrchestrator> logger) : IAgentOrchestrator
{
    [GeneratedRegex(@"\b[A-Z]{4}\d{7}\b", RegexOptions.IgnoreCase)]
    private static partial Regex ContainerNumberRegex();

    /// <inheritdoc/>
    public async Task<AgentResult> ExecuteAsync(AgentContext context, CancellationToken cancellationToken = default)
    {
        var prompt = context.Prompt?.Trim() ?? string.Empty;
        if (logger.IsEnabled(LogLevel.Information))
            logger.LogInformation("Agent orchestrator processing prompt: {Prompt}", prompt);

        // 1. Thử giải quyết thông qua Google Gemini AI nếu đã cấu hình
        if (aiService.IsConfigured && !string.IsNullOrWhiteSpace(prompt))
        {
            try
            {
                var systemInstruction = await BuildDepotContextAsync(prompt, cancellationToken);
                var geminiResponse = await aiService.GenerateResponseAsync(systemInstruction, prompt, cancellationToken);

                if (!string.IsNullOrWhiteSpace(geminiResponse))
                {
                    if (logger.IsEnabled(LogLevel.Information))
                        logger.LogInformation("Successfully generated AI response via Google Gemini.");

                    return AgentResult.Success(geminiResponse, new { Source = "Google Gemini AI", Prompt = prompt });
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Gemini AI processing failed. Falling back to local skills.");
            }
        }

        // 2. Fallback về Skill Agent định tuyến cục bộ nếu Gemini không có hoặc lỗi
        return await ExecuteLocalSkillAsync(context, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<AgentResult> ExecuteSkillAsync(string skillId, AgentContext context, CancellationToken cancellationToken = default)
    {
        var skill = skills.FirstOrDefault(s => s.SkillId.Equals(skillId, StringComparison.OrdinalIgnoreCase));

        if (skill is null)
            return AgentResult.Failure($"Skill '{skillId}' không tìm thấy.");

        return await skill.ExecuteAsync(context, cancellationToken);
    }

    /// <inheritdoc/>
    public IReadOnlyList<SkillInfo> GetAvailableSkills() =>
        [.. skills.Select(s => new SkillInfo(s.SkillId, s.Name, s.Description, s.ExamplePrompts))];

    /// <summary>
    /// Thu thập và đóng gói dữ liệu thời gian thực từ cơ sở dữ liệu bãi thành ngữ cảnh (RAG Context) cho Gemini AI.
    /// </summary>
    private async Task<string> BuildDepotContextAsync(string prompt, CancellationToken cancellationToken)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Bạn là Trợ lý AI Thông Minh (Depot Copilot) chuyên trách hệ thống điều hành cảng/bãi container TechSpherex Clean Architecture.");
        sb.AppendLine("Quy tắc nghiệp vụ bãi (TOS Domain Rules):");
        sb.AppendLine("• Quy tắc chẵn/lẻ Bay (Bay Parity):");
        sb.AppendLine("  - Bay số LẺ (1, 3, 5, 7...) CHỈ dành cho container 20 FEET (20ft).");
        sb.AppendLine("  - Bay số CHẴN (2, 4, 6, 8...) CHỈ dành cho container 40 FEET (40ft) hoặc 45 FEET.");
        sb.AppendLine("• Mỗi vị trí ô (Slot) tại một thời điểm chỉ chứa duy nhất 1 container.");
        sb.AppendLine("• Tầng (Tier): Tier 1 là mặt đất, Tier 2 xếp trên Tier 1.");
        sb.AppendLine();
        sb.AppendLine("Quy tắc ứng xử và định dạng phản hồi:");
        sb.AppendLine("1. Luôn trả lời hoàn toàn bằng TIẾNG VIỆT, văn phong chuyên nghiệp, lịch sự, rõ ràng, gãy gọn.");
        sb.AppendLine("2. Định dạng câu trả lời bằng Markdown rõ ràng:");
        sb.AppendLine("   - Mỗi thông tin hoặc ý chính phải ngắt xuống dòng riêng biệt (dùng ký hiệu - hoặc • ở đầu dòng).");
        sb.AppendLine("   - In đậm tiêu đề mục hoặc từ khóa quan trọng (**Tên mục:** giá trị).");
        sb.AppendLine("3. Dựa trên dữ liệu thực tế được cung cấp dưới đây để tư vấn hoặc trả lời câu hỏi của người dùng.");
        sb.AppendLine();
        sb.AppendLine("=== DỮ LIỆU THỰC TẾ HỆ THỐNG DEPOT HIỆN TẠI ===");

        try
        {
            // 1. Thống kê container đang trong bãi
            var totalInYard = await dbContext.ContainerMovements
                .CountAsync(m => m.Status == MovementStatus.InYard, cancellationToken);

            var lineOpStats = await dbContext.ContainerMovements
                .Where(m => m.Status == MovementStatus.InYard)
                .GroupBy(m => m.LineOperatorId)
                .Select(g => new { LineOperatorId = g.Key, Count = g.Count() })
                .Join(dbContext.LineOperators, x => x.LineOperatorId, l => l.Id, (x, l) => new { l.Code, l.Name, x.Count })
                .OrderByDescending(x => x.Count)
                .ToListAsync(cancellationToken);

            sb.AppendLine($"• Tổng container hiện có trong bãi (InYard): {totalInYard} containers.");
            if (lineOpStats.Count > 0)
            {
                var lineOpStr = string.Join(", ", lineOpStats.Select(s => $"{s.Code} ({s.Name}): {s.Count} cont"));
                sb.AppendLine($"• Phân bổ theo hãng tàu: {lineOpStr}.");
            }

            // 2. Danh sách Block trong bãi
            var blocks = await dbContext.Blocks.Take(10).ToListAsync(cancellationToken);
            if (blocks.Count > 0)
            {
                var blockStr = string.Join(", ", blocks.Select(b => $"{b.Code} ({b.Name}, MaxBay={b.MaxBay}, MaxRow={b.MaxRow}, MaxTier={b.MaxTier})"));
                sb.AppendLine($"• Danh sách khu vực/Block bãi: {blockStr}.");
            }

            // 3. Danh sách một số container mẫu và kích thước
            var containers = await dbContext.Containers
                .Include(c => c.ContainerType)
                .Take(15)
                .ToListAsync(cancellationToken);
            if (containers.Count > 0)
            {
                var cListStr = string.Join(", ", containers.Select(c => $"{c.ContainerNumberRaw} ({c.SizeFeet}ft, {c.Owner}, {c.Condition})"));
                sb.AppendLine($"• Một số container đã đăng ký: {cListStr}.");
            }

            // 4. Tra cứu container cụ thể nếu có nhắc đến trong câu hỏi
            var match = ContainerNumberRegex().Match(prompt);
            if (match.Success)
            {
                var containerNo = match.Value.ToUpperInvariant();
                var cont = await dbContext.Containers
                    .Include(c => c.ContainerType)
                    .FirstOrDefaultAsync(c => c.ContainerNumberRaw == containerNo, cancellationToken);

                if (cont != null)
                {
                    var latestMove = await dbContext.ContainerMovements
                        .Include(m => m.Block)
                        .Include(m => m.YardSlot)
                        .Include(m => m.LineOperator)
                        .Where(m => m.ContainerId == cont.Id)
                        .OrderByDescending(m => m.GateInAt)
                        .FirstOrDefaultAsync(cancellationToken);

                    var statusStr = latestMove?.Status == MovementStatus.InYard ? "Đang trong bãi (InYard)" : "Đã Gate-Out";
                    var slotStr = latestMove?.YardSlot != null
                        ? $"Block {latestMove.Block?.Code}, Bay {latestMove.YardSlot.Bay}, Row {latestMove.YardSlot.Row}, Tier {latestMove.YardSlot.Tier}"
                        : "Chưa phân slot";

                    sb.AppendLine();
                    sb.AppendLine($"[THÔNG TIN CONTAINER {containerNo} TÌM THẤY]:");
                    sb.AppendLine($"- Mã ISO: {cont.IsoCode}, Size: {cont.SizeFeet}ft, Chủ sở hữu: {cont.Owner}");
                    sb.AppendLine($"- Tình trạng vỏ: {cont.Condition}");
                    sb.AppendLine($"- Trạng thái di chuyển: {statusStr}, Thời điểm Gate-In: {latestMove?.GateInAt:dd/MM/yyyy HH:mm}");
                    sb.AppendLine($"- Vị trí bãi hiện tại: {slotStr}");
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to build full DB context for Gemini prompt.");
        }

        return sb.ToString();
    }

    private async Task<AgentResult> ExecuteLocalSkillAsync(AgentContext context, CancellationToken cancellationToken)
    {
        var skill = SelectSkill(context.Prompt);

        if (skill is null)
        {
            var available = GetAvailableSkills();
            return AgentResult.NeedsMoreInfo(
                "Tôi không thể xác định skill nào phù hợp. Các skill khả dụng:\n" +
                string.Join("\n", available.Select(s => $"* **{s.Name}** — {s.Description}")));
        }

        try
        {
            return await skill.ExecuteAsync(context, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Skill {SkillId} failed with exception", skill.SkillId);
            return AgentResult.Failure($"Đã xảy ra lỗi khi thực thi '{skill.Name}': {ex.Message}");
        }
    }

    private ISkillAgent? SelectSkill(string prompt)
    {
        var lower = prompt.ToLowerInvariant();

        if (lower.Contains("todo") || lower.Contains("task") || lower.Contains("nhiệm vụ") || lower.Contains("công việc"))
        {
            var todoSkill = skills.FirstOrDefault(s => s.SkillId.Equals("todo-skill", StringComparison.OrdinalIgnoreCase));
            if (todoSkill is not null) return todoSkill;
        }

        var depotSkill = skills.FirstOrDefault(s => s.SkillId.Equals("depot-query", StringComparison.OrdinalIgnoreCase));
        if (depotSkill is not null)
        {
            var depotKeywords = new[]
            {
                "container", "bãi", "yard", "lưu", "aging", "khẩu lượng", "sản lượng", "thông lượng",
                "hàng", "hôm nay", "ngày", "gate", "cổng", "nhập", "xuất", "tìm", "tra cứu", "đếm", "bao nhiêu", "tổng"
            };

            if (depotKeywords.Any(k => lower.Contains(k)))
                return depotSkill;
        }

        return depotSkill ?? skills.FirstOrDefault();
    }
}