using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using System.DirectoryServices.Protocols;
using TestIA.Application;
using TestIA.Domain;

namespace TestIA.Tests;

/// <summary>
/// Unit tests for <see cref="UserAttributeMapper"/>.
/// </summary>
public class UserAttributeMapperTests
{
    [Fact]
    public void Map_WithNullEntry_ThrowsArgumentNullException()
    {
        // Arrange
        var mapper = new UserAttributeMapper();

        // Act
        var act = () => mapper.Map(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("entry");
    }

    [Fact]
    public void GetDisplayValues_WithNullDto_ThrowsArgumentNullException()
    {
        // Arrange
        var mapper = new UserAttributeMapper();

        // Act
        var act = () => mapper.GetDisplayValues(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("dto");
    }

    [Fact]
    public void GetDisplayValues_WithDto_ReturnsAllLabels()
    {
        // Arrange
        var mapper = new UserAttributeMapper();
        var dto = new UserDto(
            DisplayName: "John Doe",
            EmployeeId: "EMP001",
            Mail: "john.doe@example.com",
            UserPrincipalName: "john.doe@example.com",
            Info: "Test user",
            Mobile: "+1234567890",
            SamAccountName: "jdoe",
            StreetAddress: "123 Main St",
            City: "Madrid",
            State: "Madrid",
            PostalCode: "28001",
            Department: "IT",
            Title: "Administrator",
            PhoneNumber: "+1234567890");

        // Act
        var displayValues = mapper.GetDisplayValues(dto);

        // Assert
        displayValues.Should().ContainKey("Display Name");
        displayValues.Should().ContainKey("Employee ID");
        displayValues.Should().ContainKey("Email");
        displayValues.Should().ContainKey("UPN");
        displayValues.Should().ContainKey("Description");
        displayValues.Should().ContainKey("Mobile Phone");
        displayValues.Should().ContainKey("samAccountName");
        displayValues.Should().ContainKey("Street Address");
        displayValues.Should().ContainKey("City");
        displayValues.Should().ContainKey("State");
        displayValues.Should().ContainKey("Postal Code");
        displayValues.Should().ContainKey("Department");
        displayValues.Should().ContainKey("Title");
        displayValues.Should().ContainKey("Phone Number");
        displayValues["Display Name"].Should().Be("John Doe");
        displayValues["Email"].Should().Be("john.doe@example.com");
    }

    [Fact]
    public void GetDisplayValues_WithNullValues_FormatsAsNotSet()
    {
        // Arrange
        var mapper = new UserAttributeMapper();
        var dto = new UserDto(
            DisplayName: "Test User",
            EmployeeId: null,
            Mail: null,
            UserPrincipalName: null,
            Info: null,
            Mobile: null,
            SamAccountName: null,
            StreetAddress: null,
            City: null,
            State: null,
            PostalCode: null,
            Department: null,
            Title: null,
            PhoneNumber: null);

        // Act
        var displayValues = mapper.GetDisplayValues(dto);

        // Assert
        displayValues["Email"].Should().Be("(not set)");
        displayValues["UPN"].Should().Be("(not set)");
        displayValues["Department"].Should().Be("(not set)");
    }
}
