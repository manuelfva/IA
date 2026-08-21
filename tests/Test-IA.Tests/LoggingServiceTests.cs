using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using TestIA.Logging;

namespace TestIA.Tests;

/// <summary>
/// Unit tests for <see cref="LoggingService"/>.
/// </summary>
public class LoggingServiceTests
{
    /// <summary>
    /// Tests that LoggingService throws ArgumentNullException when logger is null.
    /// </summary>
    [Fact]
    public void Constructor_WhenLoggerIsNull_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new LoggingService(null!, Array.Empty<ILoggingSink>());

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    /// <summary>
    /// Tests that LoggingService throws ArgumentNullException when sinks is null.
    /// </summary>
    [Fact]
    public void Constructor_WhenSinksIsNull_ThrowsArgumentNullException()
    {
        // Arrange
        var logger = Substitute.For<ILogger<LoggingService>>();

        // Act
        var act = () => new LoggingService(logger, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("sinks");
    }

    /// <summary>
    /// Tests that LogInformation throws ArgumentNullException when message is null.
    /// </summary>
    [Fact]
    public void LogInformation_WhenMessageIsNull_ThrowsArgumentNullException()
    {
        // Arrange
        var logger = Substitute.For<ILogger<LoggingService>>();
        var service = new LoggingService(logger, Array.Empty<ILoggingSink>());

        // Act
        var act = () => service.LogInformation(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("message");
    }

    /// <summary>
    /// Tests that LogWarning throws ArgumentNullException when message is null.
    /// </summary>
    [Fact]
    public void LogWarning_WhenMessageIsNull_ThrowsArgumentNullException()
    {
        // Arrange
        var logger = Substitute.For<ILogger<LoggingService>>();
        var service = new LoggingService(logger, Array.Empty<ILoggingSink>());

        // Act
        var act = () => service.LogWarning(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("message");
    }

    /// <summary>
    /// Tests that LogError throws ArgumentNullException when message is null.
    /// </summary>
    [Fact]
    public void LogError_WhenMessageIsNull_ThrowsArgumentNullException()
    {
        // Arrange
        var logger = Substitute.For<ILogger<LoggingService>>();
        var service = new LoggingService(logger, Array.Empty<ILoggingSink>());

        // Act
        var act = () => service.LogError(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("message");
    }

    /// <summary>
    /// Tests that LogInformation does not throw when message is valid and no sinks are registered.
    /// </summary>
    [Fact]
    public void LogInformation_WhenMessageIsValidAndNoSinks_DoesNotThrow()
    {
        // Arrange
        var logger = Substitute.For<ILogger<LoggingService>>();
        var service = new LoggingService(logger, Array.Empty<ILoggingSink>());

        // Act
        var act = () => service.LogInformation("Test message {Param}", "value");

        // Assert
        act.Should().NotThrow();
    }

    /// <summary>
    /// Tests that LogWarning does not throw when message is valid and no sinks are registered.
    /// </summary>
    [Fact]
    public void LogWarning_WhenMessageIsValidAndNoSinks_DoesNotThrow()
    {
        // Arrange
        var logger = Substitute.For<ILogger<LoggingService>>();
        var service = new LoggingService(logger, Array.Empty<ILoggingSink>());

        // Act
        var act = () => service.LogWarning("Warning message {Param}", "value");

        // Assert
        act.Should().NotThrow();
    }

    /// <summary>
    /// Tests that LogError does not throw when message is valid and no sinks are registered.
    /// </summary>
    [Fact]
    public void LogError_WhenMessageIsValidAndNoSinks_DoesNotThrow()
    {
        // Arrange
        var logger = Substitute.For<ILogger<LoggingService>>();
        var service = new LoggingService(logger, Array.Empty<ILoggingSink>());

        // Act
        var act = () => service.LogError("Error message {Param}", "value");

        // Assert
        act.Should().NotThrow();
    }

    /// <summary>
    /// Tests that LogInformation delegates to all registered sinks.
    /// </summary>
    [Fact]
    public void LogInformation_WhenSinksRegistered_DelegatesToAllSinks()
    {
        // Arrange
        var logger = Substitute.For<ILogger<LoggingService>>();
        var sink1 = Substitute.For<ILoggingSink>();
        var sink2 = Substitute.For<ILoggingSink>();
        var service = new LoggingService(logger, new[] { sink1, sink2 });

        // Act
        service.LogInformation("Test message {Param}", "value");

        // Assert
        sink1.Received(1).LogInformation("Test message {Param}", "value");
        sink2.Received(1).LogInformation("Test message {Param}", "value");
    }

    /// <summary>
    /// Tests that LogError delegates to all registered sinks with an exception.
    /// </summary>
    [Fact]
    public void LogError_WhenSinksRegisteredWithException_DelegatesToAllSinks()
    {
        // Arrange
        var logger = Substitute.For<ILogger<LoggingService>>();
        var sink = Substitute.For<ILoggingSink>();
        var exception = new InvalidOperationException("Test exception");
        var service = new LoggingService(logger, new[] { sink });

        // Act
        service.LogError(exception, "Error occurred {Context}", "context");

        // Assert
        sink.Received(1).LogError(exception, "Error occurred {Context}", "context");
    }
}
