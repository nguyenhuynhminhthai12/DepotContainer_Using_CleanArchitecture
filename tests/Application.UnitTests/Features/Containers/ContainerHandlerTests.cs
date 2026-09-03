/**
 * Bộ test cho chức năng Container (Container Handlers).
 * Bao gồm test tạo container, từ chối số chẵn lẻ không hợp lệ, từ chối trùng lặp,
 * và phân trang danh sách container.
 * Bản quyền (c) 2026 TechSpherex.
 */
using FluentAssertions;
using TechSpherex.CleanArchitecture.Application.Features.Containers;
using TechSpherex.CleanArchitecture.Domain.Common;
using TechSpherex.CleanArchitecture.Domain.Common.Rules;
using TechSpherex.CleanArchitecture.Domain.Entities;

namespace TechSpherex.CleanArchitecture.Application.UnitTests.Features.Containers;

public sealed class CreateContainerCommandHandlerTests
{
    private const string TestContainerNumber = "CMAU1234564";

    [Fact]
    public async Task HandleAsync_Should_Reject_Invalid_CheckDigit()
    {
        await using var db = TestDbContextFactory.Create();
        db.ContainerTypes.Add(new ContainerType { Code = "22G1", Name = "Dry 20'", Family = "Dry" });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new CreateContainerCommandHandler(db);
        var cmd = new CreateContainerCommand(
            "CMAU1234560",
            (await db.ContainerTypes.FirstAsync(TestContext.Current.CancellationToken)).Id,
            "22G1", 20, 30000m, 2200m, DateTimeOffset.UtcNow, "CMA", "Normal");

        var result = await handler.HandleAsync(cmd, TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Container.NumberCheckDigit");
    }

    [Fact]
    public async Task HandleAsync_Should_Reject_Duplicate()
    {
        await using var db = TestDbContextFactory.Create();
        var ct = new ContainerType { Code = "22G1", Name = "Dry 20'", Family = "Dry" };
        db.ContainerTypes.Add(ct);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Create the first container with a valid check-digit number via factory
        db.Containers.Add(Container.Create(TestContainerNumber, ct.Id, "22G1", 20, 30000m, 2200m,
            DateTimeOffset.UtcNow, "CMA"));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new CreateContainerCommandHandler(db);
        var result = await handler.HandleAsync(
            new CreateContainerCommand(TestContainerNumber, ct.Id, "22G1", 20, 30000m, 2200m,
                DateTimeOffset.UtcNow, "CMA", "Normal"),
            TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Conflict);
    }

    [Fact]
    public async Task HandleAsync_Should_Create_Successfully()
    {
        await using var db = TestDbContextFactory.Create();
        var ct = new ContainerType { Code = "22G1", Name = "Dry 20'", Family = "Dry" };
        db.ContainerTypes.Add(ct);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new CreateContainerCommandHandler(db);
        var result = await handler.HandleAsync(
            new CreateContainerCommand(TestContainerNumber, ct.Id, "22G1", 20, 30000m, 2200m,
                DateTimeOffset.UtcNow, "CMA", "Normal"),
            TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.ContainerNumber.Should().Be(TestContainerNumber);
        result.Value!.SizeFeet.Should().Be(20);
    }
}

public sealed class GetContainersQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Paginate()
    {
        await using var db = TestDbContextFactory.Create();
        var ct = new ContainerType { Code = "22G1", Name = "Dry 20'", Family = "Dry" };
        db.ContainerTypes.Add(ct);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        for (var i = 0; i < 5; i++)
        {
            db.Containers.Add(new Container
            {
                ContainerNumberRaw = $"CMAU1234{i:000}",
                ContainerTypeId = ct.Id,
                IsoCode = "22G1",
                SizeFeet = 20,
                MaxWeightKg = 30000m,
                TareWeightKg = 2200m,
                ManufactureDate = DateTimeOffset.UtcNow,
                Owner = "CMA",
                Condition = ContainerCondition.Normal
            });
        }
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetContainersQueryHandler(db);
        var result = await handler.HandleAsync(new GetContainersQuery(1, 3), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.TotalCount.Should().Be(5);
        result.Value!.Items.Should().HaveCount(3);
        result.Value!.TotalPages.Should().Be(2);
    }
}