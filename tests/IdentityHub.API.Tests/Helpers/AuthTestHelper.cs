using System.Net;
using System.Net.Http.Json;
using IdentityHub.API.Endpoints.Auth;

namespace IdentityHub.API.Tests.Helpers;

internal static class AuthTestHelper
{
    public const string TestPassword = "1r3ally$1llyP@assw0rd";
    
    public static async Task<AuthenticatedTestUser> CreateAuthenticatedUserAsync(HttpClient client)
    {
        // Register test user
        var email = $"user-{Guid.NewGuid()}@test.com";
        var registerPayload = new { email = email, password = TestPassword };
        
        var registerResponse = await client.PostAsJsonAsync(
            "auth/register",
            registerPayload);

        Assert.Equal(HttpStatusCode.Created, registerResponse.StatusCode);
        
        // Login to get the token
        var loginPayload = new { email = email, password = TestPassword };

        var loginResponse = await client.PostAsJsonAsync(
            "auth/login",
            loginPayload);

        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

        var loginTokenData = await loginResponse.Content.ReadFromJsonAsync<TokenResponse>();
        Assert.NotNull(loginTokenData);

        return new AuthenticatedTestUser(
            email, 
            loginTokenData.AccessToken, 
            loginTokenData.RefreshToken);
    }

    internal sealed record ApiProblemDetails(
        string? Detail,
        int? Status,
        string[] ErrorCodes);
    
    internal sealed record AuthenticatedTestUser(
        string Email,
        string AccessToken,
        string RefreshToken);
}