using System.Security.Cryptography;
using IdentityHub.Domain.Entities;

namespace IdentityHub.Domain.Tests;

public sealed class RefreshTokenTests
{
    private static readonly DateTimeOffset FixedUtcNow =
        new(2026, 7, 21, 12, 0, 0, TimeSpan.Zero);
    
    [Fact]
    public void Issue_WithValidArguments_ShouldCreateRefreshToken()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var userId = Guid.NewGuid();
        var token = Convert.ToBase64String(
            RandomNumberGenerator.GetBytes(64));

        var expiresAt = now.AddMinutes(5);
        var sessionStartedAt = now;
        var issuedStartedAt = now;

        // Act
        var refreshToken = RefreshToken.Issue(
            userId,
            token,
            expiresAt,
            sessionStartedAt,
            issuedStartedAt);

        // Assert
        Assert.NotEqual(Guid.Empty, refreshToken.Id);
        Assert.Equal(userId, refreshToken.UserId);
        Assert.Equal(token, refreshToken.Token);
        Assert.Equal(expiresAt, refreshToken.ExpiresAt);
        Assert.Equal(sessionStartedAt, refreshToken.SessionStartedAt);

        Assert.False(refreshToken.IsRevoked);
        Assert.Null(refreshToken.RevokedAt);
        Assert.Null(refreshToken.ReplacedByToken);
        Assert.True(refreshToken.IsActiveAt(now));
    }
    
    [Fact]
    public void Issue_WithEmptyUserId_ShouldThrow()
    {
        Assert.Throws<ArgumentException>(() => CreateToken(userId: Guid.Empty));
    }
    
    [Fact]
    public void Issue_WithExpiryInPast_ShouldThrow()
    {
        var now = DateTime.UtcNow;

        Assert.Throws<ArgumentOutOfRangeException>(
            () => CreateToken(expiresAt: now.AddHours(-1)));
    }
    
    [Fact]
    public void Issue_WithSessionStartedAtInFuture_ShouldThrow()
    {
        var now = DateTime.UtcNow;
        
        Assert.Throws<ArgumentOutOfRangeException>(
            () => CreateToken(sessionStartedAt: now.AddHours(1)));
    }
    
    [Fact]
    public void Issue_WithDefaultSessionStartedAt_ShouldThrow()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var randomBytes = RandomNumberGenerator.GetBytes(64);
        var token = Convert.ToBase64String(randomBytes);
        var expiresAt = DateTime.UtcNow.AddMinutes(5);
        var issuedAt = DateTime.UtcNow;
        DateTime sessionStartedAt = default;
        
        // Act/Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => RefreshToken.Issue(userId, token, expiresAt, sessionStartedAt, issuedAt));
    }
    
    [Fact]
    public void Revoke_WithValidReplacementToken_ShouldSetRevocationDetails()
    {
        // Arrange
        var issuedAt = FixedUtcNow.UtcDateTime;
        
        var revokedAt = issuedAt.AddMinutes(1);
        var replacementToken = "replacement-token";
        
        var sessionStartedAt = issuedAt;

        var refreshToken = CreateToken(
            refreshTokenValue: replacementToken,
            sessionStartedAt: sessionStartedAt,
            expiresAt: issuedAt.AddDays(7),
            issuedAt: issuedAt);
        
        // Act
        refreshToken.Revoke(
            revokedAt: revokedAt, 
            replacedByToken: replacementToken);
        
        // Assert
        Assert.True(refreshToken.IsRevoked);
        Assert.NotNull(refreshToken.RevokedAt);
        Assert.Equal(replacementToken, refreshToken.ReplacedByToken);
        Assert.Equal(sessionStartedAt, refreshToken.SessionStartedAt);
    }
    
    [Fact]
    public void Revoke_WhenAlreadyRevoked_ShouldPreserveOriginalRevocationDetails()
    {
        // Arrange
        var utcNow = DateTime.UtcNow;
        
        var firstReplacementToken =
            Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

        var secondReplacementToken =
            Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        
        var sessionStartedAt = utcNow;
        var revokedAt = utcNow.AddMinutes(1);
        
        var refreshToken = CreateToken(sessionStartedAt: sessionStartedAt);
        
        refreshToken.Revoke(
            revokedAt: revokedAt, 
            replacedByToken: firstReplacementToken);
        
        var originalRevokedAt = refreshToken.RevokedAt;
        
        // Act
        refreshToken.Revoke(
            revokedAt: revokedAt,
            replacedByToken: secondReplacementToken);
        
        // Assert
        Assert.True(refreshToken.IsRevoked);
        Assert.Equal(originalRevokedAt, refreshToken.RevokedAt);
        Assert.Equal(firstReplacementToken, refreshToken.ReplacedByToken);
        Assert.Equal(sessionStartedAt, refreshToken.SessionStartedAt);
    }

    private static RefreshToken CreateToken(
        Guid? userId = null, 
        string? refreshTokenValue = null, 
        DateTime? expiresAt = null, 
        DateTime? sessionStartedAt = null, 
        DateTime? issuedAt = null)
    {
        var now = DateTime.UtcNow;
        
        return RefreshToken.Issue(
            userId ?? Guid.NewGuid(),
            refreshTokenValue ?? Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)),
            expiresAt ?? now.AddDays(7),
            sessionStartedAt ?? now,
            issuedAt ?? now);
    }
}