using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using TestIA.Application;
using TestIA.Domain;

namespace TestIA;

/// <summary>
/// Provides extension methods for registering all Test-IA services in the dependency injection container.
/// <para>
/// This class serves as a single entry point for service registration. Call
/// <c>services.AddTestIAServices()</c> from <c>Program.cs</c> to register every service
/// used by the application with the appropriate lifetime.
/// </para>
/// </summary>
/// <remarks>
/// <b>Service lifetimes:</b> All services are registered as <c>Scoped</c>, meaning a new
/// instance is created per DI scope (e.g., per HTTP request in the WebApp, or per operation
/// in the ConsoleApp). This ensures thread safety and prevents shared state between operations.
/// </remarks>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers all Test-IA services with the dependency injection container.
    /// <para>
    /// This method adds the following services to the container:
    /// <list type="bullet">
    ///   <item><description><see cref="ADDomainDiscoveryService"/> — Dynamic AD environment discovery.</description></item>
    ///   <item><description><see cref="IAttributeMapper&lt;UserDto&gt;"/> / <see cref="UserAttributeMapper"/> — User attribute mapping.</description></item>
    ///   <item><description><see cref="IAttributeMapper&lt;GroupDto&gt;"/> / <see cref="GroupAttributeMapper"/> — Group attribute mapping.</description></item>
    ///   <item><description><see cref="Domain.IGetADUserInfo"/> / <see cref="GetADUserInfoService"/> — User lookup service.</description></item>
    ///   <item><description><see cref="Domain.IGetADGroupInfo"/> / <see cref="GetADGroupInfoService"/> — Group lookup service.</description></item>
    ///   <item><description><see cref="Domain.IUserGroupAuthorizationService"/> / <see cref="UserGroupAuthorizationService"/> — Group membership authorization.</description></item>
    ///   <item><description><see cref="Domain.IUserWriter"/> / <see cref="UserWriterService"/> — User attribute update service.</description></item>
    /// </list>
    /// </para>
    /// </summary>
    /// <param name="services">
    /// The <see cref="IServiceCollection"/> to add services to. Must not be null.
    /// </param>
    /// <returns>
    /// The <see cref="IServiceCollection"/> for method chaining.
    /// </returns>
    /// <example>
    /// <c>builder.Services.AddTestIAServices();</c>
    /// </example>
    public static IServiceCollection AddTestIAServices(this IServiceCollection services)
    {
        // Register the AD discovery service as scoped.
        // Each scope gets its own discovery instance.
        services.AddScoped<ADDomainDiscoveryService>();

        // Register the user attribute mapper, mapped to its interface.
        services.AddScoped<IAttributeMapper<UserDto>, UserAttributeMapper>();

        // Register the group attribute mapper, mapped to its interface.
        services.AddScoped<IAttributeMapper<GroupDto>, GroupAttributeMapper>();

        // Register the user lookup service, mapped to its interface.
        services.AddScoped<IGetADUserInfo, GetADUserInfoService>();

        // Register the group lookup service, mapped to its interface.
        services.AddScoped<IGetADGroupInfo, GetADGroupInfoService>();

        // Register the group authorization service, mapped to its interface.
        services.AddScoped<IUserGroupAuthorizationService, UserGroupAuthorizationService>();

        // Register the user writer service, mapped to its interface.
        services.AddScoped<IUserWriter, UserWriterService>();

        // Register the group membership writer service, mapped to its interface.
        services.AddScoped<IGroupMembershipWriter, GroupMembershipWriterService>();

        // Register the user update attribute mapper, mapped to its interface.
        services.AddScoped<IAttributeMapper<UserUpdateRequest>, UserUpdateAttributeMapper>();

        // Return the service collection for chaining.
        return services;
    }
}
