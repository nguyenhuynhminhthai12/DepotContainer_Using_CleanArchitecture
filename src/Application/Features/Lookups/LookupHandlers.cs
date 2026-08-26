using TechSpherex.CleanArchitecture.Application.Abstractions.Caching;
using TechSpherex.CleanArchitecture.Application.Abstractions.Data;
using TechSpherex.CleanArchitecture.Application.Abstractions.Messaging;
using TechSpherex.CleanArchitecture.Domain.Common;
using TechSpherex.CleanArchitecture.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace TechSpherex.CleanArchitecture.Application.Features.Lookups;

public sealed record LineOperatorResponse(Guid Id, string Code, string Name, string? Country);

public sealed record GetLineOperatorsQuery() : IQuery<Result<IReadOnlyList<LineOperatorResponse>>>;

public sealed record ContainerTypeResponse(Guid Id, string Code, string Name, string Family);

public sealed record GetContainerTypesQuery() : IQuery<Result<IReadOnlyList<ContainerTypeResponse>>>;

public sealed record CustomerResponse(Guid Id, string TaxCode, string Name, string? Address, string? Phone, bool IsActive);

public sealed record GetCustomersQuery() : IQuery<Result<IReadOnlyList<CustomerResponse>>>;

public sealed record CreateCustomerCommand(string TaxCode, string Name, string? Address, string? Phone, string? Email)
    : ICommand<Result<CustomerResponse>>;

public sealed class GetLineOperatorsQueryHandler(IAppDbContext dbContext, ICacheService cache) :
    IQueryHandler<GetLineOperatorsQuery, Result<IReadOnlyList<LineOperatorResponse>>>
{
    public async Task<Result<IReadOnlyList<LineOperatorResponse>>> HandleAsync(GetLineOperatorsQuery query, CancellationToken cancellationToken = default)
    {
        var list = await cache.GetOrCreateAsync(
            "line-operators",
            async ct => await dbContext.LineOperators.AsNoTracking()
                .Where(l => l.IsActive)
                .OrderBy(l => l.Code)
                .Select(l => new LineOperatorResponse(l.Id, l.Code, l.Name, l.Country))
                .ToListAsync(ct),
            expiration: TimeSpan.FromMinutes(10),
            localExpiration: TimeSpan.FromMinutes(5),
            tags: ["line-operators"],
            cancellationToken: cancellationToken);
        return Result.Success<IReadOnlyList<LineOperatorResponse>>(list);
    }
}

public sealed class GetContainerTypesQueryHandler(IAppDbContext dbContext, ICacheService cache) :
    IQueryHandler<GetContainerTypesQuery, Result<IReadOnlyList<ContainerTypeResponse>>>
{
    public async Task<Result<IReadOnlyList<ContainerTypeResponse>>> HandleAsync(GetContainerTypesQuery query, CancellationToken cancellationToken = default)
    {
        var list = await cache.GetOrCreateAsync(
            "container-types",
            async ct => await dbContext.ContainerTypes.AsNoTracking()
                .Where(t => t.IsActive)
                .OrderBy(t => t.Family).ThenBy(t => t.Code)
                .Select(t => new ContainerTypeResponse(t.Id, t.Code, t.Name, t.Family))
                .ToListAsync(ct),
            expiration: TimeSpan.FromMinutes(10),
            localExpiration: TimeSpan.FromMinutes(5),
            tags: ["container-types"],
            cancellationToken: cancellationToken);
        return Result.Success<IReadOnlyList<ContainerTypeResponse>>(list);
    }
}

public sealed class GetCustomersQueryHandler(IAppDbContext dbContext) :
    IQueryHandler<GetCustomersQuery, Result<IReadOnlyList<CustomerResponse>>>
{
    public async Task<Result<IReadOnlyList<CustomerResponse>>> HandleAsync(GetCustomersQuery query, CancellationToken cancellationToken = default)
    {
        var list = await dbContext.Customers.AsNoTracking()
            .OrderBy(c => c.TaxCode)
            .Select(c => new CustomerResponse(c.Id, c.TaxCode, c.Name, c.Address, c.Phone, c.IsActive))
            .ToListAsync(cancellationToken);
        return Result.Success<IReadOnlyList<CustomerResponse>>(list);
    }
}

public sealed class CreateCustomerCommandHandler(IAppDbContext dbContext) :
    ICommandHandler<CreateCustomerCommand, Result<CustomerResponse>>
{
    public async Task<Result<CustomerResponse>> HandleAsync(CreateCustomerCommand command, CancellationToken cancellationToken = default)
    {
        if (await dbContext.Customers.AnyAsync(c => c.TaxCode == command.TaxCode, cancellationToken))
            return Result.Failure<CustomerResponse>(Error.Conflict("Customer.DuplicateTaxCode",
                $"A customer with tax code '{command.TaxCode}' already exists."));

        var c = new Customer
        {
            TaxCode = command.TaxCode.Trim(),
            Name = command.Name.Trim(),
            Address = command.Address,
            Phone = command.Phone,
            Email = command.Email
        };
        dbContext.Customers.Add(c);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success(new CustomerResponse(c.Id, c.TaxCode, c.Name, c.Address, c.Phone, c.IsActive));
    }
}