/**
 * Bộ test cho chức năng Báo cáo (Report Handlers).
 * Bao gồm test báo cáo thời gian lưu trữ container trong Yard
 * và báo cáo thông lượng cổng theo ngày và hãng vận tải.
 * Bản quyền (c) 2026 TechSpherex.
 */
using FluentAssertions;
using TechSpherex.CleanArchitecture.Application.Features.Reports;
using TechSpherex.CleanArchitecture.Domain.Entities;

namespace TechSpherex.CleanArchitecture.Application.UnitTests.Features.Reports;

public sealed class YardAgingReportHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Bucket_Containers_By_Days()
    {
        await using var db = TestDbContextFactory.Create();

        var cma = new LineOperator { Code = "CMA", Name = "CMA CGM" };
        var msc = new LineOperator { Code = "MSC", Name = "MSC" };
        db.LineOperators.AddRange(cma, msc);

        var ct = new ContainerType { Code = "22G1", Name = "Dry 20'", Family = "Dry" };
        db.ContainerTypes.Add(ct);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        // CMA — 2 fresh, 1 old
        for (var i = 0; i < 3; i++)
        {
            var c = new Container
            {
                ContainerNumberRaw = $"CMAU1234{i:000}",
                ContainerTypeId = ct.Id, IsoCode = "22G1", SizeFeet = 20,
                MaxWeightKg = 30000m, TareWeightKg = 2200m,
                ManufactureDate = DateTimeOffset.UtcNow, Owner = "CMA",
                Condition = ContainerCondition.Normal
            };
            db.Containers.Add(c);
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
            db.ContainerMovements.Add(new ContainerMovement
            {
                ContainerId = c.Id,
                LineOperatorId = cma.Id,
                Classification = "A",
                ConditionAtGateIn = ContainerCondition.Normal,
                GateInAt = i < 2 ? DateTimeOffset.UtcNow.AddDays(-3) : DateTimeOffset.UtcNow.AddDays(-15),
                Status = MovementStatus.InYard
            });
        }
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetYardAgingReportQueryHandler(db);
        var result = await handler.HandleAsync(new GetYardAgingReportQuery(), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        var row = result.Value!.Rows.First(r => r.LineOperatorCode == "CMA");
        row.Buckets.WithinTenDays.Should().Be(2);
        row.Buckets.TenDaysOrMore.Should().Be(1);
        row.Buckets.Total.Should().Be(3);
    }
}

public sealed class DailyThroughputReportHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Group_By_Operator_And_Day()
    {
        await using var db = TestDbContextFactory.Create();
        var op = new LineOperator { Code = "CMA", Name = "CMA" };
        db.LineOperators.Add(op);
        var ct = new ContainerType { Code = "22G1", Name = "Dry 20'", Family = "Dry" };
        db.ContainerTypes.Add(ct);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        for (var i = 0; i < 2; i++)
        {
            var c = new Container
            {
                ContainerNumberRaw = $"CMAU1234{i:000}",
                ContainerTypeId = ct.Id, IsoCode = "22G1", SizeFeet = 20,
                MaxWeightKg = 30000m, TareWeightKg = 2200m,
                ManufactureDate = DateTimeOffset.UtcNow, Owner = "CMA",
                Condition = ContainerCondition.Normal
            };
            db.Containers.Add(c);
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
            db.ContainerMovements.Add(new ContainerMovement
            {
                ContainerId = c.Id,
                LineOperatorId = op.Id,
                Classification = "A",
                ConditionAtGateIn = ContainerCondition.Normal,
                GateInAt = DateTimeOffset.UtcNow.AddDays(-2),
                GateOutAt = DateTimeOffset.UtcNow.AddDays(-1),
                Status = MovementStatus.GateOut
            });
        }
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetDailyThroughputReportQueryHandler(db);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var result = await handler.HandleAsync(
            new GetDailyThroughputReportQuery(today.AddDays(-7), today),
            TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Rows.Should().NotBeEmpty();
        result.Value!.Rows.Sum(r => r.GateInCount).Should().Be(2);
    }
}