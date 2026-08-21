using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TestIA.Application;
using TestIA.Domain;
using TestIA.Logging;

namespace TestIA.WebApp.Pages;

/// <summary>
/// Page model for the Groups management page. Handles group search operations
/// and group membership management by delegating to the IGetADGroupInfo and IGroupMembershipWriter services.
/// Requires the user to be authenticated and a member of the configured Active Directory group.
/// </summary>
[Authorize(Policy = "RequiredGroup")]
public class GroupsModel : PageModel
{
    private readonly IGetADGroupInfo _groupInfoService;
    private readonly ILoggerService _logger;
    private readonly IAttributeMapper<GroupDto> _groupMapper;
    private readonly IGroupMembershipWriter _groupMembershipWriter;

    /// <summary>
    /// Initializes a new instance of the GroupsModel class.
    /// </summary>
    /// <param name="groupInfoService">The group information service.</param>
    /// <param name="logger">The logging service.</param>
    /// <param name="groupMapper">The group attribute mapper for dynamic display rendering.</param>
    /// <param name="groupMembershipWriter">The group membership writer service.</param>
    public GroupsModel(
        IGetADGroupInfo groupInfoService,
        ILoggerService logger,
        IAttributeMapper<GroupDto> groupMapper,
        IGroupMembershipWriter groupMembershipWriter)
    {
        _groupInfoService = groupInfoService ?? throw new ArgumentNullException(nameof(groupInfoService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _groupMapper = groupMapper ?? throw new ArgumentNullException(nameof(groupMapper));
        _groupMembershipWriter = groupMembershipWriter ?? throw new ArgumentNullException(nameof(groupMembershipWriter));
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
    public Dictionary<string, string>? GroupDisplayValues { get; set; }

    /// <summary>
    /// Error message displayed on the page.
    /// </summary>
    public string? Error { get; set; }

    /// <summary>
    /// The samAccountName of the group to add a member to (for the Add Member form).
    /// </summary>
    [BindProperty]
    public string? AddMemberGroupSamAccountName { get; set; }

    /// <summary>
    /// The samAccountName of the user to add as a member (for the Add Member form).
    /// </summary>
    [BindProperty]
    public string? AddMemberMemberSamAccountName { get; set; }

    /// <summary>
    /// Result message from the add member operation.
    /// </summary>
    public GroupMemberOperationResult? AddMemberResult { get; set; }

    /// <summary>
    /// The samAccountName of the group to remove a member from (for the Remove Member form).
    /// </summary>
    [BindProperty]
    public string? RemoveMemberGroupSamAccountName { get; set; }

    /// <summary>
    /// The samAccountName of the user to remove as a member (for the Remove Member form).
    /// </summary>
    [BindProperty]
    public string? RemoveMemberMemberSamAccountName { get; set; }

    /// <summary>
    /// Result message from the remove member operation.
    /// </summary>
    public GroupMemberOperationResult? RemoveMemberResult { get; set; }

    /// <summary>
    /// The form action submitted by the user (SearchGroup or AddMember).
    /// Used to distinguish between multiple submit buttons in the same form.
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public string? Action { get; set; }

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
    /// Handles POST requests for group search and add member operations.
    /// Routes to the appropriate handler based on the submitted Action value.
    /// </summary>
    public IActionResult OnPost()
    {
        // Route to AddMember handler when the Add Member form was submitted.
        if (Action == "AddMember")
        {
            return OnPostAddMember();
        }

        // Route to RemoveMember handler when the Remove Member form was submitted.
        if (Action == "RemoveMember")
        {
            return OnPostRemoveMember();
        }

        if (string.IsNullOrWhiteSpace(GroupSearchTerm))
        {
            Error = "Please enter a samAccountName to search for a group.";
            return Page();
        }

        try
        {
            GroupResult = _groupInfoService.GetGroup(GroupSearchTerm);
            Error = null;

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

    /// <summary>
    /// Handles POST requests for adding a member to a group.
    /// </summary>
    public IActionResult OnPostAddMember()
    {
        if (string.IsNullOrWhiteSpace(AddMemberGroupSamAccountName))
        {
            Error = "Please enter the samAccountName of the target group.";
            return Page();
        }

        if (string.IsNullOrWhiteSpace(AddMemberMemberSamAccountName))
        {
            Error = "Please enter the samAccountName of the user to add as a member.";
            return Page();
        }

        try
        {
            AddMemberResult = _groupMembershipWriter.AddMember(AddMemberGroupSamAccountName, AddMemberMemberSamAccountName);
            Error = null;

            _logger.LogInformation("Successfully added member via OnPostAddMember");
        }
        catch (GroupNotFoundException ex)
        {
            AddMemberResult = null;
            _logger.LogError("Group not found during add member: {SamAccountName}", AddMemberGroupSamAccountName);
            Error = ex.Message;
        }
        catch (UserNotFoundException ex)
        {
            AddMemberResult = null;
            _logger.LogError("User not found during add member: {SamAccountName}", AddMemberMemberSamAccountName);
            Error = ex.Message;
        }
        catch (DomainException ex)
        {
            AddMemberResult = null;
            _logger.LogError(ex, "Error adding member to group {GroupSamAccountName}", AddMemberGroupSamAccountName);
            Error = $"Error adding member to group: {ex.Message}";
        }

        return Page();
    }

    /// <summary>
    /// Handles POST requests for removing a member from a group.
    /// </summary>
    public IActionResult OnPostRemoveMember()
    {
        if (string.IsNullOrWhiteSpace(RemoveMemberGroupSamAccountName))
        {
            Error = "Please enter the samAccountName of the target group.";
            return Page();
        }

        if (string.IsNullOrWhiteSpace(RemoveMemberMemberSamAccountName))
        {
            Error = "Please enter the samAccountName of the user to remove as a member.";
            return Page();
        }

        try
        {
            RemoveMemberResult = _groupMembershipWriter.RemoveMember(RemoveMemberGroupSamAccountName, RemoveMemberMemberSamAccountName);
            Error = null;

            _logger.LogInformation("Successfully removed member via OnPostRemoveMember");
        }
        catch (GroupNotFoundException ex)
        {
            RemoveMemberResult = null;
            _logger.LogError("Group not found during remove member: {SamAccountName}", RemoveMemberGroupSamAccountName);
            Error = ex.Message;
        }
        catch (UserNotFoundException ex)
        {
            RemoveMemberResult = null;
            _logger.LogError("User not found during remove member: {SamAccountName}", RemoveMemberMemberSamAccountName);
            Error = ex.Message;
        }
        catch (DomainException ex)
        {
            RemoveMemberResult = null;
            _logger.LogError(ex, "Error removing member from group {GroupSamAccountName}", RemoveMemberGroupSamAccountName);
            Error = $"Error removing member from group: {ex.Message}";
        }

        return Page();
    }
}