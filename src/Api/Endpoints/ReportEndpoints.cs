using TechSpherex.CleanArchitecture.Api.Extensions;
using TechSpherex.CleanArchitecture.Application.Abstractions.Messaging;
using TechSpherex.CleanArchitecture.Application.Features.Lookups;
using TechSpherex.CleanArchitecture.Application.Features.Reports;
using TechSpherex.CleanArchitecture.Domain.Common;
namespace TechSpherex.CleanArchitecture.Api.Endpoints;

public static class ReportEndpoints
{
    public static void MapReportEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/reports")
            .WithTags("Reports");

        group.MapGet("/yard-aging", YardAging)
            .WithName("GetYardAgingReport")
            .WithSummary("Containers in-yard by Line Operator (0–10 days vs ≥10 days).");

        group.MapGet("/yard-occupancy", YardAging)
            .WithName("GetYardOccupancyReport")
            .WithSummary("Yard occupancy & aging report by Line Operator.");

        group.MapGet("/daily-throughput", DailyThroughput)
            .WithName("GetDailyThroughputReport")
            .WithSummary("Daily gate-in / gate-out throughput by Line Operator.");
    }

    private static async Task<IResult> YardAging(
        IQueryHandler<GetYardAgingReportQuery, Result<YardAgingReport>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetYardAgingReportQuery(), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

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

public static class LookupEndpoints
{
    public static void MapLookupEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/lookups")
            .WithTags("Lookups");

        group.MapGet("/line-operators", GetLineOperators)
            .WithName("GetLineOperators")
            .WithSummary("List active line operators (shipping lines).");

        group.MapGet("/container-types", GetContainerTypes)
            .WithName("GetContainerTypes")
            .WithSummary("List active container types.");

        group.MapGet("/customers", GetCustomers)
            .WithName("GetCustomers")
            .WithSummary("List customers.");

        group.MapPost("/customers", CreateCustomer)
            .RequireAuthorization()
            .WithName("CreateCustomer")
            .WithSummary("Create a new customer.");
    }

    private static async Task<IResult> GetLineOperators(
        IQueryHandler<GetLineOperatorsQuery, Result<IReadOnlyList<LineOperatorResponse>>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetLineOperatorsQuery(), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> GetContainerTypes(
        IQueryHandler<GetContainerTypesQuery, Result<IReadOnlyList<ContainerTypeResponse>>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetContainerTypesQuery(), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> GetCustomers(
        IQueryHandler<GetCustomersQuery, Result<IReadOnlyList<CustomerResponse>>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetCustomersQuery(), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

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