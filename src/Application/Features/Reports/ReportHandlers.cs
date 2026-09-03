using TechSpherex.CleanArchitecture.Application.Abstractions.Data;
using TechSpherex.CleanArchitecture.Application.Abstractions.Messaging;
using TechSpherex.CleanArchitecture.Domain.Common;
using TechSpherex.CleanArchitecture.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace TechSpherex.CleanArchitecture.Application.Features.Reports;

/// <summary>
/// Xử lý truy vấn lấy báo cáo thời gian lưu trữ container trong yard theo hành đường.
/// Bucket hóa thành hai nhóm: trong vòng 10 ngày và 10 ngày trở lên.
/// </summary>
public sealed class GetYardAgingReportQueryHandler(IAppDbContext dbContext) :
    IQueryHandler<GetYardAgingReportQuery, Result<YardAgingReport>>
{
    /// <inheritdoc/>
    public async Task<Result<YardAgingReport>> HandleAsync(GetYardAgingReportQuery query, CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var cutoff = now.AddDays(-10);

        // Lấy các movement đang InYard và bucket trong bộ nhớ
        // để duy trì khả năng port khẩp các phiên bản Npgsql
        // và đảm bảo kết quả 0-10 / >=10 chính xác.
        var movements = await dbContext.ContainerMovements
            .AsNoTracking()
            .Where(m => m.Status == MovementStatus.InYard)
            .Select(m => new { m.LineOperatorId, m.GateInAt })
            .ToListAsync(cancellationToken);

        var grouped = movements
            .GroupBy(m => m.LineOperatorId)
            .Select(g => new
            {
                LineOperatorId = g.Key,
                WithinTenDays = g.Count(m => m.GateInAt >= cutoff),
                TenDaysOrMore = g.Count(m => m.GateInAt < cutoff),
                Total = g.Count()
            })
            .ToList();

        var lineOperators = await dbContext.LineOperators.AsNoTracking().ToListAsync(cancellationToken);
        var lookup = lineOperators.ToDictionary(l => l.Id);

        var rows = grouped
            .Where(g => lookup.ContainsKey(g.LineOperatorId))
            .Select(g =>
            {
                var op = lookup[g.LineOperatorId];
                return new YardAgingRow(
                    g.LineOperatorId,
                    op.Code,
                    op.Name,
                    new YardAgingBucket(g.WithinTenDays, g.TenDaysOrMore, g.Total));
            })
            .OrderBy(r => r.LineOperatorCode)
            .ToList();

        return Result.Success(new YardAgingReport(now, rows));
    }
}

/// <summary>
/// Xử lý truy vấn lấy báo cáo khẩu lượng giao nhận (Gate-In/Gate-Out) hàng ngày theo hành đường.
/// </summary>
public sealed class GetDailyThroughputReportQueryHandler(IAppDbContext dbContext) :
    IQueryHandler<GetDailyThroughputReportQuery, Result<DailyThroughputReport>>
{
    /// <inheritdoc/>
    public async Task<Result<DailyThroughputReport>> HandleAsync(GetDailyThroughputReportQuery query, CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);
        var fromDate = query.FromDate ?? today.AddDays(-30);
        var toDate = query.ToDate ?? today;

        var fromDateTime = new DateTimeOffset(fromDate.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var toDateTime = new DateTimeOffset(toDate.AddDays(1).ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);

        var movements = await dbContext.ContainerMovements
            .AsNoTracking()
            .Where(m => m.GateInAt >= fromDateTime && m.GateInAt < toDateTime)
            .ToListAsync(cancellationToken);

        var lineOperators = await dbContext.LineOperators.AsNoTracking().ToListAsync(cancellationToken);
        var opLookup = lineOperators.ToDictionary(l => l.Id);

        // Gom nhóm cả số lần Gate-In (đếm) và Gate-Out (đếm, trong khoảng thời gian).
        var rows = movements
            .GroupBy(m => new
            {
                m.LineOperatorId,
                Day = DateOnly.FromDateTime(m.GateInAt.UtcDateTime)
            })
            .Select(g => new
            {
                g.Key.LineOperatorId,
                g.Key.Day,
                GateInCount = g.Count(),
                GateOutCount = g.Count(m => m.GateOutAt >= fromDateTime && m.GateOutAt < toDateTime)
            })
            .Where(r => opLookup.ContainsKey(r.LineOperatorId))
            .Select(r =>
            {
                var op = opLookup[r.LineOperatorId];
                return new DailyThroughputRow(
                    r.LineOperatorId, op.Code, op.Name, r.Day, r.GateInCount, r.GateOutCount);
            })
            .OrderBy(r => r.DateOffset)
            .ThenBy(r => r.LineOperatorCode)
            .ToList();

        return Result.Success(new DailyThroughputReport(rows));
    }
}
