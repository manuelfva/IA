using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Text.RegularExpressions;
using TestIA.Logging;

namespace TestIA.Logging;

/// <summary>
/// Implementation of <see cref="ILoggingSink"/> that writes log entries to a text file.
/// Each entry includes a timestamp, log level, message, and optional exception details.
/// File writes are thread-safe using a lock.
/// The parent directory is created automatically if it does not exist.
/// </summary>
public class FileLoggingSink : ILoggingSink
{
    private readonly string _path;
    private readonly object _lock = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="FileLoggingSink"/> class.
    /// </summary>
    /// <param name="path">The full file path where log entries will be appended.</param>
    public FileLoggingSink(string path)
    {
        _path = path ?? throw new ArgumentNullException(nameof(path));

        // Ensure the parent directory exists
        var directory = Path.GetDirectoryName(_path);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }

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
    /// Formats a message template by replacing numbered placeholders (e.g., {0}, {1})
    /// or named placeholders (e.g., {Key}, {Value}) with the corresponding arguments.
    /// Escaped braces ({{ or }}) are ignored.
    /// </summary>
    /// <param name="message">The message template containing placeholders.</param>
    /// <param name="args">The arguments to use for formatting.</param>
    /// <returns>The formatted message string.</returns>
    private static string FormatMessage(string message, object?[] args)
    {
        if (args == null || args.Length == 0)
            return message;

        // Try numbered placeholders first (e.g., {0}, {1})
        if (HasNumberedPlaceholders(message))
        {
            try
            {
                return string.Format(message, args);
            }
            catch (FormatException)
            {
                // Fall back to positional placeholder formatting
            }
        }

        // Positional placeholder formatting (e.g., {Key}, {Value}).
        // Replaces each {placeholder} with the next argument in order,
        // regardless of the placeholder name (matches ILogger behavior).
        var argIndex = 0;
        const string PlaceholderPattern = @"{([^{}]+)}";

        return Regex.Replace(message, PlaceholderPattern, match =>
        {
            if (argIndex < args.Length)
            {
                var value = args[argIndex];
                argIndex++;
                if (value != null)
                {
                    return value.ToString() ?? string.Empty;
                }
            }
            // Leave placeholder as-is if no argument available
            return match.Value;
        });
    }

    /// <summary>
    /// Checks whether a format string contains any numbered placeholders (e.g., {0}, {1}).
    /// Escaped braces ({{ or }}) are ignored.
    /// </summary>
    /// <param name="format">The format string to inspect.</param>
    /// <returns>True if the string contains at least one numbered placeholder; otherwise, false.</returns>
    private static bool HasNumberedPlaceholders(string format)
    {
        var i = 0;
        while ((i = format.IndexOf('{', i)) != -1)
        {
            // Skip escaped braces
            if (i + 1 < format.Length && format[i + 1] == '{')
            {
                i += 2;
                continue;
            }

            // Check for numbered placeholder like {0}, {1}, etc.
            if (i + 1 < format.Length && char.IsDigit(format[i + 1]))
            {
                return true;
            }

            i++;
        }

        return false;
    }

    /// <summary>
    /// Writes a log entry to the file with the specified level, exception, and message.
    /// </summary>
    /// <param name="level">The log level.</param>
    /// <param name="exception">The optional exception to include.</param>
    /// <param name="message">The message template.</param>
    /// <param name="args">Optional arguments for message formatting.</param>
    private void WriteEntry(LogLevel level, Exception? exception, string message, object?[] args)
    {
        ArgumentNullException.ThrowIfNull(message);

        lock (_lock)
        {
            var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");
            var levelStr = level.ToString();
            var formattedMessage = FormatMessage(message, args ?? Array.Empty<object>());

            var entry = $"[{timestamp}] [{levelStr}] {formattedMessage}";

            if (exception != null)
            {
                entry += Environment.NewLine + $"  Exception: {exception}";
            }

            entry += Environment.NewLine + "---";

            File.AppendAllText(_path, entry + Environment.NewLine);
        }
    }
}
