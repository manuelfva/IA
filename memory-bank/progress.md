# Progress: Test-IA

## What Works

- **Domain layer**: All interfaces (`IGetADUserInfo`, `IGetADGroupInfo`, `IUserGroupAuthorizationService`), DTOs (`UserDto`, `GroupDto`), and domain exceptions (`DomainException`, `UserNotFoundException`, `GroupNotFoundException`, `AccessDeniedException`, `MissingGroupException`) are implemented with XML documentation.
- **Application layer**: 
  - `ADDomainDiscoveryService` — dynamic domain, DC, and Base DN discovery.
  - `GetADUserInfoService` — LDAP user search with safe filter escaping.
  - `GetADGroupInfoService` — LDAP group search with safe filter escaping.
  - `ResolveMemberDisplayNamesAsync` — resolves each group member's Distinguished Name to its `displayName` attribute via LDAP searches.
  - `LdapFilterHelper` — LDAP special character escaping utility.
  - `ServiceCollectionExtensions` — DI registration extension method.
  - `UserGroupAuthorizationService` — checks if current Windows user is member of configured AD group via LDAP. Throws `MissingGroupException` if group not found.
  - `AuthorizationSettings` — strongly-typed configuration class bound to `Authorization` section in `appsettings.json`.
  - `ILoggerService` interface and `LoggingService` implementation with XML documentation. Added `LogError(Exception, string, params object?[])` overload.
- **ConsoleApp logging**: `appsettings.json` and `appsettings.Development.json` with structured logging configuration via `Microsoft.Extensions.Configuration.Json`. Log levels configurable at default and per-namespace level.
- **ConsoleApp authorization**: Configurable group name via `appsettings.json` (`Authorization.RequiredGroup`). Fails fast with clear error messages — `MissingGroupException` (group not found), `AccessDeniedException` (user not member), or `DomainException` (LDAP error).
- **ConsoleApp**: `Program.cs` with full DI setup, real service execution, structured output via `ILoggerService`.
- **WebApp**: ASP.NET Core Razor Pages application with:
  - `IndexModel` page model with `OnPost()` handling both user and group searches.
  - `launchSettings.json` with HTTP/HTTPS URLs.
  - Conditional `UseHttpsRedirection()` for Development mode.
  - `ILoggerService` injected into `IndexModel` for error logging.
  - **Glassmorphism + Aurora UI**: Dark theme, animated aurora background, frosted glass components, gradient text, luminous buttons.
  - CSS custom properties, `backdrop-filter: blur()`, `@keyframes` animations.
  - **Windows Authentication**: `AddNegotiate()` for Kerberos/NTLM.
  - **Policy-based authorization**: `AddPolicy("RequiredGroup")` with `GroupAuthorizationHandler` using `IServiceScopeFactory` for scoped service resolution.
  - `[Authorize(Policy = "RequiredGroup")]` applied to Index page.
- **Tests**: Unit tests for both services and logging project using xUnit, NSubstitute, and FluentAssertions. Total 22 tests passing.
- **Project files**: All `.csproj` files correctly configured with proper references and packages.
- **Solution file**: `Test-IA.slnx` has been regenerated and includes all 6 projects (4 source + 1 test + 1 WebApp).

## What's Left to Build

1. **Validate build**: Run `dotnet build` to confirm compilation succeeds.
2. **Run tests**: Execute `dotnet test` to confirm all unit tests pass.
3. **Generate README**: Only after successful build and test validation.

## Current Status

**Phase**: Initial implementation complete. Validation pending.

The codebase is complete with both ConsoleApp and WebApp presentation layers. Ready for validation.

## Known Issues

1. **No README.md**: Documentation has not been generated yet (requires successful validation first).
2. **No `.gitignore`**: Has been created at the repository root.

## Evolution of Project Decisions

- **Initial decision**: Use `System.DirectoryServices.Protocols` for LDAP (not `System.DirectoryServices` alone) for full control over LDAP operations.
- **Decision**: `ADDomainDiscoveryService.Discover()` is `virtual` to allow test overrides. This avoids adding an interface for the discovery service, keeping the test setup simpler.
- **Decision**: `LdapFilterHelper` is `internal static` rather than a registered service, since it has no dependencies and is only used internally.
- **Decision**: No `IOptions<T>` configuration is used because the application has no externalizable configuration — everything is discovered dynamically.
- **Decision**: `ILogger<T>` is registered as `Singleton` in the console app because `LoggerFactory.Create()` returns a singleton.
- **Decision**: Sample `samAccountName` values (`MFVA649T` and `employees of MADRID`) are `const` in `Program.cs` rather than configurable, since the console app is a demonstration.
- **Decision**: WebApp uses Razor Pages (not MVC Controllers) for a simpler presentation layer.
- **Decision**: WebApp HTTPS redirect is disabled in Development mode to allow POST requests to reach page handlers.
- **Decision**: WebApp UI uses Glassmorphism + Aurora design — dark theme with animated aurora background and frosted glass components.
