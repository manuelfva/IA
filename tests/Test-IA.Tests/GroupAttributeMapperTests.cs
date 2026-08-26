using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using System.DirectoryServices.Protocols;
using TestIA.Application;
using TestIA.Domain;

namespace TestIA.Tests;

/// <summary>
/// Unit tests for <see cref="GroupAttributeMapper"/>.
/// </summary>
public class GroupAttributeMapperTests
{
    [Fact]
    public void Map_WithNullEntry_ThrowsArgumentNullException()
    {
        // Arrange
        var mapper = new GroupAttributeMapper();

        // Act
        var act = () => mapper.Map(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("entry");
    }

    [Fact]
    public void GetDisplayValues_WithNullDto_ThrowsArgumentNullException()
    {
        // Arrange
        var mapper = new GroupAttributeMapper();

        // Act
        var act = () => mapper.GetDisplayValues(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("dto");
    }

    [Fact]
    public void GetDisplayValues_WithDto_ReturnsCorrectLabels()
    {
        // Arrange
        var mapper = new GroupAttributeMapper();
        var dto = new GroupDto("Test Group", new[] { "CN=User1,DC=example,DC=com", "CN=User2,DC=example,DC=com" });

        // Act
        var displayValues = mapper.GetDisplayValues(dto);

        // Assert
        displayValues.Should().ContainKey("Display Name");
        displayValues.Should().ContainKey("Members");
        displayValues["Display Name"].Should().Be("Test Group");
        displayValues["Members"].Should().Be("CN=User1,DC=example,DC=com, CN=User2,DC=example,DC=com");
    }

    [Fact]
    public void GetDisplayValues_WithEmptyMembers_FormatsAsNoMembers()
    {
        // Arrange
        var mapper = new GroupAttributeMapper();
        var dto = new GroupDto("Empty Group", Array.Empty<string>());

        // Act
        var displayValues = mapper.GetDisplayValues(dto);

        // Assert
        displayValues["Members"].Should().Be("(no members)");
    }
}
