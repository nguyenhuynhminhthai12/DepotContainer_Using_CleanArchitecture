using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using TechSpherex.CleanArchitecture.Application.Abstractions.Caching;
using TechSpherex.CleanArchitecture.Application.Abstractions.Rules;
using TechSpherex.CleanArchitecture.Application.Features.Gate;
using TechSpherex.CleanArchitecture.Domain.Common;
using TechSpherex.CleanArchitecture.Domain.Entities;

namespace TechSpherex.CleanArchitecture.Application.UnitTests.Features.Gate;

public sealed class GateInContainerCommandHandlerTests
{
    private static ILogger<GateInContainerCommandHandler> CreateLogger() =>
        Substitute.For<ILogger<GateInContainerCommandHandler>>();

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
        var logger = CreateLogger();
        var handler = new GateInContainerCommandHandler(db, rules, cache, logger);

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

        var slot = new YardSlot { BlockId = block.Id, Bay = 1, Row = 1, Tier = 1, IsOccupied = false };
        db.YardSlots.Add(slot);
        await db.SaveChangesAsync();

        var rules = Substitute.For<IRuleEngine>();
        rules.Evaluate(Arg.Any<string>(), Arg.Any<IDictionary<string, object?>>())
            .Returns(RuleResult.Pass());
        var cache = Substitute.For<ICacheService>();
        var logger = CreateLogger();

        var handler = new GateInContainerCommandHandler(db, rules, cache, logger);
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
        var logger = CreateLogger();

        var handler = new GateInContainerCommandHandler(db, rules, cache, logger);
        var result = await handler.HandleAsync(
            new GateInContainerCommand("CMAU1234564", lineOp.Id, block.Id, 1, 1, 1, "A", "Normal", "ABC-1", "Drv"),
            TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be("InYard");
        slot.IsOccupied.Should().BeTrue();
        slot.CurrentContainerId.Should().NotBeNull();
        await cache.Received(1).InvalidateByTagAsync("yard-map", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_Should_Reject_When_Already_In_Yard()
    {
        await using var db = TestDbContextFactory.Create();
        var ct = new ContainerType { Code = "22G1", Name = "Dry 20'", Family = "Dry" };
        var lineOp = new LineOperator { Code = "CMA", Name = "CMA CGM" };
        var depot = new Depot { Code = "D1", Name = "D1", Address = "addr" };
        db.ContainerTypes.Add(ct);
        db.LineOperators.Add(lineOp);
        db.Depots.Add(depot);
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

        var block = new Block { DepotId = depot.Id, Code = "A", Name = "A", IsVirtual = false, MaxBay = 3, MaxRow = 1, MaxTier = 1 };
        db.Blocks.Add(block);
        await db.SaveChangesAsync();

        var existingMovement = new ContainerMovement
        {
            ContainerId = container.Id,
            LineOperatorId = lineOp.Id,
            BlockId = block.Id,
            Classification = "A",
            ConditionAtGateIn = ContainerCondition.Normal,
            GateInAt = DateTimeOffset.UtcNow.AddDays(-1),
            Status = MovementStatus.InYard
        };
        db.ContainerMovements.Add(existingMovement);
        await db.SaveChangesAsync();

        var rules = Substitute.For<IRuleEngine>();
        rules.Evaluate(Arg.Any<string>(), Arg.Any<IDictionary<string, object?>>())
            .Returns(RuleResult.Pass());
        var cache = Substitute.For<ICacheService>();
        var logger = CreateLogger();

        var handler = new GateInContainerCommandHandler(db, rules, cache, logger);
        var result = await handler.HandleAsync(
            new GateInContainerCommand("CMAU1234564", lineOp.Id, block.Id, 3, 1, 1, "A", "Normal", null, null),
            TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Gate.AlreadyInYard");
    }

    [Fact]
    public async Task HandleAsync_Should_Reject_When_Slot_Is_Occupied()
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

        var occupiedSlot = new YardSlot { BlockId = block.Id, Bay = 1, Row = 1, Tier = 1, IsOccupied = true, CurrentContainerId = Guid.NewGuid() };
        db.YardSlots.Add(occupiedSlot);
        await db.SaveChangesAsync();

        var rules = Substitute.For<IRuleEngine>();
        rules.Evaluate(Arg.Any<string>(), Arg.Any<IDictionary<string, object?>>())
            .Returns(RuleResult.Pass());
        var cache = Substitute.For<ICacheService>();
        var logger = CreateLogger();

        var handler = new GateInContainerCommandHandler(db, rules, cache, logger);
        var result = await handler.HandleAsync(
            new GateInContainerCommand("CMAU1234564", lineOp.Id, block.Id, 1, 1, 1, "A", "Normal", null, null),
            TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Yard.SlotNotOccupied");
    }

    [Fact]
    public async Task HandleAsync_Should_Succeed_For_Virtual_Block_Without_BayRowTier()
    {
        await using var db = TestDbContextFactory.Create();
        var ct = new ContainerType { Code = "22G1", Name = "Dry 20'", Family = "Dry" };
        var lineOp = new LineOperator { Code = "MSC", Name = "MSC" };
        var depot = new Depot { Code = "D1", Name = "D1", Address = "addr" };
        db.ContainerTypes.Add(ct);
        db.LineOperators.Add(lineOp);
        db.Depots.Add(depot);
        await db.SaveChangesAsync();

        db.Containers.Add(new Container
        {
            ContainerNumberRaw = "MSCU7654321",
            ContainerTypeId = ct.Id,
            IsoCode = "22G1",
            SizeFeet = 20,
            MaxWeightKg = 28000m,
            TareWeightKg = 2100m,
            ManufactureDate = DateTimeOffset.UtcNow,
            Owner = "MSC",
            Condition = ContainerCondition.Normal
        });

        var virtualBlock = new Block { DepotId = depot.Id, Code = "V1", Name = "Virtual 1", IsVirtual = true };
        db.Blocks.Add(virtualBlock);
        await db.SaveChangesAsync();

        var rules = Substitute.For<IRuleEngine>();
        rules.Evaluate(Arg.Any<string>(), Arg.Any<IDictionary<string, object?>>())
            .Returns(RuleResult.Pass());
        var cache = Substitute.For<ICacheService>();
        var logger = CreateLogger();

        var handler = new GateInContainerCommandHandler(db, rules, cache, logger);
        var result = await handler.HandleAsync(
            new GateInContainerCommand("MSCU7654321", lineOp.Id, virtualBlock.Id, null, null, null, "A", "Normal", null, null),
            TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be("InYard");
    }

    [Fact]
    public async Task HandleAsync_Should_Reject_Invalid_Condition()
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

        var rules = Substitute.For<IRuleEngine>();
        var cache = Substitute.For<ICacheService>();
        var logger = CreateLogger();

        var handler = new GateInContainerCommandHandler(db, rules, cache, logger);
        var result = await handler.HandleAsync(
            new GateInContainerCommand("CMAU1234564", lineOp.Id, block.Id, 1, 1, 1, "A", "InvalidCondition", null, null),
            TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Gate.InvalidCondition");
    }
}

public sealed class GateOutContainerCommandHandlerTests
{
    private static ILogger<GateOutContainerCommandHandler> CreateLogger() =>
        Substitute.For<ILogger<GateOutContainerCommandHandler>>();

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
        var logger = CreateLogger();

        var handler = new GateOutContainerCommandHandler(db, rules, cache, logger);

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
        var logger = CreateLogger();

        var handler = new GateOutContainerCommandHandler(db, rules, cache, logger);

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
        var logger = CreateLogger();
        var handler = new GateOutContainerCommandHandler(db, rules, cache, logger);

        var result = await handler.HandleAsync(
            new GateOutContainerCommand("CMAU1234564", order.Id, null, null, "Normal"),
            TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("DeliveryOrder.NotExpired");
    }

    [Fact]
    public async Task GateOut_Should_Reject_Closed_Order()
    {
        await using var db = TestDbContextFactory.Create();
        var lineOp = new LineOperator { Code = "MSC", Name = "MSC" };
        db.LineOperators.Add(lineOp);
        var customer = new Customer { TaxCode = "456", Name = "Corp" };
        db.Customers.Add(customer);
        var ct = new ContainerType { Code = "22G1", Name = "Dry 20'", Family = "Dry" };
        db.ContainerTypes.Add(ct);
        await db.SaveChangesAsync();

        var order = new DeliveryOrder
        {
            OrderNumber = "DO-CLOSED",
            CustomerId = customer.Id,
            LineOperatorId = lineOp.Id,
            ExpiryDate = DateTimeOffset.UtcNow.AddDays(10),
            IsClosed = true
        };
        order.Lines.Add(new DeliveryOrderLine { ContainerTypeId = ct.Id, RequestedQuantity = 5, DeliveredQuantity = 0 });
        db.DeliveryOrders.Add(order);
        await db.SaveChangesAsync();

        var container = new Container
        {
            ContainerNumberRaw = "MSCU7654321",
            ContainerTypeId = ct.Id, IsoCode = "22G1", SizeFeet = 20,
            MaxWeightKg = 28000m, TareWeightKg = 2100m,
            ManufactureDate = DateTimeOffset.UtcNow, Owner = "MSC",
            Condition = ContainerCondition.Normal
        };
        db.Containers.Add(container);
        await db.SaveChangesAsync();

        var movement = new ContainerMovement
        {
            ContainerId = container.Id, LineOperatorId = lineOp.Id,
            Classification = "A", ConditionAtGateIn = ContainerCondition.Normal,
            GateInAt = DateTimeOffset.UtcNow.AddDays(-2), Status = MovementStatus.InYard
        };
        db.ContainerMovements.Add(movement);
        await db.SaveChangesAsync();

        var rules = Substitute.For<IRuleEngine>();
        var cache = Substitute.For<ICacheService>();
        var logger = CreateLogger();
        var handler = new GateOutContainerCommandHandler(db, rules, cache, logger);

        var result = await handler.HandleAsync(
            new GateOutContainerCommand("MSCU7654321", order.Id, "VEH-001", "Driver", "Normal"),
            TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("DeliveryOrder.Closed");
    }

    [Fact]
    public async Task GateOut_Should_Reject_Wrong_LineOperator()
    {
        await using var db = TestDbContextFactory.Create();
        var lineOp1 = new LineOperator { Code = "CMA", Name = "CMA CGM" };
        var lineOp2 = new LineOperator { Code = "MSK", Name = "Maersk" };
        db.LineOperators.Add(lineOp1);
        db.LineOperators.Add(lineOp2);

        var customer = new Customer { TaxCode = "789", Name = "Shipper" };
        db.Customers.Add(customer);
        var ct = new ContainerType { Code = "22G1", Name = "Dry 20'", Family = "Dry" };
        db.ContainerTypes.Add(ct);
        await db.SaveChangesAsync();

        var order = new DeliveryOrder
        {
            OrderNumber = "DO-CMA",
            CustomerId = customer.Id,
            LineOperatorId = lineOp1.Id,
            ExpiryDate = DateTimeOffset.UtcNow.AddDays(5)
        };
        order.Lines.Add(new DeliveryOrderLine { ContainerTypeId = ct.Id, RequestedQuantity = 2, DeliveredQuantity = 0 });
        db.DeliveryOrders.Add(order);
        await db.SaveChangesAsync();

        var container = new Container
        {
            ContainerNumberRaw = "MSKU1234567",
            ContainerTypeId = ct.Id, IsoCode = "22G1", SizeFeet = 20,
            MaxWeightKg = 30000m, TareWeightKg = 2200m,
            ManufactureDate = DateTimeOffset.UtcNow, Owner = "MSK",
            Condition = ContainerCondition.Normal
        };
        db.Containers.Add(container);
        await db.SaveChangesAsync();

        var movement = new ContainerMovement
        {
            ContainerId = container.Id, LineOperatorId = lineOp2.Id,
            Classification = "A", ConditionAtGateIn = ContainerCondition.Normal,
            GateInAt = DateTimeOffset.UtcNow.AddDays(-1), Status = MovementStatus.InYard
        };
        db.ContainerMovements.Add(movement);
        await db.SaveChangesAsync();

        var rules = Substitute.For<IRuleEngine>();
        var cache = Substitute.For<ICacheService>();
        var logger = CreateLogger();
        var handler = new GateOutContainerCommandHandler(db, rules, cache, logger);

        var result = await handler.HandleAsync(
            new GateOutContainerCommand("MSKU1234567", order.Id, "VEH-1", "Driver", "Normal"),
            TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Gate.LineOperatorMismatch");
    }

    [Fact]
    public async Task GateOut_Should_Reject_When_Quantity_Exceeded()
    {
        await using var db = TestDbContextFactory.Create();
        var lineOp = new LineOperator { Code = "CMA", Name = "CMA" };
        db.LineOperators.Add(lineOp);
        var customer = new Customer { TaxCode = "123", Name = "Buyer" };
        db.Customers.Add(customer);
        var ct = new ContainerType { Code = "22G1", Name = "Dry 20'", Family = "Dry" };
        db.ContainerTypes.Add(ct);
        await db.SaveChangesAsync();

        var order = new DeliveryOrder
        {
            OrderNumber = "DO-FULL",
            CustomerId = customer.Id,
            LineOperatorId = lineOp.Id,
            ExpiryDate = DateTimeOffset.UtcNow.AddDays(10)
        };
        order.Lines.Add(new DeliveryOrderLine { ContainerTypeId = ct.Id, RequestedQuantity = 1, DeliveredQuantity = 1 });
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
            GateInAt = DateTimeOffset.UtcNow.AddDays(-1), Status = MovementStatus.InYard
        };
        db.ContainerMovements.Add(movement);
        await db.SaveChangesAsync();

        var rules = Substitute.For<IRuleEngine>();
        var cache = Substitute.For<ICacheService>();
        var logger = CreateLogger();
        var handler = new GateOutContainerCommandHandler(db, rules, cache, logger);

        var result = await handler.HandleAsync(
            new GateOutContainerCommand("CMAU1234564", order.Id, "VEH-1", "Driver", "Normal"),
            TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("DeliveryOrder.QuantityAvailable");
    }
}
