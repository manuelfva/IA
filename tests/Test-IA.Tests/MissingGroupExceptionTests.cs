using FluentAssertions;
using TestIA.Domain;

namespace TestIA.Tests;

/// <summary>
/// Unit tests for <see cref="MissingGroupException"/>.
/// </summary>
public class MissingGroupExceptionTests
{
    /// <summary>
    /// Tests that MissingGroupException can be constructed with a message.
    /// </summary>
    [Fact]
    public void Constructor_WithMessage_SetsMessage()
    {
        // Arrange
        const string message = "Authorization group 'IT' does not exist in Active Directory.";

        // Act
        var exception = new MissingGroupException(message);

        // Assert
        exception.Message.Should().Be(message);
        exception.Should().BeAssignableTo<DomainException>();
    }

    /// <summary>
    /// Tests that MissingGroupException can be constructed with a message and inner exception.
    /// </summary>
    [Fact]
    public void Constructor_WithMessageAndInnerException_SetsMessageAndInnerException()
    {
        // Arrange
        const string message = "Authorization group 'IT' does not exist in Active Directory.";
        var innerException = new Exception("Inner error");

        // Act
        var exception = new MissingGroupException(message, innerException);

        // Assert
        exception.Message.Should().Be(message);
        exception.InnerException.Should().Be(innerException);
        exception.Should().BeAssignableTo<DomainException>();
    }

    /// <summary>
    /// Tests that MissingGroupException inherits from DomainException.
    /// </summary>
    [Fact]
    public void MissingGroupException_InheritsFromDomainException()
    {
        // Act
        var exception = new MissingGroupException("Test message");

        // Assert
        exception.Should().BeAssignableTo<DomainException>();
    }
}
