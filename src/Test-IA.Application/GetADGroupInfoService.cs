using System.DirectoryServices.Protocols;
using Microsoft.Extensions.Logging;
using TestIA.Domain;

namespace TestIA.Application;

/// <summary>
/// Retrieves group information from Active Directory Domain Services (AD DS) via LDAP.
/// <para>
/// This service connects to the domain's LDAP directory using Windows Integrated Authentication
/// to search for a group by its <c>sAMAccountName</c> and returns its <c>displayName</c> and
/// a list of members with their display names (resolved from their Distinguished Names).
/// </para>
/// <para>
/// The service performs dynamic Active Directory discovery at runtime and safely escapes all
/// user-supplied input to prevent LDAP injection attacks.
/// </para>
/// </summary>
/// <remarks>
/// <b>Retrieved attributes:</b>
/// <list type="bullet">
///   <item><description><c>displayName</c> — The group's full display name.</description></item>
///   <item><description><c>member</c> — An array of member display names (resolved from Distinguished Names).</description></item>
/// </list>
/// <para>
/// <b>Member resolution:</b> Each member's Distinguished Name (DN) is resolved to its
/// <c>displayName</c> attribute via additional LDAP searches. If resolution fails, the DN
/// is used as a fallback and a warning is logged.
/// </para>
/// <para>
/// <b>Authentication:</b> Uses the current Windows user's security context via
/// <c>AuthType.Negotiate</c>. No credentials are stored, transmitted, or prompted.
/// </para>
/// </remarks>
public class GetADGroupInfoService : IGetADGroupInfo
{
    /// <summary>
    /// Maps LDAP attribute names to their human-readable display labels.
    /// <para>
    /// This dictionary defines the attributes that will be requested from Active Directory
    /// during the group search. Only the <c>displayName</c> is fetched here; the <c>member</c>
    /// attribute is added separately because it requires special handling.
    /// </para>
    /// </summary>
    private static readonly Dictionary<string, string> _attributeNames = new()
    {
        { "displayName", "Display Name" },
    };

    /// <summary>
    /// Service responsible for dynamically discovering the Active Directory environment.
    /// </summary>
    private readonly ADDomainDiscoveryService _discoveryService;

    /// <summary>
    /// Logger for structured application events (searches, errors, results).
    /// </summary>
    private readonly ILogger<GetADGroupInfoService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetADGroupInfoService"/> class.
    /// </summary>
    /// <param name="discoveryService">
    /// The Active Directory discovery service used to locate the domain, Domain Controller,
    /// and LDAP Base DN dynamically at runtime. Must not be null.
    /// </param>
    /// <param name="logger">
    /// The logger instance used for structured logging of group operations.
    /// Must not be null.
    /// </param>
    public GetADGroupInfoService(ADDomainDiscoveryService discoveryService, ILogger<GetADGroupInfoService> logger)
    {
        _discoveryService = discoveryService ?? throw new ArgumentNullException(nameof(discoveryService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    /// <summary>
    /// Retrieves group information by <c>sAMAccountName</c> from Active Directory.
    /// <para>
    /// This method performs dynamic discovery, connects to LDAP, searches for the group,
    /// and resolves each member's Distinguished Name to its display name.
    /// </para>
    /// </summary>
    /// <param name="samAccountName">
    /// The <c>sAMAccountName</c> (logon name) of the Active Directory group to search for.
    /// Must not be null.
    /// </param>
    /// <returns>
    /// A <see cref="GroupDto"/> record containing the group's display name and an array
    /// of member display names.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="samAccountName"/> is null.</exception>
    /// <exception cref="GroupNotFoundException">Thrown when the group does not exist in Active Directory.</exception>
    /// <exception cref="DomainException">Thrown when an LDAP error occurs during the search.</exception>
    public GroupDto GetGroup(string samAccountName)
    {
        // Validate that the input parameter is not null.
        ArgumentNullException.ThrowIfNull(samAccountName);

        // Log the search intent for observability.
        _logger.LogInformation("Searching for group with samAccountName: {SamAccountName}", samAccountName);

        // Discover the Active Directory environment (domain, DC, Base DN).
        var (domainName, domainController, baseDN) = _discoveryService.Discover();

        // Create an LDAP connection to the discovered Domain Controller.
        // AuthType.Negotiate uses Windows Integrated Authentication (Kerberos/NTLM).
        using var connection = new LdapConnection(domainController);
        connection.AuthType = AuthType.Negotiate;

        try
        {
            // Safely escape the user-supplied group name to prevent LDAP injection.
            // Special characters (\, *, (, ), null) are escaped with backslash prefixes.
            var escapedSamAccountName = LdapFilterHelper.Escape(samAccountName);

            // Build the LDAP search filter to find a group with the matching sAMAccountName.
            // - objectCategory=group: ensures we only search group objects.
            // - sAMAccountName={escaped}: matches the group's logon name.
            var filter = $"(&(objectCategory=group)(sAMAccountName={escapedSamAccountName}))";
            _logger.LogInformation("Executing LDAP search with filter: {Filter}", filter);

            // Request both displayName and member attributes.
            // displayName comes from _attributeNames; member is added separately.
            var allAttributes = _attributeNames.Keys.Concat(new[] { "member" }).ToArray();

            // Create a subtree search that looks for the group anywhere under the Base DN.
            var request = new SearchRequest(baseDN, filter, SearchScope.Subtree, allAttributes);
            var response = (SearchResponse)connection.SendRequest(request);

            // If no entries were returned, the group does not exist in Active Directory.
            if (response.Entries.Count == 0)
            {
                _logger.LogWarning("Group with samAccountName {SamAccountName} not found.", samAccountName);
                throw new GroupNotFoundException($"Group with samAccountName '{samAccountName}' not found in Active Directory.");
            }

            // Extract the requested attributes from the first (and expected only) matching entry.
            var entry = response.Entries[0];
            var attributes = entry.ExtractAttributes(_attributeNames);
            var displayName = attributes["displayName"] ?? "(unknown)";

            // Extract the raw member Distinguished Names from the entry.
            var memberDns = GetMemberValues(entry);

            // Resolve each member's Distinguished Name to its display name.
            // This performs additional LDAP searches — one per member.
            var memberDisplayNames = ResolveMemberDisplayNamesAsync(memberDns, connection).GetAwaiter().GetResult();

            // Log the successful lookup with structured output for auditability.
            _logger.LogInformation("Found group: {DisplayName} with {MemberCount} members", displayName, memberDisplayNames.Length);

            // Return the strongly-typed DTO record to the caller.
            return new GroupDto(displayName, memberDisplayNames);
        }
        catch (LdapException ex)
        {
            // Catch low-level LDAP exceptions (network errors, authentication failures, timeouts)
            // and wrap them in a domain-friendly exception with full context.
            _logger.LogError(ex, "LDAP error while searching for group {SamAccountName}", samAccountName);
            throw new DomainException($"LDAP error while searching for group '{samAccountName}'.", ex);
        }
        catch (DirectoryOperationException ex)
        {
            // Catch directory operation errors (e.g., access denied, invalid filter, server-side errors)
            // and wrap them in a domain-friendly exception.
            _logger.LogError(ex, "Directory operation error while searching for group {SamAccountName}", samAccountName);
            throw new DomainException($"Directory operation failed while searching for group '{samAccountName}'.", ex);
        }
    }

    /// <summary>
    /// Resolves each member's Distinguished Name to its <c>displayName</c> attribute.
    /// <para>
    /// For every member DN in the input array, this method performs a separate LDAP search
    /// using <c>SearchScope.Subtree</c> to locate the object and extract its display name.
    /// If resolution fails (object not found, no display name, or exception), the original
    /// DN is used as a fallback and a warning or error is logged accordingly.
    /// </para>
    /// </summary>
    /// <param name="memberDns">
    /// An array of Distinguished Names (DNs) representing group members.
    /// </param>
    /// <param name="connection">
    /// An already-open LDAP connection to the Domain Controller. Reused to avoid
    /// creating a new connection for each member lookup.
    /// </param>
    /// <returns>
    /// An array of display names corresponding to the input DNs. If a display name
    /// cannot be resolved, the original DN is returned in its place.
    /// </returns>
    private async Task<string[]> ResolveMemberDisplayNamesAsync(string[] memberDns, LdapConnection connection)
    {
        // Discover the AD environment again for each member lookup (needed for Base DN).
        var (domainName, domainController, baseDN) = _discoveryService.Discover();
        var displayNames = new string[memberDns.Length];

        // Process each member DN sequentially, resolving its display name.
        for (var i = 0; i < memberDns.Length; i++)
        {
            var dn = memberDns[i];

            try
            {
                // Safely escape the DN to prevent LDAP injection.
                var escapedDn = LdapFilterHelper.Escape(dn);

                // Build a search filter that matches the member's exact Distinguished Name.
                var filter = $"(distinguishedName={escapedDn})";
                _logger.LogDebug("Resolving display name for member: {DistinguishedName}", dn);

                // Search the entire directory tree (Subtree) for the member object.
                // We only need the displayName attribute.
                var request = new SearchRequest(baseDN, filter, SearchScope.Subtree, "displayName");
                var response = (SearchResponse)connection.SendRequest(request);

                if (response.Entries.Count > 0)
                {
                    var entry = response.Entries[0];
                    var displayName = entry.GetAttributeValue("displayName");

                    if (!string.IsNullOrEmpty(displayName))
                    {
                        // Successfully resolved the display name.
                        displayNames[i] = displayName;
                        _logger.LogDebug("Resolved member {DistinguishedName} to display name: {DisplayName}", dn, displayName);
                    }
                    else
                    {
                        // The object was found but has no displayName — use DN as fallback.
                        displayNames[i] = dn;
                        _logger.LogWarning("Member {DistinguishedName} has no displayName attribute. Using DN as fallback.", dn);
                    }
                }
                else
                {
                    // The member object could not be found — use DN as fallback.
                    displayNames[i] = dn;
                    _logger.LogWarning("Member {DistinguishedName} not found in Active Directory. Using DN as fallback.", dn);
                }
            }
            catch (Exception ex)
            {
                // Any unexpected error (e.g., network timeout) — use DN as fallback.
                displayNames[i] = dn;
                _logger.LogError(ex, "Error resolving display name for member {DistinguishedName}. Using DN as fallback.", dn);
            }
        }

        return displayNames;
    }

    /// <summary>
    /// Extracts the <c>member</c> attribute values from a search result entry.
    /// <para>
    /// The <c>member</c> attribute in Active Directory is a multi-valued attribute that
    /// contains the Distinguished Names of all group members. This method converts those
    /// values into a string array for further processing.
    /// </para>
    /// </summary>
    /// <param name="entry">
    /// The search result entry containing the group object.
    /// </param>
    /// <returns>
    /// An array of Distinguished Names, or an empty array if the <c>member</c> attribute
    /// is missing or has no values.
    /// </returns>
    private static string[] GetMemberValues(SearchResultEntry entry)
    {
        // Retrieve the member attribute from the entry.
        var attribute = entry.Attributes["member"];
        if (attribute == null || attribute.Count == 0)
        {
            // The group has no members (empty group) or the attribute is missing.
            return [];
        }

        // Convert each byte-array value to a string (Distinguished Name).
        var members = new string[attribute.Count];
        for (var i = 0; i < attribute.Count; i++)
        {
            members[i] = (string)attribute[i];
        }

        return members;
    }
}
