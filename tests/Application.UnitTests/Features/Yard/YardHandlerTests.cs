using FluentAssertions;
using TechSpherex.CleanArchitecture.Application.Abstractions.Caching;
using TechSpherex.CleanArchitecture.Application.Features.Yard;
using TechSpherex.CleanArchitecture.Domain.Common;
using TechSpherex.CleanArchitecture.Domain.Entities;
using NSubstitute;
using Microsoft.EntityFrameworkCore;

namespace TechSpherex.CleanArchitecture.Application.UnitTests.Features.Yard;

public sealed class CreateBlockCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Fail_When_Depot_Missing()
    {
        await using var db = TestDbContextFactory.Create();
        var cache = Substitute.For<ICacheService>();
        var handler = new CreateBlockCommandHandler(db, cache);
        var cmd = new CreateBlockCommand(Guid.NewGuid(), "A", "Block A", false, 5, 4, 3);

        var result = await handler.HandleAsync(cmd, TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task HandleAsync_Should_Create_Real_Block_And_Invalidate_Cache()
    {
        await using var db = TestDbContextFactory.Create();
        var depot = new Depot { Code = "D1", Name = "D1", Address = "addr" };
        db.Depots.Add(depot);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var cache = Substitute.For<ICacheService>();
        var handler = new CreateBlockCommandHandler(db, cache);
        var cmd = new CreateBlockCommand(depot.Id, "A", "Block A", false, 5, 4, 3);

        var result = await handler.HandleAsync(cmd, TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Code.Should().Be("A");
        result.Value!.MaxBay.Should().Be(5);
        db.Blocks.Should().HaveCount(1);
        await cache.Received(1).InvalidateByTagAsync("yard-map", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_Should_Create_Virtual_Block_Without_Max_Dimensions()
    {
        await using var db = TestDbContextFactory.Create();
        var depot = new Depot { Code = "D1", Name = "D1", Address = "addr" };
        db.Depots.Add(depot);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var cache = Substitute.For<ICacheService>();
        var handler = new CreateVirtualBlockCommandHandler(db, cache);
        var cmd = new CreateVirtualBlockCommand(depot.Id, "V", "Virtual block");

        var result = await handler.HandleAsync(cmd, TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.IsVirtual.Should().BeTrue();
        result.Value!.MaxBay.Should().BeNull();
    }
}

public sealed class ResizeBlockCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Add_Missing_Slots()
    {
        await using var db = TestDbContextFactory.Create();
        var depot = new Depot { Code = "D1", Name = "D1", Address = "addr" };
        db.Depots.Add(depot);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var block = new Block { DepotId = depot.Id, Code = "A", Name = "A", IsVirtual = false, MaxBay = 2, MaxRow = 2, MaxTier = 1 };
        db.Blocks.Add(block);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var cache = Substitute.For<ICacheService>();
        var handler = new ResizeBlockCommandHandler(db, cache);

        var result = await handler.HandleAsync(
            new ResizeBlockCommand(block.Id, MaxBay: 3, MaxRow: 3, MaxTier: 2),
            TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        var slotCount = await db.YardSlots.CountAsync(TestContext.Current.CancellationToken);
        slotCount.Should().Be(3 * 3 * 2); // all slots created
    }

    [Fact]
    public async Task HandleAsync_Should_Reject_Resizing_Virtual_Block()
    {
        await using var db = TestDbContextFactory.Create();
        var depot = new Depot { Code = "D1", Name = "D1", Address = "addr" };
        db.Depots.Add(depot);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var block = new Block { DepotId = depot.Id, Code = "V", Name = "V", IsVirtual = true };
        db.Blocks.Add(block);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var cache = Substitute.For<ICacheService>();
        var handler = new ResizeBlockCommandHandler(db, cache);

        var result = await handler.HandleAsync(
            new ResizeBlockCommand(block.Id, 3, 3, 2),
            TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Contain("VirtualResizeNotSupported");
    }
}

public sealed class GetYardMapQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Build_Map_From_Cache_Then_Db()
    {
        await using var db = TestDbContextFactory.Create();
        var depot = new Depot { Code = "D1", Name = "Depot 1", Address = "addr" };
        db.Depots.Add(depot);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var block = new Block { DepotId = depot.Id, Code = "A", Name = "A", IsVirtual = false, MaxBay = 2, MaxRow = 1, MaxTier = 1 };
        db.Blocks.Add(block);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        for (var bay = 1; bay <= 2; bay++)
        {
            db.YardSlots.Add(new YardSlot
            {
                BlockId = block.Id,
                Bay = bay,
                Row = 1,
                Tier = 1,
                IsOccupied = false
            });
        }
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var cache = Substitute.For<ICacheService>();
        cache.GetOrCreateAsync(
            Arg.Any<string>(),
            Arg.Any<Func<CancellationToken, Task<YardMapDto?>>>(),
            Arg.Any<TimeSpan?>(),
            Arg.Any<TimeSpan?>(),
            Arg.Any<IEnumerable<string>?>(),
            Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                var factory = call.ArgAt<Func<CancellationToken, Task<YardMapDto?>>>(1);
                return factory(TestContext.Current.CancellationToken);
            });

        var handler = new GetYardMapQueryHandler(db, cache);
        var result = await handler.HandleAsync(new GetYardMapQuery(depot.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Blocks.Should().HaveCount(1);
        result.Value!.Blocks[0].Slots.Should().HaveCount(2);
    }
}