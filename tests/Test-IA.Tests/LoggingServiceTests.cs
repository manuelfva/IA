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
        var act = () => new LoggingService(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    /// <summary>
    /// Tests that LogInformation throws ArgumentNullException when message is null.
    /// </summary>
    [Fact]
    public void LogInformation_WhenMessageIsNull_ThrowsArgumentNullException()
    {
        // Arrange
        var logger = Substitute.For<ILogger<LoggingService>>();
        var service = new LoggingService(logger);

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
        var service = new LoggingService(logger);

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
        var service = new LoggingService(logger);

        // Act
        var act = () => service.LogError(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("message");
    }

    /// <summary>
    /// Tests that LogInformation does not throw when message is valid.
    /// </summary>
    [Fact]
    public void LogInformation_WhenMessageIsValid_DoesNotThrow()
    {
        // Arrange
        var logger = Substitute.For<ILogger<LoggingService>>();
        var service = new LoggingService(logger);

        // Act
        var act = () => service.LogInformation("Test message {Param}", "value");

        // Assert
        act.Should().NotThrow();
    }

    /// <summary>
    /// Tests that LogWarning does not throw when message is valid.
    /// </summary>
    [Fact]
    public void LogWarning_WhenMessageIsValid_DoesNotThrow()
    {
        // Arrange
        var logger = Substitute.For<ILogger<LoggingService>>();
        var service = new LoggingService(logger);

        // Act
        var act = () => service.LogWarning("Warning message {Param}", "value");

        // Assert
        act.Should().NotThrow();
    }

    /// <summary>
    /// Tests that LogError does not throw when message is valid.
    /// </summary>
    [Fact]
    public void LogError_WhenMessageIsValid_DoesNotThrow()
    {
        // Arrange
        var logger = Substitute.For<ILogger<LoggingService>>();
        var service = new LoggingService(logger);

        // Act
        var act = () => service.LogError("Error message {Param}", "value");

        // Assert
        act.Should().NotThrow();
    }
}
