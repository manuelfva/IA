using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using TestIA.Application;
using TestIA.Domain;

namespace TestIA.Tests;

/// <summary>
/// Unit tests for <see cref="LdapFilterHelper"/>.
/// </summary>
public class LdapFilterHelperTests
{
    [Fact]
    public void Escape_WithNull_ReturnsEmptyString()
    {
        // Act
        var result = LdapFilterHelper.Escape(null!);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void Escape_WithPlainString_ReturnsUnchanged()
    {
        // Arrange
        const string input = "testuser";

        // Act
        var result = LdapFilterHelper.Escape(input);

        // Assert
        result.Should().Be("testuser");
    }

    [Fact]
    public void Escape_WithBackslash_EscapesIt()
    {
        // Arrange
        const string input = "test\\user";

        // Act
        var result = LdapFilterHelper.Escape(input);

        // Assert
        result.Should().Be("test\\\\user");
    }

    [Fact]
    public void Escape_WithAsterisk_EscapesIt()
    {
        // Arrange
        const string input = "test*user";

        // Act
        var result = LdapFilterHelper.Escape(input);

        // Assert
        result.Should().Be("test\\2auser");
    }

    [Fact]
    public void Escape_WithParentheses_EscapesThem()
    {
        // Arrange
        const string input = "test(user)";

        // Act
        var result = LdapFilterHelper.Escape(input);

        // Assert
        result.Should().Be("test\\28user\\29");
    }

    [Fact]
    public void Escape_WithNullCharacter_EscapesIt()
    {
        // Arrange
        var input = "test\0user";

        // Act
        var result = LdapFilterHelper.Escape(input);

        // Assert
        result.Should().Be("test\\00user");
    }

    [Fact]
    public void Escape_WithMultipleSpecialChars_EscapesAll()
    {
        // Arrange
        const string input = "admin\\*(test)";

        // Act
        var result = LdapFilterHelper.Escape(input);

        // Assert
        result.Should().Be("admin\\\\\\2a\\28test\\29");
    }
}
