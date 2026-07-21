using IdentityHub.Application.Common.Errors;
using IdentityHub.Application.Common.Interfaces;
using IdentityHub.Application.Common.Options;
using IdentityHub.Application.Common.Results;
using IdentityHub.Application.Features.Login;
using IdentityHub.Domain.Entities;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace IdentityHub.Application.Tests;

public sealed class LoginUserUseCaseTests
{
    private readonly IIdentityService _identityService;
    private readonly ITokenService _tokenService;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly LoginUserUseCase _sut;
    
    private static readonly DateTimeOffset FixedUtcNow =
        new(2026, 7, 21, 12, 0, 0, TimeSpan.Zero);
    
    public LoginUserUseCaseTests()
    {
        _identityService = Substitute.For<IIdentityService>();
        _tokenService = Substitute.For<ITokenService>();
        _refreshTokenRepository = Substitute.For<IRefreshTokenRepository>();
        
        var timeProvider = Substitute.For<TimeProvider>();
        
        timeProvider
            .GetUtcNow()
            .Returns(FixedUtcNow);
        
        var options = Options.Create(new AuthenticationOptions
        {
            AccessTokenLifetimeMinutes = 15,
            RefreshTokenLifetimeDays = 7
        });

        _sut = new LoginUserUseCase(
            _identityService,
            _tokenService,
            _refreshTokenRepository,
            timeProvider,
            options);
    }

    [Fact]
    public async Task LoginAsync_ShouldReturnTokens_WhenCredentialsAreValid()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var email = "user@example.com";
        var password = "Password123!";
        var roles = new List<string> { "User", "Admin" };

        _identityService
            .ValidateCredentialsAsync(email, password)
            .Returns(Result<Guid>.Success(userId));

        _identityService
            .GetRolesAsync(userId)
            .Returns(roles);

        _tokenService
            .CreateAccessToken(userId, email, roles)
            .Returns("access-token");

        _tokenService
            .GenerateRefreshTokenValue()
            .Returns("refresh-token");
        
        // Act
        var result = await _sut.LoginAsync(email, password, CancellationToken.None);
        
        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("access-token", result.Value.AccessToken);
        Assert.Equal("refresh-token", result.Value.RefreshToken);
        
        await _refreshTokenRepository
            .Received(1)
            .AddAsync(
                Arg.Is<RefreshToken>(rt =>
                    rt!.UserId == userId),
                Arg.Any<CancellationToken>());
        
        await _refreshTokenRepository
            .Received(1)
            .SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task LoginAsync_ShouldNotPersistTokens_WhenCredentialsAreInvalid()
    {
        // Arrange
        var email = "user@example.com";
        var password = "Password123!";

        _identityService
            .ValidateCredentialsAsync(email, password)
            .Returns(Result<Guid>.Failure(UserErrors.InvalidCredentials));

        var result = await _sut.LoginAsync(email, password, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Contains(UserErrors.InvalidCredentials, result.Errors);

        await _refreshTokenRepository.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
        await _refreshTokenRepository.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }
}