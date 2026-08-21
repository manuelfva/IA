namespace TestIA.Domain;

/// <summary>
/// Abstraction for retrieving the current authenticated user's identity.
/// <para>
/// This interface decouples the Application layer from the presentation layer's
/// identity mechanism. In a console application, the identity comes from the
/// Windows security context. In a web application, it comes from the HTTP
/// request's authentication context (e.g., ASP.NET Core Windows Authentication).
/// </para>
/// </summary>
/// <remarks>
/// <b>Implementation note:</b> Each presentation layer provides its own implementation:
/// <list type="bullet">
///   <item><description><c>ConsoleCurrentUser</c> — reads from <c>WindowsIdentity.GetCurrent()</c>.</description></item>
///   <item><description><c>WebCurrentUser</c> — reads from <c>HttpContext.User.Identity</c>.</description></item>
/// </list>
/// </remarks>
public interface ICurrentUser
{
    /// <summary>
    /// The authenticated user's identity in <c>DOMAIN\Username</c> format.
    /// <para>
    /// For example: <c>CORP\john.doe</c>. Returns an empty string if the user
    /// identity cannot be determined.
    /// </para>
    /// </summary>
    string UserName { get; }

    /// <summary>
    /// Indicates whether the user has been successfully authenticated.
    /// <para>
    /// Returns <c>true</c> if the user identity is available and non-empty;
    /// <c>false</c> otherwise.
    /// </para>
    /// </summary>
    bool IsAuthenticated { get; }
}