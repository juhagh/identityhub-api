using IdentityHub.Application.Common.Interfaces;

namespace IdentityHub.Application.Features.LogoutAll;

public sealed class LogoutAllSessionsUseCase 
{
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly TimeProvider _timeProvider;

    public LogoutAllSessionsUseCase(
        IRefreshTokenRepository refreshTokenRepository,
        TimeProvider timeProvider)
    {
        _refreshTokenRepository = refreshTokenRepository;
        _timeProvider = timeProvider;
    }
    
    public async Task HandleAsync(Guid userId, CancellationToken ct)
    {
        var utcNow = _timeProvider.GetUtcNow().UtcDateTime;
        
        var activeRefreshTokens = 
            await _refreshTokenRepository.GetActiveByUserIdAsync(userId, utcNow, ct);
        
        if (activeRefreshTokens.Count is 0)
            return;

        foreach (var token in activeRefreshTokens)
        {
            token.Revoke(utcNow);
        }
        
        await _refreshTokenRepository.SaveChangesAsync(ct);
    }
}