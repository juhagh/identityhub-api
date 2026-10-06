using IdentityHub.Application.Common.Interfaces;
using IdentityHub.Application.Features.LogoutAll;
using IdentityHub.Domain.Entities;
using NSubstitute;

namespace IdentityHub.Application.Tests;

public sealed class LogoutAllSessionsUseCaseTests
{
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly LogoutAllSessionsUseCase _sut;

    private static readonly DateTimeOffset FixedUtcNow =
        new(2026, 10, 05, 15, 0, 0, TimeSpan.Zero);

    public LogoutAllSessionsUseCaseTests()
    {
        _refreshTokenRepository = Substitute.For<IRefreshTokenRepository>();
        var timeProvider = Substitute.For<TimeProvider>();

        timeProvider
            .GetUtcNow()
            .Returns(FixedUtcNow);

        _sut = new LogoutAllSessionsUseCase(_refreshTokenRepository, timeProvider);
    }
    
    [Fact]
    public async Task HandleAsync_WhenUserHasOneActiveToken_ShouldRevokeTokenAndSave()
    {
        var userId = Guid.NewGuid();
        var utcNow = FixedUtcNow.UtcDateTime;
        var sessionStartedAt = utcNow.AddDays(-2);
        var issuedAt = utcNow.AddDays(-1);
        var refreshTokenValue = "refresh-token-value";

        var refreshToken = RefreshToken.Issue(
            userId: userId,
            refreshTokenValue: refreshTokenValue,
            expiresAt: utcNow.AddDays(7),
            sessionStartedAt: sessionStartedAt,
            issuedAt: issuedAt);

        _refreshTokenRepository
            .GetActiveByUserIdAsync(
                userId,
                utcNow,
                Arg.Any<CancellationToken>())
            .Returns(new List<RefreshToken> { refreshToken });

        await _sut.HandleAsync(userId, CancellationToken.None);
        Assert.True(refreshToken.IsRevoked);
        Assert.Equal(utcNow, refreshToken.RevokedAt);

        await _refreshTokenRepository
            .Received(1)
            .GetActiveByUserIdAsync(
                userId,
                utcNow,
                Arg.Any<CancellationToken>());

        await _refreshTokenRepository.Received(1).SaveChangesAsync(
            Arg.Any<CancellationToken>());
    }
    
    [Fact]
    public async Task HandleAsync_WhenUserHasMultipleActiveTokens_ShouldRevokeAllTokensAndSaveOnce()
    {
        var userId = Guid.NewGuid();
        var utcNow = FixedUtcNow.UtcDateTime;
        var firstSessionStartedAt = utcNow.AddDays(-2);
        var secondSessionStartedAt = utcNow.AddDays(-2);
        var firstIssuedAt = utcNow.AddDays(-1);
        var secondIssuedAt = utcNow.AddHours(-1);
        var firstRefreshTokenValue = "first-refresh-token-value";
        var secondRefreshTokenValue = "second-refresh-token-value";
        
        var firstRefreshToken = RefreshToken.Issue(
            userId: userId,
            refreshTokenValue: firstRefreshTokenValue,
            expiresAt: utcNow.AddDays(7),
            sessionStartedAt: firstSessionStartedAt,
            issuedAt: firstIssuedAt);
        
        var secondRefreshToken = RefreshToken.Issue(
            userId: userId,
            refreshTokenValue: secondRefreshTokenValue,
            expiresAt: utcNow.AddDays(7),
            sessionStartedAt: secondSessionStartedAt,
            issuedAt: secondIssuedAt);
        
        _refreshTokenRepository
            .GetActiveByUserIdAsync(
                userId,
                utcNow,
                Arg.Any<CancellationToken>())
            .Returns([firstRefreshToken, secondRefreshToken]);
        
        await _sut.HandleAsync(userId, CancellationToken.None);
        Assert.True(firstRefreshToken.IsRevoked);
        Assert.True(secondRefreshToken.IsRevoked);
        Assert.Equal(utcNow, firstRefreshToken.RevokedAt);
        Assert.Equal(utcNow, secondRefreshToken.RevokedAt);
        
        await _refreshTokenRepository
            .Received(1)
            .GetActiveByUserIdAsync(
                userId,
                utcNow,
                Arg.Any<CancellationToken>());
        
        await _refreshTokenRepository.Received(1).SaveChangesAsync(
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WhenUserHasNoActiveTokens_ShouldNotSave()
    {
        var userId = Guid.NewGuid();
        var utcNow = FixedUtcNow.UtcDateTime;
        
        _refreshTokenRepository
            .GetActiveByUserIdAsync(
                userId,
                utcNow,
                Arg.Any<CancellationToken>())
            .Returns([]);
        
        await _sut.HandleAsync(userId, CancellationToken.None);
        
        await _refreshTokenRepository
            .Received(1)
            .GetActiveByUserIdAsync(
                userId,
                utcNow,
                Arg.Any<CancellationToken>());
        
        await _refreshTokenRepository
            .DidNotReceive()
            .SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}