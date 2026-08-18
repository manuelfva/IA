using System.DirectoryServices.Protocols;
using System.Security.Principal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TestIA.Domain;
using ADS = System.DirectoryServices;

namespace TestIA.Application;

/// <summary>
/// Verifies whether the current Windows user is a member of a specific Active Directory group.
/// <para>
/// This service performs LDAP-based group membership checks by:
/// <list type="number">
///   <item><description>Obtaining the current Windows user identity.</description></item>
///   <description>Discovering the Active Directory environment (domain, DC, Base DN).</description>
///   <item><description>Verifying that the target group exists in Active Directory.</description></item>
///   <item><description>Locating the current user's Distinguished Name via LDAP search.</description></item>
///   <item><description>Checking if the user's DN appears in the group's <c>member</c> attribute.</description></item>
/// </list>
/// </para>
/// </summary>
/// <remarks>
/// <b>Authentication:</b> Uses the current Windows user's security context via
/// <c>AuthType.Negotiate</c>. No credentials are stored, transmitted, or prompted.
/// <para>
/// <b>Group verification:</b> The configured group must exist in Active Directory. If it does not,
/// a <see cref="Domain.MissingGroupException"/> is thrown immediately — this indicates a
/// misconfiguration rather than a normal "user not found" scenario.
/// </para>
/// </remarks>
public class UserGroupAuthorizationService : IUserGroupAuthorizationService
{
    /// <summary>
    /// Service for dynamically discovering the Active Directory environment.
    /// </summary>
    private readonly ADDomainDiscoveryService _discovery;

    /// <summary>
    /// Logger for structured application events (authorization checks, errors, results).
    /// </summary>
    private readonly ILogger<UserGroupAuthorizationService> _logger;

    /// <summary>
    /// Strongly-typed authorization settings containing the required group name.
    /// </summary>
    private readonly AuthorizationSettings _settings;

    /// <summary>
    /// Initializes a new instance of the <see cref="UserGroupAuthorizationService"/> class.
    /// </summary>
    /// <param name="discovery">
    /// The Active Directory discovery service used to locate the domain, Domain Controller,
    /// and LDAP Base DN dynamically at runtime. Must not be null.
    /// </param>
    /// <param name="logger">
    /// The logger instance used for structured logging of authorization operations.
    /// Must not be null.
    /// </param>
    /// <param name="settings">
    /// The authorization settings containing the required group name.
    /// Must not be null.
    /// </param>
    public UserGroupAuthorizationService(
        ADDomainDiscoveryService discovery,
        ILogger<UserGroupAuthorizationService> logger,
        IOptions<AuthorizationSettings> settings)
    {
        _discovery = discovery ?? throw new ArgumentNullException(nameof(discovery));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
    }

    /// <summary>
    /// Checks whether the current Windows user is a member of the configured Active Directory group.
    /// <para>
    /// This method orchestrates the full authorization flow: it obtains the current user's identity,
    /// discovers the AD environment, verifies the group exists, locates the user's DN, and finally
    /// checks if the user appears in the group's <c>member</c> attribute.
    /// </para>
    /// </summary>
    /// <param name="groupName">
    /// The <c>sAMAccountName</c> of the Active Directory group to check membership against.
    /// This is the configured group name from <see cref="AuthorizationSettings.RequiredGroup"/>.
    /// </param>
    /// <returns>
    /// <c>true</c> if the current user is a member of the group; <c>false</c> otherwise.
    /// </returns>
    /// <exception cref="MissingGroupException">
    /// Thrown when the authorization group does not exist in Active Directory.
    /// </exception>
    /// <exception cref="DomainException">
    /// Thrown when an LDAP error occurs while checking membership.
    /// </exception>
    public bool IsMemberOfGroup(string groupName)
    {
        // Log the authorization check intent for observability.
        _logger.LogInformation("Checking group membership for current user against group: {GroupName}", groupName);

        // Step 1: Get the current Windows user identity
        // Step 1: Get the current Windows user identity from the security context.
        var windowsIdentity = WindowsIdentity.GetCurrent();
        if (windowsIdentity == null)
        {
            // Unable to retrieve the Windows identity — this should not happen on Windows.
            _logger.LogWarning("Could not retrieve the current Windows identity.");
            return false;
        }

        // Extract the user's logon name from the DOMAIN\Username format.
        var userSamAccountName = windowsIdentity.Name.Split('\\')[1];
        _logger.LogInformation("Current Windows user: {UserSamAccountName}", userSamAccountName);

        // Step 2: Discover the Active Directory environment
        // Step 2: Discover the Active Directory environment (domain, DC, Base DN).
        var (domainName, domainController, baseDN) = _discovery.Discover();
        _logger.LogInformation("Using Domain Controller: {DomainController}, Base DN: {BaseDN}", domainController, baseDN);

        // Step 3: Verify the authorization group exists
        // Step 3: Verify the authorization group exists in Active Directory.
        // Throws MissingGroupException if the group does not exist.
        var groupDn = VerifyGroupExists(groupName, domainController, baseDN);

        // Step 4: Get the current user's Distinguished Name via LDAP search
        // Step 4: Get the current user's Distinguished Name via LDAP search.
        var userDn = GetUserDistinguishedName(userSamAccountName, domainController, baseDN);
        if (string.IsNullOrEmpty(userDn))
        {
            // The current user could not be found in Active Directory.
            _logger.LogWarning("Could not find the current user ({UserSamAccountName}) in Active Directory.", userSamAccountName);
            return false;
        }

        _logger.LogInformation("Current user Distinguished Name: {UserDn}", userDn);

        // Step 5: Check if the user's DN is in the group's member attribute
        return IsUserInGroup(groupName, groupDn, userDn, domainController, baseDN);
    }

    private string VerifyGroupExists(string groupName, string domainController, string baseDN)
    {
        using var connection = new LdapConnection(domainController);
        connection.AuthType = AuthType.Negotiate;

        try
        {
            _logger.LogInformation("Verifying that authorization group {GroupName} exists in Active Directory.", groupName);
            var escapedGroupName = LdapFilterHelper.Escape(groupName);
            var request = new SearchRequest(
                baseDN,
                $"(&(objectCategory=group)(sAMAccountName={escapedGroupName}))",
                ADS.Protocols.SearchScope.Subtree,
                "distinguishedName");

            var response = (ADS.Protocols.SearchResponse)connection.SendRequest(request);

            if (response.Entries.Count == 0)
            {
                _logger.LogError("Authorization group '{GroupName}' does not exist in Active Directory.", groupName);
                throw new MissingGroupException($"Authorization group '{groupName}' does not exist in Active Directory.");
            }

            var dnAttribute = response.Entries[0].Attributes["distinguishedName"];
            if (dnAttribute == null || dnAttribute.Count == 0)
            {
                _logger.LogError("Authorization group '{GroupName}' exists but has no distinguishedName attribute.", groupName);
                throw new MissingGroupException($"Authorization group '{groupName}' is misconfigured in Active Directory.");
            }

            var groupDn = (string)dnAttribute[0];
            _logger.LogInformation("Authorization group '{GroupName}' exists with Distinguished Name: {GroupDn}", groupName, groupDn);
            return groupDn;
        }
        catch (LdapException ex)
        {
            _logger.LogError(ex, "LDAP error while verifying group {GroupName} exists", groupName);
            throw new DomainException($"Failed to verify group {groupName} on {domainController}.", ex);
        }
        catch (DirectoryOperationException ex)
        {
            _logger.LogError(ex, "Directory operation error while verifying group {GroupName} exists", groupName);
            throw new DomainException($"Directory operation failed while verifying group {groupName} on {domainController}.", ex);
        }
    }

    private string GetUserDistinguishedName(string samAccountName, string domainController, string baseDN)
    {
        using var connection = new LdapConnection(domainController);
        connection.AuthType = AuthType.Negotiate;

        try
        {
            _logger.LogInformation("Searching for user {SamAccountName} to obtain Distinguished Name.", samAccountName);
            var escapedName = LdapFilterHelper.Escape(samAccountName);
            var request = new SearchRequest(
                baseDN,
                $"(&(objectCategory=person)(objectClass=user)(sAMAccountName={escapedName}))",
                ADS.Protocols.SearchScope.Subtree,
                "distinguishedName");

            var response = (ADS.Protocols.SearchResponse)connection.SendRequest(request);

            if (response.Entries.Count == 0)
            {
                _logger.LogWarning("User {SamAccountName} not found in Active Directory.", samAccountName);
                return string.Empty;
            }

            var dnAttribute = response.Entries[0].Attributes["distinguishedName"];
            if (dnAttribute == null || dnAttribute.Count == 0)
            {
                _logger.LogWarning("User {SamAccountName} does not have a distinguishedName attribute.", samAccountName);
                return string.Empty;
            }

            return (string)dnAttribute[0];
        }
        catch (LdapException ex)
        {
            _logger.LogError(ex, "LDAP error while searching for user {SamAccountName}", samAccountName);
            throw new DomainException($"Failed to search for user {samAccountName} on {domainController}.", ex);
        }
        catch (DirectoryOperationException ex)
        {
            _logger.LogError(ex, "Directory operation error while searching for user {SamAccountName}", samAccountName);
            throw new DomainException($"Directory operation failed while searching for user {samAccountName} on {domainController}.", ex);
        }
    }

    private bool IsUserInGroup(string groupName, string groupDn, string userDn, string domainController, string baseDN)
    {
        using var connection = new LdapConnection(domainController);
        connection.AuthType = AuthType.Negotiate;

        try
        {
            _logger.LogInformation("Checking if user {UserDn} is a member of group {GroupName}.", userDn, groupName);
            var request = new SearchRequest(
                groupDn,
                "(objectClass=*)",
                ADS.Protocols.SearchScope.Base,
                "member");

            var response = (ADS.Protocols.SearchResponse)connection.SendRequest(request);

            if (response.Entries.Count == 0)
            {
                _logger.LogWarning("Group {GroupName} returned no entries.", groupName);
                return false;
            }

            var memberAttribute = response.Entries[0].Attributes["member"];
            if (memberAttribute == null)
            {
                _logger.LogWarning("Group {GroupName} does not have a member attribute.", groupName);
                return false;
            }

            // Check if the user's DN is in the group's member list
            foreach (byte[] memberBytes in memberAttribute)
            {
                var memberDn = System.Text.Encoding.UTF8.GetString(memberBytes);
                if (string.Equals(memberDn, userDn, StringComparison.Ordinal))
                {
                    _logger.LogInformation("User {UserDn} is a member of group {GroupName}.", userDn, groupName);
                    return true;
                }
            }

            _logger.LogWarning("User {UserDn} is NOT a member of group {GroupName}.", userDn, groupName);
            return false;
        }
        catch (LdapException ex)
        {
            _logger.LogError(ex, "LDAP error while checking group membership for group {GroupName}", groupName);
            throw new DomainException($"Failed to check group membership for {groupName} on {domainController}.", ex);
        }
        catch (DirectoryOperationException ex)
        {
            _logger.LogError(ex, "Directory operation error while checking group membership for group {GroupName}", groupName);
            throw new DomainException($"Directory operation failed while checking group membership for {groupName} on {domainController}.", ex);
        }
    }
}