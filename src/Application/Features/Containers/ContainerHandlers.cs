using TechSpherex.CleanArchitecture.Application.Abstractions.Data;
using TechSpherex.CleanArchitecture.Application.Abstractions.Messaging;
using TechSpherex.CleanArchitecture.Domain.Common;
using TechSpherex.CleanArchitecture.Domain.Common.Rules;
using TechSpherex.CleanArchitecture.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace TechSpherex.CleanArchitecture.Application.Features.Containers;

/// <summary>
/// Xử lý lệnh tạo container mới — kiểm tra trùng số, loại container tồn tại,
/// và xác thực chữ số kiểm tra ISO 6346 qua domain rule.
/// </summary>
public sealed class CreateContainerCommandHandler(IAppDbContext dbContext) :
    ICommandHandler<CreateContainerCommand, Result<ContainerResponse>>
{
    /// <inheritdoc/>
    public async Task<Result<ContainerResponse>> HandleAsync(CreateContainerCommand command, CancellationToken cancellationToken = default)
    {
        var normalized = command.ContainerNumber.Trim().ToUpperInvariant();

        var existing = await dbContext.Containers
            .AnyAsync(c => c.ContainerNumberRaw == normalized, cancellationToken);
        if (existing)
        {
            return Result.Failure<ContainerResponse>(Error.Conflict("Container.Duplicate",
                $"Container '{normalized}' đã tồn tại."));
        }

        if (!Enum.TryParse<ContainerCondition>(command.Condition, true, out var condition))
        {
            return Result.Failure<ContainerResponse>(Error.Validation("Container.InvalidCondition",
                "Tình trạng container không hợp lệ."));
        }

        var typeExists = await dbContext.ContainerTypes
            .AnyAsync(t => t.Id == command.ContainerTypeId, cancellationToken);
        if (!typeExists)
        {
            return Result.Failure<ContainerResponse>(Error.NotFound("ContainerType.NotFound",
                $"Loại container '{command.ContainerTypeId}' không tìm thấy."));
        }

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

/// <summary>
/// Xử lý truy vấn lấy container theo số thùng hàng.
/// </summary>
public sealed class GetContainerByNumberQueryHandler(IAppDbContext dbContext) :
    IQueryHandler<GetContainerByNumberQuery, Result<ContainerResponse>>
{
    /// <inheritdoc/>
    public async Task<Result<ContainerResponse>> HandleAsync(GetContainerByNumberQuery query, CancellationToken cancellationToken = default)
    {
        var normalized = query.ContainerNumber.Trim().ToUpperInvariant();
        var c = await dbContext.Containers
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.ContainerNumberRaw == normalized, cancellationToken);

        if (c is null)
        {
            return Result.Failure<ContainerResponse>(Error.NotFound("Container.NotFound",
                $"Container '{normalized}' không tìm thấy."));
        }

        return Result.Success(Map(c));
    }

    /// <summary>Ánh xạ từ thực thể <see cref="Container"/> sang <see cref="ContainerResponse"/>.</summary>
    private static ContainerResponse Map(Container c) => new(
        c.Id, c.ContainerNumberRaw, c.ContainerTypeId, c.IsoCode,
        c.SizeFeet, c.MaxWeightKg, c.TareWeightKg,
        c.ManufactureDate, c.Owner, c.Condition.ToString());
}

/// <summary>
/// Xử lý truy vấn lấy danh sách container có phân trang và bộ lọc.
/// </summary>
public sealed class GetContainersQueryHandler(IAppDbContext dbContext) :
    IQueryHandler<GetContainersQuery, Result<PagedResult<ContainerResponse>>>
{
    /// <inheritdoc/>
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
            // Lọc theo hành đường: container xuất hiện trong bản ghi di chuyển InYard của hành đường đó.
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

/// <summary>
/// Xử lý lệnh cập nhật thông tin container.
/// </summary>
public sealed class UpdateContainerCommandHandler(IAppDbContext dbContext) :
    ICommandHandler<UpdateContainerCommand, Result<ContainerResponse>>
{
    /// <inheritdoc/>
    public async Task<Result<ContainerResponse>> HandleAsync(UpdateContainerCommand command, CancellationToken cancellationToken = default)
    {
        var container = await dbContext.Containers
            .FirstOrDefaultAsync(c => c.Id == command.Id, cancellationToken);
        if (container is null)
        {
            return Result.Failure<ContainerResponse>(Error.NotFound("Container.NotFound",
                $"Container '{command.Id}' không tìm thấy."));
        }

        if (!Enum.TryParse<ContainerCondition>(command.Condition, true, out var condition))
        {
            return Result.Failure<ContainerResponse>(Error.Validation("Container.InvalidCondition",
                "Tình trạng container không hợp lệ."));
        }

        var typeExists = await dbContext.ContainerTypes
            .AnyAsync(t => t.Id == command.ContainerTypeId, cancellationToken);
        if (!typeExists)
        {
            return Result.Failure<ContainerResponse>(Error.NotFound("ContainerType.NotFound",
                $"Loại container '{command.ContainerTypeId}' không tìm thấy."));
        }

        container.ContainerTypeId = command.ContainerTypeId;
        container.IsoCode = command.IsoCode.Trim();
        container.SizeFeet = command.SizeFeet;
        container.MaxWeightKg = command.MaxWeightKg;
        container.TareWeightKg = command.TareWeightKg;
        container.ManufactureDate = command.ManufactureDate;
        container.Owner = command.Owner.Trim();
        container.Condition = condition;

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

/// <summary>
/// Xử lý lệnh xóa container. Kiểm tra container không đang chiếm slot trong yard trước khi xóa.
/// </summary>
public sealed class DeleteContainerCommandHandler(IAppDbContext dbContext) :
    ICommandHandler<DeleteContainerCommand, Result>
{
    /// <inheritdoc/>
    public async Task<Result> HandleAsync(DeleteContainerCommand command, CancellationToken cancellationToken = default)
    {
        var container = await dbContext.Containers
            .FirstOrDefaultAsync(c => c.Id == command.Id, cancellationToken);
        if (container is null)
        {
            return Result.Failure(Error.NotFound("Container.NotFound",
                $"Container '{command.Id}' không tìm thấy."));
        }

        var inYard = await dbContext.YardSlots
            .AnyAsync(s => s.CurrentContainerId == container.Id && s.IsOccupied, cancellationToken);
        if (inYard)
        {
            return Result.Failure(Error.Conflict("Container.InYardCannotDelete",
                $"Container '{container.ContainerNumberRaw}' đang chiếm một yard slot. Vượt cửa ra hoặc giải phóng slot trước khi xóa."));
        }

        // Xóa các bản ghi di chuyển lịch sử của container này
        var movements = await dbContext.ContainerMovements
            .Where(m => m.ContainerId == container.Id)
            .ToListAsync(cancellationToken);
        dbContext.ContainerMovements.RemoveRange(movements);

        dbContext.Containers.Remove(container);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
