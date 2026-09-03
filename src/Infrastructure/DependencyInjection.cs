using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using TechSpherex.CleanArchitecture.Application.Abstractions.Caching;
using TechSpherex.CleanArchitecture.Application.Abstractions.Data;
using TechSpherex.CleanArchitecture.Application.Abstractions.Identity;
using TechSpherex.CleanArchitecture.Application.Abstractions.Rules;
using TechSpherex.CleanArchitecture.Application.Abstractions.Tenancy;
using TechSpherex.CleanArchitecture.Domain.Entities;
using TechSpherex.CleanArchitecture.Infrastructure.Caching;
using TechSpherex.CleanArchitecture.Infrastructure.Identity;
using TechSpherex.CleanArchitecture.Infrastructure.Persistence;
using TechSpherex.CleanArchitecture.Infrastructure.Rules;
using TechSpherex.CleanArchitecture.Infrastructure.Tenancy;

namespace TechSpherex.CleanArchitecture.Infrastructure;

/// <summary>
/// Cung cấp phương thức mở rộng <see cref="AddInfrastructure"/> để đăng ký
/// Persistence, Authentication, Caching, CORS, Multi-Tenancy và Rule Engine.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Đăng ký toàn bộ dịch vụ của tầng Infrastructure vào <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services">Bộ sưu tập dịch vụ DI.</param>
    /// <param name="configuration">Cấu hình ứng dụng.</param>
    /// <returns><see cref="IServiceCollection"/> sau khi đăng ký.</returns>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddPersistence();
        services.AddAuth(configuration);
        services.AddCachingServices();
        services.AddCorsPolicy(configuration);
        services.AddMultiTenancy();
        services.AddRuleEngineServices();

        return services;
    }

    /// <summary>
    /// Đăng ký <see cref="IAppDbContext"/> với triển khai <see cref="AppDbContext"/>.
    /// </summary>
    private static void AddPersistence(this IServiceCollection services) =>
        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());

    /// <summary>
    /// Cấu hình ASP.NET Core Identity, xác thực JWT và các dịch vụ liên quan.
    /// </summary>
    /// <param name="services">Bộ sưu tập dịch vụ DI.</param>
    /// <param name="configuration">Cấu hình ứng dụng (chứa Jwt:Secret, Jwt:Issuer, ...).</param>
    private static void AddAuth(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddIdentityCore<ApplicationUser>(options =>
            {
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequiredLength = 6;
                options.User.RequireUniqueEmail = true;
            })
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

        services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = configuration["Jwt:Issuer"],
                    ValidAudience = configuration["Jwt:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(configuration["Jwt:Secret"]!)),
                    ClockSkew = TimeSpan.Zero
                };
            });

        services.AddAuthorization();
        services.AddHttpContextAccessor();
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<ICurrentUser, CurrentUser>();
    }

    /// <summary>
    /// Cấu hình HybridCache (L1 In-Memory + L2 Redis) và đăng ký <see cref="ICacheService"/>.
    /// </summary>
    private static void AddCachingServices(this IServiceCollection services)
    {
        services.AddHybridCache(options =>
        {
            options.DefaultEntryOptions = new Microsoft.Extensions.Caching.Hybrid.HybridCacheEntryOptions
            {
                Expiration = TimeSpan.FromMinutes(5),
                LocalCacheExpiration = TimeSpan.FromMinutes(2)
            };
        });

        // Đăng ký lớp cache abstraction sạch được hỗ trợ bởi HybridCache
        services.AddSingleton<ICacheService, HybridCacheService>();
    }

    /// <summary>
    /// Cấu hình chính sách CORS dựa trên cấu hình <c>Cors</c>.
    /// </summary>
    private static void AddCorsPolicy(this IServiceCollection services, IConfiguration configuration)
    {
        var corsSection = configuration.GetSection("Cors");
        var allowedOrigins = corsSection.GetSection("AllowedOrigins").Get<string[]>() ?? ["*"];
        var allowedMethods = corsSection.GetSection("AllowedMethods").Get<string[]>() ?? ["GET", "POST", "PUT", "PATCH", "DELETE", "OPTIONS"];
        var allowedHeaders = corsSection.GetSection("AllowedHeaders").Get<string[]>() ?? ["*"];
        var allowCredentials = corsSection.GetValue("AllowCredentials", false);

        services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                ConfigureCorsOrigins(policy, allowedOrigins, allowCredentials);
                ConfigureCorsMethods(policy, allowedMethods);
                ConfigureCorsHeaders(policy, allowedHeaders);
                ConfigureCorsCredentials(policy, allowCredentials);
                policy.SetPreflightMaxAge(TimeSpan.FromMinutes(10));
            });
        });
    }

    /// <summary>
    /// Cấu hình nguồn gốc (origins) cho CORS policy.
    /// </summary>
    private static void ConfigureCorsOrigins(CorsPolicyBuilder policy, string[] allowedOrigins, bool allowCredentials)
    {
        if (allowedOrigins.Contains("*") && !allowCredentials)
        {
#pragma warning disable S5122 // Safe CORS: AllowAnyOrigin only when AllowCredentials is false
            policy.AllowAnyOrigin();
#pragma warning restore S5122 // Safe CORS: AllowAnyOrigin only when AllowCredentials is false
        }
        else
        {
            policy.WithOrigins(allowedOrigins);
        }
    }

    /// <summary>
    /// Cấu hình phương thức HTTP (methods) cho CORS policy.
    /// </summary>
    private static void ConfigureCorsMethods(CorsPolicyBuilder policy, string[] allowedMethods)
    {
        if (allowedMethods.Contains("*"))
        {
            policy.AllowAnyMethod();
        }
        else
        {
            policy.WithMethods(allowedMethods);
        }
    }

    /// <summary>
    /// Cấu hình header HTTP (headers) cho CORS policy.
    /// </summary>
    private static void ConfigureCorsHeaders(CorsPolicyBuilder policy, string[] allowedHeaders)
    {
        if (allowedHeaders.Contains("*"))
        {
            policy.AllowAnyHeader();
        }
        else
        {
            policy.WithHeaders(allowedHeaders);
        }
    }

    /// <summary>
    /// Bật hỗ trợ credentials (cookies, HTTP auth) nếu cấu hình cho phép.
    /// </summary>
    private static void ConfigureCorsCredentials(CorsPolicyBuilder policy, bool allowCredentials)
    {
        if (allowCredentials)
        {
            policy.AllowCredentials();
        }
    }

    /// <summary>
    /// Đăng ký <see cref="ITenantProvider"/> cho hệ thống nhiều tenant.
    /// </summary>
    private static void AddMultiTenancy(this IServiceCollection services) =>
        services.AddScoped<ITenantProvider, TenantProvider>();

    /// <summary>
    /// Đăng ký <see cref="IRuleEngine"/> (configuration-driven rule engine).
    /// </summary>
    private static void AddRuleEngineServices(this IServiceCollection services) =>
        services.AddSingleton<IRuleEngine, RuleEngine>();
}
