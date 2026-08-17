namespace TestIA.Domain;

/// <summary>
/// Exception thrown when the configured authorization group does not exist in Active Directory.
/// </summary>
public class MissingGroupException : DomainException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MissingGroupException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public MissingGroupException(string message) : base(message) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="MissingGroupException"/> class with a specified error message and a reference to the inner exception.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="inner">The exception that is the cause of the current exception.</param>
    public MissingGroupException(string message, Exception inner) : base(message, inner) { }
}
