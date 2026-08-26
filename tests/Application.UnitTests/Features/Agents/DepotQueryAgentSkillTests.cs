using FluentAssertions;
using TechSpherex.CleanArchitecture.Application.Abstractions.Agents;
using TechSpherex.CleanArchitecture.Application.Features.Agents.Skills;
using TechSpherex.CleanArchitecture.Application.Features.Reports;
using TechSpherex.CleanArchitecture.Domain.Common;
using TechSpherex.CleanArchitecture.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace TechSpherex.CleanArchitecture.Application.UnitTests.Features.Agents;

public sealed class DepotQueryAgentSkillTests
{
    [Fact]
    public async Task ExecuteAsync_Should_Return_NeedsMoreInfo_For_Empty_Prompt()
    {
        var db = TestDbContextFactory.Create();
        var skill = new DepotQueryAgentSkill(
            db,
            new GetYardAgingReportQueryHandler(db),
            new GetDailyThroughputReportQueryHandler(db));

        var result = await skill.ExecuteAsync(
            new AgentContext { Prompt = "   " },
            TestContext.Current.CancellationToken);

        result.Status.Should().Be(AgentResultStatus.NeedsMoreInfo);
    }

    [Fact]
    public async Task ExecuteAsync_Should_Route_Aging_Question_To_YardAgingHandler()
    {
        var db = TestDbContextFactory.Create();
        var lineOp = new LineOperator { Code = "CMA", Name = "CMA CGM" };
        var ct = new ContainerType { Code = "22G1", Name = "Dry 20'", Family = "Dry" };
        db.LineOperators.Add(lineOp);
        db.ContainerTypes.Add(ct);
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
            GateInAt = DateTimeOffset.UtcNow.AddDays(-3),
            Status = MovementStatus.InYard
        });
        await db.SaveChangesAsync();

        var skill = new DepotQueryAgentSkill(
            db,
            new GetYardAgingReportQueryHandler(db),
            new GetDailyThroughputReportQueryHandler(db));

        var result = await skill.ExecuteAsync(
            new AgentContext { Prompt = "How many containers have been here over 10 days?" },
            TestContext.Current.CancellationToken);

        result.Status.Should().Be(AgentResultStatus.Success);
        result.Message.Should().Contain("Yard aging");
    }

    [Fact]
    public async Task ExecuteAsync_Should_Route_Count_Question_To_InYard_Count()
    {
        var db = TestDbContextFactory.Create();
        var lineOp = new LineOperator { Code = "CMA", Name = "CMA" };
        var ct = new ContainerType { Code = "22G1", Name = "Dry 20'", Family = "Dry" };
        db.LineOperators.Add(lineOp);
        db.ContainerTypes.Add(ct);
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
            GateInAt = DateTimeOffset.UtcNow,
            Status = MovementStatus.InYard
        });
        await db.SaveChangesAsync();

        var skill = new DepotQueryAgentSkill(
            db,
            new GetYardAgingReportQueryHandler(db),
            new GetDailyThroughputReportQueryHandler(db));

        var result = await skill.ExecuteAsync(
            new AgentContext { Prompt = "How many containers are in the yard?" },
            TestContext.Current.CancellationToken);

        result.Status.Should().Be(AgentResultStatus.Success);
        result.Message.Should().Contain("Total in-yard: 1");
    }
}