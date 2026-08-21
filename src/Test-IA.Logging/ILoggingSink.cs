namespace TestIA.Logging;

/// <summary>
/// Defines the contract for a logging sink.
/// Each sink represents an output target (file, console, database, event log, etc.).
/// Sinks are registered in the DI container and invoked by <see cref="LoggingService"/> for each log call.
/// </summary>
public interface ILoggingSink
{
    /// <summary>
    /// Logs an informational message.
    /// </summary>
    /// <param name="message">The message template in string interpolation syntax.</param>
    /// <param name="args">Optional arguments for message formatting.</param>
    void LogInformation(string message, params object?[] args);

    /// <summary>
    /// Logs a warning message.
    /// </summary>
    /// <param name="message">The message template in string interpolation syntax.</param>
    /// <param name="args">Optional arguments for message formatting.</param>
    void LogWarning(string message, params object?[] args);

    /// <summary>
    /// Logs an error message.
    /// </summary>
    /// <param name="message">The message template in string interpolation syntax.</param>
    /// <param name="args">Optional arguments for message formatting.</param>
    void LogError(string message, params object?[] args);

    /// <summary>
    /// Logs an error message with an exception.
    /// </summary>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">The message template in string interpolation syntax.</param>
    /// <param name="args">Optional arguments for message formatting.</param>
    void LogError(Exception exception, string message, params object?[] args);
}
