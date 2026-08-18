using System.DirectoryServices.Protocols;
using Microsoft.Extensions.Logging;
using TestIA.Domain;

namespace TestIA.Application;

/// <summary>
/// Retrieves user information from Active Directory Domain Services (AD DS) via LDAP.
/// <para>
/// This service connects to the domain's LDAP directory using Windows Integrated Authentication
/// (Kerberos/Negotiate) to search for a user by their <c>sAMAccountName</c> and returns
/// a subset of their attributes as a strongly-typed <see cref="UserDto"/> record.
/// </para>
/// <para>
/// The service performs dynamic Active Directory discovery at runtime — it determines the
/// domain, locates an available Domain Controller, and obtains the LDAP naming context
/// (Base DN) without relying on any hard-coded connection parameters.
/// </para>
/// <para>
/// All user-supplied input is safely escaped before being used in LDAP search filters
/// to prevent LDAP injection attacks. The service uses <see cref="LdapFilterHelper"/>
/// for this purpose.
/// </para>
/// </summary>
/// <remarks>
/// <para>
/// <b>Retrieved attributes:</b>
/// </para>
/// <list type="bullet">
///   <item><description><c>displayName</c> — The user's full display name.</description></item>
///   <item><description><c>employeeID</c> — The user's employee identifier (nullable).</description></item>
///   <item><description><c>mail</c> — The user's email address (nullable).</description></item>
///   <item><description><c>userPrincipalName</c> — The user's principal name (UPN) (nullable).</description></item>
/// </list>
/// <para>
/// <b>Authentication:</b> Uses the current Windows user's security context via
/// <c>AuthType.Negotiate</c>. No credentials are stored, transmitted, or prompted.
/// </para>
/// <para>
/// <b>Exception behavior:</b> If the user is not found, a <see cref="UserNotFoundException"/>
/// is thrown. If an LDAP error occurs (network failure, authentication error, etc.),
/// a <see cref="DomainException"/> wrapping the underlying exception is thrown.
/// </para>
/// </remarks>
public class GetADUserInfoService : IGetADUserInfo
{
    /// <summary>
    /// Maps LDAP attribute names to their human-readable display labels.
    /// <para>
    /// This dictionary defines the exact set of attributes that will be requested from
    /// Active Directory during the LDAP search. Only these attributes are fetched,
    /// which reduces network traffic and improves performance.
    /// </para>
    /// </summary>
    private static readonly Dictionary<string, string> _attributeNames = new()
    {
        { "displayName", "Display Name" },
        { "employeeID", "Employee ID" },
        { "mail", "Email" },
        { "userPrincipalName", "UPN" },
    };

    /// <summary>
    /// Service responsible for dynamically discovering the Active Directory environment.
    /// </summary>
    private readonly ADDomainDiscoveryService _discoveryService;

    /// <summary>
    /// Logger for structured application events (searches, errors, results).
    /// </summary>
    private readonly ILogger<GetADUserInfoService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetADUserInfoService"/> class.
    /// </summary>
    /// <param name="discoveryService">
    /// The Active Directory discovery service used to locate the domain, Domain Controller,
    /// and LDAP Base DN dynamically at runtime. Must not be null.
    /// </param>
    /// <param name="logger">
    /// The logger instance used for structured logging of search operations and errors.
    /// Must not be null.
    /// </param>
    public GetADUserInfoService(ADDomainDiscoveryService discoveryService, ILogger<GetADUserInfoService> logger)
    {
        _discoveryService = discoveryService ?? throw new ArgumentNullException(nameof(discoveryService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Retrieves user information from Active Directory by the user's <c>sAMAccountName</c>.
    /// <para>
    /// This method performs a complete LDAP search lifecycle:
    /// </para>
    /// <list type="number">
    ///   <item><description>Validates the input parameter.</description></item>
    ///   <item><description>Discovers the Active Directory domain, Domain Controller, and Base DN dynamically.</description></item>
    ///   <item><description>Creates an LDAP connection using Windows Integrated Authentication.</description></item>
    ///   <item><description>Escapes the input to prevent LDAP injection, then executes a subtree search.</description></item>
    ///   <item><description>Extracts the requested attributes and returns them as a <see cref="UserDto"/> record.</description></item>
    /// </list>
    /// </summary>
    /// <param name="samAccountName">
    /// The <c>sAMAccountName</c> attribute of the Active Directory user to search for.
    /// This is typically the user's logon name (e.g., <c>john.doe</c> or <c>JDOE</c>).
    /// </param>
    /// <returns>
    /// A <see cref="UserDto"/> record containing the user's <c>displayName</c>, <c>employeeID</c>,
    /// <c>mail</c>, and <c>userPrincipalName</c> attributes. If an attribute is not set on the
    /// user object, its value will be <c>null</c> (except <c>displayName</c>, which defaults to <c>"(unknown)"</c>).
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="samAccountName"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="UserNotFoundException">
    /// Thrown when no Active Directory user with the given <c>sAMAccountName</c> exists.
    /// </exception>
    /// <exception cref="DomainException">
    /// Thrown when an LDAP error occurs during the search (e.g., network failure, authentication error,
    /// or directory operation failure).
    /// </exception>
    public UserDto GetUser(string samAccountName)
    {
        // Validate input: reject null values immediately (fail-fast principle).
        ArgumentNullException.ThrowIfNull(samAccountName);

        // Log the search intent with structured parameters for easy correlation.
        _logger.LogInformation("Searching for user with samAccountName: {SamAccountName}", samAccountName);

        // Discover the Active Directory environment dynamically — no hard-coded parameters.
        // Returns: (domain name, Domain Controller hostname, LDAP Base DN / naming context).
        var (domainName, domainController, baseDN) = _discoveryService.Discover();

        // Create an LDAP connection to the discovered Domain Controller.
        // The connection is wrapped in a using declaration to ensure deterministic disposal
        // of the underlying network socket and unmanaged resources.
        using var connection = new LdapConnection(domainController);

        // Use Windows Integrated Authentication (Kerberos/Negotiate).
        // This allows Windows to negotiate the appropriate protocol automatically.
        // No credentials are stored or transmitted by the application.
        connection.AuthType = AuthType.Negotiate;

        try
        {
            // Escape the user-supplied sAMAccountName to prevent LDAP injection.
            // Special characters (\, *, (, ), null) are replaced with their LDAP escape sequences.
            var escapedSamAccountName = LdapFilterHelper.Escape(samAccountName);

            // Build the LDAP search filter to find a person object with the matching sAMAccountName.
            // - objectCategory=person: ensures we only search user objects (not contacts, etc.).
            // - objectClass=user: further restricts to user class objects.
            // - sAMAccountName={escaped}: matches the user's logon name.
            var filter = $"(&(objectCategory=person)(objectClass=user)(sAMAccountName={escapedSamAccountName}))";
            _logger.LogInformation("Executing LDAP search with filter: {Filter}", filter);

            // Create a subtree search request that fetches only the attributes we need.
            // SearchScope.Subtree searches the entire directory tree under the Base DN.
            var request = new SearchRequest(baseDN, filter, SearchScope.Subtree, _attributeNames.Keys.ToArray());

            // Send the request and cast the response to SearchResponse.
            // SendRequest blocks until the Directory Server responds or times out.
            var response = (SearchResponse)connection.SendRequest(request);

            // If no entries were returned, the user does not exist in Active Directory.
            if (response.Entries.Count == 0)
            {
                _logger.LogWarning("User with samAccountName {SamAccountName} not found.", samAccountName);
                throw new UserNotFoundException($"User with samAccountName '{samAccountName}' not found in Active Directory.");
            }

            // Extract the requested attributes from the first (and expected only) matching entry.
            // The ExtractAttributes helper converts the DirectoryAttribute collection into a plain Dictionary.
            var entry = response.Entries[0];
            var attributes = entry.ExtractAttributes(_attributeNames);

            // Map each attribute to its corresponding DTO field.
            // displayName defaults to "(unknown)" if the attribute is not set on the user object.
            // employeeID, mail, and userPrincipalName remain null if not set.
            var displayName = attributes["displayName"] ?? "(unknown)";
            var employeeId = attributes["employeeID"];
            var mail = attributes["mail"];
            var userPrincipalName = attributes["userPrincipalName"];

            // Log the successful lookup with structured output for auditability.
            _logger.LogInformation("Found user: {DisplayName} ({UPN})", displayName, userPrincipalName);

            // Return the strongly-typed DTO record to the caller.
            return new UserDto(displayName, employeeId, mail, userPrincipalName);
        }
        catch (LdapException ex)
        {
            // Catch low-level LDAP exceptions (network errors, authentication failures, timeouts)
            // and wrap them in a domain-friendly exception with full context.
            _logger.LogError(ex, "LDAP error while searching for user {SamAccountName}", samAccountName);
            throw new DomainException($"LDAP error while searching for user '{samAccountName}'.", ex);
        }
        catch (DirectoryOperationException ex)
        {
            // Catch directory operation errors (e.g., access denied, invalid filter, server-side errors)
            // and wrap them in a domain-friendly exception.
            _logger.LogError(ex, "Directory operation error while searching for user {SamAccountName}", samAccountName);
            throw new DomainException($"Directory operation failed while searching for user '{samAccountName}'.", ex);
        }
    }
}
