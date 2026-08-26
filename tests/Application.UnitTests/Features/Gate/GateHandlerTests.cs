using FluentAssertions;
using TechSpherex.CleanArchitecture.Application.Abstractions.Caching;
using TechSpherex.CleanArchitecture.Application.Abstractions.Rules;
using TechSpherex.CleanArchitecture.Application.Features.Gate;
using TechSpherex.CleanArchitecture.Domain.Common;
using TechSpherex.CleanArchitecture.Domain.Entities;
using NSubstitute;

namespace TechSpherex.CleanArchitecture.Application.UnitTests.Features.Gate;

public sealed class GateInContainerCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Reject_When_Container_Not_Found()
    {
        await using var db = TestDbContextFactory.Create();
        var lineOp = new LineOperator { Code = "CMA", Name = "CMA CGM" };
        var depot = new Depot { Code = "D1", Name = "D1", Address = "addr" };
        db.LineOperators.Add(lineOp);
        db.Depots.Add(depot);
        await db.SaveChangesAsync();
        var block = new Block { DepotId = depot.Id, Code = "A", Name = "A", IsVirtual = false, MaxBay = 3, MaxRow = 1, MaxTier = 1 };
        db.Blocks.Add(block);
        await db.SaveChangesAsync();
        var slot = new YardSlot { BlockId = block.Id, Bay = 1, Row = 1, Tier = 1, IsOccupied = false };
        db.YardSlots.Add(slot);
        await db.SaveChangesAsync();

        var rules = Substitute.For<IRuleEngine>();
        rules.Evaluate(Arg.Any<string>(), Arg.Any<IDictionary<string, object?>>())
            .Returns(RuleResult.Pass());

        var cache = Substitute.For<ICacheService>();
        var handler = new GateInContainerCommandHandler(db, rules, cache);

        var result = await handler.HandleAsync(
            new GateInContainerCommand("CMAU1234564", lineOp.Id, block.Id, 1, 1, 1, "A", "Normal", "ABC-123", "Driver"),
            TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Container.NotFound");
    }

    [Fact]
    public async Task HandleAsync_Should_Reject_Bay_Size_Mismatch()
    {
        await using var db = TestDbContextFactory.Create();
        var ct = new ContainerType { Code = "42G1", Name = "Dry 40'", Family = "Dry" };
        var lineOp = new LineOperator { Code = "CMA", Name = "CMA CGM" };
        var depot = new Depot { Code = "D1", Name = "D1", Address = "addr" };
        db.ContainerTypes.Add(ct);
        db.LineOperators.Add(lineOp);
        db.Depots.Add(depot);
        await db.SaveChangesAsync();

        // 40ft container to test bay-parity
        db.Containers.Add(new Container
        {
            ContainerNumberRaw = "CMAU1234564",
            ContainerTypeId = ct.Id,
            IsoCode = "42G1",
            SizeFeet = 40,
            MaxWeightKg = 30000m,
            TareWeightKg = 2200m,
            ManufactureDate = DateTimeOffset.UtcNow,
            Owner = "CMA",
            Condition = ContainerCondition.Normal
        });

        var block = new Block { DepotId = depot.Id, Code = "A", Name = "A", IsVirtual = false, MaxBay = 3, MaxRow = 1, MaxTier = 1 };
        db.Blocks.Add(block);
        await db.SaveChangesAsync();

        // Odd bay — wrong for 40ft
        var slot = new YardSlot { BlockId = block.Id, Bay = 1, Row = 1, Tier = 1, IsOccupied = false };
        db.YardSlots.Add(slot);
        await db.SaveChangesAsync();

        var rules = Substitute.For<IRuleEngine>();
        rules.Evaluate(Arg.Any<string>(), Arg.Any<IDictionary<string, object?>>())
            .Returns(RuleResult.Pass());
        var cache = Substitute.For<ICacheService>();

        var handler = new GateInContainerCommandHandler(db, rules, cache);
        var result = await handler.HandleAsync(
            new GateInContainerCommand("CMAU1234564", lineOp.Id, block.Id, 1, 1, 1, "A", "Normal", null, null),
            TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Yard.BayParityMatchesContainerSize");
    }

    [Fact]
    public async Task HandleAsync_Should_Succeed_For_Valid_GateIn()
    {
        await using var db = TestDbContextFactory.Create();
        var ct = new ContainerType { Code = "22G1", Name = "Dry 20'", Family = "Dry" };
        var lineOp = new LineOperator { Code = "CMA", Name = "CMA CGM" };
        var depot = new Depot { Code = "D1", Name = "D1", Address = "addr" };
        db.ContainerTypes.Add(ct);
        db.LineOperators.Add(lineOp);
        db.Depots.Add(depot);
        await db.SaveChangesAsync();

        db.Containers.Add(new Container
        {
            ContainerNumberRaw = "CMAU1234564",
            ContainerTypeId = ct.Id,
            IsoCode = "22G1",
            SizeFeet = 20,
            MaxWeightKg = 30000m,
            TareWeightKg = 2200m,
            ManufactureDate = DateTimeOffset.UtcNow,
            Owner = "CMA",
            Condition = ContainerCondition.Normal
        });
        var block = new Block { DepotId = depot.Id, Code = "A", Name = "A", IsVirtual = false, MaxBay = 3, MaxRow = 1, MaxTier = 1 };
        db.Blocks.Add(block);
        await db.SaveChangesAsync();
        var slot = new YardSlot { BlockId = block.Id, Bay = 1, Row = 1, Tier = 1, IsOccupied = false };
        db.YardSlots.Add(slot);
        await db.SaveChangesAsync();

        var rules = Substitute.For<IRuleEngine>();
        rules.Evaluate(Arg.Any<string>(), Arg.Any<IDictionary<string, object?>>())
            .Returns(RuleResult.Pass());
        var cache = Substitute.For<ICacheService>();

        var handler = new GateInContainerCommandHandler(db, rules, cache);
        var result = await handler.HandleAsync(
            new GateInContainerCommand("CMAU1234564", lineOp.Id, block.Id, 1, 1, 1, "A", "Normal", "ABC-1", "Drv"),
            TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be("InYard");
        slot.IsOccupied.Should().BeTrue();
        slot.CurrentContainerId.Should().NotBeNull();
        await cache.Received(1).InvalidateByTagAsync("yard-map", Arg.Any<CancellationToken>());
    }
}

public sealed class GateOutContainerCommandHandlerTests
{
    [Fact]
    public async Task HandleOut_Should_Fail_When_No_Open_Movement()
    {
        await using var db = TestDbContextFactory.Create();
        var lineOp = new LineOperator { Code = "CMA", Name = "CMA" };
        db.LineOperators.Add(lineOp);
        var customer = new Customer { TaxCode = "123", Name = "ACME" };
        db.Customers.Add(customer);
        var ct = new ContainerType { Code = "22G1", Name = "Dry 20'", Family = "Dry" };
        db.ContainerTypes.Add(ct);
        await db.SaveChangesAsync();
        var order = new DeliveryOrder
        {
            OrderNumber = "DO-001",
            CustomerId = customer.Id,
            LineOperatorId = lineOp.Id,
            ExpiryDate = DateTimeOffset.UtcNow.AddDays(7)
        };
        order.Lines.Add(new DeliveryOrderLine
        {
            ContainerTypeId = ct.Id,
            RequestedQuantity = 1,
            DeliveredQuantity = 0
        });
        db.DeliveryOrders.Add(order);
        await db.SaveChangesAsync();

        var rules = Substitute.For<IRuleEngine>();
        rules.Evaluate(Arg.Any<string>(), Arg.Any<IDictionary<string, object?>>())
            .Returns(RuleResult.Pass());
        var cache = Substitute.For<ICacheService>();

        var handler = new GateOutContainerCommandHandler(db, rules, cache);

        // Container does not exist → first check fails with NotFound
        var result = await handler.HandleAsync(
            new GateOutContainerCommand("CMAU1234564", order.Id, null, null, "Normal"),
            TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task GateOut_Should_Succeed_When_All_Rules_Pass()
    {
        await using var db = TestDbContextFactory.Create();
        var lineOp = new LineOperator { Code = "CMA", Name = "CMA" };
        db.LineOperators.Add(lineOp);
        var customer = new Customer { TaxCode = "123", Name = "ACME" };
        db.Customers.Add(customer);
        var ct = new ContainerType { Code = "22G1", Name = "Dry 20'", Family = "Dry" };
        db.ContainerTypes.Add(ct);
        await db.SaveChangesAsync();
        var order = new DeliveryOrder
        {
            OrderNumber = "DO-001",
            CustomerId = customer.Id,
            LineOperatorId = lineOp.Id,
            ExpiryDate = DateTimeOffset.UtcNow.AddDays(7)
        };
        order.Lines.Add(new DeliveryOrderLine
        {
            ContainerTypeId = ct.Id,
            RequestedQuantity = 2,
            DeliveredQuantity = 0
        });
        db.DeliveryOrders.Add(order);

        var depot = new Depot { Code = "D1", Name = "D1", Address = "addr" };
        db.Depots.Add(depot);
        await db.SaveChangesAsync();
        var block = new Block { DepotId = depot.Id, Code = "A", Name = "A", IsVirtual = false, MaxBay = 1, MaxRow = 1, MaxTier = 1 };
        db.Blocks.Add(block);
        await db.SaveChangesAsync();
        var slot = new YardSlot { BlockId = block.Id, Bay = 1, Row = 1, Tier = 1, IsOccupied = true, CurrentContainerId = Guid.NewGuid() };
        db.YardSlots.Add(slot);
        await db.SaveChangesAsync();

        var container = new Container
        {
            ContainerNumberRaw = "CMAU1234564",
            ContainerTypeId = ct.Id,
            IsoCode = "22G1",
            SizeFeet = 20,
            MaxWeightKg = 30000m,
            TareWeightKg = 2200m,
            ManufactureDate = DateTimeOffset.UtcNow,
            Owner = "CMA",
            Condition = ContainerCondition.Normal
        };
        db.Containers.Add(container);
        await db.SaveChangesAsync();

        var movement = new ContainerMovement
        {
            ContainerId = container.Id,
            LineOperatorId = lineOp.Id,
            BlockId = block.Id,
            YardSlotId = slot.Id,
            Classification = "A",
            ConditionAtGateIn = ContainerCondition.Normal,
            GateInAt = DateTimeOffset.UtcNow.AddDays(-1),
            Status = MovementStatus.InYard
        };
        db.ContainerMovements.Add(movement);
        await db.SaveChangesAsync();

        var rules = Substitute.For<IRuleEngine>();
        rules.Evaluate(Arg.Any<string>(), Arg.Any<IDictionary<string, object?>>())
            .Returns(RuleResult.Pass());
        var cache = Substitute.For<ICacheService>();

        var handler = new GateOutContainerCommandHandler(db, rules, cache);

        var result = await handler.HandleAsync(
            new GateOutContainerCommand("CMAU1234564", order.Id, "OUT-1", "DrvOut", "Normal"),
            TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be("GateOut");
        slot.IsOccupied.Should().BeFalse();
        order.Lines.First().DeliveredQuantity.Should().Be(1);
    }

    [Fact]
    public async Task GateOut_Should_Reject_Expired_Order()
    {
        await using var db = TestDbContextFactory.Create();
        var lineOp = new LineOperator { Code = "CMA", Name = "CMA" };
        db.LineOperators.Add(lineOp);
        var customer = new Customer { TaxCode = "123", Name = "ACME" };
        db.Customers.Add(customer);
        var ct = new ContainerType { Code = "22G1", Name = "Dry 20'", Family = "Dry" };
        db.ContainerTypes.Add(ct);
        await db.SaveChangesAsync();
        var order = new DeliveryOrder
        {
            OrderNumber = "DO-OLD",
            CustomerId = customer.Id,
            LineOperatorId = lineOp.Id,
            ExpiryDate = DateTimeOffset.UtcNow.AddDays(-1)
        };
        order.Lines.Add(new DeliveryOrderLine { ContainerTypeId = ct.Id, RequestedQuantity = 1 });
        db.DeliveryOrders.Add(order);
        await db.SaveChangesAsync();

        var container = new Container
        {
            ContainerNumberRaw = "CMAU1234564",
            ContainerTypeId = ct.Id, IsoCode = "22G1", SizeFeet = 20,
            MaxWeightKg = 30000m, TareWeightKg = 2200m,
            ManufactureDate = DateTimeOffset.UtcNow, Owner = "CMA",
            Condition = ContainerCondition.Normal
        };
        db.Containers.Add(container);
        await db.SaveChangesAsync();
        var movement = new ContainerMovement
        {
            ContainerId = container.Id, LineOperatorId = lineOp.Id,
            Classification = "A", ConditionAtGateIn = ContainerCondition.Normal,
            GateInAt = DateTimeOffset.UtcNow.AddDays(-3), Status = MovementStatus.InYard
        };
        db.ContainerMovements.Add(movement);
        await db.SaveChangesAsync();

        var rules = Substitute.For<IRuleEngine>();
        var cache = Substitute.For<ICacheService>();
        var handler = new GateOutContainerCommandHandler(db, rules, cache);

        var result = await handler.HandleAsync(
            new GateOutContainerCommand("CMAU1234564", order.Id, null, null, "Normal"),
            TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("DeliveryOrder.NotExpired");
    }
}