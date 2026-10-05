using IdentityHub.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace IdentityHub.API.Tests;

public class IdentityHubWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public async Task InitializeAsync()
    { 
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        context.RemoveRange(context.RefreshTokens);
        context.RemoveRange(context.UserRoles);
        context.RemoveRange(context.UserClaims);
        context.RemoveRange(context.UserLogins);
        context.RemoveRange(context.UserTokens);
        context.RemoveRange(context.Users);

        await context.SaveChangesAsync();
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await base.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<ApplicationDbContext>();
            services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseNpgsql(
                    "Host=localhost;Port=5433;Database=identityhubtest;Username=postgres;Password=postgres");
            });
        });
    }
}