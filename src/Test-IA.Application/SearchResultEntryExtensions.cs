using System.DirectoryServices.Protocols;

namespace TestIA.Application;

/// <summary>
/// Provides extension methods for <see cref="SearchResultEntry"/> to simplify extraction of LDAP attributes.
/// <para>
/// These helpers reduce boilerplate code when reading attributes from LDAP search results.
/// Both methods handle null or empty attributes gracefully by returning <c>null</c> instead
/// of throwing exceptions.
/// </para>
/// </summary>
public static class SearchResultEntryExtensions
{
    /// <summary>
    /// Extracts multiple LDAP attributes from a search result entry into a dictionary.
    /// <para>
    /// Each attribute value is retrieved as a single string (the first value only). Attributes
    /// that are missing or empty are mapped to <c>null</c> in the result dictionary.
    /// </para>
    /// </summary>
    /// <param name="entry">
    /// The search result entry to extract attributes from. Must not be null.
    /// </param>
    /// <param name="attributeNames">
    /// A dictionary mapping LDAP attribute names (keys) to their display labels (values).
    /// Only the keys are used for extraction; the labels are preserved in the result for reference.
    /// </param>
    /// <returns>
    /// A dictionary with the same keys as <paramref name="attributeNames"/>, where each value
    /// is the extracted attribute string, or <c>null</c> if the attribute is missing or empty.
    /// </returns>
    public static Dictionary<string, string?> ExtractAttributes(
        this SearchResultEntry entry,
        Dictionary<string, string> attributeNames)
    {
        // Create a result dictionary with pre-allocated capacity.
        var result = new Dictionary<string, string?>(attributeNames.Count);

        // Iterate over each attribute name and extract its value from the entry.
        foreach (var (ldapName, label) in attributeNames)
        {
            // Retrieve the attribute from the entry's attribute collection.
            var attribute = entry.Attributes[ldapName];

            // If the attribute is missing or empty, map it to null; otherwise, cast the first value to string.
            result[ldapName] = (attribute == null || attribute.Count == 0)
                ? null
                : (string)attribute[0];
        }

        // Return the populated dictionary.
        return result;
    }

    /// <summary>
    /// Retrieves a single LDAP attribute value from a search result entry.
    /// <para>
    /// Returns the first value of the specified attribute as a string, or <c>null</c> if the
    /// attribute is missing or empty. This is a convenience method for single-value attributes.
    /// </para>
    /// </summary>
    /// <param name="entry">
    /// The search result entry to extract the attribute from. Must not be null.
    /// </param>
    /// <param name="attributeName">
    /// The LDAP attribute name to retrieve (e.g., <c>displayName</c>, <c>mail</c>).
    /// </param>
    /// <returns>
    /// The first value of the attribute as a string, or <c>null</c> if the attribute is missing or empty.
    /// </returns>
    public static string? GetAttributeValue(this SearchResultEntry entry, string attributeName)
    {
        // Retrieve the attribute from the entry's attribute collection.
        var attribute = entry.Attributes[attributeName];
        if (attribute == null || attribute.Count == 0)
        {
            // The attribute is missing or empty — return null.
            return null;
        }

        // Cast the first value to string and return it.
        return (string)attribute[0];
    }
}