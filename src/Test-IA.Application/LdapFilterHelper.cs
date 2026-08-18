using System.Text;

namespace TestIA.Application;

/// <summary>
/// Provides utility methods for safely escaping values used in LDAP search filters.
/// <para>
/// LDAP filters are vulnerable to injection attacks if user-supplied values are not properly
/// escaped. This class escapes the five special characters that have meaning in LDAP filters:
/// backslash, asterisk, left parenthesis, right parenthesis, and null character.
/// </para>
/// <para>
/// This class is <c>internal static</c> because it has no dependencies and is only used
/// internally by the Application layer services. It is not registered in the DI container.
/// </para>
/// </summary>
/// <remarks>
/// <b>Escaping rules:</b>
/// <list type="table">
///   <item><description><c>\</c> → <c>\\</c></description><description>Backslash must be escaped first.</description></item>
///   <item><description><c>*</c> → <c>\2a</c></description><description>Wildcard character.</description></item>
///   <item><description><c>(</c> → <c>\28</c></description><description>Start of filter expression.</description></item>
///   <item><description><c>)</c> → <c>\29</c></description><description>End of filter expression.</description></item>
///   <item><description><c>\0</c> → <c>\00</c></description><description>Null character.</description></item>
/// </list>
/// </remarks>
internal static class LdapFilterHelper
{
    /// <summary>
    /// Escapes special LDAP characters in a string value to prevent LDAP injection attacks.
    /// <para>
    /// This method iterates over each character in the input string and replaces the five
    /// LDAP-special characters with their escaped equivalents (backslash followed by the
    /// hexadecimal code). All other characters are passed through unchanged.
    /// </para>
    /// </summary>
    /// <param name="value">
    /// The string value to escape (e.g., a user-supplied <c>sAMAccountName</c> or Distinguished Name).
    /// May be null, in which case an empty string is returned.
    /// </param>
    /// <returns>
    /// The escaped string, safe to embed in an LDAP filter. Returns an empty string if the
    /// input is null.
    /// </returns>
    /// <example>
    /// <c>Escape("admin*(test)")</c> returns <c>"admin\\2a\\28test\\29"</c>
    /// </example>
    public static string Escape(string value)
    {
        // Return empty string for null input to avoid null reference issues.
        if (value is null)
        {
            return string.Empty;
        }

        // Allocate a StringBuilder with estimated capacity (each character could become 2 chars).
        var result = new StringBuilder(value.Length * 2);

        // Iterate over each character and apply the LDAP escaping rules.
        foreach (var c in value)
        {
            result.Append(c switch
            {
                // Backslash must be escaped first (it is the escape character itself).
                '\\' => "\\\\",
                // Asterisk is a wildcard — escape to prevent pattern matching.
                '*' => @"\2a",
                // Left parenthesis opens a filter expression — escape to prevent manipulation.
                '(' => @"\28",
                // Right parenthesis closes a filter expression — escape to prevent manipulation.
                ')' => @"\29",
                // Null character can truncate strings — escape to prevent injection.
                '\0' => @"\00",
                // All other characters pass through unchanged.
                _ => c.ToString()
            });
        }

        // Return the fully escaped string.
        return result.ToString();
    }
}
