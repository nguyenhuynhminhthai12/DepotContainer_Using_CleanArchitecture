using FluentAssertions;
using TechSpherex.CleanArchitecture.Application.Features.DeliveryOrders;
using TechSpherex.CleanArchitecture.Domain.Entities;

namespace TechSpherex.CleanArchitecture.Application.UnitTests.Features.DeliveryOrders;

public sealed class DeliveryOrderHandlerTests
{
    [Fact]
    public async Task Create_Should_Succeed()
    {
        await using var db = TestDbContextFactory.Create();
        var lineOp = new LineOperator { Code = "CMA", Name = "CMA" };
        var customer = new Customer { TaxCode = "123", Name = "ACME" };
        var ct = new ContainerType { Code = "22G1", Name = "Dry 20'", Family = "Dry" };
        db.LineOperators.Add(lineOp);
        db.Customers.Add(customer);
        db.ContainerTypes.Add(ct);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new CreateDeliveryOrderCommandHandler(db);
        var result = await handler.HandleAsync(
            new CreateDeliveryOrderCommand(
                "DO-001",
                customer.Id,
                lineOp.Id,
                DateTimeOffset.UtcNow.AddDays(7),
                "Vessel Voyage 1",
                "Some note",
                [new DeliveryOrderLineDto(ct.Id, 3, 0)]),
            TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.OrderNumber.Should().Be("DO-001");
        result.Value!.Lines.Should().HaveCount(1);
        result.Value!.Lines[0].RequestedQuantity.Should().Be(3);
    }

    [Fact]
    public async Task Create_Should_Reject_Duplicate_OrderNumber()
    {
        await using var db = TestDbContextFactory.Create();
        var lineOp = new LineOperator { Code = "CMA", Name = "CMA" };
        var customer = new Customer { TaxCode = "123", Name = "ACME" };
        var ct = new ContainerType { Code = "22G1", Name = "Dry 20'", Family = "Dry" };
        db.LineOperators.Add(lineOp);
        db.Customers.Add(customer);
        db.ContainerTypes.Add(ct);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        db.DeliveryOrders.Add(new DeliveryOrder
        {
            OrderNumber = "DO-DUP",
            CustomerId = customer.Id,
            LineOperatorId = lineOp.Id,
            ExpiryDate = DateTimeOffset.UtcNow.AddDays(7)
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new CreateDeliveryOrderCommandHandler(db);
        var result = await handler.HandleAsync(
            new CreateDeliveryOrderCommand("DO-DUP", customer.Id, lineOp.Id,
                DateTimeOffset.UtcNow.AddDays(7), null, null,
                [new DeliveryOrderLineDto(ct.Id, 1, 0)]),
            TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("DeliveryOrder.Duplicate");
    }

    [Fact]
    public async Task GetActive_Should_Exclude_Closed_And_Expired()
    {
        await using var db = TestDbContextFactory.Create();
        var lineOp = new LineOperator { Code = "CMA", Name = "CMA" };
        var customer = new Customer { TaxCode = "123", Name = "ACME" };
        db.LineOperators.Add(lineOp);
        db.Customers.Add(customer);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        db.DeliveryOrders.Add(new DeliveryOrder
        {
            OrderNumber = "DO-ACTIVE",
            CustomerId = customer.Id,
            LineOperatorId = lineOp.Id,
            ExpiryDate = DateTimeOffset.UtcNow.AddDays(7)
        });
        db.DeliveryOrders.Add(new DeliveryOrder
        {
            OrderNumber = "DO-EXPIRED",
            CustomerId = customer.Id,
            LineOperatorId = lineOp.Id,
            ExpiryDate = DateTimeOffset.UtcNow.AddDays(-1)
        });
        db.DeliveryOrders.Add(new DeliveryOrder
        {
            OrderNumber = "DO-CLOSED",
            CustomerId = customer.Id,
            LineOperatorId = lineOp.Id,
            ExpiryDate = DateTimeOffset.UtcNow.AddDays(7),
            IsClosed = true
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetActiveDeliveryOrdersQueryHandler(db);
        var result = await handler.HandleAsync(new GetActiveDeliveryOrdersQuery(), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Select(o => o.OrderNumber).Should().Contain("DO-ACTIVE")
            .And.NotContain("DO-EXPIRED").And.NotContain("DO-CLOSED");
    }
}