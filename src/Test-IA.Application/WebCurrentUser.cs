using Microsoft.AspNetCore.Http;
using TestIA.Domain;

namespace TestIA.Application;

/// <summary>
/// Provides the current user identity from the ASP.NET Core HTTP context.
/// <para>
/// This implementation is used by the WebApp presentation layer. It reads
/// the identity of the authenticated user from <c>HttpContext.User.Identity</c>,
/// which is populated by ASP.NET Core's authentication middleware (e.g., Windows
/// Authentication via Negotiate/Kerberos).
/// </para>
/// </summary>
/// <remarks>
/// <b>Use case:</b> ASP.NET Core web applications where the user authenticates
/// via HTTP middleware (e.g., Negotiate, JWT, Cookies).
/// <para>
/// <b>Prerequisite:</b> Requires <c>IHttpContextAccessor</c> to be registered
/// in the DI container (call <c>services.AddHttpContextAccessor()</c>).
/// </para>
/// <para>
/// <b>Identity format:</b> Returns the identity in <c>DOMAIN\Username</c> format,
/// or an empty string if the user is not authenticated or the HTTP context is unavailable.
/// </para>
/// </remarks>
public class WebCurrentUser : ICurrentUser
{
    /// <summary>
    /// The HTTP context accessor used to retrieve the current HTTP context.
    /// </summary>
    private readonly IHttpContextAccessor _httpContextAccessor;

    /// <summary>
    /// Initializes a new instance of the <see cref="WebCurrentUser"/> class.
    /// </summary>
    /// <param name="httpContextAccessor">
    /// The HTTP context accessor used to retrieve the current HTTP context.
    /// Must not be null.
    /// </param>
    public WebCurrentUser(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
    }

    /// <summary>
    /// Gets the current user's identity in <c>DOMAIN\Username</c> format.
    /// <para>
    /// Reads from <c>HttpContext.User.Identity?.Name</c>. Returns an empty string
    /// if the HTTP context is unavailable or the user is not authenticated.
    /// </para>
    /// </summary>
    public string UserName => _httpContextAccessor.HttpContext?.User?.Identity?.Name ?? string.Empty;

    /// <summary>
    /// Indicates whether the current user has been authenticated.
    /// <para>
    /// Returns <c>true</c> if the HTTP context is available, the user identity exists,
    /// and <c>IsAuthenticated</c> is <c>true</c>; <c>false</c> otherwise.
    /// </para>
    /// </summary>
    public bool IsAuthenticated => _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated == true;
}