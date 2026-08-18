using System.DirectoryServices.Protocols;
using TestIA.Domain;

namespace TestIA.Application;

/// <summary>
/// Maps LDAP group search result entries to <see cref="GroupDto"/> instances.
/// <para>
/// This mapper declares the two LDAP attributes that constitute a group record:
/// <c>displayName</c> and <c>member</c>. It extracts these attributes from a
/// <see cref="SearchResultEntry"/> and constructs a <see cref="GroupDto"/> record.
/// </para>
/// </summary>
/// <remarks>
/// <b>Attribute mapping:</b>
/// <list type="table">
///   <item><description><c>displayName</c> → <c>DisplayName</c> (defaults to "(unknown)" if missing)</description></item>
///   <item><description><c>member</c> → <c>Members</c> (array of Distinguished Names)</description></item>
/// </list>
/// <para>
/// <b>Note:</b> This mapper extracts raw Distinguished Names for the <c>member</c> attribute.
/// The calling service (<see cref="GetADGroupInfoService"/>) is responsible for resolving
/// each DN to its display name via additional LDAP searches.
/// </para>
/// </remarks>
public class GroupAttributeMapper : IAttributeMapper<GroupDto>
{
    /// <summary>
    /// Maps LDAP attribute names to their human-readable display labels.
    /// <para>
    /// This dictionary defines the attributes that will be requested from Active Directory
    /// during the group search. Only the <c>displayName</c> is mapped here; the <c>member</c>
    /// attribute is extracted separately because it requires special handling (array of DNs).
    /// </para>
    /// </summary>
    public static readonly Dictionary<string, string> Attributes = new()
    {
        { "displayName", "Display Name" },
    };

    /// <summary>
    /// Gets the dictionary mapping LDAP attribute names to their human-readable display labels.
    /// </summary>
    Dictionary<string, string> IAttributeMapper<GroupDto>.Attributes => Attributes;

    /// <summary>
    /// Maps an LDAP search result entry to a <see cref="GroupDto"/> instance.
    /// <para>
    /// This method extracts the <c>displayName</c> attribute and the raw <c>member</c>
    /// Distinguished Names from the entry, then constructs a new <see cref="GroupDto"/> record.
    /// </para>
    /// </summary>
    /// <param name="entry">
    /// The search result entry containing the group's LDAP attributes. Must not be null.
    /// </param>
    /// <returns>
    /// A new <see cref="GroupDto"/> instance populated with the extracted attribute values.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="entry"/> is null.</exception>
    public GroupDto Map(SearchResultEntry entry)
    {
        // Validate that the entry is not null.
        ArgumentNullException.ThrowIfNull(entry);

        // Extract the displayName attribute using the shared SearchResultEntryExtensions helper.
        var extracted = entry.ExtractAttributes(Attributes);
        var displayName = extracted["displayName"] ?? "(unknown)";

        // Extract the raw member Distinguished Names from the entry.
        // This is done separately because member is a multi-valued attribute that requires
        // special handling � the calling service resolves each DN to its display name.
        var memberDns = GetMemberValues(entry);

        // Return the strongly-typed DTO record with raw DNs.
        // The service layer will resolve DNs to display names before returning to the caller.
        return new GroupDto(displayName, memberDns);
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

    /// <summary>
    /// Extracts display values from a <see cref="GroupDto"/>, pairing each attribute's
    /// human-readable label with its corresponding value.
    /// <para>
    /// This method is used by presentation layers (e.g., console applications) to
    /// dynamically render group properties without hardcoding attribute names or labels.
    /// Empty member arrays are formatted as "(no members)" for clarity.
    /// </para>
    /// </summary>
    /// <param name="dto">
    /// The <see cref="GroupDto"/> instance returned by <see cref="Map"/>. Must not be null.
    /// </param>
    /// <returns>
    /// A dictionary with keys "Display Name" and "Members", and their corresponding values.
    /// The Members value is a comma-separated list of display names, or "(no members)" if empty.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="dto"/> is null.</exception>
    public Dictionary<string, string> GetDisplayValues(GroupDto dto)
    {
        // Validate that the DTO is not null.
        ArgumentNullException.ThrowIfNull(dto);

        // Build the display dictionary.
        // Members are joined as a comma-separated string, or shown as "(no members)" if empty.
        var membersDisplay = dto.Members.Length > 0
            ? string.Join(", ", dto.Members)
            : "(no members)";

        return new Dictionary<string, string>
        {
            { "Display Name", dto.DisplayName },
            { "Members", membersDisplay },
        };
    }
}
