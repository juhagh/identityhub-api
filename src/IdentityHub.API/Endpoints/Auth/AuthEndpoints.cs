using IdentityHub.Application.Features.Login;
using IdentityHub.Application.Features.RefreshTokens;
using IdentityHub.Application.Features.Register;
using Microsoft.AspNetCore.Http.HttpResults;

namespace IdentityHub.API.Endpoints.Auth;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/auth");
        group.MapPost("/register", RegisterAsync);
        group.MapPost("/login", LoginAsync);
        group.MapPost("/refresh", RefreshAsync);
        return app;
    }

    private static async Task<Results<Created<RegisterResponse>, ValidationProblem>> RegisterAsync(
        RegisterRequest request, RegisterUserUseCase useCase)
    {
        var result = await useCase.HandleAsync(request.Email, request.Password);
        if (result.IsFailure)
            return result.ToValidationProblem();

        return TypedResults.Created((string?)null, new RegisterResponse(result.Value));
    }

    private static async Task<Results<Ok<TokenResponse>, ProblemHttpResult>> LoginAsync(
        LoginRequest request, LoginUserUseCase useCase, CancellationToken ct)
    {
        var result = await useCase.LoginAsync(request.Email, request.Password, ct);
        if (result.IsFailure)
            return result.ToProblem(StatusCodes.Status401Unauthorized);

        return TypedResults.Ok(TokenResponse.From(result.Value));
    }

    private static async Task<Results<Ok<TokenResponse>, ProblemHttpResult>> RefreshAsync(
        RefreshRequest request, RefreshTokenUseCase useCase, CancellationToken ct)
    {
        var result = await useCase.HandleAsync(request.RefreshToken, ct);
        if (result.IsFailure)
            return result.ToProblem(StatusCodes.Status401Unauthorized);

        return TypedResults.Ok(TokenResponse.From(result.Value));
    }
}