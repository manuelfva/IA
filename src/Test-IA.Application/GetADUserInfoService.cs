using System.DirectoryServices.Protocols;
using Microsoft.Extensions.Logging;
using TestIA.Domain;

namespace TestIA.Application;

/// <summary>
/// Implementation of <see cref="IGetADUserInfo"/> that performs LDAP searches to retrieve Active Directory user information.
/// </summary>
public class GetADUserInfoService : IGetADUserInfo
{
    private readonly ADDomainDiscoveryService _discoveryService;
    private readonly ILogger<GetADUserInfoService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetADUserInfoService"/> class.
    /// </summary>
    /// <param name="discoveryService">The Active Directory discovery service.</param>
    /// <param name="logger">The logger instance.</param>
    public GetADUserInfoService(ADDomainDiscoveryService discoveryService, ILogger<GetADUserInfoService> logger)
    {
        _discoveryService = discoveryService ?? throw new ArgumentNullException(nameof(discoveryService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public UserDto GetUser(string samAccountName)
    {
        ArgumentNullException.ThrowIfNull(samAccountName);

        _logger.LogInformation("Searching for user with samAccountName: {SamAccountName}", samAccountName);

        var (domainName, domainController, baseDN) = _discoveryService.Discover();

        using var connection = new LdapConnection(domainController);
        connection.AuthType = AuthType.Negotiate;

        try
        {
            var escapedSamAccountName = LdapFilterHelper.Escape(samAccountName);
            var filter = $"(&(objectCategory=person)(objectClass=user)(sAMAccountName={escapedSamAccountName}))";
            _logger.LogInformation("Executing LDAP search with filter: {Filter}", filter);

            var request = new SearchRequest(baseDN, filter, SearchScope.Subtree, "displayName", "employeeID", "mail", "userPrincipalName");
            var response = (SearchResponse)connection.SendRequest(request);

            if (response.Entries.Count == 0)
            {
                _logger.LogWarning("User with samAccountName {SamAccountName} not found.", samAccountName);
                throw new UserNotFoundException($"User with samAccountName '{samAccountName}' not found in Active Directory.");
            }

            var entry = response.Entries[0];
            var displayName = GetAttributeValue(entry, "displayName") ?? "(unknown)";
            var employeeId = GetAttributeValue(entry, "employeeID");
            var mail = GetAttributeValue(entry, "mail");
            var userPrincipalName = GetAttributeValue(entry, "userPrincipalName");

            _logger.LogInformation("Found user: {DisplayName} ({UPN})", displayName, userPrincipalName);

            return new UserDto(displayName, employeeId, mail, userPrincipalName);
        }
        catch (LdapException ex)
        {
            _logger.LogError(ex, "LDAP error while searching for user {SamAccountName}", samAccountName);
            throw new DomainException($"LDAP error while searching for user '{samAccountName}'.", ex);
        }
        catch (DirectoryOperationException ex)
        {
            _logger.LogError(ex, "Directory operation error while searching for user {SamAccountName}", samAccountName);
            throw new DomainException($"Directory operation failed while searching for user '{samAccountName}'.", ex);
        }
    }

    private static string? GetAttributeValue(SearchResultEntry entry, string attributeName)
    {
        var attribute = entry.Attributes[attributeName];
        if (attribute == null || attribute.Count == 0)
        {
            return null;
        }

        return (string)attribute[0];
    }
}
