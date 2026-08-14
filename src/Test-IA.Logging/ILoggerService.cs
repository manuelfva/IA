namespace TestIA.Logging;

/// <summary>
/// Interface for logging operations. Wraps <see cref="Microsoft.Extensions.Logging.ILogger"/> to provide a consistent logging abstraction.
/// </summary>
public interface ILoggerService
{
    /// <summary>
    /// Logs an informational message.
    /// </summary>
    /// <param name="message">The message to log.</param>
    /// <param name="args">Optional arguments for message formatting.</param>
    void LogInformation(string message, params object?[] args);

    /// <summary>
    /// Logs a warning message.
    /// </summary>
    /// <param name="message">The message to log.</param>
    /// <param name="args">Optional arguments for message formatting.</param>
    void LogWarning(string message, params object?[] args);

    /// <summary>
    /// Logs an error message.
    /// </summary>
    /// <param name="message">The message to log.</param>
    /// <param name="args">Optional arguments for message formatting.</param>
    void LogError(string message, params object?[] args);

    /// <summary>
    /// Logs an error message with an exception.
    /// </summary>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">The message to log.</param>
    /// <param name="args">Optional arguments for message formatting.</param>
    void LogError(Exception exception, string message, params object?[] args);
}
