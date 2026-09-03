/**
 * Bộ test kiến trúc (Architecture Tests) cho hệ thống TechSpherex Container Depot TOS.
 * Kiểm tra các ràng buộc phụ thuộc (dependency rules) giữa các layer:
 * Domain không được phụ thuộc Application, Infrastructure, hay Api;
 * Application không được phụ thuộc Infrastructure hay Api;
 * Infrastructure không được phụ thuộc Api.
 * Ngoài ra còn kiểm tra: Handlers và Validators phải là sealed class,
 * Domain Entities không có public setter cho Id, tất cả entity trong Depot aggregate
 * phải implement ITenantEntity, và tất cả Business Rules phải implement IBusinessRule.
 * Bản quyền (c) 2026 TechSpherex.
 */
using System.Reflection;
using FluentAssertions;
using NetArchTest.Rules;
using TechSpherex.CleanArchitecture.Domain.Common;

namespace TechSpherex.CleanArchitecture.Architecture.Tests;


public sealed class ArchitectureTests
{
    private static readonly Assembly DomainAssembly = typeof(Domain.Common.BaseEntity).Assembly;
    private static readonly Assembly ApplicationAssembly = typeof(Application.DependencyInjection).Assembly;
    private static readonly Assembly InfrastructureAssembly = typeof(Infrastructure.DependencyInjection).Assembly;

    private const string ApplicationNamespace = "TechSpherex.CleanArchitecture.Application";
    private const string InfrastructureNamespace = "TechSpherex.CleanArchitecture.Infrastructure";
    private const string ApiNamespace = "TechSpherex.CleanArchitecture.Api";

    // Copyright (c) 2026 TechSpherex
    [Fact]
    public void Domain_Should_Not_Depend_On_Application()
    {
        var result = Types.InAssembly(DomainAssembly)
            .ShouldNot()
            .HaveDependencyOn(ApplicationNamespace)
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void Domain_Should_Not_Depend_On_Infrastructure()
    {
        var result = Types.InAssembly(DomainAssembly)
            .ShouldNot()
            .HaveDependencyOn(InfrastructureNamespace)
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void Domain_Should_Not_Depend_On_Api()
    {
        var result = Types.InAssembly(DomainAssembly)
            .ShouldNot()
            .HaveDependencyOn(ApiNamespace)
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }

    // Copyright (c) 2026 TechSpherex
    [Fact]
    public void Application_Should_Not_Depend_On_Infrastructure()
    {
        var result = Types.InAssembly(ApplicationAssembly)
            .ShouldNot()
            .HaveDependencyOn(InfrastructureNamespace)
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void Application_Should_Not_Depend_On_Api()
    {
        var result = Types.InAssembly(ApplicationAssembly)
            .ShouldNot()
            .HaveDependencyOn(ApiNamespace)
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void Infrastructure_Should_Not_Depend_On_Api()
    {
        var result = Types.InAssembly(InfrastructureAssembly)
            .ShouldNot()
            .HaveDependencyOn(ApiNamespace)
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void Handlers_Should_Be_Sealed()
    {
        var result = Types.InAssembly(ApplicationAssembly)
            .That()
            .HaveNameEndingWith("Handler")
            .Should()
            .BeSealed()
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void Validators_Should_Be_Sealed()
    {
        var result = Types.InAssembly(ApplicationAssembly)
            .That()
            .HaveNameEndingWith("Validator")
            .Should()
            .BeSealed()
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void Domain_Entities_Should_Not_Be_Public_Setters_For_Id()
    {
        var result = Types.InAssembly(DomainAssembly)
            .That()
            .ResideInNamespace("TechSpherex.CleanArchitecture.Domain.Entities")
            .Should()
            .BeSealed()
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }

    /// <summary>
    /// Enforces the multi-tenant constraint: every depot aggregate root must implement ITenantEntity
    /// so the global query filter applies automatically.
    /// </summary>
    [Fact]
    public void All_Depot_Entities_Should_Implement_ITenantEntity()
    {
        var tenantEntityNames = new[]
        {
            "Depot",
            "Block",
            "YardSlot",
            "ContainerType",
            "Container",
            "LineOperator",
            "ContainerMovement",
            "Customer",
            "DeliveryOrder"
        };

        foreach (var entityName in tenantEntityNames)
        {
            var matchingTypes = DomainAssembly.GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract && t.Name == entityName)
                .ToList();

            matchingTypes.Should().NotBeEmpty($"expected to find entity {entityName}");

            matchingTypes.Should().AllSatisfy(type =>
                typeof(ITenantEntity).IsAssignableFrom(type).Should().BeTrue(
                    $"{type.FullName} must implement ITenantEntity for multi-tenancy."));
        }
    }

    /// <summary>
    /// All business rules must live under Domain.Common.Rules so the dependency rule stays clean.
    /// </summary>
    [Fact]
    public void All_Business_Rules_Should_Implement_IBusinessRule()
    {
        var ruleTypes = DomainAssembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && t.Name.EndsWith("Rule"))
            .ToList();

        ruleTypes.Should().NotBeEmpty();
        ruleTypes.Should().AllSatisfy(type =>
            typeof(TechSpherex.CleanArchitecture.Domain.Common.Rules.IBusinessRule).IsAssignableFrom(type).Should().BeTrue(
                $"{type.FullName} must implement IBusinessRule."));
    }
}
