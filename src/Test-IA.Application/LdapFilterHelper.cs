using System.Text;
using System.DirectoryServices.Protocols;
using Microsoft.Extensions.Logging;
using TestIA.Domain;

namespace TestIA.Application;

/// <summary>
/// Helper class for LDAP filter escaping.
/// </summary>
internal static class LdapFilterHelper
{
    /// <summary>
    /// Escapes special characters in an LDAP filter value.
    /// </summary>
    /// <param name="value">The value to escape.</param>
    /// <returns>The escaped value suitable for use in an LDAP filter.</returns>
    public static string Escape(string value)
    {
        if (value is null)
        {
            return string.Empty;
        }

        var result = new StringBuilder(value.Length * 2);
        foreach (var c in value)
        {
            result.Append(c switch
            {
                '\\' => "\\",
                '*' => @"\2a",
                '(' => @"\28",
                ')' => @"\29",
                '\0' => @"\00",
                _ => c.ToString()
            });
        }

        return result.ToString();
    }
}