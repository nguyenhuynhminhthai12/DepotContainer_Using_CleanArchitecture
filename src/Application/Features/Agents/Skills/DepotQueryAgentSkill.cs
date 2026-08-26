using TechSpherex.CleanArchitecture.Application.Abstractions.Agents;
using TechSpherex.CleanArchitecture.Application.Abstractions.Data;
using TechSpherex.CleanArchitecture.Application.Abstractions.Messaging;
using TechSpherex.CleanArchitecture.Application.Features.Reports;
using TechSpherex.CleanArchitecture.Domain.Common;
using TechSpherex.CleanArchitecture.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace TechSpherex.CleanArchitecture.Application.Features.Agents.Skills;

/// <summary>
/// Skill agent that lets depot operators ask natural-language questions about
/// inventory and throughput — e.g. "How many MSC containers are stuck >10 days?".
/// Routes to the same CQRS handlers used by the REST endpoints so business
/// logic is never duplicated.
/// </summary>
public sealed class DepotQueryAgentSkill(
    IAppDbContext dbContext,
    IQueryHandler<GetYardAgingReportQuery, Result<YardAgingReport>> yardAgingHandler,
    IQueryHandler<GetDailyThroughputReportQuery, Result<DailyThroughputReport>> throughputHandler) : ISkillAgent
{
    public string SkillId => "depot-query";
    public string Name => "Depot Query";
    public string Description => "Answer natural-language questions about yard inventory, aging, and throughput by line operator.";

    public IReadOnlyList<string> ExamplePrompts =>
    [
        "How many containers are in the yard?",
        "How many MSC containers have been here over 10 days?",
        "What is the daily throughput for CMA CGM?",
        "Which line operators have the most containers in yard?"
    ];

    public async Task<AgentResult> ExecuteAsync(AgentContext context, CancellationToken cancellationToken = default)
    {
        var prompt = context.Prompt?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(prompt))
            return AgentResult.NeedsMoreInfo("Please ask a question about the depot — e.g. 'How many containers are in the yard?'");

        var lower = prompt.ToLowerInvariant();

        // Detect "stuck / long stay / aging" → yard aging report.
        if (ContainsAny(lower, "stuck", "long stay", "aging", "over 10 days", ">= 10", "old container"))
        {
            var agingResult = await yardAgingHandler.HandleAsync(new GetYardAgingReportQuery(), cancellationToken);
            if (agingResult.IsFailure)
                return AgentResult.Failure(agingResult.Error!.Message);

            // Optional filter on line operator mentioned in prompt.
            var row = agingResult.Value!.Rows.FirstOrDefault(r =>
                lower.Contains(r.LineOperatorCode.ToLowerInvariant())
                || lower.Contains(r.LineOperatorName.ToLowerInvariant()));
            if (row is not null)
            {
                return AgentResult.Success(
                    $"{row.LineOperatorName} ({row.LineOperatorCode}) in-yard: {row.Buckets.WithinTenDays} within 10 days, {row.Buckets.TenDaysOrMore} ≥10 days.",
                    new { Row = row });
            }

            return AgentResult.Success(
                $"Yard aging across all line operators (as of {agingResult.Value.AsOf:O}):",
                new { Report = agingResult.Value });
        }

        if (ContainsAny(lower, "throughput", "daily", "gate in", "gate out", "movements today"))
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var tp = await throughputHandler.HandleAsync(
                new GetDailyThroughputReportQuery(today.AddDays(-7), today), cancellationToken);
            if (tp.IsFailure)
                return AgentResult.Failure(tp.Error!.Message);

            var filtered = tp.Value!.Rows
                .Where(r => lower.Contains(r.LineOperatorCode.ToLowerInvariant())
                            || lower.Contains(r.LineOperatorName.ToLowerInvariant()))
                .ToList();

            var target = filtered.Count > 0 ? filtered : tp.Value.Rows;
            return AgentResult.Success(
                $"Daily throughput (last 7 days) — {target.Count} entries.",
                new { Rows = target });
        }

        if (ContainsAny(lower, "how many", "count", "in the yard", "in yard", "total containers"))
        {
            var total = await dbContext.ContainerMovements
                .CountAsync(m => m.Status == MovementStatus.InYard, cancellationToken);
            var byOp = await dbContext.ContainerMovements
                .Where(m => m.Status == MovementStatus.InYard)
                .GroupBy(m => m.LineOperatorId)
                .Select(g => new { LineOperatorId = g.Key, Count = g.Count() })
                .Join(dbContext.LineOperators, x => x.LineOperatorId, l => l.Id, (x, l) => new { l.Code, l.Name, x.Count })
                .OrderByDescending(x => x.Count)
                .ToListAsync(cancellationToken);

            var breakdown = string.Join(", ", byOp.Select(o => $"{o.Code}={o.Count}"));
            return AgentResult.Success(
                $"Total in-yard: {total}. Breakdown: {breakdown}.",
                new { Total = total, ByLineOperator = byOp });
        }

        return AgentResult.NeedsMoreInfo(
            "Try one of:\n" +
            "- 'How many containers are in the yard?'\n" +
            "- 'How many <LINE> containers have been here over 10 days?'\n" +
            "- 'Daily throughput for <LINE>'");
    }

    private static bool ContainsAny(string text, params string[] tokens)
    {
        foreach (var t in tokens)
        {
            if (text.Contains(t, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }
}