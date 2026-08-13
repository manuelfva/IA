using Microsoft.Extensions.DependencyInjection;
using TestIA.Application;
using TestIA.Domain;

namespace TestIA;

/// <summary>
/// Extension methods for registering Test-IA services in the dependency injection container.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers all Test-IA services with the dependency injection container.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddTestIAServices(this IServiceCollection services)
    {
        services.AddScoped<ADDomainDiscoveryService>();
        services.AddScoped<IGetADUserInfo, GetADUserInfoService>();
        services.AddScoped<IGetADGroupInfo, GetADGroupInfoService>();

        return services;
    }
}
