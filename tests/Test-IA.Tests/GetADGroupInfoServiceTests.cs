using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using TestIA.Application;
using TestIA.Domain;

namespace TestIA.Tests;

/// <summary>
/// Unit tests for <see cref="GetADGroupInfoService"/>.
/// </summary>
public class GetADGroupInfoServiceTests
{
    /// <summary>
    /// Tests that GetGroup throws DomainException when the discovery service throws a DomainException.
    /// </summary>
    [Fact]
    public void GetGroup_WhenDiscoveryFails_ThrowsDomainException()
    {
        // Arrange
        var discoveryService = new ThrowingADDomainDiscoveryService(new DomainException("Not joined to a domain."));
        var logger = Substitute.For<ILogger<GetADGroupInfoService>>();
        var service = new GetADGroupInfoService(discoveryService, logger);

        // Act
        var act = () => service.GetGroup("testgroup");

        // Assert
        act.Should().Throw<DomainException>().WithMessage("Not joined to a domain.");
    }

    /// <summary>
    /// Tests that GetGroup throws ArgumentException when samAccountName is null.
    /// </summary>
    [Fact]
    public void GetGroup_WhenSamAccountNameIsNull_ThrowsArgumentNullException()
    {
        // Arrange
        var discoveryService = new ThrowingADDomainDiscoveryService(new DomainException("Not joined to a domain."));
        var logger = Substitute.For<ILogger<GetADGroupInfoService>>();
        var service = new GetADGroupInfoService(discoveryService, logger);

        // Act
        var act = () => service.GetGroup(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("samAccountName");
    }

    /// <summary>
    /// Tests that GetGroup throws DomainException when samAccountName is empty (empty string is not null, so discovery is called).
    /// </summary>
    [Fact]
    public void GetGroup_WhenSamAccountNameIsEmpty_ThrowsDomainException()
    {
        // Arrange
        var discoveryService = new ThrowingADDomainDiscoveryService(new DomainException("Not joined to a domain."));
        var logger = Substitute.For<ILogger<GetADGroupInfoService>>();
        var service = new GetADGroupInfoService(discoveryService, logger);

        // Act
        var act = () => service.GetGroup("");

        // Assert
        act.Should().Throw<DomainException>().WithMessage("Not joined to a domain.");
    }
}
