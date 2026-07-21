namespace IdentityHub.Domain.Entities;

public sealed class RefreshToken
{
    public required Guid Id { get; init; }
    public required Guid UserId { get; init; }
    public required string Token { get; init; }
    public required DateTime CreatedAt { get; init; }
    public required DateTime ExpiresAt { get; init; }
    public required DateTime SessionStartedAt { get; init; }
    
    public string? ReplacedByToken { get; private set; }
    public DateTime? RevokedAt { get; private set; }
    
    public bool IsExpiredAt(DateTime utcNow)
    {
        return ExpiresAt <= utcNow;
    }
    
    public bool IsRevoked => RevokedAt is not null;
    public bool IsActiveAt(DateTime utcNow)
    {
        return !IsExpiredAt(utcNow) && !IsRevoked;
    }

    private RefreshToken() { }
    
    public static RefreshToken Issue(
        Guid userId, 
        string refreshTokenValue, 
        DateTime expiresAt, 
        DateTime sessionStartedAt, 
        DateTime issuedAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(refreshTokenValue);
        
        if (userId == Guid.Empty)
            throw new ArgumentException(
                "userId cannot be empty.",
                nameof(userId));
        
        if (issuedAt == default)
        {
            throw new ArgumentOutOfRangeException(
                nameof(issuedAt),
                "Issue date cannot be the default value.");
        }

        if (expiresAt <= issuedAt)
        {
            throw new ArgumentOutOfRangeException(
                nameof(expiresAt),
                "Expiry date must be later than the issue date.");
        }
        
        if (sessionStartedAt == default || sessionStartedAt > issuedAt)
        {
            throw new ArgumentOutOfRangeException(
                nameof(sessionStartedAt),
                "Date cannot be default or in the future.");
        }
        
        
        return new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Token = refreshTokenValue,
            CreatedAt = issuedAt,
            ExpiresAt = expiresAt,
            SessionStartedAt = sessionStartedAt
        };
    }

    public void Revoke(DateTime revokedAt, string? replacedByToken = null)
    {
        // Idempotent: revoking an already-revoked token is a no-op, so bulk
        // revocation (reuse-detection response) never throws mid-loop.
        // Reuse detection is the caller's job, it checks IsRevoked before acting.
        if (IsRevoked) return;
        
        if (revokedAt == default || revokedAt < CreatedAt)
        {
            throw new ArgumentOutOfRangeException(
                nameof(revokedAt),
                "Revocation date cannot be default or earlier than the issue date.");
        }
        
        ReplacedByToken = replacedByToken;
        RevokedAt = revokedAt;
    }

}