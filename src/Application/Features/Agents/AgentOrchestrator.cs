using TechSpherex.CleanArchitecture.Application.Abstractions.Agents;
using Microsoft.Extensions.Logging;

namespace TechSpherex.CleanArchitecture.Application.Features.Agents;

/// <summary>
/// Agent orchestrator mặc định — định tuyến prompt đến skill agent phù hợp.
/// Sử dụng keyword matching để lựa chọn skill. Trong môi trường sản xuất,
/// thay thế bằng LLM-based intent detection (ví dụ: OpenAI function calling, Semantic Kernel).
/// </summary>
public sealed class AgentOrchestrator(
    IEnumerable<ISkillAgent> skills,
    ILogger<AgentOrchestrator> logger) : IAgentOrchestrator
{
    /// <inheritdoc/>
    public async Task<AgentResult> ExecuteAsync(AgentContext context, CancellationToken cancellationToken = default)
    {
        if (logger.IsEnabled(LogLevel.Information))
            logger.LogInformation("Agent orchestrator received prompt: {Prompt}", context.Prompt);

        var skill = SelectSkill(context.Prompt);

        if (skill is null)
        {
            var available = GetAvailableSkills();
            return AgentResult.NeedsMoreInfo(
                "Tôi không thể xác định skill nào phù hợp. Các skill khả dụng:\n" +
                string.Join("\n", available.Select(s => $"* **{s.Name}** — {s.Description}")));
        }

        if (logger.IsEnabled(LogLevel.Information))
            logger.LogInformation("Selected skill: {SkillId} ({SkillName})", skill.SkillId, skill.Name);

        try
        {
            var result = await skill.ExecuteAsync(context, cancellationToken);
            if (logger.IsEnabled(LogLevel.Information))
                logger.LogInformation("Skill {SkillId} completed with status: {Status}", skill.SkillId, result.Status);
            return result;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Skill {SkillId} failed with exception", skill.SkillId);
            return AgentResult.Failure($"Đã xảy ra lỗi khi thực thi '{skill.Name}': {ex.Message}");
        }
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
    /// Chọn skill phù hợp dựa trên từ khóa trong prompt.
    /// Nếu chỉ có một skill, tự động chọn skill đó.
    /// </summary>
    /// <param name="prompt">Lời nhắn của người dùng.</param>
    /// <returns>Skill được chọn, hoặc null nếu không khớp skill nào.</returns>
    private ISkillAgent? SelectSkill(string prompt)
    {
        var lower = prompt.ToLowerInvariant();

        foreach (var skill in skills)
        {
            var keywords = skill.Name.ToLowerInvariant().Split(' ')
                .Concat(skill.ExamplePrompts.SelectMany(p => p.ToLowerInvariant().Split(' ')))
                .Where(w => w.Length > 3)
                .Distinct();

            if (keywords.Any(kw => lower.Contains(kw)))
                return skill;
        }

        if (skills.Count() == 1)
            return skills.First();

        return null;
    }
}