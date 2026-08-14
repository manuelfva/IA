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
            var memberDns = GetMemberValues(entry);
            
            // Resolve member DNs to display names
            var memberDisplayNames = ResolveMemberDisplayNamesAsync(memberDns, connection).GetAwaiter().GetResult();

            _logger.LogInformation("Found group: {DisplayName} with {MemberCount} members", displayName, memberDisplayNames.Length);

            return new GroupDto(displayName, memberDisplayNames);
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

    private async Task<string[]> ResolveMemberDisplayNamesAsync(string[] memberDns, LdapConnection connection)
    {
        var (domainName, domainController, baseDN) = _discoveryService.Discover();
        var displayNames = new string[memberDns.Length];
        
        for (var i = 0; i < memberDns.Length; i++)
        {
            var dn = memberDns[i];
            
            try
            {
                var escapedDn = LdapFilterHelper.Escape(dn);
                var filter = $"(distinguishedName={escapedDn})";
                _logger.LogDebug("Resolving display name for member: {DistinguishedName}", dn);
                
                var request = new SearchRequest(baseDN, filter, SearchScope.Subtree, "displayName");
                var response = (SearchResponse)connection.SendRequest(request);
                
                if (response.Entries.Count > 0)
                {
                    var entry = response.Entries[0];
                    var displayName = GetAttributeValue(entry, "displayName");
                    
                    if (!string.IsNullOrEmpty(displayName))
                    {
                        displayNames[i] = displayName;
                        _logger.LogDebug("Resolved member {DistinguishedName} to display name: {DisplayName}", dn, displayName);
                    }
                    else
                    {
                        displayNames[i] = dn;
                        _logger.LogWarning("Member {DistinguishedName} has no displayName attribute. Using DN as fallback.", dn);
                    }
                }
                else
                {
                    displayNames[i] = dn;
                    _logger.LogWarning("Member {DistinguishedName} not found in Active Directory. Using DN as fallback.", dn);
                }
            }
            catch (Exception ex)
            {
                displayNames[i] = dn;
                _logger.LogError(ex, "Error resolving display name for member {DistinguishedName}. Using DN as fallback.", dn);
            }
        }
        
        return displayNames;
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
