using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TestIA.Application;
using TestIA.Domain;
using TestIA.Logging;

namespace TestIA.WebApp.Pages;

/// <summary>
/// Page model for the home page. Handles user and group search operations
/// by delegating to the IGetADUserInfo and IGetADGroupInfo services.
/// Requires the user to be authenticated and a member of the configured Active Directory group.
/// </summary>
[Authorize(Policy = "RequiredGroup")]
public class IndexModel : PageModel
{
    private readonly IGetADUserInfo _userInfoService;
    private readonly IGetADGroupInfo _groupInfoService;
    private readonly IUserWriter _userWriter;
    private readonly ILoggerService _logger;
    private readonly IAttributeMapper<UserDto> _userMapper;
    private readonly IAttributeMapper<GroupDto> _groupMapper;
    private readonly IAttributeMapper<UserUpdateRequest> _updateMapper;

    /// <summary>
    /// Initializes a new instance of the IndexModel class.
    /// </summary>
    /// <param name="userInfoService">The user information service.</param>
    /// <param name="groupInfoService">The group information service.</param>
    /// <param name="userWriter">The user writer service for updating AD attributes.</param>
    /// <param name="logger">The logging service.</param>
    /// <param name="userMapper">The user attribute mapper for dynamic display rendering.</param>
    /// <param name="groupMapper">The group attribute mapper for dynamic display rendering.</param>
    /// <param name="updateMapper">The user update attribute mapper for dynamic display rendering.</param>
    public IndexModel(
        IGetADUserInfo userInfoService,
        IGetADGroupInfo groupInfoService,
        IUserWriter userWriter,
        ILoggerService logger,
        IAttributeMapper<UserDto> userMapper,
        IAttributeMapper<GroupDto> groupMapper,
        IAttributeMapper<UserUpdateRequest> updateMapper)
    {
        _userInfoService = userInfoService ?? throw new ArgumentNullException(nameof(userInfoService));
        _groupInfoService = groupInfoService ?? throw new ArgumentNullException(nameof(groupInfoService));
        _userWriter = userWriter ?? throw new ArgumentNullException(nameof(userWriter));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _userMapper = userMapper ?? throw new ArgumentNullException(nameof(userMapper));
        _groupMapper = groupMapper ?? throw new ArgumentNullException(nameof(groupMapper));
        _updateMapper = updateMapper ?? throw new ArgumentNullException(nameof(updateMapper));
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
    /// Dynamic display values for the user result, populated by <see cref="IAttributeMapper{TDto}.GetDisplayValues"/>.
    /// </summary>
    public Dictionary<string, string>? UserDisplayValues { get; set; }

    /// <summary>
    /// Dynamic display values for the group result, populated by <see cref="IAttributeMapper{TDto}.GetDisplayValues"/>.
    /// </summary>
    public Dictionary<string, string>? GroupDisplayValues { get; set; }

    /// <summary>
    /// Error message if a search operation fails.
    /// </summary>
    public string? Error { get; set; }

    /// <summary>
    /// The samAccountName entered for user update.
    /// </summary>
    [BindProperty]
    public string? UserUpdateSamAccountName { get; set; }

    /// <summary>
    /// The new value for the 'info' attribute.
    /// </summary>
    [BindProperty]
    public string? UpdateInfo { get; set; }

    /// <summary>
    /// The new value for the 'mobile' attribute.
    /// </summary>
    [BindProperty]
    public string? UpdateMobile { get; set; }

    /// <summary>
    /// The new value for the 'streetAddress' attribute.
    /// </summary>
    [BindProperty]
    public string? UpdateStreetAddress { get; set; }

    /// <summary>
    /// The new value for the 'l' (city) attribute.
    /// </summary>
    [BindProperty]
    public string? UpdateCity { get; set; }

    /// <summary>
    /// The new value for the 'st' (state) attribute.
    /// </summary>
    [BindProperty]
    public string? UpdateState { get; set; }

    /// <summary>
    /// The new value for the 'postalCode' attribute.
    /// </summary>
    [BindProperty]
    public string? UpdatePostalCode { get; set; }

    /// <summary>
    /// The new value for the 'department' attribute.
    /// </summary>
    [BindProperty]
    public string? UpdateDepartment { get; set; }

    /// <summary>
    /// The new value for the 'title' attribute.
    /// </summary>
    [BindProperty]
    public string? UpdateTitle { get; set; }

    /// <summary>
    /// The new value for the 'telephoneNumber' attribute.
    /// </summary>
    [BindProperty]
    public string? UpdatePhoneNumber { get; set; }

    /// <summary>
    /// The result of the user update operation.
    /// </summary>
    public UserUpdateResult? UpdateResult { get; set; }

    /// <summary>
    /// Dynamic display values for the update request, populated by <see cref="IAttributeMapper{TDto}.GetDisplayValues"/>.
    /// </summary>
    public Dictionary<string, string>? UpdateDisplayValues { get; set; }

    /// <summary>
    /// Handles all POST requests. Determines whether to search for a user, a group, or update a user
    /// based on the SearchAction form value.
    /// </summary>
    /// <returns>A page result that re-renders the page with search or update results.</returns>
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
                UserDisplayValues = _userMapper.GetDisplayValues(UserResult);
                GroupDisplayValues = null;
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
                UserDisplayValues = null;
                GroupDisplayValues = _groupMapper.GetDisplayValues(GroupResult);
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
        else if (string.Equals(SearchAction, "Update", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(UserUpdateSamAccountName))
            {
                Error = "Please enter a samAccountName to update a user.";
                return Page();
            }

            // Build the update request with all 10 attributes (only non-null values will be applied).
            var request = new UserUpdateRequest(
                SamAccountName: UserUpdateSamAccountName,
                Info: UpdateInfo,
                Mobile: UpdateMobile,
                StreetAddress: UpdateStreetAddress,
                City: UpdateCity,
                State: UpdateState,
                PostalCode: UpdatePostalCode,
                Department: UpdateDepartment,
                Title: UpdateTitle,
                PhoneNumber: UpdatePhoneNumber);

            // Check if at least one attribute has a value.
            bool hasAnyValue = !string.IsNullOrWhiteSpace(request.Info)
                            || !string.IsNullOrWhiteSpace(request.Mobile)
                            || !string.IsNullOrWhiteSpace(request.StreetAddress)
                            || !string.IsNullOrWhiteSpace(request.City)
                            || !string.IsNullOrWhiteSpace(request.State)
                            || !string.IsNullOrWhiteSpace(request.PostalCode)
                            || !string.IsNullOrWhiteSpace(request.Department)
                            || !string.IsNullOrWhiteSpace(request.Title)
                            || !string.IsNullOrWhiteSpace(request.PhoneNumber);

            if (!hasAnyValue)
            {
                Error = "Please enter at least one attribute value to update.";
                return Page();
            }

            try
            {
                UpdateResult = _userWriter.UpdateUser(request);
                Error = null;

                // Populate dynamic display values using the attribute mapper.
                UpdateDisplayValues = _updateMapper.GetDisplayValues(request);

                if (!UpdateResult.Success)
                {
                    _logger.LogWarning("Update failed for user {SamAccountName}: {Message}", UserUpdateSamAccountName, UpdateResult.Message);
                }
                else
                {
                    _logger.LogInformation("Successfully updated user {SamAccountName}: {Message}", UserUpdateSamAccountName, UpdateResult.Message);
                }
            }
            catch (UserNotFoundException ex)
            {
                UpdateResult = null;
                UpdateDisplayValues = null;
                _logger.LogError("User not found for update: {SamAccountName}", UserUpdateSamAccountName);
                Error = ex.Message;
            }
            catch (DomainException ex)
            {
                UpdateResult = null;
                UpdateDisplayValues = null;
                _logger.LogError(ex, "Error updating user {SamAccountName}", UserUpdateSamAccountName);
                Error = $"Error updating user: {ex.Message}";
            }
        }

        return Page();
    }
}