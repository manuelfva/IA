using System.DirectoryServices.Protocols;
using TestIA.Domain;

namespace TestIA.Application;

/// <summary>
/// Maps LDAP user search result entries to <see cref="UserDto"/> instances.
/// <para>
/// This mapper declares the four LDAP attributes that constitute a user record:
/// <c>displayName</c>, <c>employeeID</c>, <c>mail</c>, and <c>userPrincipalName</c>.
/// It extracts these attributes from a <see cref="SearchResultEntry"/> and constructs
/// a <see cref="UserDto"/> record.
/// </para>
/// </summary>
/// <remarks>
/// <b>Attribute mapping:</b>
/// <list type="table">
///   <item><description><c>displayName</c> ? <c>DisplayName</c> (defaults to "(unknown)" if missing)</description></item>
///   <item><description><c>employeeID</c> ? <c>EmployeeId</c> (nullable)</description></item>
///   <item><description><c>mail</c> ? <c>Mail</c> (nullable)</description></item>
///   <item><description><c>userPrincipalName</c> ? <c>UserPrincipalName</c> (nullable)</description></item>
/// </list>
/// </remarks>
public class UserAttributeMapper : IAttributeMapper<UserDto>
{
    /// <summary>
    /// Maps LDAP attribute names to their human-readable display labels.
    /// <para>
    /// This dictionary defines the exact set of attributes that will be requested from
    /// Active Directory during the LDAP search. Only these attributes are fetched,
    /// which reduces network traffic and improves performance.
    /// </para>
    /// </summary>
    public static readonly Dictionary<string, string> Attributes = new()
    {
        { "displayName", "Display Name" },
        { "employeeID", "Employee ID" },
        { "mail", "Email" },
        { "userPrincipalName", "UPN" },
    };

    /// <summary>
    /// Gets the dictionary mapping LDAP attribute names to their human-readable display labels.
    /// </summary>
    Dictionary<string, string> IAttributeMapper<UserDto>.Attributes => Attributes;

    /// <summary>
    /// Maps an LDAP search result entry to a <see cref="UserDto"/> instance.
    /// <para>
    /// This method extracts all four declared attributes from the entry and constructs
    /// a new <see cref="UserDto"/> record. The <c>displayName</c> defaults to "(unknown)"
    /// if the attribute is not set on the user object.
    /// </para>
    /// </summary>
    /// <param name="entry">
    /// The search result entry containing the user's LDAP attributes. Must not be null.
    /// </param>
    /// <returns>
    /// A new <see cref="UserDto"/> instance populated with the extracted attribute values.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="entry"/> is null.</exception>
    public UserDto Map(SearchResultEntry entry)
    {
        // Validate that the entry is not null.
        ArgumentNullException.ThrowIfNull(entry);

        // Extract all declared attributes using the shared SearchResultEntryExtensions helper.
        var extracted = entry.ExtractAttributes(Attributes);

        // Map each extracted attribute to the corresponding DTO field.
        // displayName defaults to "(unknown)" if the attribute is not set on the user object.
        // employeeID, mail, and userPrincipalName remain null if not set.
        var displayName = extracted["displayName"] ?? "(unknown)";
        var employeeId = extracted["employeeID"];
        var mail = extracted["mail"];
        var userPrincipalName = extracted["userPrincipalName"];

        // Return the strongly-typed DTO record.
        return new UserDto(displayName, employeeId, mail, userPrincipalName);
    }
}
