using TechSpherex.CleanArchitecture.Api.Extensions;
using TechSpherex.CleanArchitecture.Application.Abstractions.Messaging;
using TechSpherex.CleanArchitecture.Application.Features.Containers;
using TechSpherex.CleanArchitecture.Domain.Common;
namespace TechSpherex.CleanArchitecture.Api.Endpoints;

public static class ContainerEndpoints
{
    public static void MapContainerEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/containers")
            .WithTags("Containers");

        group.MapGet("/", GetContainers)
            .WithName("GetContainers")
            .WithSummary("List containers (paginated, filterable by line operator / condition / search).");

        group.MapGet("/{number}", GetByNumber)
            .WithName("GetContainerByNumber")
            .WithSummary("Get a container by its 11-character container number.");

        group.MapPost("/", Create)
            .AddEndpointFilter<ValidationFilter<CreateContainerCommand>>()
            .RequireAuthorization()
            .WithName("CreateContainer")
            .WithSummary("Register a new container (validates ISO 6346 check digit).");
    }

    private static async Task<IResult> GetContainers(
        int? page,
        int? pageSize,
        Guid? lineOperatorId,
        string? condition,
        string? search,
        IQueryHandler<GetContainersQuery, Result<PagedResult<ContainerResponse>>> handler,
        CancellationToken cancellationToken)
    {
        var query = new GetContainersQuery(
            page ?? 1,
            pageSize ?? 20,
            lineOperatorId,
            condition,
            search);
        var result = await handler.HandleAsync(query, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> GetByNumber(
        string number,
        IQueryHandler<GetContainerByNumberQuery, Result<ContainerResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetContainerByNumberQuery(number), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> Create(
        CreateContainerCommand command,
        ICommandHandler<CreateContainerCommand, Result<ContainerResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess
            ? TypedResults.Created($"/api/containers/{result.Value!.ContainerNumber}", result.Value)
            : result.ToProblemDetails();
    }
}