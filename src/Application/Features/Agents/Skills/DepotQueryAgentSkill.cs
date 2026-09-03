
using TechSpherex.CleanArchitecture.Application.Abstractions.Agents;
using TechSpherex.CleanArchitecture.Application.Abstractions.Data;
using TechSpherex.CleanArchitecture.Application.Abstractions.Messaging;
using TechSpherex.CleanArchitecture.Application.Features.Reports;
using TechSpherex.CleanArchitecture.Domain.Common;
using TechSpherex.CleanArchitecture.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace TechSpherex.CleanArchitecture.Application.Features.Agents.Skills;

/// <summary>
/// Skill agent cho phép vận hành viên depot đặt câu hỏi bằng ngôn ngữ tự nhiên về
/// tồn kho và khẩu lượng — ví dụ: "Có bao nhiêu container MSC bị kẹt >10 ngày?".
/// Điều hướng tới cùng các CQRS handler được dùng bởi REST endpoints để logic nghiệp vụ
/// không bị trùng lặp.
/// </summary>
public sealed class DepotQueryAgentSkill(
    IAppDbContext dbContext,
    IQueryHandler<GetYardAgingReportQuery, Result<YardAgingReport>> yardAgingHandler,
    IQueryHandler<GetDailyThroughputReportQuery, Result<DailyThroughputReport>> throughputHandler) : ISkillAgent
{
    /// <inheritdoc/>
    public string SkillId => "depot-query";

    /// <inheritdoc/>
    public string Name => "Depot Query";

    /// <inheritdoc/>
    public string Description => "Trả lời câu hỏi ngôn ngữ tự nhiên về tồn kho yard, thời gian lưu và khẩu lượng theo hành đường.";

    /// <inheritdoc/>
    public IReadOnlyList<string> ExamplePrompts =>
    [
        "Có bao nhiêu container trong yard?",
        "Có bao nhiêu container MSC đã ở đây hơn 10 ngày?",
        "Khẩu lượng hàng ngày của CMA CGM là bao nhiêu?",
        "Hành đường nào có nhiều container trong yard nhất?"
    ];

    /// <inheritdoc/>
    public async Task<AgentResult> ExecuteAsync(AgentContext context, CancellationToken cancellationToken = default)
    {
        var prompt = context.Prompt?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(prompt))
            return AgentResult.NeedsMoreInfo("Vui lòng hỏi một câu hỏi về depot — ví dụ: 'Có bao nhiêu container trong yard?'");

        // Phát hiện "stuck / long stay / aging" → báo cáo yard aging.
        if (ContainsAny(prompt, "stuck", "long stay", "aging", "over 10 days", ">= 10", "old container"))
        {
            var agingResult = await yardAgingHandler.HandleAsync(new GetYardAgingReportQuery(), cancellationToken);
            if (agingResult.IsFailure)
                return AgentResult.Failure(agingResult.Error!.Message);

            // Lọc theo hành đường được đề cập trong prompt (tùy chọn).
            var row = agingResult.Value!.Rows.FirstOrDefault(r =>
                prompt.Contains(r.LineOperatorCode, StringComparison.OrdinalIgnoreCase)
                || prompt.Contains(r.LineOperatorName, StringComparison.OrdinalIgnoreCase));
            if (row is not null)
            {
                return AgentResult.Success(
                    $"{row.LineOperatorName} ({row.LineOperatorCode}) trong yard: {row.Buckets.WithinTenDays} trong vòng 10 ngày, {row.Buckets.TenDaysOrMore} >=10 ngày.",
                    new { Row = row });
            }

            return AgentResult.Success(
                $"Thời gian lưu trữ container trong yard (đến {agingResult.Value.AsOf:O}):",
                new { Report = agingResult.Value });
        }

        if (ContainsAny(prompt, "throughput", "daily", "gate in", "gate out", "movements today"))
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var tp = await throughputHandler.HandleAsync(
                new GetDailyThroughputReportQuery(today.AddDays(-7), today), cancellationToken);
            if (tp.IsFailure)
                return AgentResult.Failure(tp.Error!.Message);

            var filtered = tp.Value!.Rows
                .Where(r => prompt.Contains(r.LineOperatorCode, StringComparison.OrdinalIgnoreCase)
                            || prompt.Contains(r.LineOperatorName, StringComparison.OrdinalIgnoreCase))
                .ToList();

            var target = filtered.Count > 0 ? filtered : tp.Value.Rows;
            return AgentResult.Success(
                $"Khẩu lượng hàng ngày (7 ngày qua) — {target.Count} bản ghi.",
                new { Rows = target });
        }

        if (ContainsAny(prompt, "how many", "count", "in the yard", "in yard", "total containers"))
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
                $"Tổng số trong yard: {total}. Phân bổ: {breakdown}.",
                new { Total = total, ByLineOperator = byOp });
        }

        return AgentResult.NeedsMoreInfo(
            "Thử một trong các câu sau:\n" +
            "- 'Có bao nhiêu container trong yard?'\n" +
            "- 'Có bao nhiêu container <LINE> ở đây hơn 10 ngày?'\n" +
            "- 'Khẩu lượng hàng ngày cho <LINE>'");
    }

    /// <summary>
    /// Kiểm tra xem chuỗi <paramref name="text"/> có chứa bất kỳ token nào trong <paramref name="tokens"/> không
    /// (so sánh không phân biệt chữ hoa/thường theo OrdinalIgnoreCase).
    /// </summary>
    /// <param name="text">Văn bản cần kiểm tra.</param>
    /// <param name="tokens">Danh sách các từ khóa cần tìm.</param>
    /// <returns>True nếu tìm thấy ít nhất một token.</returns>
    private static bool ContainsAny(string text, params string[] tokens) =>
        tokens.Any(t => text.Contains(t, StringComparison.OrdinalIgnoreCase));
}
