using Microsoft.AspNetCore.Authorization;

namespace TestIA.WebApp;

/// <summary>
/// Authorization requirement that specifies the required Active Directory group.
/// </summary>
public class GroupAuthorizationRequirement : IAuthorizationRequirement
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GroupAuthorizationRequirement"/> class.
    /// </summary>
    /// <param name="requiredGroup">The Active Directory group samAccountName that users must be a member of.</param>
    public GroupAuthorizationRequirement(string requiredGroup)
    {
        RequiredGroup = requiredGroup ?? throw new ArgumentNullException(nameof(requiredGroup));
    }

    /// <summary>
    /// Gets the required Active Directory group samAccountName.
    /// </summary>
    public string RequiredGroup { get; }
}
