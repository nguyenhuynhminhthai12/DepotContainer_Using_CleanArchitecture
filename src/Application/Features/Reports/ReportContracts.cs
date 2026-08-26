using TechSpherex.CleanArchitecture.Application.Abstractions.Messaging;
using TechSpherex.CleanArchitecture.Domain.Common;

namespace TechSpherex.CleanArchitecture.Application.Features.Reports;

public sealed record YardAgingBucket(int WithinTenDays, int TenDaysOrMore, int Total);

public sealed record YardAgingRow(Guid LineOperatorId, string LineOperatorCode, string LineOperatorName, YardAgingBucket Buckets);

public sealed record YardAgingReport(DateTimeOffset AsOf, IReadOnlyList<YardAgingRow> Rows);

public sealed record GetYardAgingReportQuery() : IQuery<Result<YardAgingReport>>;

public sealed record DailyThroughputRow(
    Guid LineOperatorId,
    string LineOperatorCode,
    string LineOperatorName,
    DateOnly DateOffset,
    int GateInCount,
    int GateOutCount);

public sealed record DailyThroughputReport(IReadOnlyList<DailyThroughputRow> Rows);

public sealed record GetDailyThroughputReportQuery(DateOnly? FromDate, DateOnly? ToDate)
    : IQuery<Result<DailyThroughputReport>>;