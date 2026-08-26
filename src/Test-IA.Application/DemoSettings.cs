namespace TestIA.Application;

/// <summary>
/// Provides strongly-typed configuration for the console application demo settings.
/// <para>
/// This class is bound to the <c>Demo</c> section in <c>appsettings.json</c> via
/// <c>IOptions&lt;DemoSettings&gt;</c>. It defines the sample user and group
/// samAccountNames used for the demonstration queries.
/// </para>
/// </summary>
public class DemoSettings
{
    /// <summary>
    /// Gets or sets the samAccountName of the user to look up in Active Directory.
    /// </summary>
    public string UserSamAccountName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the samAccountName of the group to look up in Active Directory.
    /// </summary>
    public string GroupSamAccountName { get; set; } = string.Empty;
}
