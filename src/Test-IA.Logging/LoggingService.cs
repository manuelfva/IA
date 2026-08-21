using Microsoft.Extensions.Logging;
using TestIA.Logging;

namespace TestIA.Logging;

/// <summary>
/// Implementation of <see cref="ILoggerService"/> that wraps <see cref="ILogger{TCategoryName}"/>
/// and delegates to a collection of <see cref="ILoggingSink"/> instances.
/// Each log call is forwarded to all registered sinks, enabling multiple output targets
/// (file, console, database, event log, etc.) simultaneously.
/// If no sinks are registered, logging falls back to <see cref="ILogger{TCategoryName}"/> only.
/// </summary>
public class LoggingService : ILoggerService
{
    private readonly ILogger<LoggingService> _logger;
    private readonly IEnumerable<ILoggingSink> _sinks;

    /// <summary>
    /// Initializes a new instance of the <see cref="LoggingService"/> class.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger{TCategoryName}"/> instance used as a fallback when no sinks are registered.</param>
    /// <param name="sinks">The collection of <see cref="ILoggingSink"/> instances to delegate log calls to.</param>
    public LoggingService(ILogger<LoggingService> logger, IEnumerable<ILoggingSink> sinks)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _sinks = sinks ?? throw new ArgumentNullException(nameof(sinks));
    }

    /// <inheritdoc />
    public void LogInformation(string message, params object?[] args)
    {
        ArgumentNullException.ThrowIfNull(message);

        if (_sinks.Any())
        {
            foreach (var sink in _sinks)
            {
                sink.LogInformation(message, args);
            }
        }
        else
        {
            _logger.LogInformation(message, args);
        }
    }

    /// <inheritdoc />
    public void LogWarning(string message, params object?[] args)
    {
        ArgumentNullException.ThrowIfNull(message);

        if (_sinks.Any())
        {
            foreach (var sink in _sinks)
            {
                sink.LogWarning(message, args);
            }
        }
        else
        {
            _logger.LogWarning(message, args);
        }
    }

    /// <inheritdoc />
    public void LogError(string message, params object?[] args)
    {
        ArgumentNullException.ThrowIfNull(message);

        if (_sinks.Any())
        {
            foreach (var sink in _sinks)
            {
                sink.LogError(message, args);
            }
        }
        else
        {
            _logger.LogError(message, args);
        }
    }

    /// <inheritdoc />
    public void LogError(Exception exception, string message, params object?[] args)
    {
        ArgumentNullException.ThrowIfNull(exception);
        ArgumentNullException.ThrowIfNull(message);

        if (_sinks.Any())
        {
            foreach (var sink in _sinks)
            {
                sink.LogError(exception, message, args);
            }
        }
        else
        {
            _logger.LogError(exception, message, args);
        }
    }
}
