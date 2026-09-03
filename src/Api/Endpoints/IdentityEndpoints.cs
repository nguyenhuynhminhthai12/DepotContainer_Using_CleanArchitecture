using TechSpherex.CleanArchitecture.Api.Extensions;
using TechSpherex.CleanArchitecture.Application.Abstractions.Identity;
using TechSpherex.CleanArchitecture.Application.Abstractions.Messaging;
using TechSpherex.CleanArchitecture.Application.Features.Identity.Login;
using TechSpherex.CleanArchitecture.Application.Features.Identity.RefreshToken;
using TechSpherex.CleanArchitecture.Application.Features.Identity.Register;
using TechSpherex.CleanArchitecture.Domain.Common;
namespace TechSpherex.CleanArchitecture.Api.Endpoints;

/// <summary>
/// Nhóm các endpoint REST cho chức năng xác thực (đăng ký, đăng nhập, làm mới token).
/// Đăng ký tại hai nhóm route: /api/identity và /api/auth.
/// </summary>
public static class IdentityEndpoints
{
    /// <summary>
    /// Đăng ký tất cả endpoint Identity vào <see cref="IEndpointRouteBuilder"/>.
    /// </summary>
    /// <param name="app">Route builder để đăng ký endpoint.</param>
    public static void MapIdentityEndpoints(this IEndpointRouteBuilder app)
    {
        MapGroup(app.MapGroup("/api/identity"));
        MapGroup(app.MapGroup("/api/auth"));
    }

    /// <summary>Đăng ký endpoint cho một nhóm route.</summary>
    /// <param name="group">Route group builder.</param>
    private static void MapGroup(RouteGroupBuilder group)
    {
        group.WithTags("Identity");

        group.MapPost("/register", Register)
            .AddEndpointFilter<ValidationFilter<RegisterCommand>>()
            .WithSummary("Đăng ký tài khoản người dùng mới");

        group.MapPost("/login", Login)
            .AddEndpointFilter<ValidationFilter<LoginCommand>>()
            .WithSummary("Đăng nhập bằng email và mật khẩu");

        group.MapPost("/refresh", Refresh)
            .AddEndpointFilter<ValidationFilter<RefreshTokenCommand>>()
            .WithSummary("Làm mới access token đã hết hạn");
    }

    /// <summary>Xử lý POST /register — đăng ký tài khoản người dùng mới.</summary>
    private static async Task<IResult> Register(
        RegisterCommand command,
        ICommandHandler<RegisterCommand, Result> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok() : result.ToProblemDetails();
    }

    /// <summary>Xử lý POST /login — đăng nhập và trả về JWT token.</summary>
    private static async Task<IResult> Login(
        LoginCommand command,
        ICommandHandler<LoginCommand, Result<TokenResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    /// <summary>Xử lý POST /refresh — làm mới access token bằng refresh token.</summary>
    private static async Task<IResult> Refresh(
        RefreshTokenCommand command,
        ICommandHandler<RefreshTokenCommand, Result<TokenResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }
}
