using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TestIA;
using TestIA.Application;
using TestIA.Domain;
using TestIA.Logging;

/// <summary>
/// Main entry point for the Test-IA console application.
/// Demonstrates the real execution of Active Directory user and group lookup services.
/// </summary>
public class Program
{
    /// <summary>
    /// The main entry method. Discovers the Active Directory environment, registers services,
    /// and demonstrates the execution of <see cref="IGetADUserInfo"/> and <see cref="IGetADGroupInfo"/>.
    /// </summary>
    /// <param name="args">Command-line arguments (not used).</param>
    public static void Main(string[] args)
    {
        // Load configuration from appsettings.json and appsettings.Development.json
        var basePath = Path.GetDirectoryName(typeof(Program).Assembly.Location) ?? Directory.GetCurrentDirectory();
        var configuration = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true)
            .Build();

        var services = new ServiceCollection();

        // Create logger factory using configuration and console output
        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConfiguration(configuration.GetSection("Logging"));
            builder.AddConsole();
        });

        // Create logging sinks from configuration
        var sinks = LoggingPipelineFactory.CreateSinks(configuration);

        // Register logging
        services.AddSingleton<ILoggerService>(sp => new LoggingService(loggerFactory.CreateLogger<LoggingService>(), sinks));
        services.AddScoped<ICurrentUser, ConsoleCurrentUser>();
        services.AddTestIAServices();

        // Register attribute mappers as singletons (stateless, no external dependencies)
        services.AddSingleton<IAttributeMapper<UserDto>, UserAttributeMapper>();
        services.AddSingleton<IAttributeMapper<GroupDto>, GroupAttributeMapper>();

        // Register configuration-based services
        services.Configure<AuthorizationSettings>(configuration.GetSection("Authorization"));

        // Register ILogger<T> for all services that need it
        services.AddSingleton<ILogger<LoggingService>>(_ => loggerFactory.CreateLogger<LoggingService>());
        services.AddSingleton<ILogger<GetADUserInfoService>>(_ => loggerFactory.CreateLogger<GetADUserInfoService>());
        services.AddSingleton<ILogger<GetADGroupInfoService>>(_ => loggerFactory.CreateLogger<GetADGroupInfoService>());
        services.AddSingleton<ILogger<ADDomainDiscoveryService>>(_ => loggerFactory.CreateLogger<ADDomainDiscoveryService>());
        services.AddSingleton<ILogger<UserGroupAuthorizationService>>(_ => loggerFactory.CreateLogger<UserGroupAuthorizationService>());

        // Build the service provider
        using var serviceProvider = services.BuildServiceProvider();

        var loggerService = serviceProvider.GetRequiredService<ILoggerService>();
        var userMapper = serviceProvider.GetRequiredService<IAttributeMapper<UserDto>>();
        var groupMapper = serviceProvider.GetRequiredService<IAttributeMapper<GroupDto>>();
        var userInfoService = serviceProvider.GetRequiredService<IGetADUserInfo>();
        var groupInfoService = serviceProvider.GetRequiredService<IGetADGroupInfo>();
        var authorizationService = serviceProvider.GetRequiredService<IUserGroupAuthorizationService>();
        var authorizationSettings = serviceProvider.GetRequiredService<IOptions<AuthorizationSettings>>();

        // Check authorization before proceeding
        var requiredGroup = authorizationSettings.Value.RequiredGroup;
        if (string.IsNullOrWhiteSpace(requiredGroup))
        {
            loggerService.LogError("Authorization configuration is missing: 'Authorization:RequiredGroup' is not set in appsettings.json.");
            return;
        }

        try
        {
            // Check if the current user is a member of the group
            // (VerifyGroupExists is called internally and throws MissingGroupException if group is not found)
            if (!authorizationService.IsMemberOfGroup(requiredGroup))
            {
                loggerService.LogError("Access denied: Current user is not a member of the '{GroupName}' group. Application will exit.", requiredGroup);
                return;
            }

            loggerService.LogInformation("Authorization successful: Current user is a member of the '{GroupName}' group.", requiredGroup);
        }
        catch (MissingGroupException ex)
        {
            loggerService.LogError(ex, "Authorization group missing: {Message}", ex.Message);
            return;
        }
        catch (AccessDeniedException ex)
        {
            loggerService.LogError(ex, "Authorization check failed: {Message}", ex.Message);
            return;
        }
        catch (DomainException ex)
        {
            loggerService.LogError(ex, "Error during authorization check: {Message}", ex.Message);
            return;
        }

        loggerService.LogInformation("=== Test-IA Console Application ===");
        loggerService.LogInformation("Starting Active Directory service demonstration...");

        // Example 1: GetADUserInfo
        const string userSamAccountName = "MFVA649T";
        try
        {
            loggerService.LogInformation("Calling GetADUserInfo with samAccountName: {SamAccountName}", userSamAccountName);
            var userInfo = userInfoService.GetUser(userSamAccountName);
            loggerService.LogInformation("User found:");

            // Dynamically render user attributes using the mapper's GetDisplayValues method.
            // This avoids hardcoding attribute names and labels in the console app.
            var userDisplayValues = userMapper.GetDisplayValues(userInfo);
            foreach (var kvp in userDisplayValues)
            {
                loggerService.LogInformation("  {Key}: {Value}", kvp.Key, kvp.Value);
            }
        }
        catch (UserNotFoundException ex)
        {
            loggerService.LogError("User not found: {Message}", ex.Message);
        }
        catch (DomainException ex)
        {
            loggerService.LogError("Domain error while retrieving user: {Message}", ex.Message);
        }

        loggerService.LogInformation("");

        // Example 2: GetADGroupInfo
        const string groupSamAccountName = "employees of MADRID";
        try
        {
            loggerService.LogInformation("Calling GetADGroupInfo with samAccountName: {SamAccountName}", groupSamAccountName);
            var groupInfo = groupInfoService.GetGroup(groupSamAccountName);
            loggerService.LogInformation("Group found:");

            // Dynamically render group attributes using the mapper's GetDisplayValues method.
            // This avoids hardcoding attribute names and labels in the console app.
            var groupDisplayValues = groupMapper.GetDisplayValues(groupInfo);
            foreach (var kvp in groupDisplayValues)
            {
                loggerService.LogInformation("  {Key}: {Value}", kvp.Key, kvp.Value);
            }
        }
        catch (GroupNotFoundException ex)
        {
            loggerService.LogError("Group not found: {Message}", ex.Message);
        }
        catch (DomainException ex)
        {
            loggerService.LogError("Domain error while retrieving group: {Message}", ex.Message);
        }

        loggerService.LogInformation("=== Test-IA Console Application Complete ===");
    }
}
