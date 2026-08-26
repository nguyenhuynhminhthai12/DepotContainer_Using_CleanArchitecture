using FluentAssertions;
using TechSpherex.CleanArchitecture.Application.Abstractions.Caching;
using TechSpherex.CleanArchitecture.Application.Features.Lookups;
using TechSpherex.CleanArchitecture.Application.Features.DeliveryOrders;
using TechSpherex.CleanArchitecture.Application.Features.Gate;
using TechSpherex.CleanArchitecture.Application.Features.Containers;
using TechSpherex.CleanArchitecture.Domain.Entities;
using NSubstitute;

namespace TechSpherex.CleanArchitecture.Application.UnitTests.Features.Misc;

public sealed class LookupHandlerTests
{
    [Fact]
    public async Task GetLineOperators_Should_Return_Active_Operators()
    {
        await using var db = TestDbContextFactory.Create();
        db.LineOperators.Add(new LineOperator { Code = "CMA", Name = "CMA CGM" });
        db.LineOperators.Add(new LineOperator { Code = "MSK", Name = "Maersk" });
        await db.SaveChangesAsync();

        // Use a no-op cache so the real DB query runs.
        var passThroughCache = new FakeCacheService();
        var handler = new GetLineOperatorsQueryHandler(db, passThroughCache);

        var result = await handler.HandleAsync(new GetLineOperatorsQuery(), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Count.Should().Be(2);
    }

    [Fact]
    public async Task GetContainerTypes_Should_Return_Active_Types()
    {
        await using var db = TestDbContextFactory.Create();
        db.ContainerTypes.Add(new ContainerType { Code = "42G1", Name = "Dry 40'", Family = "Dry" });
        db.ContainerTypes.Add(new ContainerType { Code = "22G1", Name = "Dry 20'", Family = "Dry" });
        await db.SaveChangesAsync();

        var passThroughCache = new FakeCacheService();
        var handler = new GetContainerTypesQueryHandler(db, passThroughCache);

        var result = await handler.HandleAsync(new GetContainerTypesQuery(), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Count.Should().Be(2);
    }

    [Fact]
    public async Task GetCustomers_Should_Return_All()
    {
        await using var db = TestDbContextFactory.Create();
        db.Customers.Add(new Customer { TaxCode = "1", Name = "ACME" });
        db.Customers.Add(new Customer { TaxCode = "2", Name = "ZZ", IsActive = false });
        await db.SaveChangesAsync();

        var handler = new GetCustomersQueryHandler(db);
        var result = await handler.HandleAsync(new GetCustomersQuery(), TestContext.Current.CancellationToken);
        result.IsSuccess.Should().BeTrue();
        result.Value!.Count.Should().Be(2);
    }

    [Fact]
    public async Task CreateCustomer_Should_Fail_On_Duplicate_TaxCode()
    {
        await using var db = TestDbContextFactory.Create();
        db.Customers.Add(new Customer { TaxCode = "123", Name = "Existing" });
        await db.SaveChangesAsync();

        var handler = new CreateCustomerCommandHandler(db);
        var result = await handler.HandleAsync(
            new CreateCustomerCommand("123", "Dup", null, null, null),
            TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Customer.DuplicateTaxCode");
    }

    [Fact]
    public async Task CreateCustomer_Should_Succeed()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new CreateCustomerCommandHandler(db);
        var result = await handler.HandleAsync(
            new CreateCustomerCommand("999", "New Co", "addr", "0909", "a@b.c"),
            TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.TaxCode.Should().Be("999");
    }
}

public sealed class DeliveryOrderExtraHandlerTests
{
    [Fact]
    public async Task GetById_Should_Return_NotFound_For_Missing()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new GetDeliveryOrderByIdQueryHandler(db);
        var result = await handler.HandleAsync(new GetDeliveryOrderByIdQuery(Guid.NewGuid()), TestContext.Current.CancellationToken);
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Close_Should_Return_NotFound_For_Missing()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new CloseDeliveryOrderCommandHandler(db);
        var result = await handler.HandleAsync(new CloseDeliveryOrderCommand(Guid.NewGuid()), TestContext.Current.CancellationToken);
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Close_Should_Set_IsClosed()
    {
        await using var db = TestDbContextFactory.Create();
        var lineOp = new LineOperator { Code = "CMA", Name = "CMA" };
        var customer = new Customer { TaxCode = "1", Name = "ACME" };
        db.LineOperators.Add(lineOp);
        db.Customers.Add(customer);
        await db.SaveChangesAsync();
        var order = new DeliveryOrder
        {
            OrderNumber = "DO-CLOSE",
            CustomerId = customer.Id,
            LineOperatorId = lineOp.Id,
            ExpiryDate = DateTimeOffset.UtcNow.AddDays(7)
        };
        db.DeliveryOrders.Add(order);
        await db.SaveChangesAsync();

        var handler = new CloseDeliveryOrderCommandHandler(db);
        var result = await handler.HandleAsync(new CloseDeliveryOrderCommand(order.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        order.IsClosed.Should().BeTrue();
    }
}

public sealed class MoveContainerInYardTests
{
    [Fact]
    public async Task Move_Should_Fail_When_Container_Not_Found()
    {
        await using var db = TestDbContextFactory.Create();
        var cache = new FakeCacheService();
        var handler = new MoveContainerInYardCommandHandler(db, cache);

        var result = await handler.HandleAsync(
            new MoveContainerInYardCommand("CMAU1234564", Guid.NewGuid(), 1, 1, 1),
            TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Move_Should_Fail_When_Virtual_Block()
    {
        await using var db = TestDbContextFactory.Create();
        var ct = new ContainerType { Code = "22G1", Name = "Dry 20'", Family = "Dry" };
        var lineOp = new LineOperator { Code = "CMA", Name = "CMA" };
        var depot = new Depot { Code = "D1", Name = "D1", Address = "addr" };
        db.ContainerTypes.Add(ct);
        db.LineOperators.Add(lineOp);
        db.Depots.Add(depot);
        await db.SaveChangesAsync();
        var c = new Container
        {
            ContainerNumberRaw = "CMAU1234564",
            ContainerTypeId = ct.Id, IsoCode = "22G1", SizeFeet = 20,
            MaxWeightKg = 30000m, TareWeightKg = 2200m,
            ManufactureDate = DateTimeOffset.UtcNow, Owner = "CMA",
            Condition = ContainerCondition.Normal
        };
        db.Containers.Add(c);
        var vBlock = new Block { DepotId = depot.Id, Code = "V", Name = "V", IsVirtual = true };
        db.Blocks.Add(vBlock);
        await db.SaveChangesAsync();
        // Put the container in the yard first (InYard movement)
        db.ContainerMovements.Add(new ContainerMovement
        {
            ContainerId = c.Id,
            LineOperatorId = lineOp.Id,
            Classification = "A",
            ConditionAtGateIn = ContainerCondition.Normal,
            GateInAt = DateTimeOffset.UtcNow,
            Status = MovementStatus.InYard
        });
        await db.SaveChangesAsync();

        var cache = new FakeCacheService();
        var handler = new MoveContainerInYardCommandHandler(db, cache);

        var result = await handler.HandleAsync(
            new MoveContainerInYardCommand("CMAU1234564", vBlock.Id, 1, 1, 1),
            TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Block.Virtual");
    }
}

public sealed class ContainerMovementHistoryTests
{
    [Fact]
    public async Task GetHistory_Should_Return_NotFound_For_Missing_Container()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new GetContainerMovementHistoryQueryHandler(db);
        var result = await handler.HandleAsync(new GetContainerMovementHistoryQuery("CMAU1234564"), TestContext.Current.CancellationToken);
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Container.NotFound");
    }

    [Fact]
    public async Task GetHistory_Should_Return_History()
    {
        await using var db = TestDbContextFactory.Create();
        var ct = new ContainerType { Code = "22G1", Name = "Dry 20'", Family = "Dry" };
        var lineOp = new LineOperator { Code = "CMA", Name = "CMA" };
        db.ContainerTypes.Add(ct);
        db.LineOperators.Add(lineOp);
        await db.SaveChangesAsync();
        var c = new Container
        {
            ContainerNumberRaw = "CMAU1234564",
            ContainerTypeId = ct.Id, IsoCode = "22G1", SizeFeet = 20,
            MaxWeightKg = 30000m, TareWeightKg = 2200m,
            ManufactureDate = DateTimeOffset.UtcNow, Owner = "CMA",
            Condition = ContainerCondition.Normal
        };
        db.Containers.Add(c);
        await db.SaveChangesAsync();
        db.ContainerMovements.Add(new ContainerMovement
        {
            ContainerId = c.Id,
            LineOperatorId = lineOp.Id,
            Classification = "A",
            ConditionAtGateIn = ContainerCondition.Normal,
            GateInAt = DateTimeOffset.UtcNow.AddDays(-2),
            Status = MovementStatus.InYard
        });
        await db.SaveChangesAsync();

        var handler = new GetContainerMovementHistoryQueryHandler(db);
        var result = await handler.HandleAsync(new GetContainerMovementHistoryQuery("CMAU1234564"), TestContext.Current.CancellationToken);
        result.IsSuccess.Should().BeTrue();
        result.Value.Count.Should().Be(1);
    }
}

public sealed class GetContainersByNumberTests
{
    [Fact]
    public async Task Should_Return_NotFound_When_Missing()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new GetContainerByNumberQueryHandler(db);
        var result = await handler.HandleAsync(new GetContainerByNumberQuery("CMAU1234564"), TestContext.Current.CancellationToken);
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Container.NotFound");
    }

    [Fact]
    public async Task Should_Return_Container_By_Number()
    {
        await using var db = TestDbContextFactory.Create();
        var ct = new ContainerType { Code = "22G1", Name = "Dry 20'", Family = "Dry" };
        db.ContainerTypes.Add(ct);
        await db.SaveChangesAsync();
        db.Containers.Add(new Container
        {
            ContainerNumberRaw = "CMAU1234564",
            ContainerTypeId = ct.Id, IsoCode = "22G1", SizeFeet = 20,
            MaxWeightKg = 30000m, TareWeightKg = 2200m,
            ManufactureDate = DateTimeOffset.UtcNow, Owner = "CMA",
            Condition = ContainerCondition.Normal
        });
        await db.SaveChangesAsync();

        var handler = new GetContainerByNumberQueryHandler(db);
        var result = await handler.HandleAsync(new GetContainerByNumberQuery("CMAU1234564"), TestContext.Current.CancellationToken);
        result.IsSuccess.Should().BeTrue();
        result.Value!.ContainerNumber.Should().Be("CMAU1234564");
    }
}

/// <summary>
/// A pass-through ICacheService that always calls the factory directly
/// (no caching), suitable for unit tests where we don't want to mock the cache.
/// </summary>
internal sealed class FakeCacheService : TechSpherex.CleanArchitecture.Application.Abstractions.Caching.ICacheService
{
    public Task<T> GetOrCreateAsync<T>(string key, Func<CancellationToken, Task<T>> factory,
        TimeSpan? expiration = null, TimeSpan? localExpiration = null,
        IEnumerable<string>? tags = null, CancellationToken cancellationToken = default)
        => factory(cancellationToken);

    public Task SetAsync<T>(string key, T value, TimeSpan? expiration = null,
        TimeSpan? localExpiration = null, IEnumerable<string>? tags = null,
        CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task InvalidateByTagAsync(string tag, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}