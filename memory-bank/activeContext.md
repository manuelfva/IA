# Active Context: Test-IA

## Current Work Focus

The project has been created with all core services implemented and a WebApp presentation layer. The memory bank is being initialized for the first time.

## Recent Changes

- **AD Discovery Caching**: Added internal caching to `ADDomainDiscoveryService.Discover()` to prevent redundant LDAP queries. The first successful call caches the result; subsequent calls return the cached value instantly. This eliminates duplicate "Discovered domain" log messages when multiple services (authorization, user lookup, group lookup) all require the domain, DC, and Base DN.
- **Dynamic Console Display**: Replaced hardcoded `LogInformation` calls in ConsoleApp with dynamic `foreach` loops using `IAttributeMapper.GetDisplayValues()`. Added `info` and `mobile` attributes to `UserDto` and `UserAttributeMapper`. Both User and Group display now use the same `GetDisplayValues()` pattern.
- **Dynamic WebApp Display**: Replaced hardcoded HTML table rows in WebApp `Index.cshtml` with dynamic `@foreach` loops over `Model.UserDisplayValues` and `Model.GroupDisplayValues`. Injected `IAttributeMapper<UserDto>` and `IAttributeMapper<GroupDto>` into `IndexModel`. Both ConsoleApp and WebApp now use the same `GetDisplayValues()` pattern for consistent dynamic rendering.
- **Attribute Mapper Refactoring**: Replaced static `_attributeNames` dictionaries in `GetADUserInfoService` and `GetADGroupInfoService` with a generic `IAttributeMapper<TDto>` interface. Created `UserAttributeMapper` and `GroupAttributeMapper` implementations using composition. This centralizes attribute-to-DTO mapping logic, eliminates duplication, and provides a reusable pattern for future DTOs. All 22 tests pass.
- **Comprehensive XML Documentation & Inline Comments**: Added extensive XML documentation comments (`///`) and human-friendly inline comments (`//`) to all files in the Test-IA.Application project. Documentation explains the *why* (business context, security considerations, LDAP patterns) rather than the *what* (code repetition).
- Initial project creation following Clean Architecture principles.
- All four projects created: Domain, Application, Logging, ConsoleApp.
- Unit tests created for both services and the logging project.
- Memory bank files being created for the first time.
- **Fixed `Test-IA.slnx`**: Regenerated to include all 4 source projects and 1 test project.
- **Created `Test-IA.WebApp`**: ASP.NET Core Razor Pages web application exposing the same AD lookup services via a web UI.
- **Added appsettings.json to ConsoleApp**: Configured `Test-IA.ConsoleApp` with `appsettings.json` and `appsettings.Development.json` for logging level configuration, matching the `Test-IA.WebApp` pattern. Added `Microsoft.Extensions.Configuration.Json` package. Updated `Program.cs` to load configuration via `ConfigurationBuilder` using assembly location for robust path resolution.
- **Fixed WebApp bug #1**: HTTPS redirect middleware was blocking POST requests — added `launchSettings.json` and made `UseHttpsRedirection()` conditional on non-Development environment.
- **Fixed WebApp bug #2**: Model binding mismatch — `CurrentAction` property was never populated because the hidden input sent `SearchAction` — renamed property to `SearchAction` to match form field name.
- **Added error logging**: Injected `ILoggerService` into `IndexModel` and added `LogError(Exception, string, params object?[])` overload to the logging abstraction.
- **UI redesign #1 (Professional)**: Gradient hero, elevated cards, colored icons, hover effects, animated alerts and results.
- **UI redesign #2 (Glassmorphism + Aurora)**: Complete visual overhaul — dark theme (`#0a0a1a`), animated aurora background (4 floating gradient orbs), frosted glass components (`backdrop-filter: blur(20px)`), gradient text, luminous buttons.
- **Implemented member DN to display name resolution**: `GetADGroupInfoService` now resolves each group member's Distinguished Name to its `displayName` attribute by performing additional LDAP searches. Falls back to DN if resolution fails (logged as warning).
- **Fixed member resolution bug**: Changed `SearchScope.Base` to `SearchScope.Subtree` in `ResolveMemberDisplayNamesAsync` — `SearchScope.Base` only searches the base DN object itself, not the entire directory, so member objects could never be found.
- **Implemented Windows Integrated Authentication with group-based authorization**: Both ConsoleApp and WebApp now require users to be members of a configured Active Directory group to access the application. Group name is configurable via `appsettings.json` (`Authorization.RequiredGroup`).
- **Added `MissingGroupException`**: Domain exception thrown when the configured authorization group does not exist in Active Directory.
- **Added `UserGroupAuthorizationService`**: Checks if the current Windows user is a member of the configured AD group via LDAP. Throws `MissingGroupException` if group not found, returns `false` if user not found or not a member.
- **Added `AuthorizationSettings`**: Strongly-typed configuration class bound to `Authorization` section in `appsettings.json`.
- **ConsoleApp authorization**: Fails fast with clear error messages — `MissingGroupException` (group not found), `AccessDeniedException` (user not member), or `DomainException` (LDAP error).
- **WebApp authorization**: Uses ASP.NET Core Windows Authentication (`AddNegotiate()`) + policy-based authorization (`AddPolicy("RequiredGroup")`). `GroupAuthorizationHandler` uses `IServiceScopeFactory` to resolve scoped `IUserGroupAuthorizationService` within a scope. Non-member users receive 401 Unauthorized.
- **Added unit tests**: `MissingGroupExceptionTests` (3 tests) and `UserGroupAuthorizationServiceTests` (9 tests) — total 22 tests passing.

## Next Steps

1. **Validate build**: Run `dotnet build` to verify all projects compile.
2. **Run tests**: Execute `dotnet test` to confirm unit tests pass.
3. **Run console app**: Execute on a domain-joined Windows machine to validate real AD DS connectivity and group authorization.
4. **Run WebApp**: Execute on a domain-joined Windows machine to validate Windows Authentication and group-based authorization.
5. **Generate README**: Only after successful validation.

## Active Decisions and Considerations

- The `ADDomainDiscoveryService` uses `virtual` on the `Discover()` method to allow test overrides without requiring an interface. This is a deliberate design choice to keep the test setup simple.
- The `LdapFilterHelper` is `internal static` since it's only used within the Application layer. It manually escapes LDAP special characters instead of relying on `SearchFilter.Escape` for full control.
- `ILogger<T>` is registered as `Singleton` in the console app's composition root because `LoggerFactory.Create()` produces a singleton logger factory.
- ConsoleApp now uses `appsettings.json` for logging level configuration. The `ConfigurationBuilder` uses `Path.GetDirectoryName(typeof(Program).Assembly.Location)` for robust path resolution regardless of the current working directory. `appsettings.Development.json` is optional and overrides production settings.
- **WebApp launch settings**: `launchSettings.json` defines both HTTP (`http://localhost:5000`) and HTTPS (`https://localhost:5001`) URLs. HTTPS redirect is disabled in Development mode to allow POST requests to reach `OnPost()` handlers.
- **WebApp model binding**: The `SearchAction` hidden input (`name="SearchAction"`) binds to the `SearchAction` property on `IndexModel` to determine which service to call.
- **WebApp CSS**: `wwwroot/css/site.css` uses CSS custom properties, `backdrop-filter: blur()`, and `@keyframes` for the aurora animation. All components use glassmorphism styling.
- **ConsoleApp authorization**: Group name is read from `appsettings.json` via `IOptions<AuthorizationSettings>`. Authorization check happens at startup — if the group doesn't exist or the user is not a member, the application logs an error and exits.
- **WebApp authorization**: Uses ASP.NET Core Windows Authentication (`AddNegotiate()`) + policy-based authorization (`AddPolicy("RequiredGroup")`). `GroupAuthorizationHandler` uses `IServiceScopeFactory` to resolve scoped `IUserGroupAuthorizationService` within a scope — required because `AuthorizationHandler<T>` is registered as Singleton.
- **Authorization settings**: `Authorization.RequiredGroup` in `appsettings.json` is configurable per environment. Both ConsoleApp and WebApp use the same `AuthorizationSettings` class.

## Important Patterns and Preferences

- **Namespace convention**: `TestIA.Domain`, `TestIA.Application`, `TestIA.Logging`, `TestIA.ConsoleApp`, `TestIA.Tests`, `TestIA.WebApp` (no hyphens in namespaces).
- **DTOs as records**: `UserDto` and `GroupDto` are immutable record types with positional parameters.
- **Attribute Mapper Pattern**: `IAttributeMapper<TDto>` generic interface in Application layer maps LDAP `SearchResultEntry` to DTOs. `UserAttributeMapper` and `GroupAttributeMapper` implement it. Mappers are injected via DI, replacing the previous static `_attributeNames` dictionaries. This provides a reusable, testable pattern for future DTOs. Each mapper also implements `GetDisplayValues(TDto)` for dynamic console/web display.
- **AD Discovery Caching**: `ADDomainDiscoveryService.Discover()` caches its result internally after the first successful call. Subsequent calls return the cached tuple `(DomainName, DomainController, BaseDN)` without performing another LDAP discovery. This prevents redundant Active Directory queries when multiple services (authorization, user lookup, group lookup) all require the environment parameters within the same application lifetime.
- **Exception hierarchy**: `DomainException` is the base, with `UserNotFoundException` and `GroupNotFoundException` as specialized children.
- **Service registration**: Done via `ServiceCollectionExtensions.AddTestIAServices()` extension method in the Application project.
- **Testing pattern**: `ThrowingADDomainDiscoveryService` extends `ADDomainDiscoveryService` and overrides `Discover()` to throw controlled exceptions. This avoids the need for mocking the discovery service entirely.
- **WebApp pattern**: Razor Pages with `IndexModel` class in `Pages/Index.cshtml.cs`. Form submission uses POST with hidden `SearchAction` field to trigger the correct service call. Dynamic display via `IAttributeMapper.GetDisplayValues()` — both User and Group results rendered through `@foreach` loops over display dictionaries.
- **Authorization pattern**: `IUserGroupAuthorizationService` interface in Domain layer, `UserGroupAuthorizationService` implementation in Application layer. Both ConsoleApp and WebApp reuse the same service. ConsoleApp fails fast on authorization failure; WebApp uses ASP.NET Core policy-based authorization.

## Known Issues

- None at this time.

## Learnings and Project Insights

- The solution file (`Test-IA.slnx`) has been fixed and now includes all 4 source projects and 1 test project.
- The console app's `Program.cs` manually registers `ILogger<T>` for each service type because `LoggerFactory.Create()` doesn't automatically provide scoped loggers for all types.
- **ASP.NET Core HTTPS redirect**: In Development mode, `UseHttpsRedirection()` must be disabled (or made conditional) to allow HTTP POST requests to reach page handlers. Otherwise, the middleware intercepts POSTs and tries to redirect to HTTPS, which fails when no HTTPS port is configured.
- **Model binding in Razor Pages**: Form field `name` attributes must exactly match the property names on the page model. Mismatched names result in `null` values being bound.
- **Glassmorphism CSS**: `backdrop-filter: blur(20px)` requires `-webkit-backdrop-filter` for Safari compatibility. Semi-transparent backgrounds (`rgba(255,255,255,0.10)`) combined with blur create the frosted glass effect.
- **Member DN resolution**: `ResolveMemberDisplayNamesAsync()` performs an LDAP search for each member DN using `SearchScope.Subtree` to find the `displayName` attribute. Falls back to DN if resolution fails (logged as warning).
- **SearchScope.Subtree vs Base**: `SearchScope.Base` only searches the object at the base DN itself, not the entire directory. `SearchScope.Subtree` searches the entire directory tree, which is required to find member objects by their Distinguished Name.
