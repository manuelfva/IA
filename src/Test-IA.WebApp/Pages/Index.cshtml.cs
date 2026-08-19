using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace TestIA.WebApp.Pages;

/// <summary>
/// Page model for the welcome/index page. This page serves as a landing page
/// with navigation cards to the Users and Groups management pages.
/// Requires the user to be authenticated and a member of the configured Active Directory group.
/// </summary>
[Authorize(Policy = "RequiredGroup")]
public class IndexModel : PageModel
{
    /// <summary>
    /// Initializes a new instance of the IndexModel class.
    /// </summary>
    public IndexModel()
    {
    }
}
