using TechSpherex.CleanArchitecture.Application.Abstractions.Messaging;
using TechSpherex.CleanArchitecture.Domain.Common;

namespace TechSpherex.CleanArchitecture.Application.Features.Reports;

/// <summary>
/// Thùng chứa (bucket) phân loại thời gian lưu trữ container trong yard.
/// </summary>
/// <param name="WithinTenDays">Số container lưu < 10 ngày.</param>
/// <param name="TenDaysOrMore">Số container lưu >= 10 ngày.</param>
/// <param name="Total">Tổng số container trong bucket.</param>
public sealed record YardAgingBucket(int WithinTenDays, int TenDaysOrMore, int Total);

/// <summary>
/// Dòng báo cáo thời gian lưu trữ container theo hành đường.
/// </summary>
/// <param name="LineOperatorId">Mã hành đường.</param>
/// <param name="LineOperatorCode">Mã code BIC.</param>
/// <param name="LineOperatorName">Tên hành đường.</param>
/// <param name="Buckets">Thùng chứa thời gian lưu trữ.</param>
public sealed record YardAgingRow(Guid LineOperatorId, string LineOperatorCode, string LineOperatorName, YardAgingBucket Buckets);

/// <summary>
/// Báo cáo tổng hợp thời gian lưu trữ container trong yard theo hành đường.
/// </summary>
/// <param name="AsOf">Thời điểm tính báo cáo.</param>
/// <param name="Rows">Danh sách hàng theo hành đường.</param>
public sealed record YardAgingReport(DateTimeOffset AsOf, IReadOnlyList<YardAgingRow> Rows);

/// <summary>Truy vấn lấy báo cáo thời gian lưu trữ container trong yard.</summary>
public sealed record GetYardAgingReportQuery() : IQuery<Result<YardAgingReport>>;

/// <summary>
/// Dòng báo cáo khẩu lượng giao nhận hàng ngày theo hành đường.
/// </summary>
/// <param name="LineOperatorId">Mã hành đường.</param>
/// <param name="LineOperatorCode">Mã code BIC.</param>
/// <param name="LineOperatorName">Tên hành đường.</param>
/// <param name="DateOffset">Ngày thống kê.</param>
/// <param name="GateInCount">Số lần nhập cửa trong ngày.</param>
/// <param name="GateOutCount">Số lần xuất cửa trong ngày.</param>
public sealed record DailyThroughputRow(
    Guid LineOperatorId,
    string LineOperatorCode,
    string LineOperatorName,
    DateOnly DateOffset,
    int GateInCount,
    int GateOutCount);

/// <summary>
/// Báo cáo khẩu lượng giao nhận hàng ngày.
/// </summary>
/// <param name="Rows">Danh sách hàng theo ngày và hành đường.</param>
public sealed record DailyThroughputReport(IReadOnlyList<DailyThroughputRow> Rows);

/// <summary>
/// Truy vấn lấy báo cáo khẩu lượng giao nhận trong khoảng thời gian.
/// </summary>
/// <param name="FromDate">Ngày bắt đầu (mặc định: 30 ngày trước).</param>
/// <param name="ToDate">Ngày kết thúc (mặc định: hôm nay).</param>
public sealed record GetDailyThroughputReportQuery(DateOnly? FromDate, DateOnly? ToDate)
    : IQuery<Result<DailyThroughputReport>>;
