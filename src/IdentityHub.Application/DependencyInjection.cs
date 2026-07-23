using IdentityHub.Application.Common.Options;
using IdentityHub.Application.Features.Login;
using IdentityHub.Application.Features.RefreshTokens;
using IdentityHub.Application.Features.Register;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace IdentityHub.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(
        this IServiceCollection services)
    {
        services.AddScoped<RegisterUserUseCase>();
        services.AddScoped<LoginUserUseCase>();
        services.AddScoped<RefreshTokenUseCase>();
        
        services.AddOptions<AuthenticationOptions>()
            .BindConfiguration(AuthenticationOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();
        
        services.TryAddSingleton(TimeProvider.System);
        
        return services;
    }
}