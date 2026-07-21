using IdentityHub.Application.Common.Errors;
using IdentityHub.Application.Common.Interfaces;
using IdentityHub.Application.Common.Models;
using IdentityHub.Application.Common.Options;
using IdentityHub.Application.Common.Results;
using IdentityHub.Domain.Entities;
using Microsoft.Extensions.Options;

namespace IdentityHub.Application.Features.RefreshTokens;

public sealed class RefreshTokenUseCase
{
    private readonly IIdentityService _identityService;
    private readonly ITokenService _tokenService;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly AuthenticationOptions _authenticationOptions;
    private readonly TimeProvider _timeProvider;

    public RefreshTokenUseCase(IIdentityService identityService, 
        ITokenService tokenService, 
        IRefreshTokenRepository refreshTokenRepository,
        TimeProvider timeProvider,
        IOptions<AuthenticationOptions> options
        )
    {
        _identityService = identityService;
        _tokenService = tokenService;
        _refreshTokenRepository = refreshTokenRepository;
        _authenticationOptions = options.Value;
        _timeProvider = timeProvider;
    }

    public async Task<Result<AuthenticationResult>> HandleAsync(string refreshTokenValue, CancellationToken ct)
    {
        var utcNow = _timeProvider.GetUtcNow().UtcDateTime;
        
        var refreshToken = await _refreshTokenRepository.GetByTokenAsync(refreshTokenValue, ct);
        if (refreshToken is null)
            return Result<AuthenticationResult>.Failure(TokenErrors.InvalidRefreshToken);
        
        var slidingExpiry = utcNow.AddDays(_authenticationOptions.RefreshTokenLifetimeDays);
        var absoluteExpiry = refreshToken.SessionStartedAt.AddDays(_authenticationOptions.AbsoluteRefreshTokenLifetimeDays);
        
        var expiresAt = slidingExpiry < absoluteExpiry
            ? slidingExpiry
            : absoluteExpiry;
        
        if (expiresAt <= utcNow)
            return Result<AuthenticationResult>.Failure(TokenErrors.InvalidRefreshToken);
        
        // Reuse detected
        if (refreshToken.IsRevoked)
        {
            var activeTokens = await _refreshTokenRepository.GetActiveByUserIdAsync(
                refreshToken.UserId,
                utcNow,
                ct);
            foreach (var activeToken in activeTokens)
            {
                activeToken.Revoke(
                    revokedAt: utcNow);
            }
            // Persist changes on failure path as an alarm response
            await _refreshTokenRepository.SaveChangesAsync(ct);

            return Result<AuthenticationResult>.Failure(TokenErrors.InvalidRefreshToken);
        }

        if (refreshToken.IsExpiredAt(utcNow))
        {
            return Result<AuthenticationResult>.Failure(TokenErrors.InvalidRefreshToken);
        }
        
        var emailResult = await _identityService.GetEmailAsync(refreshToken.UserId);
        
        if (emailResult.IsFailure)
            return Result<AuthenticationResult>.Failure(TokenErrors.InvalidRefreshToken);
        
        var roles = await _identityService.GetRolesAsync(refreshToken.UserId);

        var newAccessToken = _tokenService.CreateAccessToken(refreshToken.UserId, emailResult.Value, roles);
        var newRefreshTokenValue = _tokenService.GenerateRefreshTokenValue();
        
        var accessTokenExpiresIn =
            _authenticationOptions.AccessTokenLifetimeMinutes * 60L;
        
        var refreshTokenExpiresIn = 
            (long)Math.Floor((expiresAt - utcNow).TotalSeconds);
        
        var newRefreshToken = RefreshToken.Issue(
            userId: refreshToken.UserId,
            refreshTokenValue: newRefreshTokenValue,
            expiresAt: expiresAt,
            sessionStartedAt: refreshToken.SessionStartedAt,
            issuedAt: utcNow);

        refreshToken.Revoke(
            revokedAt: utcNow, 
            newRefreshToken.Token);
        
        await _refreshTokenRepository.AddAsync(newRefreshToken, ct);
        await _refreshTokenRepository.SaveChangesAsync(ct);
        
        return Result<AuthenticationResult>.Success(new AuthenticationResult
        (
            AccessToken: newAccessToken,
            RefreshToken: newRefreshToken.Token,
            AccessTokenExpiresIn: accessTokenExpiresIn,
            RefreshTokenExpiresIn: refreshTokenExpiresIn
        ));
    }
}