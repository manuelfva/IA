using System.DirectoryServices.Protocols;

namespace TestIA.Application;

/// <summary>
/// Defines a contract for mapping LDAP search result entries to strongly-typed DTOs.
/// <para>
/// This generic interface centralizes the attribute-to-DTO mapping logic, making it
/// reusable across multiple services and easy to test independently. Each implementation
/// declares which LDAP attributes it expects and how to construct the target DTO.
/// </para>
/// </summary>
/// <typeparam name="TDto">
/// The DTO type to produce from a search result entry. Typically a record type defined in the Domain layer.
/// </typeparam>
/// <remarks>
/// <b>Implementation pattern:</b>
/// <list type="number">
///   <item><description>Declare a <c>static readonly</c> dictionary mapping LDAP attribute names to display labels.</description></item>
///   <item><description>Implement <c>Map(SearchResultEntry)</c> to extract attributes and construct the DTO.</description></item>
///   <item><description>Register in DI as <c>Scoped</c> via <c>ServiceCollectionExtensions</c>.</description></item>
/// </list>
/// <para>
/// <b>Future extensibility:</b> To add a new DTO, create a new implementation of this interface
/// (e.g., <c>ComputerAttributeMapper : IAttributeMapper&lt;ComputerDto&gt;</c>) and register it in DI.
/// </para>
/// </remarks>
public interface IAttributeMapper<TDto>
{
    /// <summary>
    /// Gets the dictionary mapping LDAP attribute names to their human-readable display labels.
    /// <para>
    /// This dictionary defines the exact set of attributes that will be requested from Active Directory
    /// during the LDAP search. Only these attributes are fetched, which reduces network traffic.
    /// </para>
    /// </summary>
    Dictionary<string, string> Attributes { get; }

    /// <summary>
    /// Maps an LDAP search result entry to the target DTO type.
    /// <para>
    /// This method extracts all declared attributes from the entry and constructs a new DTO instance.
    /// Attributes that are missing or empty are mapped to <c>null</c> (or a default value if specified).
    /// </para>
    /// </summary>
    /// <param name="entry">
    /// The search result entry containing the LDAP attributes to extract. Must not be null.
    /// </param>
    /// <returns>
    /// A new instance of <typeparamref name="TDto"/> populated with the extracted attribute values.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="entry"/> is null.</exception>
    TDto Map(SearchResultEntry entry);

    /// <summary>
    /// Extracts display values from a mapped DTO, pairing each attribute's
    /// human-readable label with its corresponding value.
    /// <para>
    /// This method is used by presentation layers (e.g., console applications) to
    /// dynamically render DTO properties without hardcoding attribute names or labels.
    /// Null values are formatted as "(not set)" for clarity.
    /// </para>
    /// </summary>
    /// <param name="dto">
    /// The DTO instance returned by <see cref="Map"/>. Must not be null.
    /// </param>
    /// <returns>
    /// A dictionary where keys are display labels (e.g., "Display Name")
    /// and values are the formatted DTO property values (nulls shown as "(not set)").
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="dto"/> is null.</exception>
    Dictionary<string, string> GetDisplayValues(TDto dto);
}
