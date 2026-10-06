using IdentityHub.Application.Common.Interfaces;
using IdentityHub.Application.Common.Results;

namespace IdentityHub.Application.Features.Logout;

public sealed class LogoutUserUseCase
{
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly TimeProvider _timeProvider;

    public LogoutUserUseCase(
        IRefreshTokenRepository refreshTokenRepository,
        TimeProvider timeProvider)
    {
        _refreshTokenRepository = refreshTokenRepository;
        _timeProvider = timeProvider;
    }

    public async Task HandleAsync(string refreshTokenValue, CancellationToken ct)
    {
        var refreshToken = await _refreshTokenRepository.GetByTokenAsync(refreshTokenValue, ct);
        if (refreshToken is null)
            return;
        
        if (refreshToken.IsRevoked)
            return;

        refreshToken.Revoke(_timeProvider.GetUtcNow().UtcDateTime);
        await _refreshTokenRepository.SaveChangesAsync(ct);
    }
}