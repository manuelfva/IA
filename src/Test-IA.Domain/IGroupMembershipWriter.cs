namespace TestIA.Domain;

/// <summary>
/// Interface for adding members to Active Directory groups.
/// </summary>
public interface IGroupMembershipWriter
{
    /// <summary>
    /// Adds a user as a member to an Active Directory group.
    /// <para>
    /// This method searches for both the group and the user by <c>sAMAccountName</c>,
    /// retrieves their distinguished names, and performs an LDAP modify request
    /// to add the user's DN to the group's <c>member</c> attribute.
    /// </para>
    /// </summary>
    /// <param name="groupSamAccountName">The samAccountName of the target group. Must not be null or empty.</param>
    /// <param name="memberSamAccountName">The samAccountName of the user to add as a member. Must not be null or empty.</param>
    /// <returns>A <see cref="GroupMemberOperationResult"/> indicating success or failure with a descriptive message.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="groupSamAccountName"/> or <paramref name="memberSamAccountName"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when either parameter is empty or whitespace.</exception>
    /// <exception cref="GroupNotFoundException">Thrown when no group with the given samAccountName is found.</exception>
    /// <exception cref="UserNotFoundException">Thrown when no user with the given samAccountName is found.</exception>
    /// <exception cref="DomainException">Thrown when an error occurs during the LDAP modify operation.</exception>
    GroupMemberOperationResult AddMember(string groupSamAccountName, string memberSamAccountName);
    /// <summary>
    /// Removes a user as a member from an Active Directory group.
    /// <para>
    /// This method searches for both the group and the user by <c>sAMAccountName</c>,
    /// retrieves their distinguished names, and performs an LDAP modify request
    /// to remove the user's DN from the group's <c>member</c> attribute.
    /// </para>
    /// </summary>
    /// <param name="groupSamAccountName">The samAccountName of the target group. Must not be null or empty.</param>
    /// <param name="memberSamAccountName">The samAccountName of the user to remove as a member. Must not be null or empty.</param>
    /// <returns>A <see cref="GroupMemberOperationResult"/> indicating success or failure with a descriptive message.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="groupSamAccountName"/> or <paramref name="memberSamAccountName"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when either parameter is empty or whitespace.</exception>
    /// <exception cref="GroupNotFoundException">Thrown when no group with the given samAccountName is found.</exception>
    /// <exception cref="UserNotFoundException">Thrown when no user with the given samAccountName is found.</exception>
    /// <exception cref="DomainException">Thrown when an error occurs during the LDAP modify operation.</exception>
    GroupMemberOperationResult RemoveMember(string groupSamAccountName, string memberSamAccountName);
}

/// <summary>
/// Represents the result of a group membership add operation.
/// </summary>
/// <param name="Success">Whether the operation succeeded.</param>
/// <param name="Message">A descriptive message about the outcome.</param>
public record GroupMemberOperationResult(bool Success, string Message);