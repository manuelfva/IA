using System.DirectoryServices.Protocols;
using Microsoft.Extensions.Logging;
using TestIA.Domain;
using ADSI = System.DirectoryServices;
using ADS = System.DirectoryServices.ActiveDirectory;

namespace TestIA.Application;

/// <summary>
/// Performs dynamic discovery of the Active Directory environment by locating the current domain,
/// identifying an available Domain Controller, and obtaining the LDAP naming context (Base DN).
/// <para>
/// This service is the foundation for all LDAP operations in the application. It uses Windows
/// Active Directory APIs to discover the environment at runtime, ensuring that no connection
/// parameters are hard-coded in configuration files or source code.
/// </para>
/// </summary>
/// <remarks>
/// <b>Discovery process (3-step):</b>
/// <list type="number">
///   <item><description><b>Domain:</b> Uses <c>Domain.GetCurrentDomain()</c> to determine the domain the local machine is joined to.</description></item>
///   <item><description><b>Domain Controller:</b> Queries the RootDSE via <c>DirectoryEntry("LDAP://RootDSE")</c> to obtain the DNS host name of a Domain Controller.</description></item>
///   <item><description><b>Base DN:</b> Opens an LDAP connection to the discovered DC and queries RootDSE for the <c>defaultNamingContext</c> attribute.</description></item>
/// </list>
/// <para>
/// <b>Why virtual?</b> The <see cref="Discover"/> method is declared <c>virtual</c> so that
/// unit tests can override it and return mock values without needing to mock the entire service.
/// This simplifies test setup while keeping the production behavior intact.
/// </para>
/// </remarks>
public class ADDomainDiscoveryService
{
    /// <summary>
    /// Logger for structured application events (discovery steps, errors, results).
    /// </summary>
    private readonly ILogger<ADDomainDiscoveryService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ADDomainDiscoveryService"/> class.
    /// </summary>
    /// <param name="logger">
    /// The logger instance used for structured logging of the discovery process.
    /// Must not be null.
    /// </param>
    public ADDomainDiscoveryService(ILogger<ADDomainDiscoveryService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Executes the three-step discovery process to locate the Active Directory domain,
    /// an available Domain Controller, and the LDAP naming context (Base DN).
    /// <para>
    /// This is the public entry point for AD environment discovery. It orchestrates the
    /// discovery steps sequentially and returns all discovered values as a tuple.
    /// </para>
    /// </summary>
    /// <returns>
    /// A tuple containing:
    /// <list type="table">
    ///   <item><description><c>DomainName</c> — The DNS name of the Active Directory domain (e.g., <c>corp.example.com</c>).</description></item>
    ///   <item><description><c>DomainController</c> — The DNS host name of a Domain Controller (e.g., <c>dc01.corp.example.com</c>).</description></item>
    ///   <item><description><c>BaseDN</c> — The LDAP naming context used as the search base (e.g., <c>DC=corp,DC=example,DC=com</c>).</description></item>
    /// </list>
    /// </returns>
    /// <exception cref="DomainException">
    /// Thrown when the machine is not joined to a domain, a Domain Controller cannot be discovered,
    /// or the LDAP naming context cannot be obtained from RootDSE.
    /// </exception>
    public virtual (string DomainName, string DomainController, string BaseDN) Discover()
    {
        // Log the start of the discovery process for observability.
        _logger.LogInformation("Starting Active Directory environment discovery.");

        // Step 1: Determine the Active Directory domain the local machine belongs to.
        var domain = GetCurrentDomain();
        _logger.LogInformation("Discovered domain: {DomainName}", domain.Name);

        // Step 2: Locate a Domain Controller for the discovered domain.
        var domainController = GetDomainController(domain);
        _logger.LogInformation("Discovered Domain Controller: {DomainController}", domainController);

        // Step 3: Query the Domain Controller's RootDSE to obtain the LDAP Base DN.
        var baseDN = GetBaseDN(domainController);
        _logger.LogInformation("Discovered Base DN: {BaseDN}", baseDN);

        // Return all discovered values as a single tuple.
        return (domain.Name, domainController, baseDN);
    }

    /// <summary>
    /// Determines the Active Directory domain to which the local computer belongs.
    /// <para>
    /// Uses the <c>System.DirectoryServices.ActiveDirectory.Domain.GetCurrentDomain()</c> API,
    /// which queries the local machine's domain membership information. If the machine is not
    /// joined to a domain, this method throws a <see cref="DomainException"/>.
    /// </para>
    /// </summary>
    /// <returns>
    /// An <see cref="ADS.Domain"/> object representing the current domain.
    /// </returns>
    /// <exception cref="DomainException">
    /// Thrown when the local machine is not joined to an Active Directory domain.
    /// </exception>
    private ADS.Domain GetCurrentDomain()
    {
        try
        {
            // Ask Windows for the domain this machine is joined to.
            // Returns null if the machine is in a workgroup or not joined to any domain.
            var domain = ADS.Domain.GetCurrentDomain();
            if (domain == null)
            {
                // The machine is not domain-joined — this is a fatal configuration error.
                throw new DomainException("The local machine is not joined to an Active Directory domain.");
            }

            return domain;
        }
        catch (ADS.ActiveDirectoryObjectNotFoundException)
        {
            // Same as above: the ADSI API throws when no domain is found.
            // We wrap it in a domain exception for consistency.
            throw new DomainException("The local machine is not joined to an Active Directory domain.");
        }
    }

    /// <summary>
    /// Discovers the DNS host name of an available Domain Controller for the given domain.
    /// <para>
    /// Queries the local machine's RootDSE via <c>DirectoryEntry("LDAP://RootDSE")</c> to
    /// obtain the <c>dnsHostName</c> attribute, which contains the fully qualified domain
    /// name of the Domain Controller that responded to the RootDSE query.
    /// </para>
    /// </summary>
    /// <param name="domain">
    /// The <see cref="ADS.Domain"/> object representing the target domain.
    /// </param>
    /// <returns>
    /// The DNS host name of the Domain Controller (e.g., <c>dc01.corp.example.com</c>).
    /// </returns>
    /// <exception cref="DomainException">
    /// Thrown when the RootDSE query fails or the <c>dnsHostName</c> attribute is empty.
    /// </exception>
    private string GetDomainController(ADS.Domain domain)
    {
        try
        {
            // Query the RootDSE object on the local machine to find the Domain Controller.
            // The "LDAP://RootDSE" is a virtual object — it always exists and responds
            // with information about the directory server that handled the request.
            using var rootDse = new ADSI.DirectoryEntry("LDAP://RootDSE");
            var dnsHostName = rootDse.Properties["dnsHostName"].Value?.ToString();

            // If dnsHostName is null or empty, the RootDSE query failed to return a DC name.
            if (string.IsNullOrEmpty(dnsHostName))
            {
                throw new DomainException("Could not discover the DNS host name of the Domain Controller.");
            }

            return dnsHostName;
        }
        catch (ADS.ActiveDirectoryObjectNotFoundException)
        {
            // The ADSI API throws when it cannot reach any Domain Controller.
            throw new DomainException("Could not discover a Domain Controller for the current domain.");
        }
    }

    /// <summary>
    /// Obtains the LDAP naming context (Base DN) by querying the RootDSE of a Domain Controller.
    /// <para>
    /// Opens a real LDAP connection to the discovered Domain Controller and performs a
    /// base-level search on the RootDSE (empty DN) to read the <c>defaultNamingContext</c>
    /// attribute. This attribute contains the distinguished name of the domain's naming context,
    /// which serves as the search base for all directory operations.
    /// </para>
    /// </summary>
    /// <param name="domainController">
    /// The DNS host name of the Domain Controller to query (e.g., <c>dc01.corp.example.com</c>).
    /// </param>
    /// <returns>
    /// The LDAP Base DN (naming context) as a string (e.g., <c>DC=corp,DC=example,DC=com</c>).
    /// </returns>
    /// <exception cref="DomainException">
    /// Thrown when the LDAP connection fails, the RootDSE returns no entries, or the
    /// <c>defaultNamingContext</c> attribute is missing.
    /// </exception>
    private string GetBaseDN(string domainController)
    {
        // Create an LDAP connection to the discovered Domain Controller.
        // AuthType.Negotiate tells Windows to use the current user's security context
        // (Kerberos when available, NTLM as fallback). No credentials are needed.
        using var connection = new LdapConnection(domainController);
        connection.AuthType = AuthType.Negotiate;

        try
        {
            // Query RootDSE (empty DN) with a base-level search for the defaultNamingContext attribute.
            // The RootDSE is a special LDAP object that always exists and provides directory metadata.
            _logger.LogInformation("Querying RootDSE on {DomainController} to obtain the naming context.", domainController);
            var request = new SearchRequest(string.Empty, "(objectClass=*)", SearchScope.Base, "defaultNamingContext");
            var response = (SearchResponse)connection.SendRequest(request);

            // RootDSE should always return exactly one entry. If not, something is wrong.
            if (response.Entries.Count == 0)
            {
                throw new DomainException("RootDSE query returned no entries. Unable to obtain the naming context.");
            }

            // Extract the defaultNamingContext attribute from the RootDSE entry.
            var namingContextAttribute = response.Entries[0].Attributes["defaultNamingContext"];
            if (namingContextAttribute == null || namingContextAttribute.Count == 0)
            {
                // The RootDSE exists but does not provide a naming context — unusual but possible
                // in misconfigured or partially joined environments.
                throw new DomainException("RootDSE does not contain the defaultNamingContext attribute.");
            }

            // Cast the first (and only) value to string and return it.
            return (string)namingContextAttribute[0];
        }
        catch (LdapException ex)
        {
            // Network-level LDAP errors (connection refused, timeout, authentication failure).
            _logger.LogError(ex, "LDAP error while querying RootDSE on {DomainController}", domainController);
            throw new DomainException($"Failed to query RootDSE on {domainController}.", ex);
        }
        catch (DirectoryOperationException ex)
        {
            // Server-level errors (access denied, invalid filter, server-side processing failure).
            _logger.LogError(ex, "Directory operation error while querying RootDSE on {DomainController}", domainController);
            throw new DomainException($"Directory operation failed while querying RootDSE on {domainController}.", ex);
        }
    }
}
