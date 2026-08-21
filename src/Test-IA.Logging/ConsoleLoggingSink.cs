using Microsoft.Extensions.Logging;
using TestIA.Logging;

namespace TestIA.Logging;

/// <summary>
/// Implementation of <see cref="ILoggingSink"/> that writes log entries to the console.
/// Each entry includes a timestamp, log level, and message.
/// </summary>
public class ConsoleLoggingSink : ILoggingSink
{
    /// <inheritdoc />
    public void LogInformation(string message, params object?[] args)
    {
        WriteEntry(LogLevel.Information, exception: null, message, args);
    }

    /// <inheritdoc />
    public void LogWarning(string message, params object?[] args)
    {
        WriteEntry(LogLevel.Warning, exception: null, message, args);
    }

    /// <inheritdoc />
    public void LogError(string message, params object?[] args)
    {
        WriteEntry(LogLevel.Error, exception: null, message, args);
    }

    /// <inheritdoc />
    public void LogError(Exception exception, string message, params object?[] args)
    {
        WriteEntry(LogLevel.Error, exception, message, args);
    }

    /// <summary>
    /// Writes a log entry to the console with the specified level, exception, and message.
    /// </summary>
    /// <param name="level">The log level.</param>
    /// <param name="exception">The optional exception to include.</param>
    /// <param name="message">The message template.</param>
    /// <param name="args">Optional arguments for message formatting.</param>
    private void WriteEntry(LogLevel level, Exception? exception, string message, object?[] args)
    {
        ArgumentNullException.ThrowIfNull(message);

        var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");
        var levelStr = level.ToString();
        var formattedMessage = string.Format(message, args ?? Array.Empty<object>());
        var entry = $"[{timestamp}] [{levelStr}] {formattedMessage}";

        if (exception != null)
        {
            entry += Environment.NewLine + $"  Exception: {exception}";
        }

        Console.WriteLine(entry);
    }
}
