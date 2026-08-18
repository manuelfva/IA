using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using TestIA.Application;
using TestIA.Domain;

namespace TestIA.Tests;

/// <summary>
/// Throwing discovery service for testing.
/// </summary>
public class ThrowingADDomainDiscoveryService : ADDomainDiscoveryService
{
    private readonly Exception _exception;

    /// <summary>
    /// Initializes a new instance of the <see cref="ThrowingADDomainDiscoveryService"/> class.
    /// </summary>
    /// <param name="exception">The exception to throw.</param>
    public ThrowingADDomainDiscoveryService(Exception exception)
        : base(Substitute.For<ILogger<ThrowingADDomainDiscoveryService>>())
    {
        _exception = exception;
    }

    /// <inheritdoc />
    public override (string DomainName, string DomainController, string BaseDN) Discover()
    {
        throw _exception;
    }
}

/// <summary>
/// Unit tests for <see cref="GetADUserInfoService"/>.
/// </summary>
public class GetADUserInfoServiceTests
{
    /// <summary>
    /// Creates a mock user attribute mapper for tests.
    /// </summary>
    private static IAttributeMapper<UserDto> CreateMockUserMapper()
    {
        var mock = Substitute.For<IAttributeMapper<UserDto>>();
        return mock;
    }

    /// <summary>
    /// Tests that GetUser throws DomainException when the discovery service throws a DomainException.
    /// </summary>
    [Fact]
    public void GetUser_WhenDiscoveryFails_ThrowsDomainException()
    {
        // Arrange
        var discoveryService = new ThrowingADDomainDiscoveryService(new DomainException("Not joined to a domain."));
        var userMapper = CreateMockUserMapper();
        var logger = Substitute.For<ILogger<GetADUserInfoService>>();
        var service = new GetADUserInfoService(discoveryService, userMapper, logger);

        // Act
        var act = () => service.GetUser("testuser");

        // Assert
        act.Should().Throw<DomainException>().WithMessage("Not joined to a domain.");
    }

    /// <summary>
    /// Tests that GetUser throws ArgumentException when samAccountName is null.
    /// </summary>
    [Fact]
    public void GetUser_WhenSamAccountNameIsNull_ThrowsArgumentNullException()
    {
        // Arrange
        var discoveryService = new ThrowingADDomainDiscoveryService(new DomainException("Not joined to a domain."));
        var userMapper = CreateMockUserMapper();
        var logger = Substitute.For<ILogger<GetADUserInfoService>>();
        var service = new GetADUserInfoService(discoveryService, userMapper, logger);

        // Act
        var act = () => service.GetUser(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("samAccountName");
    }

    /// <summary>
    /// Tests that GetUser throws ArgumentException when samAccountName is empty.
    /// </summary>
    [Fact]
    public void GetUser_WhenSamAccountNameIsEmpty_ThrowsDomainException()
    {
        // Arrange
        var discoveryService = new ThrowingADDomainDiscoveryService(new DomainException("Not joined to a domain."));
        var userMapper = CreateMockUserMapper();
        var logger = Substitute.For<ILogger<GetADUserInfoService>>();
        var service = new GetADUserInfoService(discoveryService, userMapper, logger);

        // Act
        var act = () => service.GetUser("");

        // Assert
        act.Should().Throw<DomainException>().WithMessage("Not joined to a domain.");
    }
}
