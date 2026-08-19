using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TestIA.Application;
using TestIA.Domain;
using TestIA.Logging;

namespace TestIA.WebApp.Pages;

/// <summary>
/// Page model for the Groups management page. Handles group search operations
/// by delegating to the IGetADGroupInfo service.
/// Requires the user to be authenticated and a member of the configured Active Directory group.
/// </summary>
[Authorize(Policy = "RequiredGroup")]
public class GroupsModel : PageModel
{
    private readonly IGetADGroupInfo _groupInfoService;
    private readonly ILoggerService _logger;
    private readonly IAttributeMapper<GroupDto> _groupMapper;

    /// <summary>
    /// Initializes a new instance of the GroupsModel class.
    /// </summary>
    /// <param name="groupInfoService">The group information service.</param>
    /// <param name="logger">The logging service.</param>
    /// <param name="groupMapper">The group attribute mapper for dynamic display rendering.</param>
    public GroupsModel(
        IGetADGroupInfo groupInfoService,
        ILoggerService logger,
        IAttributeMapper<GroupDto> groupMapper)
    {
        _groupInfoService = groupInfoService ?? throw new ArgumentNullException(nameof(groupInfoService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _groupMapper = groupMapper ?? throw new ArgumentNullException(nameof(groupMapper));
    }

    /// <summary>
    /// The samAccountName entered for group search.
    /// </summary>
    [BindProperty]
    public string? GroupSearchTerm { get; set; }

    /// <summary>
    /// The result of the group search operation.
    /// </summary>
    public GroupDto? GroupResult { get; set; }

    /// <summary>
    /// Dynamic display values for the group search result.
    /// </summary>
    public Dictionary<string, string?>? GroupDisplayValues { get; set; }

    /// <summary>
    /// Error message displayed on the page.
    /// </summary>
    public string? Error { get; set; }

    /// <summary>
    /// Handles GET requests — clears all results and display values.
    /// </summary>
    public void OnGet()
    {
        GroupResult = null;
        GroupDisplayValues = null;
        Error = null;
    }

    /// <summary>
    /// Handles POST requests for group search operations.
    /// </summary>
    public IActionResult OnPost()
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

            // Populate dynamic display values using the attribute mapper.
            GroupDisplayValues = _groupMapper.GetDisplayValues(GroupResult);

            _logger.LogInformation("Successfully retrieved group {SamAccountName}", GroupSearchTerm);
        }
        catch (GroupNotFoundException ex)
        {
            GroupResult = null;
            GroupDisplayValues = null;
            _logger.LogError("Group not found: {SamAccountName}", GroupSearchTerm);
            Error = ex.Message;
        }
        catch (DomainException ex)
        {
            GroupResult = null;
            GroupDisplayValues = null;
            _logger.LogError(ex, "Error retrieving group information for {SamAccountName}", GroupSearchTerm);
            Error = $"Error retrieving group information: {ex.Message}";
        }

        return Page();
    }
}
