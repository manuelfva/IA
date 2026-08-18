namespace TestIA.Domain;

/// <summary>
/// Interface for updating Active Directory user attributes.
/// </summary>
public interface IUserWriter
{
    /// <summary>
    /// Updates the specified attributes of an Active Directory user.
    /// Only non-null attributes in the request will be modified.
    /// </summary>
    /// <param name="request">The update request containing the samAccountName and attributes to update.</param>
    /// <returns>A result indicating whether the update succeeded and a descriptive message.</returns>
    /// <exception cref="ArgumentException">Thrown when samAccountName is null or empty.</exception>
    /// <exception cref="UserNotFoundException">Thrown when no user with the given samAccountName is found.</exception>
    /// <exception cref="DomainException">Thrown when an error occurs during the LDAP modify operation.</exception>
    UserUpdateResult UpdateUser(UserUpdateRequest request);
}

/// <summary>
/// Represents a request to update Active Directory user attributes.
/// All attribute properties are nullable; only non-null values will be updated.
/// </summary>
/// <param name="SamAccountName">The samAccountName of the user to update.</param>
/// <param name="Info">The new value for the 'info' attribute (nullable to skip update).</param>
/// <param name="Mobile">The new value for the 'mobile' attribute (nullable to skip update).</param>
/// <param name="StreetAddress">The new value for the 'streetAddress' attribute (nullable to skip update).</param>
/// <param name="City">The new value for the 'l' attribute (nullable to skip update).</param>
/// <param name="State">The new value for the 'st' attribute (nullable to skip update).</param>
/// <param name="PostalCode">The new value for the 'postalCode' attribute (nullable to skip update).</param>
/// <param name="Department">The new value for the 'department' attribute (nullable to skip update).</param>
/// <param name="Title">The new value for the 'title' attribute (nullable to skip update).</param>
/// <param name="PhoneNumber">The new value for the 'telephoneNumber' attribute (nullable to skip update).</param>
public record UserUpdateRequest(
    string SamAccountName,
    string? Info,
    string? Mobile,
    string? StreetAddress,
    string? City,
    string? State,
    string? PostalCode,
    string? Department,
    string? Title,
    string? PhoneNumber);

/// <summary>
/// Represents the result of a user update operation.
/// </summary>
/// <param name="Success">Whether the update operation succeeded.</param>
/// <param name="Message">A descriptive message about the outcome.</param>
public record UserUpdateResult(bool Success, string Message);