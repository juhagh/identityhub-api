namespace IdentityHub.Domain.Entities;

public sealed class RefreshToken
{
    public required Guid Id { get; init; }
    public required Guid UserId { get; init; }
    public required string Token { get; init; }
    public required DateTime CreatedAt { get; init; }
    public required DateTime ExpiresAt { get; init; }
    
    public string? ReplacedByToken { get; private set; }
    public DateTime? RevokedAt { get; private set; }
    
    public bool IsExpired => ExpiresAt <= DateTime.UtcNow;
    public bool IsRevoked => RevokedAt != null;
    public bool IsActive => !IsExpired && !IsRevoked;

    private RefreshToken() { }
    
    public static RefreshToken Issue(Guid userId, string token, DateTime expiresAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(token);
        
        if (userId == Guid.Empty)
            throw new ArgumentException("userId cannot be empty.", nameof(userId));

        if (expiresAt <= DateTime.UtcNow)
            throw new ArgumentOutOfRangeException(nameof(expiresAt), "Date must be in the future.");
            
        return new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Token = token,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = expiresAt,
        };
    }

    public void Revoke(string? replacedByToken = null)
    {
        if (IsRevoked) return;
        ReplacedByToken = replacedByToken;
        RevokedAt = DateTime.UtcNow;
    }

}