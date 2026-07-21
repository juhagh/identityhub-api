using IdentityHub.Application.Common.Interfaces;
using IdentityHub.Application.Common.Models;
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
    private readonly TimeProvider _timeProvider;
    private readonly AuthenticationOptions _authenticationOptions;

    public LoginUserUseCase(IIdentityService identityService, 
        ITokenService tokenService, 
        IRefreshTokenRepository refreshTokenRepository,
        TimeProvider timeProvider,
        IOptions<AuthenticationOptions> options)
    {
        _identityService = identityService;
        _tokenService = tokenService;
        _refreshTokenRepository = refreshTokenRepository;
        _timeProvider = timeProvider;
        _authenticationOptions = options.Value;
    }

    public async Task<Result<AuthenticationResult>> LoginAsync(string email, string password, CancellationToken ct)
    {
        var utcNow = _timeProvider.GetUtcNow().UtcDateTime;
        
        var credentialsResult = await _identityService.ValidateCredentialsAsync(email, password);
        if (credentialsResult.IsFailure)
            return Result<AuthenticationResult>.Failure(credentialsResult.Errors);
        
        var userId = credentialsResult.Value;
        var roles = await _identityService.GetRolesAsync(userId);
        var accessToken = _tokenService.CreateAccessToken(userId, email, roles);
        var refreshTokenValue = _tokenService.GenerateRefreshTokenValue();
        
        var refreshTokenLifetimeDays = _authenticationOptions.RefreshTokenLifetimeDays;
        var refreshTokenExpiry = utcNow.AddDays(refreshTokenLifetimeDays);
        var sessionStartTime = utcNow;
        var issuedAt = utcNow;
        var refreshToken = RefreshToken.Issue(userId, refreshTokenValue, refreshTokenExpiry, sessionStartTime, issuedAt);
        
        await _refreshTokenRepository.AddAsync(refreshToken, ct);
        await _refreshTokenRepository.SaveChangesAsync(ct);

        var refreshTokenLifetime = TimeSpan.FromDays(refreshTokenLifetimeDays);
        var accessTokenLifetimeMinutes = _authenticationOptions.AccessTokenLifetimeMinutes;
        var accessTokenLifetime = TimeSpan.FromMinutes(accessTokenLifetimeMinutes);

        var loginResult = new AuthenticationResult
        (
             AccessToken: accessToken,
             RefreshToken: refreshToken.Token,
             AccessTokenExpiresIn: (long)accessTokenLifetime.TotalSeconds,
             RefreshTokenExpiresIn: (long)refreshTokenLifetime.TotalSeconds
        );

        return Result<AuthenticationResult>.Success(loginResult);
    }
    
}