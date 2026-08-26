
using TechSpherex.CleanArchitecture.Api.Extensions;
using TechSpherex.CleanArchitecture.Application.Abstractions.Identity;
using TechSpherex.CleanArchitecture.Application.Abstractions.Messaging;
using TechSpherex.CleanArchitecture.Application.Features.Identity.Login;
using TechSpherex.CleanArchitecture.Application.Features.Identity.RefreshToken;
using TechSpherex.CleanArchitecture.Application.Features.Identity.Register;
using TechSpherex.CleanArchitecture.Domain.Common;
namespace TechSpherex.CleanArchitecture.Api.Endpoints;

// Copyright (c) 2026 TechSpherex
public static class IdentityEndpoints
{
    public static void MapIdentityEndpoints(this IEndpointRouteBuilder app)
    {
        MapGroup(app.MapGroup("/api/identity"));
        MapGroup(app.MapGroup("/api/auth"));
    }

    private static void MapGroup(RouteGroupBuilder group)
    {
        group.WithTags("Identity");

        group.MapPost("/register", Register)
            .AddEndpointFilter<ValidationFilter<RegisterCommand>>()
            .WithSummary("Register a new user");

        group.MapPost("/login", Login)
            .AddEndpointFilter<ValidationFilter<LoginCommand>>()
            .WithSummary("Login with email and password");

        group.MapPost("/refresh", Refresh)
            .AddEndpointFilter<ValidationFilter<RefreshTokenCommand>>()
            .WithSummary("Refresh an expired access token");
    }

    private static async Task<IResult> Register(
        RegisterCommand command,
        ICommandHandler<RegisterCommand, Result> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok() : result.ToProblemDetails();
    }

    private static async Task<IResult> Login(
        LoginCommand command,
        ICommandHandler<LoginCommand, Result<TokenResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }

    private static async Task<IResult> Refresh(
        RefreshTokenCommand command,
        ICommandHandler<RefreshTokenCommand, Result<TokenResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblemDetails();
    }
}
