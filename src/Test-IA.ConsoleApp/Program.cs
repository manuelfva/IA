using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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
        var services = new ServiceCollection();

        // Create logger factory
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Information));

        // Register logging
        services.AddSingleton<ILoggerService>(sp => new LoggingService(loggerFactory.CreateLogger<LoggingService>()));
        services.AddTestIAServices();

        // Register ILogger<T> for all services that need it
        services.AddSingleton<ILogger<LoggingService>>(_ => loggerFactory.CreateLogger<LoggingService>());
        services.AddSingleton<ILogger<GetADUserInfoService>>(_ => loggerFactory.CreateLogger<GetADUserInfoService>());
        services.AddSingleton<ILogger<GetADGroupInfoService>>(_ => loggerFactory.CreateLogger<GetADGroupInfoService>());
        services.AddSingleton<ILogger<ADDomainDiscoveryService>>(_ => loggerFactory.CreateLogger<ADDomainDiscoveryService>());

        // Build the service provider
        using var serviceProvider = services.BuildServiceProvider();

        var loggerService = serviceProvider.GetRequiredService<ILoggerService>();
        var userInfoService = serviceProvider.GetRequiredService<IGetADUserInfo>();
        var groupInfoService = serviceProvider.GetRequiredService<IGetADGroupInfo>();

        loggerService.LogInformation("=== Test-IA Console Application ===");
        loggerService.LogInformation("Starting Active Directory service demonstration...");

        // Example 1: GetADUserInfo
        const string userSamAccountName = "MFVA649T";
        try
        {
            loggerService.LogInformation("Calling GetADUserInfo with samAccountName: {SamAccountName}", userSamAccountName);
            var userInfo = userInfoService.GetUser(userSamAccountName);
            loggerService.LogInformation("User found:");
            loggerService.LogInformation("  DisplayName: {DisplayName}", userInfo.DisplayName);
            loggerService.LogInformation("  EmployeeID: {EmployeeId}", userInfo.EmployeeId ?? "(not set)");
            loggerService.LogInformation("  Mail: {Mail}", userInfo.Mail ?? "(not set)");
            loggerService.LogInformation("  UPN: {UserPrincipalName}", userInfo.UserPrincipalName ?? "(not set)");
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
            loggerService.LogInformation("  DisplayName: {DisplayName}", groupInfo.DisplayName);
            loggerService.LogInformation("  Members ({Count}):", groupInfo.Members.Length);
            foreach (var member in groupInfo.Members)
            {
                loggerService.LogInformation("    - {Member}", member);
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
