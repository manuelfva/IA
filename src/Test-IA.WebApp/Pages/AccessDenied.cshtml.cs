using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace TestIA.WebApp.Pages;

/// <summary>
/// Page model for the Access Denied error page.
/// Displays a minimal, information-safe message when a user is authenticated but not authorized.
/// This page is completely independent from the shared layout to prevent any application structure disclosure.
/// </summary>
[AllowAnonymous]
public class AccessDeniedModel : PageModel
{
    /// <summary>
    /// Called by the Razor Pages framework when the page is requested.
    /// No sensitive data is exposed — the page shows only a generic message.
    /// </summary>
    public void OnGet()
    {
    }
}
