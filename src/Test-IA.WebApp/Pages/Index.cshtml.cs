using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TestIA.Domain;
using TestIA.Logging;

namespace TestIA.WebApp.Pages;

/// <summary>
/// Page model for the home page. Handles user and group search operations
/// by delegating to the IGetADUserInfo and IGetADGroupInfo services.
/// </summary>
public class IndexModel : PageModel
{
    private readonly IGetADUserInfo _userInfoService;
    private readonly IGetADGroupInfo _groupInfoService;
    private readonly ILoggerService _logger;

    /// <summary>
    /// Initializes a new instance of the IndexModel class.
    /// </summary>
    /// <param name="userInfoService">The user information service.</param>
    /// <param name="groupInfoService">The group information service.</param>
    /// <param name="logger">The logging service.</param>
    public IndexModel(IGetADUserInfo userInfoService, IGetADGroupInfo groupInfoService, ILoggerService logger)
    {
        _userInfoService = userInfoService ?? throw new ArgumentNullException(nameof(userInfoService));
        _groupInfoService = groupInfoService ?? throw new ArgumentNullException(nameof(groupInfoService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// The samAccountName entered for user search.
    /// </summary>
    [BindProperty]
    public string? UserSearchTerm { get; set; }

    /// <summary>
    /// The samAccountName entered for group search.
    /// </summary>
    [BindProperty]
    public string? GroupSearchTerm { get; set; }

    /// <summary>
    /// The selected search action (User or Group), bound from the clicked submit button.
    /// </summary>
    [BindProperty]
    public string? SearchAction { get; set; }

    /// <summary>
    /// The result of the user search operation.
    /// </summary>
    public UserDto? UserResult { get; set; }

    /// <summary>
    /// The result of the group search operation.
    /// </summary>
    public GroupDto? GroupResult { get; set; }

    /// <summary>
    /// Error message if a search operation fails.
    /// </summary>
    public string? Error { get; set; }

    /// <summary>
    /// Handles all POST requests. Determines whether to search for a user or a group
    /// based on the SearchAction form value.
    /// </summary>
    /// <returns>A page result that re-renders the page with search results.</returns>
    public IActionResult OnPost()
    {
        if (string.Equals(SearchAction, "User", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(UserSearchTerm))
            {
                Error = "Please enter a samAccountName to search for a user.";
                return Page();
            }

            try
            {
                UserResult = _userInfoService.GetUser(UserSearchTerm);
                Error = null;
            }
            catch (UserNotFoundException ex)
            {
                UserResult = null;
                _logger.LogError("User not found: {SamAccountName}", UserSearchTerm);
                Error = ex.Message;
            }
            catch (DomainException ex)
            {
                UserResult = null;
                _logger.LogError(ex, "Error retrieving user information for {SamAccountName}", UserSearchTerm);
                Error = $"Error retrieving user information: {ex.Message}";
            }
        }
        else if (string.Equals(SearchAction, "Group", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(GroupSearchTerm))
            {
                Error = "Please enter a samAccountName to search for a group.";
                return Page();
            }

            try
            {
                GroupResult = _groupInfoService.GetGroup(GroupSearchTerm);
                Error = null;
            }
            catch (GroupNotFoundException ex)
            {
                GroupResult = null;
                _logger.LogError("Group not found: {SamAccountName}", GroupSearchTerm);
                Error = ex.Message;
            }
            catch (DomainException ex)
            {
                GroupResult = null;
                _logger.LogError(ex, "Error retrieving group information for {SamAccountName}", GroupSearchTerm);
                Error = $"Error retrieving group information: {ex.Message}";
            }
        }

        return Page();
    }
}