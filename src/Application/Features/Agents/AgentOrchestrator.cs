
using TechSpherex.CleanArchitecture.Application.Abstractions.Agents;
using Microsoft.Extensions.Logging;

namespace TechSpherex.CleanArchitecture.Application.Features.Agents;

/// <summary>
/// Default agent orchestrator that routes prompts to the appropriate skill agent.
/// Uses keyword matching for skill selection. In production, replace with
/// LLM-based intent detection (e.g., OpenAI function calling, Semantic Kernel).
/// </summary>
public sealed class AgentOrchestrator(
    IEnumerable<ISkillAgent> skills,
    ILogger<AgentOrchestrator> logger) : IAgentOrchestrator
{
    public async Task<AgentResult> ExecuteAsync(AgentContext context, CancellationToken cancellationToken = default)
    {
        if (logger.IsEnabled(LogLevel.Information))
            logger.LogInformation("Agent orchestrator received prompt: {Prompt}", context.Prompt);

        var skill = SelectSkill(context.Prompt);

        if (skill is null)
        {
            var available = GetAvailableSkills();
            return AgentResult.NeedsMoreInfo(
                "I couldn't determine which skill to use. Available skills:\n" +
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
            return AgentResult.Failure($"An error occurred while executing '{skill.Name}': {ex.Message}");
        }
    }

    public async Task<AgentResult> ExecuteSkillAsync(string skillId, AgentContext context, CancellationToken cancellationToken = default)
    {
        var skill = skills.FirstOrDefault(s => s.SkillId.Equals(skillId, StringComparison.OrdinalIgnoreCase));

        if (skill is null)
            return AgentResult.Failure($"Skill '{skillId}' not found.");

        return await skill.ExecuteAsync(context, cancellationToken);
    }

    public IReadOnlyList<SkillInfo> GetAvailableSkills() =>
        [.. skills.Select(s => new SkillInfo(s.SkillId, s.Name, s.Description, s.ExamplePrompts))];

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
