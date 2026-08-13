namespace TestIA.Domain;

/// <summary>
/// Exception thrown when a user is not found in Active Directory.
/// </summary>
public class UserNotFoundException : DomainException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UserNotFoundException"/> class.
    /// </summary>
    public UserNotFoundException() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="UserNotFoundException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public UserNotFoundException(string message) : base(message) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="UserNotFoundException"/> class with a specified error message and a reference to the inner exception.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="inner">The exception that is the cause of the current exception.</param>
    public UserNotFoundException(string message, Exception inner) : base(message, inner) { }
}
