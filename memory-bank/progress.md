# Progress: Test-IA

## What Works

- **Domain layer**: All interfaces (`IGetADUserInfo`, `IGetADGroupInfo`, `IUserGroupAuthorizationService`, `IGroupMembershipWriter`), DTOs (`UserDto`, `GroupDto`, `GroupMemberOperationResult`), and domain exceptions (`DomainException`, `UserNotFoundException`, `GroupNotFoundException`, `AccessDeniedException`, `MissingGroupException`) are implemented with XML documentation.
- **Application layer**: 
  - `ADDomainDiscoveryService` — dynamic domain, DC, and Base DN discovery.
  - `IAttributeMapper<TDto>` — generic interface for LDAP-to-DTO mapping (Application layer).
  - `UserAttributeMapper` — implements `IAttributeMapper<UserDto>`, maps 14 LDAP attributes (`displayName`, `employeeID`, `mail`, `userPrincipalName`, `info`, `mobile`, `sAMAccountName`, `streetAddress`, `l`, `st`, `postalCode`, `department`, `title`, `telephoneNumber`).
  - `GroupAttributeMapper` — implements `IAttributeMapper<GroupDto>`, maps `displayName` + `member` DN array.
  - `UserUpdateAttributeMapper` — implements `IAttributeMapper<UserUpdateRequest>`, maps all 10 user update attributes using correct LDAP attribute names (`sAMAccountName`, `info`, `mobile`, `streetAddress`, `l`, `st`, `postalCode`, `department`, `title`, `telephoneNumber`).
  - `GetADUserInfoService` — LDAP user search with safe filter escaping, uses `IAttributeMapper<UserDto>`.
  - `GetADGroupInfoService` — LDAP group search with safe filter escaping, uses `IAttributeMapper<GroupDto>`.
  - `ResolveMemberDisplayNamesAsync` — resolves each group member's Distinguished Name to its `displayName` attribute via LDAP searches.
  - `LdapFilterHelper` — LDAP special character escaping utility.
  - `ServiceCollectionExtensions` — DI registration extension method (includes all mapper registrations).
  - `UserGroupAuthorizationService` — checks if current Windows user is member of configured AD group via LDAP. Throws `MissingGroupException` if group not found.
  - `AuthorizationSettings` — strongly-typed configuration class bound to `Authorization` section in `appsettings.json`.
  - `ILoggerService` interface and `LoggingService` implementation with XML documentation. Added `LogError(Exception, string, params object?[])` overload.
  - `ICurrentUser` interface (Domain layer) � abstraction for identity resolution.
  - `ConsoleCurrentUser` � implements `ICurrentUser` using `WindowsIdentity.GetCurrent()`.
  - `WebCurrentUser` � implements `ICurrentUser` using `HttpContext.User`.
- **ConsoleApp logging**: `appsettings.json` and `appsettings.Development.json` with structured logging configuration via `Microsoft.Extensions.Configuration.Json`. Log levels configurable at default and per-namespace level.
- **ConsoleApp authorization**: Configurable group name via `appsettings.json` (`Authorization.RequiredGroup`). Fails fast with clear error messages — `MissingGroupException` (group not found), `AccessDeniedException` (user not member), or `DomainException` (LDAP error).
- **ConsoleApp**: `Program.cs` with full DI setup, real service execution, structured output via `ILoggerService`.
- **WebApp**: ASP.NET Core Razor Pages application with:
  - `IndexModel` page model with `OnPost()` handling user, group, and user update operations.
  - `launchSettings.json` with HTTP/HTTPS URLs.
  - Conditional `UseHttpsRedirection()` for Development mode.
  - `ILoggerService` injected into `IndexModel` for error logging.
  - **Dynamic display**: `IAttributeMapper<UserDto>`, `IAttributeMapper<GroupDto>`, and `IAttributeMapper<UserUpdateRequest>` injected; `UserDisplayValues`, `GroupDisplayValues`, and `UpdateDisplayValues` properties populated in `OnPost()`; `Index.cshtml` uses `@foreach` loops over display dictionaries for all three operations. User Update card now supports all 10 LDAP attributes with dynamic form field rendering.
  - **Glassmorphism + Aurora UI**: Dark theme, animated aurora background, frosted glass components, gradient text, luminous buttons.
   - **Extensible Logging Service**: `ILoggingSink` interface (pluggable contract), `FileLoggingSink` (thread-safe append mode), `ConsoleLoggingSink`, and `LoggingPipelineFactory` (reads `appsettings.json` `Logging.Sinks` section). `LoggingService` delegates to `IEnumerable<ILoggingSink>` � falls back to `ILogger<T>` only if no sinks registered. `FormatMessage` handles numbered (`{0}`) and named (`{Key}`) placeholders with `HasNumberedPlaceholders` guard against `FormatException` on literal curly braces. ConsoleApp and WebApp both configured with `Logging.Sinks.File` in appsettings.json. Build: 0 errors, 0 warnings. Tests: 25/25 passed.
  - CSS custom properties, `backdrop-filter: blur()`, `@keyframes` animations.
  - **Windows Authentication**: `AddNegotiate()` for Kerberos/NTLM.
  - **Policy-based authorization**: `AddPolicy("RequiredGroup")` with `GroupAuthorizationHandler` using `IServiceScopeFactory`.
  - **Groups.cshtml / Groups.cshtml.cs**: Dedicated Razor Pages for group management. Supports group search, **adding members to groups**, and **removing members from groups** via `IGroupMembershipWriter`. Dynamic display via `IAttributeMapper<GroupDto>`.icy-based authorization**: `AddPolicy("RequiredGroup")` with `GroupAuthorizationHandler` using `IServiceScopeFactory` for scoped service resolution.
  - `[Authorize(Policy = "RequiredGroup")]` applied to Index page.
  - **Dedicated Users.cshtml Razor Page**: Separated Users management into its own page with shared `_Layout.cshtml` using `@RenderBody()`. Update User section always renders all 9 attribute textboxes (Info, Mobile, Street Address, City, State, Postal Code, Department, Title, Phone Number) using `[BindProperty]` model values. samAccountName field positioned as first row in the 2-column `.update-form` grid with `.form-group--full` class for proper visual sizing.
   - **Navbar Identity Display**: ``_Layout.cshtml`` navbar badge displays the current Windows user's identity (`DOMAIN\\Username`) using `WindowsIdentity.GetCurrent()?.Name` � no services, DTOs, or DI registrations required.
- **Tests**: Unit tests for both services and logging project using xUnit, NSubstitute, and FluentAssertions. Total 22 tests passing. Test constructors updated to inject mappers.
- **Project files**: All `.csproj` files correctly configured with proper references and packages.
- **Solution file**: `Test-IA.slnx` has been regenerated and includes all 6 projects (4 source + 1 test + 1 WebApp).

## What's Left to Build

1. ~~**Generate README**: After successful build and test validation, generate a professional README.md following `.cline/rules/solution-readme.md`.~~ ✅ Done
2. ~~**Generate `.gitignore`**: Create a comprehensive `.gitignore` file for the solution root following `.cline/rules/solution-workflow.md` Step 10.~~ ✅ Done
3. ~~**Friendly Access Denied Page**: Create a styled Access Denied page and fix navbar user identity display.~~ ✅ Done

## Current Status

**Phase**: Complete. All validations passed.

The codebase is complete with both ConsoleApp and WebApp presentation layers, Attribute Mapper refactoring, dynamic console display, AD discovery caching, friendly Access Denied page, navbar identity fix, and comprehensive documentation. All 26 tests pass.

## Known Issues

1. No known issues.

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
- **Decision**: Group Membership Writer — `IGroupMembershipWriter` interface in Domain layer with `GroupMembershipWriterService` implementation in Application layer. Uses LDAP `ModifyRequest` to add members to groups. WebApp Groups page includes an "Add Member" form that calls this service via `OnPostAddMember()`. This extends the original read-only group search functionality with write capabilities.
- **Decision**: WebApp Form Routing — Single form with `Action` buttons is the correct approach for Razor Pages when multiple actions exist on the same page. The `handler` convention (`name="handler" value="AddMember"`) does not work reliably with multiple forms. A hidden field with the same `name` as the buttons causes duplicate values and the server takes the first one. The solution is to use only submit buttons with `name="Action"` and no hidden field.
- **Decision**: CS8619 Nullability Fix — `Dictionary<string, string?>?` changed to `Dictionary<string, string>?` for display value properties to match the return type of `IAttributeMapper<T>.GetDisplayValues()`.
- **Decision**: Attribute Mapper refactoring — replaced static `_attributeNames` dictionaries with a generic `IAttributeMapper<TDto>` interface. The interface lives in the Application layer (not Domain) because it depends on `SearchResultEntry` from `System.DirectoryServices.Protocols`. This keeps the Domain layer free of Infrastructure dependencies while providing a reusable, testable mapping pattern for future DTOs.
- **Decision**: AD Discovery Caching — `ADDomainDiscoveryService.Discover()` caches its result internally after the first successful call. This eliminates duplicate LDAP queries when multiple services (authorization, user lookup, group lookup) all require the domain, DC, and Base DN. The cache is instance-level (not static), so each DI-scoped instance caches independently. Tests remain unaffected because `ThrowingADDomainDiscoveryService` overrides `Discover()`.
- **Decision**: Dynamic Console Display — ConsoleApp uses `IAttributeMapper.GetDisplayValues()` to render attributes dynamically via `foreach` loops instead of hardcoded `LogInformation` calls. Adding new attributes only requires updating the mapper, not the console app.
- **Decision**: Dynamic WebApp Display — WebApp `Index.cshtml` uses `@foreach` loops over `Model.UserDisplayValues` and `Model.GroupDisplayValues` instead of hardcoded HTML table rows. Both ConsoleApp and WebApp share the same `GetDisplayValues()` pattern, ensuring consistent dynamic rendering across all presentation layers.
- **Decision**: Dynamic WebApp User Update Display — WebApp User Update card uses `IAttributeMapper<UserUpdateRequest>.GetDisplayValues()` to render form fields dynamically. The `UserUpdateAttributeMapper` handles all 10 user update attributes (SamAccountName, Info, Mobile, StreetAddress, City, State, PostalCode, Department, Title, PhoneNumber). Adding new update attributes only requires updating the mapper, not the Razor page. All presentation layers (ConsoleApp, WebApp User, WebApp Group, WebApp Update) now use the same `GetDisplayValues()` pattern for consistent dynamic rendering.
- **Decision**: WebApp Section Redesign — `Index.cshtml` restructured into two distinct sections (Users and Groups) with visual differentiation. Users section (purple accent) contains User Search + User Update cards. Groups section (green accent) contains Group Search card. Section headers include icons, titles, and descriptions. CSS uses gradient backgrounds, colored borders, and glow effects for visual distinction. Responsive grid layout adapts to different screen sizes.
- **Decision**: UserDto Expansion — `UserDto` expanded from 6 to 14 properties to match the Update User panel attributes. `UserAttributeMapper` now maps all 14 LDAP attributes and `GetDisplayValues()` returns all 14, ensuring the Search User panel displays the same properties as the Update User panel. `UserUpdateAttributeMapper` LDAP attribute names corrected to use proper AD attribute names (`sAMAccountName`, `info`, `mobile`, `streetAddress`, `l`, `st`, `postalCode`, `department`, `title`, `telephoneNumber`) instead of C# property names. Both panels now share the same 14-attribute `GetDisplayValues()` pattern for consistent dynamic rendering.
- **Decision**: Self-Contained Deployment — `Test-IA.WebApp.csproj` includes `<RuntimeIdentifier>win-x64</RuntimeIdentifier>` and `<SelfContained>true</SelfContained>`. `PublishSingleFile` is kept as `false` because it causes >30s timeouts when combined with self-contained. The `scripts/publish-webapp.ps1` script automates the full 6-step pipeline (clean, restore, build, test, publish, zip) producing a ~46.5 MB deployment package with embedded `README-deploy.txt`. Deployment supports direct executable run or NSSM Windows service registration.
- **Fix**: Footer Text Alignment — Fixed footer text alignment to left by adding `.footer-content { padding-left: 3rem; }` in `site.css` and restoring the `.footer-content` wrapper div in `_Layout.cshtml`. Root cause was a CSS syntax error in `.members-list li::before { content: '\''2022'; ... }` which caused the browser to stop parsing all subsequent CSS rules including the footer styles. Fixed the malformed content value to `content: '\2022';`.
- **Fix**: StatusCodePages POST Bug � Replaced ``UseStatusCodePagesWithReExecute`` with a custom ``UseStatusCodePages`` middleware that issues a 302 redirect to ``/AccessDenied`` on 403 responses, fixing the POST-to-GET conversion bug that caused anti-forgery validation failures. Added ``[AllowAnonymous]`` attribute to ``AccessDeniedModel`` to prevent infinite redirect loops. Verified working.

- **Fix**: AccessDenied Navbar Badge (Remote) � Fixed empty navbar badge on AccessDenied page when accessed remotely. Updated StatusCodePages middleware to pass authenticated username via redirect query string (``userName``). Updated ``AccessDeniedModel.UserName`` to prefer query string, falling back to HTTP context. Updated ``_Layout.cshtml`` navbar to use ``ViewData["UserName"]`` with fallback. Verified working (local + remote).

- **Fix**: AccessDenied Page Security Refactoring — Refactored `AccessDenied.cshtml` to be completely independent from `_Layout.cshtml` to prevent information leakage. Added `Layout = null` with full standalone HTML structure. Removed all sensitive data exposure (`UserName`, `RequiredGroup`, `HttpStatusCode`). Simplified `AccessDeniedModel` to minimal implementation. Updated `Program.cs` to stop passing `userName` query string. Build: 0 errors, 0 warnings. Tests: 26/26 passed.

- **Fix**: README.md UTF-8 Replacement Characters — Fixed 11 occurrences of U+FFFD (bytes 0xEF 0xBF 0xBD) replaced with right arrow (U+2192). Affected: Test Categories, Public Services, Deployment, Layer Responsibilities, Configuration sections. Verified 0 remaining.

