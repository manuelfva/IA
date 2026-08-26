using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using TestIA.Application;
using TestIA.Domain;

namespace TestIA.Tests;

/// <summary>
/// Unit tests for <see cref="GroupMembershipWriterService"/> constructor validation.
/// </summary>
public class GroupMembershipWriterServiceTests
{
    [Fact]
    public void Constructor_WithNullDiscoveryService_ThrowsArgumentNullException()
    {
        // Arrange
        var logger = Substitute.For<ILogger<GroupMembershipWriterService>>();

        // Act
        var act = () => new GroupMembershipWriterService(null!, logger);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("discoveryService");
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Arrange
        var loggerDiscovery = Substitute.For<ILogger<ADDomainDiscoveryService>>();
        var discoveryService = new ADDomainDiscoveryService(loggerDiscovery);

        // Act
        var act = () => new GroupMembershipWriterService(discoveryService, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [Fact]
    public void Constructor_WithValidDependencies_DoesNotThrow()
    {
        // Arrange
        var loggerDiscovery = Substitute.For<ILogger<ADDomainDiscoveryService>>();
        var discoveryService = new ADDomainDiscoveryService(loggerDiscovery);
        var logger = Substitute.For<ILogger<GroupMembershipWriterService>>();

        // Act
        var act = () => new GroupMembershipWriterService(discoveryService, logger);

        // Assert
        act.Should().NotThrow();
    }
}
