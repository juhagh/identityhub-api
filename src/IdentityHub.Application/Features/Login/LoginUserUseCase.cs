using IdentityHub.Application.Common.Interfaces;
using IdentityHub.Application.Common.Options;
using IdentityHub.Application.Common.Results;
using IdentityHub.Domain.Entities;
using Microsoft.Extensions.Options;

namespace IdentityHub.Application.Features.Login;

public sealed class LoginUserUseCase
{
    private readonly IIdentityService _identityService;
    private readonly ITokenService _tokenService;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly AuthenticationOptions _authenticationOptions;

    public LoginUserUseCase(IIdentityService identityService, 
        ITokenService tokenService, 
        IRefreshTokenRepository refreshTokenRepository, 
        IOptions<AuthenticationOptions> options)
    {
        _identityService = identityService;
        _tokenService = tokenService;
        _refreshTokenRepository = refreshTokenRepository;
        _authenticationOptions = options.Value;
    }

    public async Task<Result<LoginResult>> LoginAsync(string email, string password, CancellationToken ct)
    {
        var credentialsResult = await _identityService.ValidateCredentialsAsync(email, password);
        if (credentialsResult.IsFailure)
            return Result<LoginResult>.Failure(credentialsResult.Errors);
        
        var userId = credentialsResult.Value;
        var roles = await _identityService.GetRolesAsync(userId);
        // Short‑lived access tokens (default 15 min, see AuthenticationOptions)
        var accessToken = _tokenService.CreateAccessToken(userId, email, roles);
        var refreshTokenValue = _tokenService.GenerateRefreshTokenValue();
        
        var refreshTokenLifetimeDays = _authenticationOptions.RefreshTokenLifetimeDays;
        var refreshTokenExpiry = DateTime.UtcNow.AddDays(refreshTokenLifetimeDays);
        var refreshToken = RefreshToken.Issue(userId, refreshTokenValue, refreshTokenExpiry);
        
        await _refreshTokenRepository.AddAsync(refreshToken, ct);
        await _refreshTokenRepository.SaveChangesAsync(ct);

        var refreshTokenLifetime = TimeSpan.FromDays(refreshTokenLifetimeDays);
        var accessTokenLifetimeMinutes = _authenticationOptions.AccessTokenLifetimeMinutes;
        var accessTokenLifetime = TimeSpan.FromMinutes(accessTokenLifetimeMinutes);

        var loginResult = new LoginResult
        (
             AccessToken: accessToken,
             RefreshToken: refreshToken.Token,
             AccessTokenExpiresIn: (long)accessTokenLifetime.TotalSeconds,
             RefreshTokenExpiresIn: (long)refreshTokenLifetime.TotalSeconds
        );

        return Result<LoginResult>.Success(loginResult);
    }
    
}