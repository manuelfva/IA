using TestIA.Domain;

namespace TestIA.Application;

/// <summary>
/// Maps user update request properties to their human-readable display labels.
/// <para>
/// This mapper provides a centralized mapping between <see cref="UserUpdateRequest"/> properties
/// and the display labels used in the presentation layer (WebApp Razor Pages). It enables
/// dynamic rendering of update form fields without hardcoding attribute names or labels in HTML.
/// </para>
/// </summary>
/// <remarks>
/// <b>Supported attributes (all 10):</b>
/// <list type="bullet">
///   <item><description><c>SamAccountName</c> — User logon name (required).</description></item>
///   <item><description><c>Info</c> — Free-form text field.</description></item>
///   <item><description><c>Mobile</c> — Mobile phone number.</description></item>
///   <item><description><c>StreetAddress</c> — Street address.</description></item>
///   <item><description><c>City</c> — City (locality).</description></item>
///   <item><description><c>State</c> — State or province.</description></item>
///   <item><description><c>PostalCode</c> — Postal/ZIP code.</description></item>
///   <item><description><c>Department</c> — User's department.</description></item>
///   <item><description><c>Title</c> — User's job title.</description></item>
///   <item><description><c>PhoneNumber</c> — Office phone number.</description></item>
/// </list>
/// <para>
/// <b>Usage:</b> Inject <c>IAttributeMapper&lt;UserUpdateRequest&gt;</c> into the page model
/// and call <c>GetDisplayValues(request)</c> to obtain a dictionary of labels and values
/// for dynamic rendering in Razor Pages.
/// </para>
/// </remarks>
public class UserUpdateAttributeMapper : IAttributeMapper<UserUpdateRequest>
{
    /// <summary>
    /// Maps user update request property names to their human-readable display labels.
    /// </summary>
    public static readonly Dictionary<string, string> Attributes = new()
    {
        { "SamAccountName", "samAccountName" },
        { "Info", "Info" },
        { "Mobile", "Mobile" },
        { "StreetAddress", "Street Address" },
        { "City", "City" },
        { "State", "State" },
        { "PostalCode", "Postal Code" },
        { "Department", "Department" },
        { "Title", "Title" },
        { "PhoneNumber", "Phone Number" },
    };

    /// <summary>
    /// Gets the dictionary mapping property names to their human-readable display labels.
    /// </summary>
    Dictionary<string, string> IAttributeMapper<UserUpdateRequest>.Attributes => Attributes;

    /// <summary>
    /// Maps an LDAP search result entry to a <see cref="UserUpdateRequest"/> instance.
    /// <para>
    /// This method extracts all declared attributes from the entry and constructs
    /// a new <see cref="UserUpdateRequest"/> record. Attributes that are missing or empty
    /// are mapped to <c>null</c>.
    /// </para>
    /// </summary>
    /// <param name="entry">
    /// The search result entry containing the LDAP attributes to extract. Must not be null.
    /// </param>
    /// <returns>
    /// A new <see cref="UserUpdateRequest"/> instance populated with the extracted attribute values.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="entry"/> is null.</exception>
    public UserUpdateRequest Map(System.DirectoryServices.Protocols.SearchResultEntry entry)
    {
        // Validate that the entry is not null.
        ArgumentNullException.ThrowIfNull(entry);

        // Extract all declared attributes using the shared SearchResultEntryExtensions helper.
        var extracted = entry.ExtractAttributes(Attributes);

        // Map each extracted attribute to the corresponding request field.
        // All fields remain null if not set on the entry except SamAccountName (required).
        return new UserUpdateRequest(
            SamAccountName: extracted["SamAccountName"] ?? string.Empty,
            Info: extracted["Info"],
            Mobile: extracted["Mobile"],
            StreetAddress: extracted["StreetAddress"],
            City: extracted["City"],
            State: extracted["State"],
            PostalCode: extracted["PostalCode"],
            Department: extracted["Department"],
            Title: extracted["Title"],
            PhoneNumber: extracted["PhoneNumber"]);
    }

    /// <summary>
    /// Extracts display values from a <see cref="UserUpdateRequest"/>, pairing each property's
    /// human-readable label with its corresponding value.
    /// Null or empty values are formatted as "(not set)" for clarity.
    /// </summary>
    /// <param name="request">The <see cref="UserUpdateRequest"/> instance. Must not be null.</param>
    /// <returns>A dictionary where keys are display labels and values are the formatted property values.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="request"/> is null.</exception>
    public Dictionary<string, string> GetDisplayValues(UserUpdateRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new Dictionary<string, string>
        {
            { "samAccountName", !string.IsNullOrWhiteSpace(request.SamAccountName) ? request.SamAccountName : "(not set)" },
            { "Info", !string.IsNullOrWhiteSpace(request.Info) ? request.Info : "(not set)" },
            { "Mobile", !string.IsNullOrWhiteSpace(request.Mobile) ? request.Mobile : "(not set)" },
            { "Street Address", !string.IsNullOrWhiteSpace(request.StreetAddress) ? request.StreetAddress : "(not set)" },
            { "City", !string.IsNullOrWhiteSpace(request.City) ? request.City : "(not set)" },
            { "State", !string.IsNullOrWhiteSpace(request.State) ? request.State : "(not set)" },
            { "Postal Code", !string.IsNullOrWhiteSpace(request.PostalCode) ? request.PostalCode : "(not set)" },
            { "Department", !string.IsNullOrWhiteSpace(request.Department) ? request.Department : "(not set)" },
            { "Title", !string.IsNullOrWhiteSpace(request.Title) ? request.Title : "(not set)" },
            { "Phone Number", !string.IsNullOrWhiteSpace(request.PhoneNumber) ? request.PhoneNumber : "(not set)" },
        };
    }
}