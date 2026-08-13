using System.DirectoryServices.Protocols;
using Microsoft.Extensions.Logging;
using TestIA.Domain;

namespace TestIA.Application;

/// <summary>
/// Implementation of <see cref="IGetADGroupInfo"/> that performs LDAP searches to retrieve Active Directory group information.
/// </summary>
public class GetADGroupInfoService : IGetADGroupInfo
{
    private readonly ADDomainDiscoveryService _discoveryService;
    private readonly ILogger<GetADGroupInfoService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetADGroupInfoService"/> class.
    /// </summary>
    /// <param name="discoveryService">The Active Directory discovery service.</param>
    /// <param name="logger">The logger instance.</param>
    public GetADGroupInfoService(ADDomainDiscoveryService discoveryService, ILogger<GetADGroupInfoService> logger)
    {
        _discoveryService = discoveryService ?? throw new ArgumentNullException(nameof(discoveryService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public GroupDto GetGroup(string samAccountName)
    {
        ArgumentNullException.ThrowIfNull(samAccountName);

        _logger.LogInformation("Searching for group with samAccountName: {SamAccountName}", samAccountName);

        var (domainName, domainController, baseDN) = _discoveryService.Discover();

        using var connection = new LdapConnection(domainController);
        connection.AuthType = AuthType.Negotiate;

        try
        {
            var escapedSamAccountName = LdapFilterHelper.Escape(samAccountName);
            var filter = $"(&(objectCategory=group)(sAMAccountName={escapedSamAccountName}))";
            _logger.LogInformation("Executing LDAP search with filter: {Filter}", filter);

            var request = new SearchRequest(baseDN, filter, SearchScope.Subtree, "displayName", "member");
            var response = (SearchResponse)connection.SendRequest(request);

            if (response.Entries.Count == 0)
            {
                _logger.LogWarning("Group with samAccountName {SamAccountName} not found.", samAccountName);
                throw new GroupNotFoundException($"Group with samAccountName '{samAccountName}' not found in Active Directory.");
            }

            var entry = response.Entries[0];
            var displayName = GetAttributeValue(entry, "displayName") ?? "(unknown)";
            var members = GetMemberValues(entry);

            _logger.LogInformation("Found group: {DisplayName} with {MemberCount} members", displayName, members.Length);

            return new GroupDto(displayName, members);
        }
        catch (LdapException ex)
        {
            _logger.LogError(ex, "LDAP error while searching for group {SamAccountName}", samAccountName);
            throw new DomainException($"LDAP error while searching for group '{samAccountName}'.", ex);
        }
        catch (DirectoryOperationException ex)
        {
            _logger.LogError(ex, "Directory operation error while searching for group {SamAccountName}", samAccountName);
            throw new DomainException($"Directory operation failed while searching for group '{samAccountName}'.", ex);
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

    private static string[] GetMemberValues(SearchResultEntry entry)
    {
        var attribute = entry.Attributes["member"];
        if (attribute == null || attribute.Count == 0)
        {
            return [];
        }

        var members = new string[attribute.Count];
        for (var i = 0; i < attribute.Count; i++)
        {
            members[i] = (string)attribute[i];
        }

        return members;
    }
}
