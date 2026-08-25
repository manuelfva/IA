using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using TestIA.Application;
using TestIA.WebApp;

namespace TestIA.WebApp.Pages;

/// <summary>
/// Page model for the Access Denied error page.
/// Displays a friendly message when a user is authenticated but not authorized to access the application.
/// The status code is passed via query string by the StatusCodePages middleware.
/// </summary>
[AllowAnonymous]
public class AccessDeniedModel : PageModel
{
    private readonly AuthorizationSettings _settings;
    private readonly IHttpContextAccessor _httpContextAccessor;

    /// <summary>
    /// Initializes a new instance of the <see cref="AccessDeniedModel"/> class.
    /// </summary>
    /// <param name="options">The authorization settings containing the required group name.</param>
    /// <param name="httpContextAccessor">The HTTP context accessor for reading the current user identity.</param>
    public AccessDeniedModel(IOptions<AuthorizationSettings> options, IHttpContextAccessor httpContextAccessor)
    {
        _settings = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
    }

    /// <summary>
    /// Gets the HTTP status code that triggered this error page (typically 403).
    /// </summary>
    public int HttpStatusCode { get; set; }

    /// <summary>
    /// Gets the required Active Directory group name.
    /// </summary>
    public string RequiredGroup => _settings.RequiredGroup;

    /// <summary>
    /// Gets the current user's identity name. Prefers the userName query string parameter
    /// (passed by the StatusCodePages middleware redirect), falling back to the HTTP context.
    /// </summary>
    public string UserName
    {
        get
        {
            var queryUserName = Request.Query["userName"].ToString();
            if (!string.IsNullOrEmpty(queryUserName))
            {
                return queryUserName;
            }

            return _httpContextAccessor.HttpContext?.User?.Identity?.Name ?? string.Empty;
        }
    }

    /// <summary>
    /// Called by the Razor Pages framework. Initializes the status code from the query string
    /// and stores the username in ViewData for use in the shared layout.
    /// </summary>
    public void OnGet()
    {
        if (int.TryParse(Request.Query["statusCode"], out var code))
        {
            HttpStatusCode = code;
        }

        ViewData["UserName"] = UserName;
    }
}
