using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TestIA.Application;
using TestIA.Domain;
using TestIA.Logging;

namespace TestIA.WebApp.Pages;

/// <summary>
/// Page model for the Users management page. Handles user search and user update operations
/// by delegating to the IGetADUserInfo and IUserWriter services.
/// Requires the user to be authenticated and a member of the configured Active Directory group.
/// </summary>
[Authorize(Policy = "RequiredGroup")]
public class UsersModel : PageModel
{
    private readonly IGetADUserInfo _userInfoService;
    private readonly IUserWriter _userWriter;
    private readonly ILoggerService _logger;
    private readonly IAttributeMapper<UserDto> _userMapper;
    private readonly IAttributeMapper<UserUpdateRequest> _updateMapper;

    /// <summary>
    /// Initializes a new instance of the UsersModel class.
    /// </summary>
    /// <param name="userInfoService">The user information service.</param>
    /// <param name="userWriter">The user writer service for updating AD attributes.</param>
    /// <param name="logger">The logging service.</param>
    /// <param name="userMapper">The user attribute mapper for dynamic display rendering.</param>
    /// <param name="updateMapper">The user update attribute mapper for dynamic display rendering.</param>
    public UsersModel(
        IGetADUserInfo userInfoService,
        IUserWriter userWriter,
        ILoggerService logger,
        IAttributeMapper<UserDto> userMapper,
        IAttributeMapper<UserUpdateRequest> updateMapper)
    {
        _userInfoService = userInfoService ?? throw new ArgumentNullException(nameof(userInfoService));
        _userWriter = userWriter ?? throw new ArgumentNullException(nameof(userWriter));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _userMapper = userMapper ?? throw new ArgumentNullException(nameof(userMapper));
        _updateMapper = updateMapper ?? throw new ArgumentNullException(nameof(updateMapper));
    }

    /// <summary>
    /// The samAccountName entered for user search.
    /// </summary>
    [BindProperty]
    public string? UserSearchTerm { get; set; }

    /// <summary>
    /// The result of the user search operation.
    /// </summary>
    public UserDto? UserResult { get; set; }

    /// <summary>
    /// Dynamic display values for the user search result.
    /// </summary>
    public Dictionary<string, string?>? UserDisplayValues { get; set; }

    /// <summary>
    /// The samAccountName entered for user update.
    /// </summary>
    [BindProperty]
    public string? UserUpdateSamAccountName { get; set; }

    /// <summary>
    /// The result of the user update operation.
    /// </summary>
    public UserUpdateResult? UserUpdateResult { get; set; }

    /// <summary>
    /// Dynamic display values for the user update request.
    /// </summary>
    public Dictionary<string, string?>? UpdateDisplayValues { get; set; }

    /// <summary>
    /// Individual update attribute values bound from the form.
    /// </summary>
    [BindProperty]
    public string? UpdateInfo { get; set; }

    [BindProperty]
    public string? UpdateMobile { get; set; }

    [BindProperty]
    public string? UpdateStreetAddress { get; set; }

    [BindProperty]
    public string? UpdateCity { get; set; }

    [BindProperty]
    public string? UpdateState { get; set; }

    [BindProperty]
    public string? UpdatePostalCode { get; set; }

    [BindProperty]
    public string? UpdateDepartment { get; set; }

    [BindProperty]
    public string? UpdateTitle { get; set; }

    [BindProperty]
    public string? UpdatePhoneNumber { get; set; }

    /// <summary>
    /// Error message displayed on the page.
    /// </summary>
    public string? Error { get; set; }

    /// <summary>
    /// The action triggered by the form submission (SearchUser or UpdateUser).
    /// </summary>
    [BindProperty]
    public string? Action { get; set; }

    /// <summary>
    /// Handles GET requests — clears all results and display values.
    /// </summary>
    public void OnGet()
    {
        UserResult = null;
        UserDisplayValues = null;
        UserUpdateResult = null;
        UpdateDisplayValues = null;
        Error = null;
    }

    /// <summary>
    /// Handles POST requests for both user search and user update operations.
    /// The Action form field determines which operation to perform.
    /// </summary>
    public IActionResult OnPost()
    {
        if (string.Equals(Action, "SearchUser", StringComparison.OrdinalIgnoreCase))
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

                // Populate dynamic display values using the attribute mapper.
                UserDisplayValues = _userMapper.GetDisplayValues(UserResult);

                _logger.LogInformation("Successfully retrieved user {SamAccountName}", UserSearchTerm);
            }
            catch (UserNotFoundException ex)
            {
                UserResult = null;
                UserDisplayValues = null;
                _logger.LogError("User not found: {SamAccountName}", UserSearchTerm);
                Error = ex.Message;
            }
            catch (DomainException ex)
            {
                UserResult = null;
                UserDisplayValues = null;
                _logger.LogError(ex, "Error retrieving user information for {SamAccountName}", UserSearchTerm);
                Error = $"Error retrieving user information: {ex.Message}";
            }
        }
        else if (string.Equals(Action, "UpdateUser", StringComparison.OrdinalIgnoreCase))
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
                UserUpdateResult = _userWriter.UpdateUser(request);
                Error = null;

                // Populate dynamic display values using the attribute mapper.
                UpdateDisplayValues = _updateMapper.GetDisplayValues(request);

                if (!UserUpdateResult.Success)
                {
                    _logger.LogWarning("Update failed for user {SamAccountName}: {Message}", UserUpdateSamAccountName, UserUpdateResult.Message);
                }
                else
                {
                    _logger.LogInformation("Successfully updated user {SamAccountName}: {Message}", UserUpdateSamAccountName, UserUpdateResult.Message);
                }
            }
            catch (UserNotFoundException ex)
            {
                UserUpdateResult = null;
                UpdateDisplayValues = null;
                _logger.LogError("User not found for update: {SamAccountName}", UserUpdateSamAccountName);
                Error = ex.Message;
            }
            catch (DomainException ex)
            {
                UserUpdateResult = null;
                UpdateDisplayValues = null;
                _logger.LogError(ex, "Error updating user {SamAccountName}", UserUpdateSamAccountName);
                Error = $"Error updating user: {ex.Message}";
            }
        }

        return Page();
    }
}
