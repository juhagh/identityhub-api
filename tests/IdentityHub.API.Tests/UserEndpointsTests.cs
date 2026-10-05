using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using IdentityHub.API.Endpoints.Users;
using IdentityHub.API.Tests.Helpers;

namespace IdentityHub.API.Tests;

[Collection("API integration tests")]
public sealed class UserEndpointsTests
{
    private readonly HttpClient _client;

    public UserEndpointsTests(IdentityHubWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }
    
    [Fact]
    public async Task GetMe_WithoutAccessToken_ShouldReturnUnauthorized()
    {
        var result = await _client.GetAsync("users/me");
        Assert.Equal(HttpStatusCode.Unauthorized, result.StatusCode);
    }

    [Fact]
    public async Task GetMe_WithInvalidAccessToken_ShouldReturnUnauthorized()
    {
        var request = new HttpRequestMessage(
            HttpMethod.Get, 
            "users/me");

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "invalid_token");
        
        var response = await _client.SendAsync(request);
        
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
    
    [Fact]
    public async Task GetMe_WithValidAccessToken_ShouldReturnCurrentUser()
    {
        var user = await AuthTestHelper.CreateAuthenticatedUserAsync(_client);
        
        var request = new HttpRequestMessage(
            HttpMethod.Get, 
            "users/me");
    
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", user.AccessToken);
        
        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    
        var meResponse = await response.Content.ReadFromJsonAsync<MeResponse>();
        Assert.NotNull(meResponse);
        Assert.Equal(user.Email, meResponse.Email);
        Assert.NotEqual(Guid.Empty, meResponse.UserId);
        Assert.Contains("User", meResponse.Roles);
    }
}