using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using IdentityHub.API.Endpoints.Auth;
using IdentityHub.API.Tests.Helpers;

namespace IdentityHub.API.Tests;

[Collection("API integration tests")]
public sealed class AuthEndpointsTests
{
    private readonly HttpClient _client;
    
    public AuthEndpointsTests(IdentityHubWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }
    
    [Fact]
    public async Task Refresh_WithValidRefreshToken_ShouldReturnNewTokenPair()
    {
        var user = await AuthTestHelper.CreateAuthenticatedUserAsync(_client);
        
        // Refresh token
        var refreshToken = user.RefreshToken;
        var refreshPayload = new { refreshToken };
        var refreshResponse = await _client.PostAsJsonAsync(
            "auth/refresh",
            refreshPayload);
        
        Assert.Equal(HttpStatusCode.OK, refreshResponse.StatusCode);
        var refreshTokenData = await refreshResponse.Content.ReadFromJsonAsync<TokenResponse>();
        Assert.NotNull(refreshTokenData);
        Assert.False(string.IsNullOrWhiteSpace(refreshTokenData.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(refreshTokenData.RefreshToken));
        Assert.NotEqual(user.AccessToken, refreshTokenData.AccessToken);
        Assert.NotEqual(refreshToken, refreshTokenData.RefreshToken);
    }

    [Fact]
    public async Task Refresh_WithReusedRefreshToken_ShouldReturnUnauthorized()
    {
        var user = await AuthTestHelper.CreateAuthenticatedUserAsync(_client);
        
        // First refresh attempt
        var originalRefreshToken = user.RefreshToken;
        var refreshPayload = new { refreshToken = originalRefreshToken };
        var rotationResponse = await _client.PostAsJsonAsync(
            "auth/refresh",
            refreshPayload);
        
        Assert.Equal(HttpStatusCode.OK, rotationResponse.StatusCode);
        
        // Reuse the original refresh token (should fail)
        var reuseResponse = await _client.PostAsJsonAsync(
            "auth/refresh",
            refreshPayload);
        
        Assert.Equal(HttpStatusCode.Unauthorized, reuseResponse.StatusCode);
    }

    [Fact]
    public async Task Refresh_AfterRefreshTokenReuse_ShouldRevokeReplacementToken()
    {
        var user = await AuthTestHelper.CreateAuthenticatedUserAsync(_client);
        
        // First refresh attempt
        var refreshPayload = new { refreshToken = user.RefreshToken };
        var rotationResponse = await _client.PostAsJsonAsync(
            "auth/refresh",
            refreshPayload);
        
        Assert.Equal(HttpStatusCode.OK, rotationResponse.StatusCode);
        
        var firstRefreshTokenData = await rotationResponse.Content.ReadFromJsonAsync<TokenResponse>();
        Assert.NotNull(firstRefreshTokenData);
        
        // Reuse the original refresh token (should fail)
        var reuseResponse = await _client.PostAsJsonAsync(
            "auth/refresh",
            refreshPayload);
        
        Assert.Equal(HttpStatusCode.Unauthorized, reuseResponse.StatusCode);
        
        // Try the replacement refresh token, which should now be revoked
        var replacementRefreshPayload = new { refreshToken = firstRefreshTokenData.RefreshToken };
        
        var replacementResponse = await _client.PostAsJsonAsync(
            "auth/refresh",
            replacementRefreshPayload);
        
        Assert.Equal(HttpStatusCode.Unauthorized, replacementResponse.StatusCode);
    }
    
    [Fact]
    public async Task Refresh_WithInvalidRefreshToken_ShouldReturnUnauthorizedWithInvalidRefreshTokenError()
    {
        var refreshPayload = new { refreshToken = "invalid-refresh-token" };
        var refreshResponse = await _client.PostAsJsonAsync(
            "auth/refresh",
            refreshPayload);
        
        Assert.Equal(HttpStatusCode.Unauthorized, refreshResponse.StatusCode);
        
        var problem =
            await refreshResponse.Content.ReadFromJsonAsync<AuthTestHelper.ApiProblemDetails>();
        
        Assert.NotNull(problem);
        Assert.Equal(401, problem.Status);
        Assert.Contains(
            "Tokens.InvalidRefreshToken",
            problem.ErrorCodes);
    }

    [Fact]
    public async Task Login_WithInvalidPassword_ShouldReturnUnauthorizedWithInvalidCredentialsError()
    {
        var email = $"user-{Guid.NewGuid()}@test.com";
        var registerPayload = new { email = email, password = AuthTestHelper.TestPassword };
        
        var registerResponse = await _client.PostAsJsonAsync(
            "auth/register",
            registerPayload);

        Assert.Equal(HttpStatusCode.Created, registerResponse.StatusCode);
        
        // Attempt login with invalid password
        var loginPayload = new { email = email, password = "invalid_password" };

        var loginResponse = await _client.PostAsJsonAsync(
            "auth/login",
            loginPayload);

        Assert.Equal(HttpStatusCode.Unauthorized, loginResponse.StatusCode);
        
        var problem =
            await loginResponse.Content
                .ReadFromJsonAsync<AuthTestHelper.ApiProblemDetails>();

        Assert.NotNull(problem);

        Assert.Equal(401, problem.Status);
        Assert.Equal(
            "The supplied credentials are invalid.",
            problem.Detail);

        Assert.Contains(
            "Users.InvalidCredentials",
            problem.ErrorCodes);
    }

    [Fact]
    public async Task Login_WithUnknownEmail_ShouldReturnUnauthorized()
    {
        var email = $"unknown-{Guid.NewGuid()}@test.com";
        
        var loginPayload = new { email = email, password = AuthTestHelper.TestPassword };
        
        var loginResponse = await _client.PostAsJsonAsync(
            "auth/login",
            loginPayload);

        Assert.Equal(HttpStatusCode.Unauthorized, loginResponse.StatusCode);
    }
    
    [Fact]
    public async Task Login_WithValidCredentials_ShouldReturnTokenPair()
    {
        // Register test user
        var email = $"user-{Guid.NewGuid()}@test.com";
        var registerPayload = new { email = email, password = AuthTestHelper.TestPassword };
        
        var registerResponse = await _client.PostAsJsonAsync(
            "auth/register",
            registerPayload);

        Assert.Equal(HttpStatusCode.Created, registerResponse.StatusCode);
        
        // Login to get the token pair
        var loginPayload = new { email = email, password = AuthTestHelper.TestPassword };

        var loginResponse = await _client.PostAsJsonAsync(
            "auth/login",
            loginPayload);

        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

        var loginTokenData = await loginResponse.Content.ReadFromJsonAsync<TokenResponse>();
        Assert.NotNull(loginTokenData);
        Assert.False(string.IsNullOrWhiteSpace(loginTokenData.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(loginTokenData.RefreshToken));
    }

    [Fact]
    public async Task Register_WithValidCredentials_ShouldReturnCreated()
    {
        var email = $"user-{Guid.NewGuid()}@test.com";
        var registerPayload = new { email = email, password = AuthTestHelper.TestPassword };
        
        var registerResponse = await _client.PostAsJsonAsync(
            "auth/register",
            registerPayload);

        Assert.Equal(HttpStatusCode.Created, registerResponse.StatusCode);
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_ShouldReturnBadRequest()
    {
        var email = $"user-{Guid.NewGuid()}@test.com";
        var registerPayload = new { email = email, password = AuthTestHelper.TestPassword };
        
        var registerResponse = await _client.PostAsJsonAsync(
            "auth/register",
            registerPayload);

        Assert.Equal(HttpStatusCode.Created, registerResponse.StatusCode);
        
        var duplicateRegisterResponse = await _client.PostAsJsonAsync(
            "auth/register",
            registerPayload);
        // Registration failures use the validation-problem contract.
        Assert.Equal(HttpStatusCode.BadRequest, duplicateRegisterResponse.StatusCode);
    }

    [Fact]
    public async Task Register_WithWeakPassword_ShouldReturnBadRequest()
    {
        var email = $"user-{Guid.NewGuid()}@test.com";
        var registerPayload = new { email = email, password = "password" };
        
        var registerResponse = await _client.PostAsJsonAsync(
            "auth/register",
            registerPayload);

        Assert.Equal(HttpStatusCode.BadRequest, registerResponse.StatusCode);
    }

    [Fact]
    public async Task Logout_WithValidRefreshToken_ShouldRevokeRefreshToken()
    {
        var user = await AuthTestHelper.CreateAuthenticatedUserAsync(_client);

        var requestPayload = new { refreshToken = user.RefreshToken };
        var logoutResponse = await _client.PostAsJsonAsync(
            "auth/logout",
            requestPayload);

        Assert.Equal(HttpStatusCode.NoContent, logoutResponse.StatusCode);
        
        var refreshResponse = await _client.PostAsJsonAsync(
            "auth/refresh",
            requestPayload);
        
        Assert.Equal(HttpStatusCode.Unauthorized, refreshResponse.StatusCode);
    }

    [Fact]
    public async Task Logout_WithAlreadyRevokedRefreshToken_ShouldReturnNoContent()
    {
        var user = await AuthTestHelper.CreateAuthenticatedUserAsync(_client);
        var refreshToken = user.RefreshToken;
        
        var requestPayload = new { refreshToken };

        var logoutResponse = await _client.PostAsJsonAsync(
            "auth/logout",
            requestPayload);
        
        Assert.Equal(HttpStatusCode.NoContent, logoutResponse.StatusCode);
        
        var repeatedLogoutResponse = await _client.PostAsJsonAsync(
            "auth/logout",
            requestPayload);

        Assert.Equal(HttpStatusCode.NoContent, repeatedLogoutResponse.StatusCode);
    }

    [Fact]
    public async Task Logout_WithUnknownRefreshToken_ShouldReturnNoContent()
    {
        var requestPayload = new { refreshToken = "i-do-not-exist" };
        var logoutResponse = await _client.PostAsJsonAsync(
            "auth/logout",
            requestPayload);

        Assert.Equal(HttpStatusCode.NoContent, logoutResponse.StatusCode);
    }

    [Fact]
    public async Task LogoutAll_WithoutAccessToken_ShouldReturnUnauthorized()
    {
        var request = new HttpRequestMessage(
            HttpMethod.Post, 
            "auth/logout-all");
        
        var response = await _client.SendAsync(request);
        
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task LogoutAll_WithValidAccessToken_ShouldRevokeAllRefreshTokens()
    {
        // Register new test user
        var email = $"user-{Guid.NewGuid()}@test.com";
        var registerPayload = new { email, password = AuthTestHelper.TestPassword };
        
        var registerResponse = await _client.PostAsJsonAsync(
            "auth/register",
            registerPayload);

        Assert.Equal(HttpStatusCode.Created, registerResponse.StatusCode);
        
        // Login to get the first token
        var firstLoginPayload = new { email, password = AuthTestHelper.TestPassword };
        var firstLoginResponse = await _client.PostAsJsonAsync(
            "auth/login",
            firstLoginPayload);
        Assert.Equal(HttpStatusCode.OK, firstLoginResponse.StatusCode);
        
        // Login to get the second token
        var secondLoginPayload = new { email, password = AuthTestHelper.TestPassword };
        var secondLoginResponse = await _client.PostAsJsonAsync(
            "auth/login",
            secondLoginPayload);
        Assert.Equal(HttpStatusCode.OK, secondLoginResponse.StatusCode);
        
        // Get token data for both logins
        var firstLoginTokenData = await firstLoginResponse.Content.ReadFromJsonAsync<TokenResponse>();
        Assert.NotNull(firstLoginTokenData);
        var secondLoginTokenData = await secondLoginResponse.Content.ReadFromJsonAsync<TokenResponse>();
        Assert.NotNull(secondLoginTokenData);
        Assert.NotEqual(
            firstLoginTokenData.RefreshToken,
            secondLoginTokenData.RefreshToken);
        
        // Logout all sessions
        var request = new HttpRequestMessage(
            HttpMethod.Post, 
            "auth/logout-all");
        
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", firstLoginTokenData.AccessToken);
        
        var logoutAllResponse = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.NoContent, logoutAllResponse.StatusCode);
 
        
        var firstRefreshPayload = new
        {
            refreshToken = firstLoginTokenData.RefreshToken
        };

        var secondRefreshPayload = new
        {
            refreshToken = secondLoginTokenData.RefreshToken
        };
        
        // Try refresh with refreshToken from first login
        var firstRefreshResponse = await _client.PostAsJsonAsync(
            "auth/refresh",
            firstRefreshPayload);
        
        Assert.Equal(HttpStatusCode.Unauthorized, firstRefreshResponse.StatusCode);

        // Try refresh with refreshToken from second login
        var secondRefreshResponse = await _client.PostAsJsonAsync(
            "auth/refresh",
            secondRefreshPayload);
        
        Assert.Equal(HttpStatusCode.Unauthorized, secondRefreshResponse.StatusCode);

    }
}