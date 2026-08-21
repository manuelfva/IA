using System.Security.Principal;
using TestIA.Domain;

namespace TestIA.Application;

/// <summary>
/// Provides the current user identity from the Windows security context.
/// <para>
/// This implementation is used by the ConsoleApp presentation layer. It reads
/// the identity of the Windows user under which the process is running via
/// <c>WindowsIdentity.GetCurrent()</c>.
/// </para>
/// </summary>
/// <remarks>
/// <b>Use case:</b> Console applications where the process runs under the
/// logged-in Windows user's security context.
/// <para>
/// <b>Identity format:</b> Returns the identity in <c>DOMAIN\Username</c> format,
/// or an empty string if the identity cannot be determined.
/// </para>
/// </remarks>
public class ConsoleCurrentUser : ICurrentUser
{
    /// <summary>
    /// Gets the current user's identity in <c>DOMAIN\Username</c> format.
    /// <para>
    /// Reads from <c>WindowsIdentity.GetCurrent()</c>. Returns an empty string
    /// if the identity cannot be determined.
    /// </para>
    /// </summary>
    public string UserName => WindowsIdentity.GetCurrent()?.Name ?? string.Empty;

    /// <summary>
    /// Indicates whether the current user has been authenticated.
    /// <para>
    /// Returns <c>true</c> if the user identity is available and non-empty;
    /// <c>false</c> otherwise.
    /// </para>
    /// </summary>
    public bool IsAuthenticated => !string.IsNullOrEmpty(UserName);
}