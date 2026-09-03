using TechSpherex.CleanArchitecture.Api.Extensions;
using TechSpherex.CleanArchitecture.Application.Abstractions.Messaging;
using TechSpherex.CleanArchitecture.Application.Features.Yard;
using TechSpherex.CleanArchitecture.Domain.Common;
namespace TechSpherex.CleanArchitecture.Api.Endpoints;

public static class YardEndpoints
{
    public static void MapYardEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/yard")
            .WithTags("Yard");

        group.MapGet("/depots", GetDepots)
            .WithName("GetDepots")
            .WithSummary("List all depots");

        group.MapGet("/depots/{depotId:guid}/map", GetYardMap)
            .WithName("GetYardMap")
            .WithSummary("Get the live yard map (Block → Bay → Row → Tier grid + occupancy).");

        var blocks = app.MapGroup("/api/blocks")
            .WithTags("Yard")
            .RequireAuthorization();

        blocks.MapPost("/", CreateBlock)
            .AddEndpointFilter<ValidationFilter<CreateBlockCommand>>()
            .WithName("CreateBlock")
            .WithSummary("Create a non-virtual Block with a Bay/Row/Tier grid.");

        blocks.MapPost("/virtual", CreateVirtualBlock)
            .AddEndpointFilter<ValidationFilter<CreateVirtualBlockCommand>>()
            .WithName("CreateVirtualBlock")
            .WithSummary("Create a virtual Block (no Bay/Row/Tier grid).");

        blocks.MapPatch("/{id:guid}/resize", ResizeBlock)
            .AddEndpointFilter<ValidationFilter<ResizeBlockCommand>>()
            .WithName("ResizeBlock")
            .WithSummary("Resize a Block's MaxBay/MaxRow/MaxTier and auto-create missing slots.");

        blocks.MapPut("/{id:guid}", UpdateBlock)
            .WithName("UpdateBlock")
            .WithSummary("Update a Block's code and name.");

        blocks.MapDelete("/{id:guid}", DeleteBlock)
            .WithName("DeleteBlock")
            .WithSummary("Delete a Block if all its slots are unoccupied.");
    }

    private static async Task<IResult> GetDepots(
        IQueryHandler<GetDepotsQuery, Result<IReadOnlyList<DepotDto>>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetDepotsQuery(), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> GetYardMap(
        Guid depotId,
        IQueryHandler<GetYardMapQuery, Result<YardMapDto>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetYardMapQuery(depotId), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> CreateBlock(
        CreateBlockCommand command,
        ICommandHandler<CreateBlockCommand, Result<CreateBlockResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess
            ? TypedResults.Created($"/api/blocks/{result.Value!.Id}", result.Value)
            : result.ToProblemDetails();
    }

    private static async Task<IResult> CreateVirtualBlock(
        CreateVirtualBlockCommand command,
        ICommandHandler<CreateVirtualBlockCommand, Result<CreateBlockResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess
            ? TypedResults.Created($"/api/blocks/{result.Value!.Id}", result.Value)
            : result.ToProblemDetails();
    }

    private static async Task<IResult> ResizeBlock(
        Guid id,
        ResizeBlockRequest request,
        ICommandHandler<ResizeBlockCommand, Result> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new ResizeBlockCommand(id, request.MaxBay, request.MaxRow, request.MaxTier), cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }

    private static async Task<IResult> UpdateBlock(
        Guid id,
        UpdateBlockRequest request,
        ICommandHandler<UpdateBlockCommand, Result<CreateBlockResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new UpdateBlockCommand(id, request.Code, request.Name), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> DeleteBlock(
        Guid id,
        ICommandHandler<DeleteBlockCommand, Result> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new DeleteBlockCommand(id), cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblemDetails();
    }
}

public sealed record ResizeBlockRequest(int MaxBay, int MaxRow, int MaxTier);
public sealed record UpdateBlockRequest(string Code, string Name);