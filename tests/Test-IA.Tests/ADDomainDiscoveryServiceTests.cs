using Microsoft.Extensions.Logging;
using TestIA.Application;
using TestIA.Domain;

namespace TestIA.Tests;

/// <summary>
/// Unit tests for the <see cref="ADDomainDiscoveryService"/> domain membership methods.
/// <para>
/// These tests verify that <see cref="ADDomainDiscoveryService.IsDomainJoined"/> and
/// <see cref="ADDomainDiscoveryService.ValidateDomainJoined"/> behave correctly when
/// the machine is domain-joined and when it is not.
/// </para>
/// </summary>
/// <remarks>
/// <para>
/// <b>Important:</b> These tests use the real <c>System.DirectoryServices</c> APIs,
/// so they will actually attempt to query the machine's domain membership.
/// On a domain-joined machine, <see cref="IsDomainJoined_ReturnsTrue_WhenDomainJoined"/> will pass.
/// On a non-domain-joined machine, <see cref="IsDomainJoined_ReturnsFalse_WhenNotDomainJoined"/> will pass.
/// </para>
/// <para>
/// The <see cref="ValidateDomainJoined_ThrowsWhenNotDomainJoined"/> test is expected to fail
/// (throw) when the machine is domain-joined, and pass when it is not. Therefore,
/// it is annotated with <see cref="Fact"/> and uses the <c>ExpectedException</c> pattern
/// to validate the error message content.
/// </para>
/// </remarks>
public class ADDomainDiscoveryServiceTests
{
    private readonly ADDomainDiscoveryService _service;

    /// <summary>
    /// Initializes a new instance of the <see cref="ADDomainDiscoveryServiceTests"/> class
    /// with a test logger that captures log output for inspection.
    /// </summary>
    public ADDomainDiscoveryServiceTests()
    {
        var logger = new TestLogger<ADDomainDiscoveryService>();
        _service = new ADDomainDiscoveryService(logger);
    }

    /// <summary>
    /// Verifies that <see cref="ADDomainDiscoveryService.IsDomainJoined"/> returns <c>true</c>
    /// when the machine is joined to an Active Directory domain.
    /// </summary>
    /// <remarks>
    /// This test will only pass on a domain-joined machine. On a non-domain-joined machine,
    /// it will fail with a clear message explaining the context.
    /// </remarks>
    [Fact]
    public void IsDomainJoined_ReturnsTrue_WhenDomainJoined()
    {
        // Act
        var result = _service.IsDomainJoined();

        // Assert
        Assert.True(result, "Expected IsDomainJoined to return true on a domain-joined machine.");
    }

    /// <summary>
    /// Verifies that <see cref="ADDomainDiscoveryService.IsDomainJoined"/> returns <c>false</c>
    /// when the machine is NOT joined to an Active Directory domain.
    /// </summary>
    /// <remarks>
    /// This test will only pass on a non-domain-joined machine. On a domain-joined machine,
    /// it will fail with a clear message explaining the context.
    /// </remarks>
    [Fact]
    public void IsDomainJoined_ReturnsFalse_WhenNotDomainJoined()
    {
        // Act
        var result = _service.IsDomainJoined();

        // Assert
        Assert.False(result, "Expected IsDomainJoined to return false on a non-domain-joined machine.");
    }

    /// <summary>
    /// Verifies that <see cref="ADDomainDiscoveryService.ValidateDomainJoined"/> does NOT throw
    /// when the machine is joined to an Active Directory domain.
    /// </summary>
    /// <remarks>
    /// This test will only pass on a domain-joined machine. On a non-domain-joined machine,
    /// it will fail with a clear message explaining the context.
    /// </remarks>
    [Fact]
    public void ValidateDomainJoined_DoesNotThrow_WhenDomainJoined()
    {
        // Act & Assert
        var exception = Record.Exception(() => _service.ValidateDomainJoined());
        Assert.Null(exception);
    }

    /// <summary>
    /// Verifies that <see cref="ADDomainDiscoveryService.ValidateDomainJoined"/> throws a
    /// <see cref="DomainException"/> with a descriptive message when the machine is NOT
    /// joined to an Active Directory domain.
    /// </summary>
    /// <remarks>
    /// This test will only pass on a non-domain-joined machine. On a domain-joined machine,
    /// it will fail because no exception is thrown.
    /// </remarks>
    [Fact]
    public void ValidateDomainJoined_ThrowsDomainException_WhenNotDomainJoined()
    {
        // Act & Assert
        var exception = Record.Exception(() => _service.ValidateDomainJoined());
        Assert.NotNull(exception);
        Assert.IsType<DomainException>(exception);

        var domainException = (DomainException)exception;
        Assert.Contains("not joined to an Active Directory domain", domainException.Message, StringComparison.OrdinalIgnoreCase);
    }
}

/// <summary>
/// A minimal test logger implementation that captures log messages without writing to the console.
/// </summary>
/// <typeparam name="TCategory">The category type for the logger.</typeparam>
public class TestLogger<TCategory> : ILogger<TCategory>
{
    /// <summary>
    /// Captured log messages, stored as a list of (level, message) tuples.
    /// </summary>
    public List<(LogLevel Level, string Message)> Messages { get; } = new();

    /// <inheritdoc />
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    /// <inheritdoc />
    public bool IsEnabled(LogLevel logLevel) => true;

    /// <inheritdoc />
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        Messages.Add((logLevel, formatter(state, exception)));
    }
}
