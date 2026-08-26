using System.DirectoryServices.Protocols;
using Microsoft.Extensions.Logging;
using TestIA.Domain;

namespace TestIA.Application;

/// <summary>
/// Adds members to Active Directory groups via LDAP modify operations.
/// <para>
/// This service searches for a group and a user by their <c>sAMAccountName</c>,
/// retrieves their distinguished names (DNs), and performs an LDAP modify request
/// to add the user''s DN to the group''s <c>member</c> attribute.
/// </para>
/// </summary>
/// <remarks>
/// <b>Supported groups:</b> Security groups (domain local, global, universal) and
/// distribution groups. The authenticated user must have write permission
/// on the group''s <c>member</c> attribute.
/// </remarks>
public class GroupMembershipWriterService : IGroupMembershipWriter
{
    /// <summary>
    /// Service for dynamically discovering the Active Directory environment.
    /// </summary>
    private readonly ADDomainDiscoveryService _discoveryService;

    /// <summary>
    /// Logger for structured application events (operations, errors, results).
    /// </summary>
    private readonly ILogger<GroupMembershipWriterService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="GroupMembershipWriterService"/> class.
    /// </summary>
    /// <param name="discoveryService">
    /// The Active Directory discovery service used to locate the domain, Domain Controller,
    /// and LDAP Base DN dynamically at runtime. Must not be null.
    /// </param>
    /// <param name="logger">
    /// The logger instance used for structured logging of group membership operations.
    /// Must not be null.
    /// </param>
    public GroupMembershipWriterService(ADDomainDiscoveryService discoveryService, ILogger<GroupMembershipWriterService> logger)
    {
        _discoveryService = discoveryService ?? throw new ArgumentNullException(nameof(discoveryService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public GroupMemberOperationResult AddMember(string groupSamAccountName, string memberSamAccountName)
    {
        ArgumentNullException.ThrowIfNull(groupSamAccountName);
        ArgumentNullException.ThrowIfNull(memberSamAccountName);

        if (string.IsNullOrWhiteSpace(groupSamAccountName))
        {
            throw new ArgumentException("Group samAccountName cannot be empty.", nameof(groupSamAccountName));
        }

        if (string.IsNullOrWhiteSpace(memberSamAccountName))
        {
            throw new ArgumentException("Member samAccountName cannot be empty.", nameof(memberSamAccountName));
        }

        _logger.LogInformation("Attempting to add user ''{MemberSamAccountName}'' as member to group ''{GroupSamAccountName}''", memberSamAccountName, groupSamAccountName);

        // Discover the Active Directory environment to get the Domain Controller and Base DN.
        var (domainName, domainController, baseDN) = _discoveryService.Discover();

        // Create an LDAP connection to the discovered Domain Controller.
        using var connection = new LdapConnection(domainController);
        connection.AuthType = AuthType.Negotiate;

        // Search for the group by samAccountName to get its Distinguished Name.
        var groupDn = SearchDistinguishedName(connection, baseDN, "group", groupSamAccountName, _logger);

        // Search for the user by samAccountName to get their Distinguished Name.
        var memberDn = SearchDistinguishedName(connection, baseDN, "person", memberSamAccountName, _logger);

        // Log the DNs being used for the modify operation.
        _logger.LogInformation("LDAP modify — Group DN: {GroupDn}, Member DN: {MemberDn}", groupDn, memberDn);

        // Build and send the LDAP modify request to add the member.
        var modification = new DirectoryAttributeModification { Name = "member", Operation = DirectoryAttributeOperation.Add };
        modification.Add(memberDn);
        var modifyRequest = new ModifyRequest(groupDn, new[] { modification });

        try
        {
            connection.SendRequest(modifyRequest);
        }
        catch (LdapException ex)
        {
            _logger.LogError(ex, "LDAP error while adding ''{MemberSamAccountName}'' to group ''{GroupSamAccountName}'' (Group DN: {GroupDn})", memberSamAccountName, groupSamAccountName, groupDn);
            throw new DomainException($"LDAP error while adding member to group ''{groupSamAccountName}''.", ex);
        }
        catch (DirectoryOperationException ex)
        {
            _logger.LogError(ex, "Directory operation error while adding ''{MemberSamAccountName}'' to group ''{GroupSamAccountName}'' (Group DN: {GroupDn})", memberSamAccountName, groupSamAccountName, groupDn);
            throw new DomainException($"Directory operation failed while adding member to group ''{groupSamAccountName}''. {ex.Message}", ex);
        }

        _logger.LogInformation("Successfully added user ''{MemberSamAccountName}'' as member to group ''{GroupSamAccountName}''", memberSamAccountName, groupSamAccountName);

        return new GroupMemberOperationResult(true, $"Successfully added ''{memberSamAccountName}'' as a member to group ''{groupSamAccountName}''.");
    }

    /// <inheritdoc />
    public GroupMemberOperationResult RemoveMember(string groupSamAccountName, string memberSamAccountName)
    {
        ArgumentNullException.ThrowIfNull(groupSamAccountName);
        ArgumentNullException.ThrowIfNull(memberSamAccountName);

        if (string.IsNullOrWhiteSpace(groupSamAccountName))
        {
            throw new ArgumentException("Group samAccountName cannot be empty.", nameof(groupSamAccountName));
        }

        if (string.IsNullOrWhiteSpace(memberSamAccountName))
        {
            throw new ArgumentException("Member samAccountName cannot be empty.", nameof(memberSamAccountName));
        }

        _logger.LogInformation("Attempting to remove user ''{MemberSamAccountName}'' from group ''{GroupSamAccountName}''", memberSamAccountName, groupSamAccountName);

        // Discover the Active Directory environment to get the Domain Controller and Base DN.
        var (domainName, domainController, baseDN) = _discoveryService.Discover();

        // Create an LDAP connection to the discovered Domain Controller.
        using var connection = new LdapConnection(domainController);
        connection.AuthType = AuthType.Negotiate;

        // Search for the group by samAccountName to get its Distinguished Name.
        var groupDn = SearchDistinguishedName(connection, baseDN, "group", groupSamAccountName, _logger);

        // Search for the user by samAccountName to get their Distinguished Name.
        var memberDn = SearchDistinguishedName(connection, baseDN, "person", memberSamAccountName, _logger);

        // Build the LDAP modify request to remove the member from the group.
        var modification = new DirectoryAttributeModification { Name = "member", Operation = DirectoryAttributeOperation.Delete };
        modification.Add(memberDn);
        var modifyRequest = new ModifyRequest(groupDn, new[] { modification });

        try
        {
            connection.SendRequest(modifyRequest);
        }
        catch (LdapException ex)
        {
            _logger.LogError(ex, "LDAP error while removing ''{MemberSamAccountName}'' from group ''{GroupSamAccountName}'' (Group DN: {GroupDn})", memberSamAccountName, groupSamAccountName, groupDn);
            throw new DomainException($"LDAP error while removing member from group ''{groupSamAccountName}''.", ex);
        }
        catch (DirectoryOperationException ex)
        {
            _logger.LogError(ex, "Directory operation error while removing ''{MemberSamAccountName}'' from group ''{GroupSamAccountName}'' (Group DN: {GroupDn})", memberSamAccountName, groupSamAccountName, groupDn);
            throw new DomainException($"Directory operation failed while removing member from group ''{groupSamAccountName}''. {ex.Message}", ex);
        }

        _logger.LogInformation("Successfully removed user ''{MemberSamAccountName}'' from group ''{GroupSamAccountName}''", memberSamAccountName, groupSamAccountName);

        return new GroupMemberOperationResult(true, $"Successfully removed ''{memberSamAccountName}'' from group ''{groupSamAccountName}''.");
    }

    /// <summary>
    /// Searches for an object in Active Directory and returns its Distinguished Name.
    /// </summary>
    /// <param name="connection">The active LDAP connection. Must not be null.</param>
    /// <param name="baseDN">The LDAP search base (naming context). Must not be null or empty.</param>
    /// <param name="objectCategory">The objectCategory to search for (e.g., "group" or "person").</param>
    /// <param name="samAccountName">The samAccountName to search for. Must not be null or empty.</param>
    /// <param name="logger">The logger instance. Must not be null.</param>
    /// <returns>The Distinguished Name of the found object.</returns>
    /// <exception cref="GroupNotFoundException">Thrown when no group is found.</exception>
    /// <exception cref="UserNotFoundException">Thrown when no user is found.</exception>
    /// <exception cref="DomainException">Thrown when an LDAP error occurs during the search.</exception>
    private static string SearchDistinguishedName(LdapConnection connection, string baseDN, string objectCategory, string samAccountName, ILogger logger)
    {
        // Build the search filter manually with proper escaping.
        var escapedSamAccountName = LdapFilterHelper.Escape(samAccountName);
        var filter = $"(&(objectCategory={objectCategory})(sAMAccountName={escapedSamAccountName}))";

        var searchRequest = new SearchRequest(baseDN, filter, SearchScope.Subtree, "distinguishedName");
        var response = (SearchResponse)connection.SendRequest(searchRequest);

        if (response.Entries.Count == 0)
        {
            var message = objectCategory.Equals("group", StringComparison.OrdinalIgnoreCase)
                ? $"Group not found: {samAccountName}"
                : $"User not found: {samAccountName}";

            logger.LogError("{ObjectType} not found: {SamAccountName}",
                objectCategory.Equals("group", StringComparison.OrdinalIgnoreCase) ? "Group" : "User",
                samAccountName);

            if (objectCategory.Equals("group", StringComparison.OrdinalIgnoreCase))
            {
                throw new GroupNotFoundException(message);
            }
            else
            {
                throw new UserNotFoundException(message);
            }
        }

        var entry = response.Entries[0];
        var dn = entry.DistinguishedName;

        logger.LogDebug("Found {ObjectType} ''{SamAccountName}'' with DN {Dn}", objectCategory, samAccountName, dn);

        return dn;
    }
}
