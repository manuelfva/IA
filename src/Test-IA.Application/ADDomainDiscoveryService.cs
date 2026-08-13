using System.DirectoryServices.Protocols;
using Microsoft.Extensions.Logging;
using TestIA.Domain;
using ADSI = System.DirectoryServices;
using ADS = System.DirectoryServices.ActiveDirectory;

namespace TestIA.Application;

/// <summary>
/// Service for dynamically discovering the Active Directory environment, Domain Controller, and LDAP naming context.
/// </summary>
public class ADDomainDiscoveryService
{
    private readonly ILogger<ADDomainDiscoveryService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ADDomainDiscoveryService"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    public ADDomainDiscoveryService(ILogger<ADDomainDiscoveryService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Discovers the current Active Directory domain, a suitable Domain Controller, and the LDAP Base DN (naming context).
    /// </summary>
    /// <returns>A tuple containing the domain name, Domain Controller DNS host name, and the LDAP Base DN.</returns>
    /// <exception cref="DomainException">Thrown when the machine is not joined to a domain, a Domain Controller cannot be discovered, or the LDAP naming context cannot be obtained.</exception>
    public virtual (string DomainName, string DomainController, string BaseDN) Discover()
    {
        _logger.LogInformation("Starting Active Directory environment discovery.");

        // Step 1: Discover the current domain
        var domain = GetCurrentDomain();
        _logger.LogInformation("Discovered domain: {DomainName}", domain.Name);

        // Step 2: Discover a Domain Controller
        var domainController = GetDomainController(domain);
        _logger.LogInformation("Discovered Domain Controller: {DomainController}", domainController);

        // Step 3: Obtain the Base DN via RootDSE
        var baseDN = GetBaseDN(domainController);
        _logger.LogInformation("Discovered Base DN: {BaseDN}", baseDN);

        return (domain.Name, domainController, baseDN);
    }

    private ADS.Domain GetCurrentDomain()
    {
        try
        {
            var domain = ADS.Domain.GetCurrentDomain();
            if (domain == null)
            {
                throw new DomainException("The local machine is not joined to an Active Directory domain.");
            }

            return domain;
        }
        catch (ADS.ActiveDirectoryObjectNotFoundException)
        {
            throw new DomainException("The local machine is not joined to an Active Directory domain.");
        }
    }

    private string GetDomainController(ADS.Domain domain)
    {
        try
        {
            // Use DirectoryEntry to get the domain controller DNS host name
            using var rootDse = new ADSI.DirectoryEntry("LDAP://RootDSE");
            var dnsHostName = rootDse.Properties["dnsHostName"].Value?.ToString();

            if (string.IsNullOrEmpty(dnsHostName))
            {
                throw new DomainException("Could not discover the DNS host name of the Domain Controller.");
            }

            return dnsHostName;
        }
        catch (ADS.ActiveDirectoryObjectNotFoundException)
        {
            throw new DomainException("Could not discover a Domain Controller for the current domain.");
        }
    }

    private string GetBaseDN(string domainController)
    {
        using var connection = new LdapConnection(domainController);
        connection.AuthType = AuthType.Negotiate;

        try
        {
            _logger.LogInformation("Querying RootDSE on {DomainController} to obtain the naming context.", domainController);
            var request = new SearchRequest(string.Empty, "(objectClass=*)", SearchScope.Base, "defaultNamingContext");
            var response = (SearchResponse)connection.SendRequest(request);

            if (response.Entries.Count == 0)
            {
                throw new DomainException("RootDSE query returned no entries. Unable to obtain the naming context.");
            }

            var namingContextAttribute = response.Entries[0].Attributes["defaultNamingContext"];
            if (namingContextAttribute == null || namingContextAttribute.Count == 0)
            {
                throw new DomainException("RootDSE does not contain the defaultNamingContext attribute.");
            }

            return (string)namingContextAttribute[0];
        }
        catch (LdapException ex)
        {
            _logger.LogError(ex, "LDAP error while querying RootDSE on {DomainController}", domainController);
            throw new DomainException($"Failed to query RootDSE on {domainController}.", ex);
        }
        catch (DirectoryOperationException ex)
        {
            _logger.LogError(ex, "Directory operation error while querying RootDSE on {DomainController}", domainController);
            throw new DomainException($"Directory operation failed while querying RootDSE on {domainController}.", ex);
        }
    }
}
