using TechSpherex.CleanArchitecture.Api.Extensions;
using TechSpherex.CleanArchitecture.Application.Abstractions.Messaging;
using TechSpherex.CleanArchitecture.Application.Features.Lookups;
using TechSpherex.CleanArchitecture.Application.Features.Reports;
using TechSpherex.CleanArchitecture.Domain.Common;
namespace TechSpherex.CleanArchitecture.Api.Endpoints;

/// <summary>
/// Nhóm các endpoint REST cho chức năng Báo cáo (thời gian lưu, khẩu lượng).
/// </summary>
public static class ReportEndpoints
{
    /// <summary>
    /// Đăng ký tất cả endpoint Báo cáo vào <see cref="IEndpointRouteBuilder"/>.
    /// </summary>
    /// <param name="app">Route builder để đăng ký endpoint.</param>
    public static void MapReportEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/reports")
            .WithTags("Reports");

        group.MapGet("/yard-aging", YardAging)
            .WithName("GetYardAgingReport")
            .WithSummary("Container trong yard theo hành đường (0–10 ngày vs ≥10 ngày).");

        group.MapGet("/yard-occupancy", YardAging)
            .WithName("GetYardOccupancyReport")
            .WithSummary("Báo cáo lưu kho và thời gian lưu theo hành đường.");

        group.MapGet("/daily-throughput", DailyThroughput)
            .WithName("GetDailyThroughputReport")
            .WithSummary("Khẩu lượng Gate-In/Gate-Out hàng ngày theo hành đường.");
    }

    /// <summary>Xử lý GET /api/reports/yard-aging — báo cáo thời gian lưu container.</summary>
    private static async Task<IResult> YardAging(
        IQueryHandler<GetYardAgingReportQuery, Result<YardAgingReport>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetYardAgingReportQuery(), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>Xử lý GET /api/reports/daily-throughput — báo cáo khẩu lượng hàng ngày.</summary>
    private static async Task<IResult> DailyThroughput(
        DateOnly? from,
        DateOnly? to,
        IQueryHandler<GetDailyThroughputReportQuery, Result<DailyThroughputReport>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetDailyThroughputReportQuery(from, to), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }
}

/// <summary>
/// Nhóm các endpoint REST cho chức năng Tra cứu (hành đường, loại container, khách hàng).
/// </summary>
public static class LookupEndpoints
{
    /// <summary>
    /// Đăng ký tất cả endpoint Tra cứu vào <see cref="IEndpointRouteBuilder"/>.
    /// </summary>
    /// <param name="app">Route builder để đăng ký endpoint.</param>
    public static void MapLookupEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/lookups")
            .WithTags("Lookups");

        group.MapGet("/line-operators", GetLineOperators)
            .WithName("GetLineOperators")
            .WithSummary("Liệt kê các hành đường đang hoạt động.");

        group.MapGet("/container-types", GetContainerTypes)
            .WithName("GetContainerTypes")
            .WithSummary("Liệt kê các loại container đang hoạt động.");

        group.MapGet("/customers", GetCustomers)
            .WithName("GetCustomers")
            .WithSummary("Liệt kê khách hàng.");

        group.MapPost("/customers", CreateCustomer)
            .RequireAuthorization()
            .WithName("CreateCustomer")
            .WithSummary("Tạo khách hàng mới.");
    }

    /// <summary>Xử lý GET /api/lookups/line-operators — lấy danh sách hành đường.</summary>
    private static async Task<IResult> GetLineOperators(
        IQueryHandler<GetLineOperatorsQuery, Result<IReadOnlyList<LineOperatorResponse>>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetLineOperatorsQuery(), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>Xử lý GET /api/lookups/container-types — lấy danh sách loại container.</summary>
    private static async Task<IResult> GetContainerTypes(
        IQueryHandler<GetContainerTypesQuery, Result<IReadOnlyList<ContainerTypeResponse>>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetContainerTypesQuery(), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>Xử lý GET /api/lookups/customers — lấy danh sách khách hàng.</summary>
    private static async Task<IResult> GetCustomers(
        IQueryHandler<GetCustomersQuery, Result<IReadOnlyList<CustomerResponse>>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetCustomersQuery(), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>Xử lý POST /api/lookups/customers — tạo khách hàng mới.</summary>
    private static async Task<IResult> CreateCustomer(
        CreateCustomerCommand command,
        ICommandHandler<CreateCustomerCommand, Result<CustomerResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess
            ? TypedResults.Created($"/api/lookups/customers/{result.Value!.Id}", result.Value)
            : result.ToProblemDetails();
    }
}
