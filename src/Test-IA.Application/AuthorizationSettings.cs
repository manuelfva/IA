namespace TestIA.Application;

/// <summary>
/// Strongly-typed configuration class for application authorization settings.
/// </summary>
public class AuthorizationSettings
{
    /// <summary>
    /// The Active Directory group samAccountName that users must be a member of to access the application.
    /// </summary>
    public string RequiredGroup { get; set; } = string.Empty;
}
