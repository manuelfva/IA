namespace TestIA.Domain;

/// <summary>
/// Interface for retrieving Active Directory user information.
/// </summary>
public interface IGetADUserInfo
{
    /// <summary>
    /// Retrieves user information from Active Directory by the user's samAccountName.
    /// </summary>
    /// <param name="samAccountName">The samAccountName of the user to search for.</param>
    /// <returns>A record containing the user's displayName, employeeID, mail, and userPrincipalName attributes.</returns>
    /// <exception cref="ArgumentException">Thrown when samAccountName is null or empty.</exception>
    /// <exception cref="UserNotFoundException">Thrown when no user with the given samAccountName is found.</exception>
    /// <exception cref="DomainException">Thrown when an error occurs during the LDAP operation.</exception>
    UserDto GetUser(string samAccountName);
}

/// <summary>
/// Represents a user's information retrieved from Active Directory.
/// </summary>
/// <param name="DisplayName">The user's display name.</param>
/// <param name="EmployeeId">The user's employee ID (nullable if not set).</param>
/// <param name="Mail">The user's email address (nullable if not set).</param>
/// <param name="UserPrincipalName">The user's principal name (UPN) (nullable if not set).</param>
/// <param name="Info">The user's description/info attribute (nullable if not set).</param>
/// <param name="Mobile">The user's mobile phone number (nullable if not set).</param>
/// <param name="SamAccountName">The user's samAccountName (nullable if not set).</param>
/// <param name="StreetAddress">The user's street address (nullable if not set).</param>
/// <param name="City">The user's city (nullable if not set).</param>
/// <param name="State">The user's state or province (nullable if not set).</param>
/// <param name="PostalCode">The user's postal/ZIP code (nullable if not set).</param>
/// <param name="Department">The user's department (nullable if not set).</param>
/// <param name="Title">The user's job title (nullable if not set).</param>
/// <param name="PhoneNumber">The user's office phone number (nullable if not set).</param>
public record UserDto(
    string DisplayName,
    string? EmployeeId,
    string? Mail,
    string? UserPrincipalName,
    string? Info,
    string? Mobile,
    string? SamAccountName,
    string? StreetAddress,
    string? City,
    string? State,
    string? PostalCode,
    string? Department,
    string? Title,
    string? PhoneNumber);
