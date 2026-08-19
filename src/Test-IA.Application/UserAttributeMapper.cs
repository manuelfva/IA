using System.DirectoryServices.Protocols;
using TestIA.Domain;

namespace TestIA.Application;

/// <summary>
/// Maps LDAP user search result entries to <see cref="UserDto"/> instances.
/// <para>
/// This mapper declares the twelve LDAP attributes that constitute a user record:
/// <c>displayName</c>, <c>employeeID</c>, <c>mail</c>, <c>userPrincipalName</c>,
/// <c>info</c>, <c>mobile</c>, <c>sAMAccountName</c>, <c>streetAddress</c>,
/// <c>l</c> (city), <c>st</c> (state), <c>postalCode</c>, <c>department</c>,
/// <c>title</c>, and <c>telephoneNumber</c>.
/// It extracts these attributes from a <see cref="SearchResultEntry"/> and constructs
/// a <see cref="UserDto"/> record.
/// </para>
/// </summary>
/// <remarks>
/// <b>Attribute mapping:</b>
/// <list type="table">
///   <item><description><c>displayName</c> → <c>DisplayName</c> (defaults to "(unknown)" if missing)</description></item>
///   <item><description><c>employeeID</c> → <c>EmployeeId</c> (nullable)</description></item>
///   <item><description><c>mail</c> → <c>Mail</c> (nullable)</description></item>
///   <item><description><c>userPrincipalName</c> → <c>UserPrincipalName</c> (nullable)</description></item>
///   <item><description><c>info</c> → <c>Info</c> (nullable)</description></item>
///   <item><description><c>mobile</c> → <c>Mobile</c> (nullable)</description></item>
///   <item><description><c>sAMAccountName</c> → <c>SamAccountName</c> (nullable)</description></item>
///   <item><description><c>streetAddress</c> → <c>StreetAddress</c> (nullable)</description></item>
///   <item><description><c>l</c> → <c>City</c> (nullable)</description></item>
///   <item><description><c>st</c> → <c>State</c> (nullable)</description></item>
///   <item><description><c>postalCode</c> → <c>PostalCode</c> (nullable)</description></item>
///   <item><description><c>department</c> → <c>Department</c> (nullable)</description></item>
///   <item><description><c>title</c> → <c>Title</c> (nullable)</description></item>
///   <item><description><c>telephoneNumber</c> → <c>PhoneNumber</c> (nullable)</description></item>
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
        { "info", "Description" },
        { "mobile", "Mobile Phone" },
        { "sAMAccountName", "samAccountName" },
        { "streetAddress", "Street Address" },
        { "l", "City" },
        { "st", "State" },
        { "postalCode", "Postal Code" },
        { "department", "Department" },
        { "title", "Title" },
        { "telephoneNumber", "Phone Number" },
    };

    /// <summary>
    /// Gets the dictionary mapping LDAP attribute names to their human-readable display labels.
    /// </summary>
    Dictionary<string, string> IAttributeMapper<UserDto>.Attributes => Attributes;

    /// <summary>
    /// Maps an LDAP search result entry to a <see cref="UserDto"/> instance.
    /// <para>
    /// This method extracts all declared attributes from the entry and constructs
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
        // employeeID, mail, userPrincipalName, info, mobile, samAccountName, streetAddress,
        // city (l), state (st), postalCode, department, title, and telephoneNumber remain null if not set.
        var displayName = extracted["displayName"] ?? "(unknown)";
        var employeeId = extracted["employeeID"];
        var mail = extracted["mail"];
        var userPrincipalName = extracted["userPrincipalName"];
        var info = extracted["info"];
        var mobile = extracted["mobile"];
        var samAccountName = extracted["sAMAccountName"];
        var streetAddress = extracted["streetAddress"];
        var city = extracted["l"];
        var state = extracted["st"];
        var postalCode = extracted["postalCode"];
        var department = extracted["department"];
        var title = extracted["title"];
        var phoneNumber = extracted["telephoneNumber"];

        // Return the strongly-typed DTO record with all fourteen attributes.
        return new UserDto(
            DisplayName: displayName,
            EmployeeId: employeeId,
            Mail: mail,
            UserPrincipalName: userPrincipalName,
            Info: info,
            Mobile: mobile,
            SamAccountName: samAccountName,
            StreetAddress: streetAddress,
            City: city,
            State: state,
            PostalCode: postalCode,
            Department: department,
            Title: title,
            PhoneNumber: phoneNumber);
    }

    /// <summary>
    /// Extracts display values from a <see cref="UserDto"/>, pairing each attribute's
    /// human-readable label with its corresponding value.
    /// <para>
    /// This method is used by presentation layers (e.g., console applications) to
    /// dynamically render user properties without hardcoding attribute names or labels.
    /// Null values are formatted as "(not set)" for clarity.
    /// </para>
    /// </summary>
    /// <param name="dto">
    /// The <see cref="UserDto"/> instance returned by <see cref="Map"/>. Must not be null.
    /// </param>
    /// <returns>
    /// A dictionary with keys such as "Display Name", "Employee ID", "Email", "UPN",
    /// "Description", "Mobile Phone", "samAccountName", "Street Address", "City",
    /// "State", "Postal Code", "Department", "Title", and "Phone Number",
    /// and their corresponding values.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="dto"/> is null.</exception>
    public Dictionary<string, string> GetDisplayValues(UserDto dto)
    {
        // Validate that the DTO is not null.
        ArgumentNullException.ThrowIfNull(dto);

        // Build the display dictionary by pairing each attribute label with its value.
        // Null values are formatted as "(not set)" for user-friendly output.
        return new Dictionary<string, string>
        {
            { "Display Name", dto.DisplayName },
            { "Employee ID", dto.EmployeeId ?? "(not set)" },
            { "Email", dto.Mail ?? "(not set)" },
            { "UPN", dto.UserPrincipalName ?? "(not set)" },
            { "Description", dto.Info ?? "(not set)" },
            { "Mobile Phone", dto.Mobile ?? "(not set)" },
            { "samAccountName", dto.SamAccountName ?? "(not set)" },
            { "Street Address", dto.StreetAddress ?? "(not set)" },
            { "City", dto.City ?? "(not set)" },
            { "State", dto.State ?? "(not set)" },
            { "Postal Code", dto.PostalCode ?? "(not set)" },
            { "Department", dto.Department ?? "(not set)" },
            { "Title", dto.Title ?? "(not set)" },
            { "Phone Number", dto.PhoneNumber ?? "(not set)" },
        };
    }
}
