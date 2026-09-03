using TechSpherex.CleanArchitecture.Application.Abstractions.Caching;
using TechSpherex.CleanArchitecture.Application.Abstractions.Data;
using TechSpherex.CleanArchitecture.Application.Abstractions.Messaging;
using TechSpherex.CleanArchitecture.Domain.Common;
using TechSpherex.CleanArchitecture.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace TechSpherex.CleanArchitecture.Application.Features.Yard;

public sealed class CreateBlockCommandHandler(IAppDbContext dbContext, ICacheService cache) :
    ICommandHandler<CreateBlockCommand, Result<CreateBlockResponse>>
{
#pragma warning disable S3776 // Cognitive Complexity: handler methods contain necessary validation logic
    public async Task<Result<CreateBlockResponse>> HandleAsync(CreateBlockCommand command, CancellationToken cancellationToken = default)
    {
        var depotExists = await dbContext.Depots.AnyAsync(d => d.Id == command.DepotId, cancellationToken);
        if (!depotExists)
        {
            return Result.Failure<CreateBlockResponse>(Error.NotFound("Depot.NotFound",
                $"Depot '{command.DepotId}' was not found."));
        }

        var code = command.Code.Trim();
        if (await dbContext.Blocks.AnyAsync(b => b.DepotId == command.DepotId && b.Code == code, cancellationToken))
        {
            return Result.Failure<CreateBlockResponse>(Error.Conflict("Block.DuplicateCode",
                $"Block code '{code}' already exists in depot '{command.DepotId}'."));
        }

        var block = new Block
        {
            DepotId = command.DepotId,
            Code = code,
            Name = command.Name.Trim(),
            IsVirtual = command.IsVirtual,
            MaxBay = command.IsVirtual ? null : command.MaxBay,
            MaxRow = command.IsVirtual ? null : command.MaxRow,
            MaxTier = command.IsVirtual ? null : command.MaxTier,
            DisplayOrder = command.DisplayOrder
        };

        dbContext.Blocks.Add(block);
        await dbContext.SaveChangesAsync(cancellationToken);

        if (!block.IsVirtual && block.MaxBay.HasValue && block.MaxRow.HasValue && block.MaxTier.HasValue)
        {
            var slots = new List<YardSlot>();
            for (var bay = 1; bay <= block.MaxBay.Value; bay++)
            {
                for (var row = 1; row <= block.MaxRow.Value; row++)
                {
                    for (var tier = 1; tier <= block.MaxTier.Value; tier++)
                    {
                        slots.Add(new YardSlot
                        {
                            BlockId = block.Id,
                            Bay = bay,
                            Row = row,
                            Tier = tier,
                            IsOccupied = false
                        });
                    }
                }
            }
            dbContext.YardSlots.AddRange(slots);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        await cache.InvalidateByTagAsync("yard-map", cancellationToken);

        var response = new CreateBlockResponse(block.Id, block.Code, block.Name, block.IsVirtual,
            block.MaxBay, block.MaxRow, block.MaxTier);
        return Result.Success(response);
#pragma warning restore S3776 // Cognitive Complexity: handler methods contain necessary validation logic
    }
}

public sealed class CreateVirtualBlockCommandHandler(IAppDbContext dbContext, ICacheService cache) :
    ICommandHandler<CreateVirtualBlockCommand, Result<CreateBlockResponse>>
{
    public async Task<Result<CreateBlockResponse>> HandleAsync(CreateVirtualBlockCommand command, CancellationToken cancellationToken = default)
    {
        var depotExists = await dbContext.Depots.AnyAsync(d => d.Id == command.DepotId, cancellationToken);
        if (!depotExists)
        {
            return Result.Failure<CreateBlockResponse>(Error.NotFound("Depot.NotFound",
                $"Depot '{command.DepotId}' was not found."));
        }

        var code = command.Code.Trim();
        if (await dbContext.Blocks.AnyAsync(b => b.DepotId == command.DepotId && b.Code == code, cancellationToken))
        {
            return Result.Failure<CreateBlockResponse>(Error.Conflict("Block.DuplicateCode",
                $"Block code '{code}' already exists in depot '{command.DepotId}'."));
        }

        var block = new Block
        {
            DepotId = command.DepotId,
            Code = code,
            Name = command.Name.Trim(),
            IsVirtual = true,
            MaxBay = null,
            MaxRow = null,
            MaxTier = null,
            DisplayOrder = command.DisplayOrder
        };

        dbContext.Blocks.Add(block);
        await dbContext.SaveChangesAsync(cancellationToken);

        await cache.InvalidateByTagAsync("yard-map", cancellationToken);

        var response = new CreateBlockResponse(block.Id, block.Code, block.Name, block.IsVirtual,
            block.MaxBay, block.MaxRow, block.MaxTier);
        return Result.Success(response);
    }
}

public sealed class ResizeBlockCommandHandler(IAppDbContext dbContext, ICacheService cache) :
    ICommandHandler<ResizeBlockCommand, Result>
{
#pragma warning disable S3776 // Cognitive Complexity: handler methods contain necessary validation logic
    public async Task<Result> HandleAsync(ResizeBlockCommand command, CancellationToken cancellationToken = default)
    {
        var block = await dbContext.Blocks
            .FirstOrDefaultAsync(b => b.Id == command.BlockId, cancellationToken);

        if (block is null)
        {
            return Result.Failure(Error.NotFound("Block.NotFound",
                $"Block '{command.BlockId}' was not found."));
        }

        if (block.IsVirtual)
        {
            return Result.Failure(Error.Validation("Block.VirtualResizeNotSupported",
                "Virtual blocks cannot be resized — they don't have a Bay/Row/Tier grid."));
        }

        // Shrinking dimensions below already-occupied slots is rejected.
        var occupied = await dbContext.YardSlots
            .Where(s => s.BlockId == block.Id && s.IsOccupied)
            .ToListAsync(cancellationToken);

        if (command.MaxBay < (occupied.Count > 0 ? occupied.Max(s => s.Bay) : 0)
            || command.MaxRow < (occupied.Count > 0 ? occupied.Max(s => s.Row) : 0)
            || command.MaxTier < (occupied.Count > 0 ? occupied.Max(s => s.Tier) : 0))
        {
            return Result.Failure(Error.Conflict("Block.ResizeShrinksOccupied",
                "Cannot shrink the block: at least one occupied slot is outside the new dimensions."));
        }

        block.MaxBay = command.MaxBay;
        block.MaxRow = command.MaxRow;
        block.MaxTier = command.MaxTier;

        // Ensure slots exist up to the new max dimensions.
        var existingSlots = await dbContext.YardSlots
            .Where(s => s.BlockId == block.Id)
            .ToListAsync(cancellationToken);

        var existingKeys = existingSlots
            .Select(s => (s.Bay, s.Row, s.Tier))
            .ToHashSet();

        for (var bay = 1; bay <= command.MaxBay; bay++)
        {
            for (var row = 1; row <= command.MaxRow; row++)
            {
                for (var tier = 1; tier <= command.MaxTier; tier++)
                {
                    if (!existingKeys.Contains((bay, row, tier)))
                    {
                        dbContext.YardSlots.Add(new YardSlot
                        {
                            BlockId = block.Id,
                            Bay = bay,
                            Row = row,
                            Tier = tier,
                            IsOccupied = false
                        });
                    }
                }
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await cache.InvalidateByTagAsync("yard-map", cancellationToken);

        return Result.Success();
#pragma warning restore S3776 // Cognitive Complexity: handler methods contain necessary validation logic
    }
}

public sealed class GetDepotsQueryHandler(IAppDbContext dbContext) :
    IQueryHandler<GetDepotsQuery, Result<IReadOnlyList<DepotDto>>>
{
    public async Task<Result<IReadOnlyList<DepotDto>>> HandleAsync(GetDepotsQuery query, CancellationToken cancellationToken = default)
    {
        var depots = await dbContext.Depots
            .AsNoTracking()
            .OrderBy(d => d.Code)
            .Select(d => new DepotDto(d.Id, d.Code, d.Name, d.Address, d.IsActive))
            .ToListAsync(cancellationToken);

        return Result.Success<IReadOnlyList<DepotDto>>(depots);
    }
}

public sealed record DepotDto(Guid Id, string Code, string Name, string Address, bool IsActive);

public sealed class GetYardMapQueryHandler(IAppDbContext dbContext, ICacheService cache) :
    IQueryHandler<GetYardMapQuery, Result<YardMapDto>>
{
    public async Task<Result<YardMapDto>> HandleAsync(GetYardMapQuery query, CancellationToken cancellationToken = default)
    {
        var key = $"yard-map:{query.DepotId}";

        var map = await cache.GetOrCreateAsync(
            key,
            async ct => await BuildMapAsync(query.DepotId, ct),
            expiration: TimeSpan.FromMinutes(5),
            localExpiration: TimeSpan.FromMinutes(2),
            tags: ["yard-map"],
            cancellationToken: cancellationToken);

        return map is null
            ? Result.Failure<YardMapDto>(Error.NotFound("Depot.NotFound",
                $"Depot '{query.DepotId}' was not found."))
            : Result.Success(map);
    }

    private async Task<YardMapDto?> BuildMapAsync(Guid depotId, CancellationToken ct)
    {
        var depot = await dbContext.Depots
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == depotId, ct);

        if (depot is null) return null;

        var blocks = await dbContext.Blocks
            .AsNoTracking()
            .Where(b => b.DepotId == depotId)
            .OrderBy(b => b.DisplayOrder).ThenBy(b => b.Code)
            .ToListAsync(ct);

        var blockIds = blocks.Select(b => b.Id).ToHashSet();
        var slots = await dbContext.YardSlots
            .AsNoTracking()
            .Where(s => blockIds.Contains(s.BlockId))
            .ToListAsync(ct);

        var slotsByBlock = slots.ToLookup(s => s.BlockId);

        var blockDtos = new List<BlockMapDto>(blocks.Count);
        foreach (var b in blocks)
        {
            var blockSlots = b.IsVirtual
                ? []
                : slotsByBlock[b.Id]
                    .OrderBy(s => s.Bay)
                    .ThenBy(s => s.Row)
                    .ThenBy(s => s.Tier)
                    .Select(s => new YardSlotDto(s.Id, s.Bay, s.Row, s.Tier, s.IsOccupied, s.CurrentContainerId))
                    .ToList();

            blockDtos.Add(new BlockMapDto(
                b.Id,
                b.Code,
                b.Name,
                b.IsVirtual,
                b.MaxBay,
                b.MaxRow,
                b.MaxTier,
                blockSlots));
        }

        return new YardMapDto(depot.Id, depot.Name, blockDtos);
    }
}

public sealed class UpdateBlockCommandHandler(IAppDbContext dbContext, ICacheService cache) :
    ICommandHandler<UpdateBlockCommand, Result<CreateBlockResponse>>
{
    public async Task<Result<CreateBlockResponse>> HandleAsync(UpdateBlockCommand command, CancellationToken cancellationToken = default)
    {
        var block = await dbContext.Blocks
            .FirstOrDefaultAsync(b => b.Id == command.BlockId, cancellationToken);
        if (block is null)
        {
            return Result.Failure<CreateBlockResponse>(Error.NotFound("Block.NotFound",
                $"Block '{command.BlockId}' was not found."));
        }

        block.Code = command.Code.Trim().ToUpperInvariant();
        block.Name = command.Name.Trim();

        await dbContext.SaveChangesAsync(cancellationToken);
        await cache.InvalidateByTagAsync("yard-map", cancellationToken);

        return Result.Success(new CreateBlockResponse(
            block.Id,
            block.Code,
            block.Name,
            block.IsVirtual,
            block.MaxBay,
            block.MaxRow,
            block.MaxTier));
    }
}

public sealed class DeleteBlockCommandHandler(IAppDbContext dbContext, ICacheService cache) :
    ICommandHandler<DeleteBlockCommand, Result>
{
    public async Task<Result> HandleAsync(DeleteBlockCommand command, CancellationToken cancellationToken = default)
    {
        var block = await dbContext.Blocks
            .FirstOrDefaultAsync(b => b.Id == command.BlockId, cancellationToken);
        if (block is null)
        {
            return Result.Failure(Error.NotFound("Block.NotFound",
                $"Block '{command.BlockId}' was not found."));
        }

        var hasOccupiedSlots = await dbContext.YardSlots
            .AnyAsync(s => s.BlockId == block.Id && s.IsOccupied, cancellationToken);
        if (hasOccupiedSlots)
        {
            return Result.Failure(Error.Conflict("Block.OccupiedSlotsCannotDelete",
                $"Block '{block.Code}' contains occupied container slots. Vacate or gate-out all containers before deleting."));
        }

        var slots = await dbContext.YardSlots
            .Where(s => s.BlockId == block.Id)
            .ToListAsync(cancellationToken);
        dbContext.YardSlots.RemoveRange(slots);

        dbContext.Blocks.Remove(block);
        await dbContext.SaveChangesAsync(cancellationToken);
        await cache.InvalidateByTagAsync("yard-map", cancellationToken);

        return Result.Success();
    }
}