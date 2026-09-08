using System.Text.RegularExpressions;
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
/// tồn kho, khẩu lượng, vị trí slot và tra cứu container.
/// </summary>
public sealed partial class DepotQueryAgentSkill(
    IAppDbContext dbContext,
    IQueryHandler<GetYardAgingReportQuery, Result<YardAgingReport>> yardAgingHandler,
    IQueryHandler<GetDailyThroughputReportQuery, Result<DailyThroughputReport>> throughputHandler) : ISkillAgent
{
    [GeneratedRegex(@"\b[A-Z]{4}\d{7}\b", RegexOptions.IgnoreCase)]
    private static partial Regex ContainerNumberRegex();

    [GeneratedRegex(@"B(?<bay>\d+)\s*R(?<row>\d+)\s*T(?<tier>\d+)", RegexOptions.IgnoreCase)]
    private static partial Regex SlotPatternRegex();

    /// <inheritdoc/>
    public string SkillId => "depot-query";

    /// <inheritdoc/>
    public string Name => "Depot Query";

    /// <inheritdoc/>
    public string Description => "Trả lời câu hỏi ngôn ngữ tự nhiên về tồn kho yard, thời gian lưu và khẩu lượng theo hãng tàu.";

    /// <inheritdoc/>
    public IReadOnlyList<string> ExamplePrompts =>
    [
        "Có bao nhiêu container trong bãi?",
        "Có bao nhiêu container MSC đã ở đây hơn 10 ngày?",
        "Khẩu lượng hàng ngày của CMA CGM là bao nhiêu?",
        "Tìm container CMAU1234564",
        "Vị trí B1R2T1 xếp container nào phù hợp?"
    ];

    /// <inheritdoc/>
    public async Task<AgentResult> ExecuteAsync(AgentContext context, CancellationToken cancellationToken = default)
    {
        var prompt = context.Prompt?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(prompt))
            return AgentResult.NeedsMoreInfo("Vui lòng hỏi một câu hỏi về depot — ví dụ: 'Có bao nhiêu container trong bãi?'");

        // 1. Tư vấn vị trí Slot (Bay / Row / Tier / Bay Parity)
        if (IsSlotQuery(prompt))
            return HandleSlotParityQuery(prompt);

        // 2. Báo cáo thời gian lưu bãi (Aging)
        if (IsAgingQuery(prompt))
            return await HandleAgingReportQueryAsync(prompt, cancellationToken);

        // 3. Khẩu lượng / sản lượng / throughput hàng ngày
        if (IsThroughputQuery(prompt))
            return await HandleThroughputReportQueryAsync(prompt, cancellationToken);

        // 4. Số lượng container trong bãi / phân bổ hãng tàu
        if (IsInventoryCountQuery(prompt))
            return await HandleInventoryCountQueryAsync(cancellationToken);

        // 5. Tra cứu container cụ thể theo mã container ISO (ví dụ: CMAU1234564)
        var match = ContainerNumberRegex().Match(prompt);
        if (match.Success)
            return await HandleContainerLookupQueryAsync(match.Value, cancellationToken);

        return AgentResult.NeedsMoreInfo(
            "Tôi có thể hỗ trợ bạn các nội dung sau:\n" +
            "• 'Có bao nhiêu container trong bãi?'\n" +
            "• 'Báo cáo thời gian lưu bãi container (Aging)?'\n" +
            "• 'Khẩu lượng / sản lượng hàng ngày là bao nhiêu?'\n" +
            "• 'Tìm container CMAU1234564'\n" +
            "• 'Vị trí B1R2T1 xếp container nào phù hợp?'");
    }

    private static bool IsSlotQuery(string prompt) =>
        SlotPatternRegex().IsMatch(prompt) ||
        ContainsAny(prompt, "vị trí", "xếp vào", "phù hợp", "thêm vào", "bay 1", "bay 2", "bay 3", "bay 4", "bay lẻ", "bay chẵn", "tier");

    private static bool IsAgingQuery(string prompt) =>
        ContainsAny(prompt, "stuck", "long stay", "aging", "over 10 days", ">= 10", "old container", "lưu bãi", "thời gian lưu", "kẹt", "hơn 10 ngày", "trên 10 ngày", "tồn lâu");

    private static bool IsThroughputQuery(string prompt) =>
        ContainsAny(prompt, "throughput", "daily", "gate in", "gate out", "movements today", "khẩu lượng", "sản lượng", "thông lượng", "hàng ngày", "hôm nay", "nhập xuất", "lượt");

    private static bool IsInventoryCountQuery(string prompt) =>
        ContainsAny(prompt, "how many", "count", "in the yard", "in yard", "total containers", "bao nhiêu", "đếm", "tổng số", "tồn bãi", "trong bãi", "trong yard", "hiện có", "hãng tàu", "hãng");

    private static AgentResult HandleSlotParityQuery(string prompt)
    {
        var slotMatch = SlotPatternRegex().Match(prompt);
        int bayNum;
        if (slotMatch.Success)
        {
            bayNum = int.Parse(slotMatch.Groups["bay"].Value);
        }
        else
        {
            bayNum = prompt.Contains("bay 2", StringComparison.OrdinalIgnoreCase) ? 2 : 1;
        }

        var isOddBay = bayNum % 2 != 0;
        var suitableSize = isOddBay ? "20 FEET (20ft)" : "40 FEET (40ft) hoặc 45 FEET";
        var parityType = isOddBay ? "Bay số LẺ" : "Bay số CHẴN";

        var ruleDesc = $"• Vị trí Bay {bayNum} là **{parityType}** $\\rightarrow$ Theo quy tắc hàng hải (Bay Parity), chỉ được phép xếp **container {suitableSize}**.";

        return AgentResult.Success(
            "📍 **Tư vấn vị trí xếp container:**\n\n" +
            ruleDesc + "\n" +
            "• **Quy chuẩn:** Bay Lẻ (1, 3, 5...) $\\rightarrow$ 20ft | Bay Chẵn (2, 4, 6...) $\\rightarrow$ 40ft/45ft.\n" +
            "• **Tầng (Tier):** Tier 1 là tầng dưới cùng (mặt đất), Tier 2 xếp chồng lên trên Tier 1.\n\n" +
            $"💡 *Gợi ý: Bạn có thể chọn container {suitableSize} tại form Gate-In hoặc chuyển bãi (Yard Relocation).*");
    }

    private async Task<AgentResult> HandleAgingReportQueryAsync(string prompt, CancellationToken cancellationToken)
    {
        var agingResult = await yardAgingHandler.HandleAsync(new GetYardAgingReportQuery(), cancellationToken);
        if (agingResult.IsFailure)
            return AgentResult.Failure(agingResult.Error!.Message);

        var row = agingResult.Value!.Rows.FirstOrDefault(r =>
            prompt.Contains(r.LineOperatorCode, StringComparison.OrdinalIgnoreCase)
            || prompt.Contains(r.LineOperatorName, StringComparison.OrdinalIgnoreCase));
        if (row is not null)
        {
            return AgentResult.Success(
                $"⏳ Thời gian lưu trữ container của **{row.LineOperatorName} ({row.LineOperatorCode})**:\n" +
                $"• Trong vòng 10 ngày: **{row.Buckets.WithinTenDays}** cont\n" +
                $"• Từ 10 ngày trở lên: **{row.Buckets.TenDaysOrMore}** cont\n" +
                $"• Tổng cộng: **{row.Buckets.Total}** cont.",
                new { Row = row });
        }

        var totalWithin10 = agingResult.Value.Rows.Sum(r => r.Buckets.WithinTenDays);
        var totalOver10 = agingResult.Value.Rows.Sum(r => r.Buckets.TenDaysOrMore);
        var totalAll = agingResult.Value.Rows.Sum(r => r.Buckets.Total);

        var summaryLines = agingResult.Value.Rows
            .Select(r => $"• **{r.LineOperatorCode}**: {r.Buckets.Total} cont (≤10 ngày: {r.Buckets.WithinTenDays}, ≥10 ngày: {r.Buckets.TenDaysOrMore})")
            .ToList();

        return AgentResult.Success(
            "⏳ **Thời gian lưu trữ container trong yard (Aging Report):**\n\n" +
            $"• **Tổng container tồn bãi:** {totalAll} cont\n" +
            $"• **Tồn ≤ 10 ngày:** {totalWithin10} cont\n" +
            $"• **Tồn ≥ 10 ngày:** {totalOver10} cont\n\n" +
            $"**Chi tiết theo hãng tàu:**\n{string.Join("\n", summaryLines)}",
            new { Report = agingResult.Value });
    }

    private async Task<AgentResult> HandleThroughputReportQueryAsync(string prompt, CancellationToken cancellationToken)
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
        var totalIn = target.Sum(r => r.GateInCount);
        var totalOut = target.Sum(r => r.GateOutCount);

        return AgentResult.Success(
            "📈 **Báo cáo khẩu lượng / sản lượng (7 ngày qua):**\n\n" +
            $"• **Tổng lượt Gate-In (Nhập bãi):** {totalIn} lượt\n" +
            $"• **Tổng lượt Gate-Out (Xuất bãi):** {totalOut} lượt\n" +
            $"• **Tổng lượt di chuyển:** {totalIn + totalOut} lượt.",
            new { Rows = target, TotalIn = totalIn, TotalOut = totalOut });
    }

    private async Task<AgentResult> HandleInventoryCountQueryAsync(CancellationToken cancellationToken)
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

        var breakdown = string.Join("\n", byOp.Select(o => $"• **{o.Code}** ({o.Name}): **{o.Count}** container"));
        return AgentResult.Success(
            $"📦 **Tổng số trong yard: {total} container.**\n\n" +
            $"**Phân bổ theo hãng tàu:**\n{breakdown}",
            new { Total = total, ByLineOperator = byOp });
    }

    private async Task<AgentResult> HandleContainerLookupQueryAsync(string rawNumber, CancellationToken cancellationToken)
    {
        var normNumber = rawNumber.ToUpperInvariant();
        var cont = await dbContext.Containers
            .Include(c => c.ContainerType)
            .FirstOrDefaultAsync(c => c.ContainerNumberRaw == normNumber, cancellationToken);

        if (cont is not null)
        {
            var latestMove = await dbContext.ContainerMovements
                .Include(m => m.Block)
                .Include(m => m.YardSlot)
                .Include(m => m.LineOperator)
                .Where(m => m.ContainerId == cont.Id)
                .OrderByDescending(m => m.GateInAt)
                .FirstOrDefaultAsync(cancellationToken);

            var statusStr = latestMove?.Status == MovementStatus.InYard ? "Đang trong bãi (InYard)" : "Đã xuất bãi (GateOut)";
            var locationStr = latestMove?.YardSlot is not null
                ? $"Block {latestMove.Block?.Code}, Bay {latestMove.YardSlot.Bay}, Row {latestMove.YardSlot.Row}, Tier {latestMove.YardSlot.Tier}"
                : "Chưa phân slot";

            return AgentResult.Success(
                $"🔍 **Thông tin tra cứu container {cont.ContainerNumberRaw}:**\n\n" +
                $"• **Kích thước / Mã ISO:** {cont.SizeFeet}ft ({cont.IsoCode})\n" +
                $"• **Hãng tàu / Chủ sở hữu:** {cont.Owner}\n" +
                $"• **Tình trạng vỏ:** {cont.Condition}\n" +
                $"• **Trạng thái:** {statusStr}\n" +
                $"• **Vị trí bãi:** {locationStr}",
                new { Container = cont, LatestMovement = latestMove });
        }

        return AgentResult.Success(
            $"🔍 Không tìm thấy container **{normNumber}** trong dữ liệu hệ thống bãi hiện tại.\n" +
            "Vui lòng kiểm tra lại số container hoặc thực hiện Gate-In cho container này.",
            new { ContainerNumber = normNumber, Found = false });
    }

    private static bool ContainsAny(string text, params string[] tokens) =>
        tokens.Any(t => text.Contains(t, StringComparison.OrdinalIgnoreCase));
}
