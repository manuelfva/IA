using Microsoft.Extensions.Logging;

namespace TestIA.Logging;

/// <summary>
/// Implementation of <see cref="ILoggerService"/> that wraps <see cref="ILogger{TCategoryName}"/>.
/// </summary>
public class LoggingService : ILoggerService
{
    private readonly ILogger<LoggingService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="LoggingService"/> class.
    /// </summary>
    /// <param name="logger">The logger instance to use for logging operations.</param>
    public LoggingService(ILogger<LoggingService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public void LogInformation(string message, params object?[] args)
    {
        ArgumentNullException.ThrowIfNull(message);

        _logger.LogInformation(message, args);
    }

    /// <inheritdoc />
    public void LogWarning(string message, params object?[] args)
    {
        ArgumentNullException.ThrowIfNull(message);

        _logger.LogWarning(message, args);
    }

    /// <inheritdoc />
    public void LogError(string message, params object?[] args)
    {
        ArgumentNullException.ThrowIfNull(message);

        _logger.LogError(message, args);
    }

    /// <inheritdoc />
    public void LogError(Exception exception, string message, params object?[] args)
    {
        ArgumentNullException.ThrowIfNull(exception);
        ArgumentNullException.ThrowIfNull(message);

        _logger.LogError(exception, message, args);
    }
}
