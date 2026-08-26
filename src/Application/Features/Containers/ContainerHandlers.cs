using TechSpherex.CleanArchitecture.Application.Abstractions.Data;
using TechSpherex.CleanArchitecture.Application.Abstractions.Messaging;
using TechSpherex.CleanArchitecture.Domain.Common;
using TechSpherex.CleanArchitecture.Domain.Common.Rules;
using TechSpherex.CleanArchitecture.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace TechSpherex.CleanArchitecture.Application.Features.Containers;

public sealed class CreateContainerCommandHandler(IAppDbContext dbContext) :
    ICommandHandler<CreateContainerCommand, Result<ContainerResponse>>
{
    public async Task<Result<ContainerResponse>> HandleAsync(CreateContainerCommand command, CancellationToken cancellationToken = default)
    {
        var normalized = command.ContainerNumber.Trim().ToUpperInvariant();

        var existing = await dbContext.Containers
            .AnyAsync(c => c.ContainerNumberRaw == normalized, cancellationToken);
        if (existing)
            return Result.Failure<ContainerResponse>(Error.Conflict("Container.Duplicate",
                $"Container '{normalized}' already exists."));

        if (!Enum.TryParse<ContainerCondition>(command.Condition, true, out var condition))
            return Result.Failure<ContainerResponse>(Error.Validation("Container.InvalidCondition",
                "Invalid container condition."));

        var typeExists = await dbContext.ContainerTypes
            .AnyAsync(t => t.Id == command.ContainerTypeId, cancellationToken);
        if (!typeExists)
            return Result.Failure<ContainerResponse>(Error.NotFound("ContainerType.NotFound",
                $"Container type '{command.ContainerTypeId}' was not found."));

        Container container;
        try
        {
            container = Container.Create(
                normalized,
                command.ContainerTypeId,
                command.IsoCode.Trim(),
                command.SizeFeet,
                command.MaxWeightKg,
                command.TareWeightKg,
                command.ManufactureDate,
                command.Owner.Trim(),
                condition);
        }
        catch (BusinessRuleException ex)
        {
            return Result.Failure<ContainerResponse>(Error.Validation(ex.RuleCode, ex.Message));
        }

        dbContext.Containers.Add(container);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success(new ContainerResponse(
            container.Id,
            container.ContainerNumberRaw,
            container.ContainerTypeId,
            container.IsoCode,
            container.SizeFeet,
            container.MaxWeightKg,
            container.TareWeightKg,
            container.ManufactureDate,
            container.Owner,
            container.Condition.ToString()));
    }
}

public sealed class GetContainerByNumberQueryHandler(IAppDbContext dbContext) :
    IQueryHandler<GetContainerByNumberQuery, Result<ContainerResponse>>
{
    public async Task<Result<ContainerResponse>> HandleAsync(GetContainerByNumberQuery query, CancellationToken cancellationToken = default)
    {
        var normalized = query.ContainerNumber.Trim().ToUpperInvariant();
        var c = await dbContext.Containers
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.ContainerNumberRaw == normalized, cancellationToken);

        if (c is null)
            return Result.Failure<ContainerResponse>(Error.NotFound("Container.NotFound",
                $"Container '{normalized}' was not found."));

        return Result.Success(Map(c));
    }

    private static ContainerResponse Map(Container c) => new(
        c.Id, c.ContainerNumberRaw, c.ContainerTypeId, c.IsoCode,
        c.SizeFeet, c.MaxWeightKg, c.TareWeightKg,
        c.ManufactureDate, c.Owner, c.Condition.ToString());
}

public sealed class GetContainersQueryHandler(IAppDbContext dbContext) :
    IQueryHandler<GetContainersQuery, Result<PagedResult<ContainerResponse>>>
{
    public async Task<Result<PagedResult<ContainerResponse>>> HandleAsync(GetContainersQuery query, CancellationToken cancellationToken = default)
    {
        var q = dbContext.Containers.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Condition) && Enum.TryParse<ContainerCondition>(query.Condition, true, out var cond))
            q = q.Where(c => c.Condition == cond);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var s = query.Search.Trim().ToUpperInvariant();
            q = q.Where(c => c.ContainerNumberRaw.Contains(s) || c.Owner.Contains(s));
        }

        if (query.LineOperatorId is not null)
        {
            // Filter by line operator: latest in-yard movement for this container.
            var allowedContainerIds = dbContext.ContainerMovements
                .Where(m => m.LineOperatorId == query.LineOperatorId)
                .Select(m => m.ContainerId);
            q = q.Where(c => allowedContainerIds.Contains(c.Id));
        }

        var totalCount = await q.CountAsync(cancellationToken);

        var items = await q
            .OrderByDescending(c => c.CreatedAt)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(c => new ContainerResponse(
                c.Id, c.ContainerNumberRaw, c.ContainerTypeId, c.IsoCode,
                c.SizeFeet, c.MaxWeightKg, c.TareWeightKg,
                c.ManufactureDate, c.Owner, c.Condition.ToString()))
            .ToListAsync(cancellationToken);

        return Result.Success(new PagedResult<ContainerResponse>(items, totalCount, query.Page, query.PageSize));
    }
}