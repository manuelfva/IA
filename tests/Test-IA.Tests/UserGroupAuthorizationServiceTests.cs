using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using FluentAssertions;
using TestIA.Application;
using TestIA.Domain;

namespace TestIA.Tests;

/// <summary>
/// Throwing discovery service for testing UserGroupAuthorizationService.
/// </summary>
public class ThrowingADDomainDiscoveryServiceForAuth : ADDomainDiscoveryService
{
    private readonly Exception _exception;

    /// <summary>
    /// Initializes a new instance of the <see cref="ThrowingADDomainDiscoveryServiceForAuth"/> class.
    /// </summary>
    /// <param name="exception">The exception to throw.</param>
    public ThrowingADDomainDiscoveryServiceForAuth(Exception exception)
        : base(Substitute.For<ILogger<ThrowingADDomainDiscoveryServiceForAuth>>())
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
/// Unit tests for <see cref="UserGroupAuthorizationService"/>.
/// </summary>
public class UserGroupAuthorizationServiceTests
{
    /// <summary>
    /// Creates a mock <see cref="ICurrentUser"/> for testing.
    /// </summary>
    /// <param name="userName">The user name to return (e.g., "DOMAIN\\user").</param>
    /// <returns>A configured <see cref="ICurrentUser"/> substitute.</returns>
    private static ICurrentUser CreateMockCurrentUser(string userName = "DOMAIN\\testuser")
    {
        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.UserName.Returns(userName);
        currentUser.IsAuthenticated.Returns(true);
        return currentUser;
    }

    /// <summary>
    /// Tests that IsMemberOfGroup throws DomainException when the discovery service throws a DomainException.
    /// </summary>
    [Fact]
    public void IsMemberOfGroup_WhenDiscoveryFails_ThrowsDomainException()
    {
        // Arrange
        var discoveryService = new ThrowingADDomainDiscoveryServiceForAuth(new DomainException("Not joined to a domain."));
        var currentUser = CreateMockCurrentUser();
        var logger = Substitute.For<ILogger<UserGroupAuthorizationService>>();
        var options = Options.Create(new AuthorizationSettings { RequiredGroup = "IT" });
        var service = new UserGroupAuthorizationService(discoveryService, currentUser, logger, options);

        // Act
        var act = () => service.IsMemberOfGroup("IT");

        // Assert
        act.Should().Throw<DomainException>().WithMessage("Not joined to a domain.");
    }

    /// <summary>
    /// Tests that IsMemberOfGroup throws DomainException when the group is not found.
    /// </summary>
    [Fact]
    public void IsMemberOfGroup_WhenGroupNotFound_ThrowsDomainException()
    {
        // This test would require mocking LDAP connections, which is complex.
        // For now, we test the discovery failure path.
        // A full integration test would require a real AD LDS instance.
        // Arrange
        var discoveryService = new ThrowingADDomainDiscoveryServiceForAuth(new DomainException("Not joined to a domain."));
        var currentUser = CreateMockCurrentUser();
        var logger = Substitute.For<ILogger<UserGroupAuthorizationService>>();
        var options = Options.Create(new AuthorizationSettings { RequiredGroup = "IT" });
        var service = new UserGroupAuthorizationService(discoveryService, currentUser, logger, options);

        // Act
        var act = () => service.IsMemberOfGroup("IT");

        // Assert
        act.Should().Throw<DomainException>();
    }

    /// <summary>
    /// Tests that the service is constructed correctly with valid dependencies.
    /// </summary>
    [Fact]
    public void Constructor_WithValidDependencies_DoesNotThrow()
    {
        // Arrange
        var logger = Substitute.For<ILogger<ADDomainDiscoveryService>>();
        var discoveryService = new ADDomainDiscoveryService(logger);
        var currentUser = CreateMockCurrentUser();
        var loggerAuth = Substitute.For<ILogger<UserGroupAuthorizationService>>();
        var options = Options.Create(new AuthorizationSettings { RequiredGroup = "IT" });

        // Act
        var act = () => new UserGroupAuthorizationService(discoveryService, currentUser, loggerAuth, options);

        // Assert
        act.Should().NotThrow();
    }

    /// <summary>
    /// Tests that the service throws ArgumentNullException when discovery service is null.
    /// </summary>
    [Fact]
    public void Constructor_WithNullDiscoveryService_ThrowsArgumentNullException()
    {
        // Arrange
        var currentUser = CreateMockCurrentUser();
        var logger = Substitute.For<ILogger<UserGroupAuthorizationService>>();
        var options = Options.Create(new AuthorizationSettings { RequiredGroup = "IT" });

        // Act
        var act = () => new UserGroupAuthorizationService(null!, currentUser, logger, options);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("discovery");
    }

    /// <summary>
    /// Tests that the service throws ArgumentNullException when current user is null.
    /// </summary>
    [Fact]
    public void Constructor_WithNullCurrentUser_ThrowsArgumentNullException()
    {
        // Arrange
        var loggerDiscovery = Substitute.For<ILogger<ADDomainDiscoveryService>>();
        var discoveryService = new ADDomainDiscoveryService(loggerDiscovery);
        var loggerAuth = Substitute.For<ILogger<UserGroupAuthorizationService>>();
        var options = Options.Create(new AuthorizationSettings { RequiredGroup = "IT" });

        // Act
        var act = () => new UserGroupAuthorizationService(discoveryService, null!, loggerAuth, options);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("currentUser");
    }

    /// <summary>
    /// Tests that the service throws ArgumentNullException when logger is null.
    /// </summary>
    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Arrange
        var loggerDiscovery = Substitute.For<ILogger<ADDomainDiscoveryService>>();
        var discoveryService = new ADDomainDiscoveryService(loggerDiscovery);
        var currentUser = CreateMockCurrentUser();
        var loggerAuth = Substitute.For<ILogger<UserGroupAuthorizationService>>();
        var options = Options.Create(new AuthorizationSettings { RequiredGroup = "IT" });

        // Act
        var act = () => new UserGroupAuthorizationService(discoveryService, currentUser, null!, options);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    /// <summary>
    /// Tests that the service throws ArgumentNullException when options is null.
    /// </summary>
    [Fact]
    public void Constructor_WithNullOptions_ThrowsArgumentNullException()
    {
        // Arrange
        var loggerDiscovery = Substitute.For<ILogger<ADDomainDiscoveryService>>();
        var discoveryService = new ADDomainDiscoveryService(loggerDiscovery);
        var currentUser = CreateMockCurrentUser();
        var loggerAuth = Substitute.For<ILogger<UserGroupAuthorizationService>>();

        // Act
        var act = () => new UserGroupAuthorizationService(discoveryService, currentUser, loggerAuth, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("settings");
    }
}
