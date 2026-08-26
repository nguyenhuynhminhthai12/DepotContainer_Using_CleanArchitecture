using TechSpherex.CleanArchitecture.Application.Abstractions.Messaging;
using TechSpherex.CleanArchitecture.Domain.Common;

namespace TechSpherex.CleanArchitecture.Application.Features.Yard;

public sealed record CreateBlockCommand(
    Guid DepotId,
    string Code,
    string Name,
    bool IsVirtual,
    int? MaxBay,
    int? MaxRow,
    int? MaxTier,
    int DisplayOrder = 0) : ICommand<Result<CreateBlockResponse>>;

public sealed record CreateBlockResponse(
    Guid Id,
    string Code,
    string Name,
    bool IsVirtual,
    int? MaxBay,
    int? MaxRow,
    int? MaxTier);

public sealed record CreateVirtualBlockCommand(
    Guid DepotId,
    string Code,
    string Name,
    int DisplayOrder = 0) : ICommand<Result<CreateBlockResponse>>;

public sealed record ResizeBlockCommand(
    Guid BlockId,
    int MaxBay,
    int MaxRow,
    int MaxTier) : ICommand<Result>;

public sealed record YardSlotDto(
    Guid Id,
    int Bay,
    int Row,
    int Tier,
    bool IsOccupied,
    Guid? CurrentContainerId);

public sealed record BlockMapDto(
    Guid Id,
    string Code,
    string Name,
    bool IsVirtual,
    int? MaxBay,
    int? MaxRow,
    int? MaxTier,
    IReadOnlyList<YardSlotDto> Slots);

public sealed record YardMapDto(Guid DepotId, string DepotName, IReadOnlyList<BlockMapDto> Blocks);

public sealed record GetYardMapQuery(Guid DepotId) : IQuery<Result<YardMapDto>>;

public sealed record GetDepotsQuery() : IQuery<Result<IReadOnlyList<DepotDto>>>;