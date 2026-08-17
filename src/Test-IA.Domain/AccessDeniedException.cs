namespace TestIA.Domain;

/// <summary>
/// Exception thrown when a user is not authorized to access the application.
/// </summary>
public class AccessDeniedException : DomainException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AccessDeniedException"/> class.
    /// </summary>
    public AccessDeniedException() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="AccessDeniedException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public AccessDeniedException(string message) : base(message) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="AccessDeniedException"/> class with a specified error message and a reference to the inner exception.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="inner">The exception that is the cause of the current exception.</param>
    public AccessDeniedException(string message, Exception inner) : base(message, inner) { }
}
