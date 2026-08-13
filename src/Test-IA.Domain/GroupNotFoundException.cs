namespace TestIA.Domain;

/// <summary>
/// Exception thrown when a group is not found in Active Directory.
/// </summary>
public class GroupNotFoundException : DomainException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GroupNotFoundException"/> class.
    /// </summary>
    public GroupNotFoundException() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="GroupNotFoundException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public GroupNotFoundException(string message) : base(message) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="GroupNotFoundException"/> class with a specified error message and a reference to the inner exception.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="inner">The exception that is the cause of the current exception.</param>
    public GroupNotFoundException(string message, Exception inner) : base(message, inner) { }
}
