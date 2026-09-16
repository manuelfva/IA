using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TestIA.Application;

namespace TestIA.WebApp.Pages;

/// <summary>
/// Base class for Razor Pages that require an Active Directory domain connection.
/// <para>
/// This mixin checks whether the machine is joined to an Active Directory domain
/// before allowing page operations. If the machine is not domain-joined, it sets
/// an <see cref="DomainNotAvailableError"/> message that can be displayed to the user.
/// </para>
/// </summary>
/// <remarks>
/// All Razor Pages that interact with Active Directory should inherit from this class
/// instead of <see cref="PageModel"/> directly. The domain check is performed automatically
/// on every GET and POST request, ensuring that users see a clear message instead of
/// encountering a cryptic LDAP error.
/// <para>
/// Derived pages should override <see cref="OnGetCoreAsync"/> and <see cref="OnPostCoreAsync"/>
/// to implement their page logic. The domain check always runs first through the non-virtual
/// <see cref="OnGet"/> and <see cref="OnPost"/> methods.
/// </para>
/// </remarks>
public class DomainRequiredMixin : PageModel
{
    /// <summary>
    /// Error message displayed when the machine is not joined to an Active Directory domain.
    /// </summary>
    /// <remarks>
    /// This property is set by the <see cref="OnGet"/> and <see cref="OnPost"/> overrides
    /// when <see cref="IsDomainJoined"/> returns <c>false</c>. The Razor page can display
    /// this message using the standard <c>TempData</c> or a dedicated error section.
    /// </remarks>
    public string? DomainNotAvailableError { get; protected set; }

    /// <summary>
    /// The Active Directory discovery service used to validate domain membership.
    /// </summary>
    protected readonly ADDomainDiscoveryService DomainDiscoveryService;

    /// <summary>
    /// Initializes a new instance of the <see cref="DomainRequiredMixin"/> class.
    /// </summary>
    /// <param name="domainDiscoveryService">
    /// The service responsible for discovering the Active Directory environment.
    /// Must not be null.
    /// </param>
    public DomainRequiredMixin(ADDomainDiscoveryService domainDiscoveryService)
    {
        DomainDiscoveryService = domainDiscoveryService ?? throw new ArgumentNullException(nameof(domainDiscoveryService));
    }

    /// <summary>
    /// Checks whether the machine is joined to an Active Directory domain.
    /// <para>
    /// Returns <c>true</c> if domain membership is confirmed; otherwise <c>false</c>.
    /// The result is cached in the <see cref="IsDomainJoined"/> property for reuse within the same request.
    /// </para>
    /// </summary>
    /// <returns>
    /// <c>true</c> if the machine is domain-joined; otherwise <c>false</c>.
    /// </returns>
    protected bool IsDomainJoined => DomainDiscoveryService.IsDomainJoined();

    /// <summary>
    /// Handles GET requests. Validates domain membership before processing the page.
    /// <para>
    /// If the machine is not domain-joined, sets <see cref="DomainNotAvailableError"/>
    /// and returns the page without executing the derived class's <see cref="OnGetCoreAsync"/> handler.
    /// </para>
    /// </summary>
    /// <returns>
    /// The <see cref="PageResult"/> if domain membership is confirmed; otherwise the page
    /// with <see cref="DomainNotAvailableError"/> set for display.
    /// </returns>
    public async Task<IActionResult> OnGet()
    {
        if (!IsDomainJoined)
        {
            DomainNotAvailableError = "This machine is not joined to an Active Directory domain. " +
                "Active Directory features are not available. Please join the machine to the domain and try again.";
            return Page();
        }

        return await OnGetCoreAsync();
    }

    /// <summary>
    /// Handles POST requests. Validates domain membership before processing the form.
    /// <para>
    /// If the machine is not domain-joined, sets <see cref="DomainNotAvailableError"/>
    /// and returns the page without executing the derived class's <see cref="OnPostCoreAsync"/> handler.
    /// </para>
    /// </summary>
    /// <returns>
    /// The <see cref="PageResult"/> if domain membership is confirmed; otherwise the page
    /// with <see cref="DomainNotAvailableError"/> set for display.
    /// </returns>
    public async Task<IActionResult> OnPost()
    {
        if (!IsDomainJoined)
        {
            DomainNotAvailableError = "This machine is not joined to an Active Directory domain. " +
                "Active Directory features are not available. Please join the machine to the domain and try again.";
            return Page();
        }

        return await OnPostCoreAsync();
    }

    /// <summary>
    /// Overridable async GET handler. Derived pages should implement their GET logic here
    /// instead of overriding <see cref="OnGet"/> directly, so the domain check is always applied.
    /// </summary>
    /// <returns>
    /// The <see cref="PageResult"/> or another <see cref="IActionResult"/>.
    /// </returns>
    /// <remarks>
    /// This method is called only after the domain check in <see cref="OnGet"/> succeeds.
    /// Derived classes should override this method to implement their page-specific GET logic.
    /// </remarks>
    public virtual Task<IActionResult> OnGetCoreAsync() => Task.FromResult<IActionResult>(Page());

    /// <summary>
    /// Overridable async POST handler. Derived pages should implement their POST logic here
    /// instead of overriding <see cref="OnPost"/> directly, so the domain check is always applied.
    /// </summary>
    /// <returns>
    /// The <see cref="PageResult"/> or another <see cref="IActionResult"/>.
    /// </returns>
    /// <remarks>
    /// This method is called only after the domain check in <see cref="OnPost"/> succeeds.
    /// Derived classes should override this method to implement their page-specific POST logic.
    /// </remarks>
    public virtual Task<IActionResult> OnPostCoreAsync() => Task.FromResult<IActionResult>(Page());
}
