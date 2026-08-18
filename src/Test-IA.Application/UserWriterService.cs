using System.DirectoryServices.Protocols;
using Microsoft.Extensions.Logging;
using TestIA.Domain;

namespace TestIA.Application;

/// <summary>
/// Updates Active Directory user attributes via LDAP modify operations.
/// <para>
/// This service searches for a user by <c>sAMAccountName</c>, collects the non-null attributes
/// from the update request, and performs a single LDAP modify request to apply all changes at once.
/// </para>
/// </summary>
/// <remarks>
/// <b>Supported attributes:</b>
/// <list type="bullet">
///   <item><description><c>info</c> — Free-form text field.</description></item>
///   <item><description><c>mobile</c> — Mobile phone number.</description></item>
///   <item><description><c>streetAddress</c> — Street address.</description></item>
///   <item><description><c>l</c> — City (locality).</description></item>
///   <item><description><c>st</c> — State or province.</description></item>
///   <item><description><c>postalCode</c> — Postal/ZIP code.</description></item>
///   <item><description><c>department</c> — User's department.</description></item>
///   <item><description><c>title</c> — User's job title.</description></item>
///   <item><description><c>telephoneNumber</c> — Office phone number.</description></item>
/// </list>
/// <para>
/// <b>Authentication:</b> Uses the current Windows user's security context via
/// <c>AuthType.Negotiate</c>. No credentials are stored, transmitted, or prompted.
/// </para>
/// </remarks>
public class UserWriterService : IUserWriter
{
    /// <summary>
    /// Service for dynamically discovering the Active Directory environment.
    /// </summary>
    private readonly ADDomainDiscoveryService _discoveryService;

    /// <summary>
    /// Logger for structured application events (updates, errors, results).
    /// </summary>
    private readonly ILogger<UserWriterService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="UserWriterService"/> class.
    /// </summary>
    /// <param name="discoveryService">
    /// The Active Directory discovery service used to locate the domain, Domain Controller,
    /// and LDAP Base DN dynamically at runtime. Must not be null.
    /// </param>
    /// <param name="logger">
    /// The logger instance used for structured logging of user update operations.
    /// Must not be null.
    /// </param>
    public UserWriterService(ADDomainDiscoveryService discoveryService, ILogger<UserWriterService> logger)
    {
        _discoveryService = discoveryService ?? throw new ArgumentNullException(nameof(discoveryService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    /// <summary>
    /// Updates multiple Active Directory user attributes in a single LDAP modify operation.
    /// <para>
    /// This method searches for the user by <c>sAMAccountName</c>, collects all non-null attributes
    /// from the request, and sends a single <c>ModifyRequest</c> to apply all changes atomically.
    /// </para>
    /// </summary>
    /// <param name="request">
    /// The update request containing the user's <c>sAMAccountName</c> and the attributes to update.
    /// Must not be null, and <c>SamAccountName</c> must not be empty.
    /// </param>
    /// <returns>
    /// A <see cref="UserUpdateResult"/> indicating success or failure, along with a descriptive message.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="request"/> is null.</exception>
    /// <exception cref="UserNotFoundException">Thrown when the user does not exist in Active Directory.</exception>
    /// <exception cref="DomainException">Thrown when an LDAP error occurs during the update.</exception>
    public UserUpdateResult UpdateUser(UserUpdateRequest request)
    {
        // Validate that the request object is not null.
        ArgumentNullException.ThrowIfNull(request);
        // Validate that the samAccountName is not null or empty.
        ArgumentNullException.ThrowIfNullOrEmpty(request.SamAccountName);

        // Log the update intent for observability.
        _logger.LogInformation("Updating user attributes for samAccountName: {SamAccountName}", request.SamAccountName);

        // Discover the Active Directory environment (domain, DC, Base DN).
        var (domainName, domainController, baseDN) = _discoveryService.Discover();

        // Create an LDAP connection to the discovered Domain Controller.
        // AuthType.Negotiate uses Windows Integrated Authentication (Kerberos/NTLM).
        using var connection = new LdapConnection(domainController);
        connection.AuthType = AuthType.Negotiate;

        try
        {
            // Safely escape the samAccountName to prevent LDAP injection.
            var escapedSamAccountName = LdapFilterHelper.Escape(request.SamAccountName);

            // Build the LDAP search filter to find the user object.
            // - objectCategory=person: ensures we only search user objects.
            // - objectClass=user: further restricts to user class objects.
            // - sAMAccountName={escaped}: matches the user's logon name.
            var searchFilter = $"(&(objectCategory=person)(objectClass=user)(sAMAccountName={escapedSamAccountName}))";
            _logger.LogInformation("Searching for user DN with filter: {Filter}", searchFilter);

            // Search for the user to obtain their Distinguished Name.
            var searchRequest = new SearchRequest(baseDN, searchFilter, SearchScope.Subtree);
            var searchResponse = (SearchResponse)connection.SendRequest(searchRequest);

            // If no entries were returned, the user does not exist in Active Directory.
            if (searchResponse.Entries.Count == 0)
            {
                _logger.LogWarning("User with samAccountName {SamAccountName} not found for update.", request.SamAccountName);
                throw new UserNotFoundException($"User with samAccountName '{request.SamAccountName}' not found in Active Directory.");
            }

            // Extract the user's Distinguished Name from the search result.
            var userEntry = searchResponse.Entries[0];
            var userDn = userEntry.DistinguishedName;
            _logger.LogInformation("Found user DN: {DistinguishedName}", userDn);

            // Prepare a list to collect the LDAP modifications.
            var modifications = new List<DirectoryAttributeModification>();

            // Helper lambda to queue an attribute modification.
            var addModification = (string name, string value) =>
            {
                // Create a Replace operation for this attribute.
                var mod = new DirectoryAttributeModification { Name = name, Operation = DirectoryAttributeOperation.Replace };
                mod.Add(value);
                modifications.Add(mod);
            };

            // Queue each non-null attribute from the request.
            if (request.Info is not null)
            {
                addModification("info", request.Info);
                _logger.LogDebug("Queued 'info' attribute update.");
            }

            if (request.Mobile is not null)
            {
                addModification("mobile", request.Mobile);
                _logger.LogDebug("Queued 'mobile' attribute update.");
            }

            if (request.StreetAddress is not null)
            {
                addModification("streetAddress", request.StreetAddress);
                _logger.LogDebug("Queued 'streetAddress' attribute update.");
            }

            if (request.City is not null)
            {
                // 'l' is the LDAP attribute name for city (locality).
                addModification("l", request.City);
                _logger.LogDebug("Queued 'l' (city) attribute update.");
            }

            if (request.State is not null)
            {
                // 'st' is the LDAP attribute name for state/province.
                addModification("st", request.State);
                _logger.LogDebug("Queued 'st' (state) attribute update.");
            }

            if (request.PostalCode is not null)
            {
                addModification("postalCode", request.PostalCode);
                _logger.LogDebug("Queued 'postalCode' attribute update.");
            }

            if (request.Department is not null)
            {
                addModification("department", request.Department);
                _logger.LogDebug("Queued 'department' attribute update.");
            }

            if (request.Title is not null)
            {
                addModification("title", request.Title);
                _logger.LogDebug("Queued 'title' attribute update.");
            }

            if (request.PhoneNumber is not null)
            {
                // 'telephoneNumber' is the LDAP attribute name for office phone.
                addModification("telephoneNumber", request.PhoneNumber);
                _logger.LogDebug("Queued 'telephoneNumber' attribute update.");
            }

            // If no attributes were queued, return early with a warning.
            if (modifications.Count == 0)
            {
                _logger.LogWarning("No attributes to update for user {SamAccountName}.", request.SamAccountName);
                return new UserUpdateResult(false, "No attributes provided for update.");
            }

            // Log the number of attributes being updated.
            _logger.LogInformation("Executing LDAP modify request with {ModificationCount} attribute(s) for user {SamAccountName}", modifications.Count, request.SamAccountName);

            // Build and send the LDAP modify request to update all attributes atomically.
            var modifyRequest = new ModifyRequest(userDn, modifications.ToArray());
            connection.SendRequest(modifyRequest);

            // Log the successful update with the list of modified attributes.
            _logger.LogInformation("Successfully updated {ModificationCount} attribute(s) for user {SamAccountName}", modifications.Count, request.SamAccountName);

            var updatedAttributes = string.Join(", ", modifications.Select(m => m.Name));
            return new UserUpdateResult(true, $"Successfully updated: {updatedAttributes}");
        }
        catch (LdapException ex)
        {
            // Catch low-level LDAP exceptions (network errors, authentication failures, timeouts)
            // and wrap them in a domain-friendly exception with full context.
            _logger.LogError(ex, "LDAP error while updating user {SamAccountName}", request.SamAccountName);
            throw new DomainException($"LDAP error while updating user '{request.SamAccountName}'.", ex);
        }
        catch (DirectoryOperationException ex)
        {
            // Catch directory operation errors (e.g., access denied, invalid filter, server-side errors)
            // and wrap them in a domain-friendly exception.
            _logger.LogError(ex, "Directory operation error while updating user {SamAccountName}", request.SamAccountName);
            throw new DomainException($"Directory operation failed while updating user '{request.SamAccountName}'.", ex);
        }
    }
}
