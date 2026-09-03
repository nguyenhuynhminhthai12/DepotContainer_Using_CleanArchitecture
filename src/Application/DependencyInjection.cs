
using System.Reflection;
using TechSpherex.CleanArchitecture.Application.Abstractions.Agents;
using TechSpherex.CleanArchitecture.Application.Abstractions.Messaging;
using TechSpherex.CleanArchitecture.Application.Features.Agents;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
namespace TechSpherex.CleanArchitecture.Application;

/// <summary>
/// Lớp đăng ký dịch vụ (DI) cho tầng Application.
/// Cung cấp phương thức mở rộng <see cref="AddApplication"/> để đăng ký Validators, Handlers và Skill Agents.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Đăng ký toàn bộ dịch vụ của tầng Application vào <see cref="IServiceCollection"/>.
    /// Bao gồm FluentValidation validators, CQRS handlers và skill agents.
    /// </summary>
    /// <param name="services">Bộ sưu tập dịch vụ DI.</param>
    /// <returns><see cref="IServiceCollection"/> sau khi đăng ký.</returns>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = typeof(DependencyInjection).Assembly;

        services.AddValidatorsFromAssembly(assembly);
        services.AddHandlersFromAssembly(assembly);
        services.AddSkillAgents(assembly);

        return services;
    }

    /// <summary>
    /// Đăng ký tất cả các handler (ICommandHandler và IQueryHandler) tìm thấy trong assembly.
    /// </summary>
    /// <param name="services">Bộ sưu tập dịch vụ DI.</param>
    /// <param name="assembly">Assembly cần quét để tìm handler.</param>
    private static void AddHandlersFromAssembly(this IServiceCollection services, Assembly assembly)
    {
        var handlerInterfaceTypes = new[]
        {
            typeof(ICommandHandler<,>),
            typeof(IQueryHandler<,>)
        };

        var types = assembly.GetTypes()
            .Where(t => t is { IsAbstract: false, IsInterface: false })
            .ToList();

        foreach (var type in types)
        {
            var interfaces = type.GetInterfaces()
                .Where(i => i.IsGenericType &&
                            handlerInterfaceTypes.Contains(i.GetGenericTypeDefinition()));

            foreach (var handlerInterface in interfaces)
            {
                services.AddScoped(handlerInterface, type);
            }
        }
    }

    /// <summary>
    /// Đăng ký tất cả các lớp triển khai <see cref="ISkillAgent"/> và
    /// <see cref="IAgentOrchestrator"/> từ assembly vào DI container.
    /// </summary>
    /// <param name="services">Bộ sưu tập dịch vụ DI.</param>
    /// <param name="assembly">Assembly cần quét để tìm skill agents.</param>
    private static void AddSkillAgents(this IServiceCollection services, Assembly assembly)
    {
        // Đăng ký tất cả các lớp triển khai ISkillAgent
        var skillTypes = assembly.GetTypes()
            .Where(t => t is { IsAbstract: false, IsInterface: false }
                        && typeof(ISkillAgent).IsAssignableFrom(t));

        foreach (var skillType in skillTypes)
        {
            services.AddScoped(typeof(ISkillAgent), skillType);
        }

        // Đăng ký orchestrator
        services.AddScoped<IAgentOrchestrator, AgentOrchestrator>();
    }
}
