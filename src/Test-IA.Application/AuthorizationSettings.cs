namespace TestIA.Application;

/// <summary>
/// Provides strongly-typed configuration for application authorization settings.
/// <para>
/// This class is bound to the <c>Authorization</c> section in <c>appsettings.json</c> (or
/// environment variables) via <c>IOptions&lt;AuthorizationSettings&gt;</c>. It defines the
/// Active Directory group that users must be a member of to access the application.
/// </para>
/// </summary>
/// <b>Configuration key:</b> <c>Authorization:RequiredGroup</c>
/// <para>
/// <b>Usage:</b> Injected into <see cref="UserGroupAuthorizationService"/> via <c>IOptions&lt;AuthorizationSettings&gt;</c>.
/// The group name is used to verify that the current Windows user has permission to use the application.
/// </para>
/// </remarks>
public class AuthorizationSettings
{
    /// <summary>
    /// Gets or sets the Active Directory group <c>sAMAccountName</c> that users must be a member of
    /// to access the application.
    /// <para>
    /// This value is read from the <c>Authorization:RequiredGroup</c> configuration section.
    /// It should be set to the <c>sAMAccountName</c> (not the display name or distinguished name)
    /// of an AD group that contains authorized users.
    /// </para>
    /// </summary>
    /// <example>
    /// <c>"ADTestIA-Users"</c> or <c>"TestIA-Access"</c>
    /// </example>
    public string RequiredGroup { get; set; } = string.Empty;
}
