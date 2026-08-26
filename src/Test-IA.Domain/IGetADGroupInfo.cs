namespace TestIA.Domain;

/// <summary>
/// Interface for retrieving Active Directory group information.
/// </summary>
public interface IGetADGroupInfo
{
    /// <summary>
    /// Retrieves group information from Active Directory by the group's samAccountName.
    /// </summary>
    /// <param name="samAccountName">The samAccountName of the group to search for.</param>
    /// <returns>A task containing a record with the group's displayName and member attributes.</returns>
    /// <exception cref="ArgumentException">Thrown when samAccountName is null or empty.</exception>
    /// <exception cref="GroupNotFoundException">Thrown when no group with the given samAccountName is found.</exception>
    /// <exception cref="DomainException">Thrown when an error occurs during the LDAP operation.</exception>
    Task<GroupDto> GetGroupAsync(string samAccountName);
}

/// <summary>
/// Represents a group's information retrieved from Active Directory.
/// </summary>
/// <param name="DisplayName">The group's display name.</param>
/// <param name="Members">The list of member display names in the group.</param>
public record GroupDto(string DisplayName, string[] Members);
