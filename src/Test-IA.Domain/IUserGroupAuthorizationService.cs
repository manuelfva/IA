namespace TestIA.Domain;

/// <summary>
/// Service interface for checking if the current Windows user is a member of a specific Active Directory group.
/// </summary>
public interface IUserGroupAuthorizationService
{
    /// <summary>
    /// Checks whether the current Windows user is a member of the configured Active Directory group.
    /// </summary>
    /// <param name="groupName">The samAccountName of the Active Directory group to check membership against.</param>
    /// <returns>True if the current user is a member of the group; otherwise, false.</returns>
    /// <exception cref="MissingGroupException">Thrown when the authorization group does not exist in Active Directory.</exception>
    /// <exception cref="DomainException">Thrown when an LDAP error occurs while checking membership.</exception>
    bool IsMemberOfGroup(string groupName);
}
