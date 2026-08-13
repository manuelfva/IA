# Active Context: Test-IA

## Current Work Focus

The project has been created with all core services implemented. The memory bank is being initialized for the first time.

## Recent Changes

- Initial project creation following Clean Architecture principles.
- All four projects created: Domain, Application, Logging, ConsoleApp.
- Unit tests created for both services and the logging project.
- Memory bank files being created for the first time.
- **Fixed `Test-IA.slnx`**: Regenerated to include all 4 source projects and 1 test project.

## Next Steps

1. **Validate build**: Run `dotnet build` to verify all projects compile.
2. **Run tests**: Execute `dotnet test` to confirm unit tests pass.
3. **Run console app**: Execute on a domain-joined Windows machine to validate real AD DS connectivity.
4. **Generate README**: Only after successful validation.

## Active Decisions and Considerations

- The `ADDomainDiscoveryService` uses `virtual` on the `Discover()` method to allow test overrides without requiring an interface. This is a deliberate design choice to keep the test setup simple.
- The `LdapFilterHelper` is `internal static` since it's only used within the Application layer. It manually escapes LDAP special characters instead of relying on `SearchFilter.Escape` for full control.
- `ILogger<T>` is registered as `Singleton` in the console app's composition root because `LoggerFactory.Create()` produces a singleton logger factory.
- No `appsettings.json` is used because the application relies entirely on dynamic discovery -- there are no configuration values to externalize.

## Important Patterns and Preferences

- **Namespace convention**: `TestIA.Domain`, `TestIA.Application`, `TestIA.Logging`, `TestIA.ConsoleApp`, `TestIA.Tests` (no hyphens in namespaces).
- **DTOs as records**: `UserDto` and `GroupDto` are immutable record types with positional parameters.
- **Exception hierarchy**: `DomainException` is the base, with `UserNotFoundException` and `GroupNotFoundException` as specialized children.
- **Service registration**: Done via `ServiceCollectionExtensions.AddTestIAServices()` extension method in the Application project.
- **Testing pattern**: `ThrowingADDomainDiscoveryService` extends `ADDomainDiscoveryService` and overrides `Discover()` to throw controlled exceptions. This avoids the need for mocking the discovery service entirely.

## Known Issues

- None at this time.

## Learnings and Project Insights

- The solution file (`Test-IA.slnx`) has been fixed and now includes all 4 source projects and 1 test project.
- The console app's `Program.cs` manually registers `ILogger<T>` for each service type because `LoggerFactory.Create()` doesn't automatically provide scoped loggers for all types.
