# Active Context: Test-IA

## Current Work Focus

The project has been created with all core services implemented and a WebApp presentation layer. The memory bank is being initialized for the first time.

## Recent Changes

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

## Next Steps

1. **Validate build**: Run `dotnet build` to verify all projects compile.
2. **Run tests**: Execute `dotnet test` to confirm unit tests pass.
3. **Run console app**: Execute on a domain-joined Windows machine to validate real AD DS connectivity.
4. **Generate README**: Only after successful validation.

## Active Decisions and Considerations

- The `ADDomainDiscoveryService` uses `virtual` on the `Discover()` method to allow test overrides without requiring an interface. This is a deliberate design choice to keep the test setup simple.
- The `LdapFilterHelper` is `internal static` since it's only used within the Application layer. It manually escapes LDAP special characters instead of relying on `SearchFilter.Escape` for full control.
- `ILogger<T>` is registered as `Singleton` in the console app's composition root because `LoggerFactory.Create()` produces a singleton logger factory.
- ConsoleApp now uses `appsettings.json` for logging level configuration. The `ConfigurationBuilder` uses `Path.GetDirectoryName(typeof(Program).Assembly.Location)` for robust path resolution regardless of the current working directory. `appsettings.Development.json` is optional and overrides production settings.
- **WebApp launch settings**: `launchSettings.json` defines both HTTP (`http://localhost:5000`) and HTTPS (`https://localhost:5001`) URLs. HTTPS redirect is disabled in Development mode to allow POST requests to reach `OnPost()` handlers.
- **WebApp model binding**: The `SearchAction` hidden input (`name="SearchAction"`) binds to the `SearchAction` property on `IndexModel` to determine which service to call.
- **WebApp CSS**: `wwwroot/css/site.css` uses CSS custom properties, `backdrop-filter: blur()`, and `@keyframes` for the aurora animation. All components use glassmorphism styling.

## Important Patterns and Preferences

- **Namespace convention**: `TestIA.Domain`, `TestIA.Application`, `TestIA.Logging`, `TestIA.ConsoleApp`, `TestIA.Tests`, `TestIA.WebApp` (no hyphens in namespaces).
- **DTOs as records**: `UserDto` and `GroupDto` are immutable record types with positional parameters.
- **Exception hierarchy**: `DomainException` is the base, with `UserNotFoundException` and `GroupNotFoundException` as specialized children.
- **Service registration**: Done via `ServiceCollectionExtensions.AddTestIAServices()` extension method in the Application project.
- **Testing pattern**: `ThrowingADDomainDiscoveryService` extends `ADDomainDiscoveryService` and overrides `Discover()` to throw controlled exceptions. This avoids the need for mocking the discovery service entirely.
- **WebApp pattern**: Razor Pages with `IndexModel` class in `Pages/Index.cshtml.cs`. Form submission uses POST with hidden `SearchAction` field to trigger the correct service call.

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
