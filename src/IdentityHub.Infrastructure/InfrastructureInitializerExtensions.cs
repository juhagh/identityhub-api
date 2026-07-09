using IdentityHub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace IdentityHub.Infrastructure;

public static class InfrastructureInitializerExtensions
{
    public static async Task InitializeInfrastructureAsync(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        await db.Database.MigrateAsync();
        await RoleSeeder.SeedRolesAsync(scope.ServiceProvider);
    }
}