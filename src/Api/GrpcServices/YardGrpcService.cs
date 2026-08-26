using Grpc.Core;
using TechSpherex.CleanArchitecture.Application.Abstractions.Messaging;
using TechSpherex.CleanArchitecture.Application.Features.Yard;
using TechSpherex.CleanArchitecture.Domain.Common;

namespace TechSpherex.CleanArchitecture.Api.GrpcServices;

/// <summary>
/// gRPC implementation of YardService. Reuses the same CQRS handler as the REST endpoint.
/// </summary>
public sealed class YardGrpcService(
    IQueryHandler<GetYardMapQuery, Result<YardMapDto>> yardMapHandler)
    : YardService.YardServiceBase
{
    public override async Task<YardMapResponse> GetYardMap(GetYardMapRequest request, ServerCallContext context)
    {
        if (!Guid.TryParse(request.DepotId, out var depotId))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid depot_id."));

        var result = await yardMapHandler.HandleAsync(new GetYardMapQuery(depotId), context.CancellationToken);
        if (result.IsFailure)
            throw new RpcException(new Status(StatusCode.NotFound, result.Error!.Message));

        var response = new YardMapResponse
        {
            DepotId = result.Value!.DepotId.ToString(),
            DepotName = result.Value.DepotName
        };

        foreach (var b in result.Value.Blocks)
        {
            var block = new BlockMap
            {
                Id = b.Id.ToString(),
                Code = b.Code,
                Name = b.Name,
                IsVirtual = b.IsVirtual,
                MaxBay = b.MaxBay ?? 0,
                MaxRow = b.MaxRow ?? 0,
                MaxTier = b.MaxTier ?? 0
            };
            foreach (var s in b.Slots)
            {
                block.Slots.Add(new SlotMap
                {
                    Id = s.Id.ToString(),
                    Bay = s.Bay,
                    Row = s.Row,
                    Tier = s.Tier,
                    IsOccupied = s.IsOccupied,
                    CurrentContainerId = s.CurrentContainerId?.ToString() ?? string.Empty
                });
            }
            response.Blocks.Add(block);
        }

        return response;
    }
}