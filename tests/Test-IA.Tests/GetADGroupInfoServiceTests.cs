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
    /// Creates a mock group attribute mapper for tests.
    /// </summary>
    private static IAttributeMapper<GroupDto> CreateMockGroupMapper()
    {
        var mock = Substitute.For<IAttributeMapper<GroupDto>>();
        return mock;
    }

    /// <summary>
    /// Tests that GetGroupAsync throws DomainException when the discovery service throws a DomainException.
    /// </summary>
    [Fact]
    public async Task GetGroupAsync_WhenDiscoveryFails_ThrowsDomainException()
    {
        // Arrange
        var discoveryService = new ThrowingADDomainDiscoveryService(new DomainException("Not joined to a domain."));
        var groupMapper = CreateMockGroupMapper();
        var logger = Substitute.For<ILogger<GetADGroupInfoService>>();
        var service = new GetADGroupInfoService(discoveryService, groupMapper, logger);

        // Act
        var act = async () => await service.GetGroupAsync("testgroup");

        // Assert
        await act.Should().ThrowAsync<DomainException>().WithMessage("Not joined to a domain.");
    }

    /// <summary>
    /// Tests that GetGroupAsync throws ArgumentException when samAccountName is null.
    /// </summary>
    [Fact]
    public async Task GetGroupAsync_WhenSamAccountNameIsNull_ThrowsArgumentNullException()
    {
        // Arrange
        var discoveryService = new ThrowingADDomainDiscoveryService(new DomainException("Not joined to a domain."));
        var groupMapper = CreateMockGroupMapper();
        var logger = Substitute.For<ILogger<GetADGroupInfoService>>();
        var service = new GetADGroupInfoService(discoveryService, groupMapper, logger);

        // Act
        var act = async () => await service.GetGroupAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("samAccountName");
    }

    /// <summary>
    /// Tests that GetGroupAsync throws DomainException when samAccountName is empty (empty string is not null, so discovery is called).
    /// </summary>
    [Fact]
    public async Task GetGroupAsync_WhenSamAccountNameIsEmpty_ThrowsDomainException()
    {
        // Arrange
        var discoveryService = new ThrowingADDomainDiscoveryService(new DomainException("Not joined to a domain."));
        var groupMapper = CreateMockGroupMapper();
        var logger = Substitute.For<ILogger<GetADGroupInfoService>>();
        var service = new GetADGroupInfoService(discoveryService, groupMapper, logger);

        // Act
        var act = async () => await service.GetGroupAsync("");

        // Assert
        await act.Should().ThrowAsync<DomainException>().WithMessage("Not joined to a domain.");
    }
}
