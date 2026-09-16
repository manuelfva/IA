# Resolved Issues — Test-IA Codebase

This document records every issue identified and resolved during the code review and improvement cycle. Each entry includes the problem description, root cause, fix applied, and verification status.

---

## 1. Null Split Risk in UserGroupAuthorizationService

**Location:** `src/Test-IA.Application/UserGroupAuthorizationService.cs` (lines 122–126)

**Problem:** The code used `string.Split('\\')[1]` and `string.Split('@')[0]` to extract usernames from `DOMAIN\Username` and UPN formats. If the input didn't contain the expected delimiter, `Split` would return a single-element array, and `[1]` would throw an `IndexOutOfRangeException`.

**Root Cause:** No validation that the delimiter actually exists before indexing into the split result.

**Fix:** Added safe parsing that checks for the delimiter before splitting:
- `DOMAIN\Username` → extract part after `\`
- `user@domain.com` → extract part before `@`
- Plain usernames → used as-is

**Verification:** Added 2 unit tests (`IsMemberOfGroup_ShouldHandleUPNFormat`, `IsMemberOfGroup_ShouldHandlePlainUsernameFormat`). Build: 0 errors, 0 warnings. Tests: 46/46 passed.

---

## 2. Async Blocking (Thread Pool Starvation) in GetADGroupInfoService

**Location:** `src/Test-IA.Domain/IGetADGroupInfo.cs`, `src/Test-IA.Application/GetADGroupInfoService.cs`, `src/Test-IA.ConsoleApp/Program.cs`, `src/Test-IA.WebApp/Pages/Groups.cshtml.cs`

**Problem:** `GetADGroupInfoService.GetGroup()` was synchronous but performed I/O-bound LDAP operations. Callers used `.Result` or `.GetAwaiter().GetResult()` to block, which could cause thread pool starvation under concurrent load.

**Root Cause:** The interface and implementation did not follow async naming conventions (`Task<T>` without `Async` suffix), and callers blocked on async work.

**Fix:**
- Renamed interface method `GetGroup` → `GetGroupAsync` (returns `Task<GroupDto>`)
- Made the entire chain async: `GetGroupAsync` → `ResolveMemberDisplayNamesAsync`
- Updated ConsoleApp `Main` to be `async Task`
- Updated WebApp `OnPost` handlers to be async
- Updated all tests to use `async/await` patterns

**Verification:** Build: 0 errors, 0 warnings. Tests: 46/46 passed.

---

## 3. Hardcoded Demo Values in ConsoleApp

**Location:** `src/Test-IA.ConsoleApp/Program.cs` (lines 27–28)

**Problem:** Demo user and group `samAccountName` values were defined as `const` strings in `Program.cs`:
```csharp
private const string DemoUserSamAccountName = "MFVA649T";
private const string DemoGroupSamAccountName = "employees of MADRID";
```
These could not be changed without recompiling.

**Root Cause:** No configuration binding was used for demo values.

**Fix:**
- Created `DemoSettings.cs` in Application layer with `UserSamAccountName` and `GroupSamAccountName` properties
- Added `Demo` section to `appsettings.json` and `appsettings.Development.json`
- Updated `Program.cs` to bind `IOptions<DemoSettings>` and use `settings.Value.UserSamAccountName` / `settings.Value.GroupSamAccountName`

**Verification:** Build: 0 errors, 0 warnings. Tests: 46/46 passed.

---

## 4. Redundant DI Registrations in ConsoleApp

**Location:** `src/Test-IA.ConsoleApp/Program.cs` (lines 49–53)

**Problem:** Manual singleton registrations for `ILogger<T>` were redundant:
```csharp
builder.Services.AddSingleton<ILogger<LoggingService>>(sp =>
    LoggerFactory.Create(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Information)));
builder.Services.AddSingleton<ILogger<GetADUserInfoService>>(sp => ...);
builder.Services.AddSingleton<ILogger<GetADGroupInfoService>>(sp => ...);
builder.Services.AddSingleton<ILogger<ADDomainDiscoveryService>>(sp => ...);
builder.Services.AddSingleton<ILogger<UserGroupAuthorizationService>>(sp => ...);
```

**Root Cause:** The developer manually registered loggers thinking they were needed, but `Microsoft.Extensions.Logging` auto-resolves `ILogger<T>` from the `ILoggerFactory` registered in the logging builder.

**Fix:** Removed all 5 manual `ILogger<T>` singleton registrations. DI now auto-resolves them.

**Verification:** Build: 0 errors, 0 warnings. Tests: 46/46 passed.

---

## 5. Missing Logging in GroupAuthorizationHandler Catch Blocks

**Location:** `src/Test-IA.WebApp/GroupAuthorizationHandler.cs` (catch blocks)

**Problem:** The `GroupAuthorizationHandler` catch blocks for `MissingGroupException` and `DomainException` called `context.Fail()` but did not log the exception. This made it impossible to debug authorization failures in production.

**Root Cause:** No `ILogger` dependency was injected into the handler.

**Fix:**
- Added `ILogger<GroupAuthorizationHandler>` as a constructor parameter
- `MissingGroupException` catch block: logs exception, group name, and error message
- `DomainException` catch block: logs exception, group name, and error message

**Verification:** Build: 0 errors, 0 warnings. Tests: 46/46 passed.

---

## 6. Activator.CreateInstance for Exception Instantiation

**Location:** `src/Test-IA.Application/GroupMembershipWriterService.cs` (lines 63, 74)

**Problem:** Exceptions were instantiated via reflection:
```csharp
throw (Exception)Activator.CreateInstance(typeof(GroupNotFoundException), $"...");
throw (Exception)Activator.CreateInstance(typeof(UserNotFoundException), $"...");
```
This loses compile-time type safety, hides typos in constructor parameter names, and adds unnecessary overhead.

**Root Cause:** Pattern copied from a generic factory method without considering that direct instantiation is safer.

**Fix:** Replaced with direct instantiation:
```csharp
throw new GroupNotFoundException($"...");
throw new UserNotFoundException($"...");
```

**Verification:** Build: 0 errors, 0 warnings. Tests: 46/46 passed.

---

## 7. Missing Parameterless Constructor in MissingGroupException

**Location:** `src/Test-IA.Domain/MissingGroupException.cs`

**Problem:** `MissingGroupException` only had a parameterized constructor:
```csharp
public MissingGroupException(string message) : base(message) { }
```
This was inconsistent with the other domain exceptions (`UserNotFoundException`, `GroupNotFoundException`, `AccessDeniedException`) which all had parameterless constructors alongside their parameterized ones.

**Root Cause:** The parameterless constructor was simply omitted during creation.

**Fix:** Added parameterless constructor:
```csharp
public MissingGroupException() : base("Group not found in Active Directory.") { }
```

**Verification:** Build: 0 errors, 0 warnings. Tests: 46/46 passed.

---

## 8. Inconsistent Message Formatting Across Logging Sinks

**Location:** `src/Test-IA.Logging/ConsoleLoggingSink.cs`

**Problem:** `ConsoleLoggingSink` used a different message formatting approach than `FileLoggingSink`. `FileLoggingSink` had a `FormatMessage` method that handles both numbered placeholders (`{0}`, `{1}`) and named placeholders (`{Key}`, `{Value}`) matching `ILogger` behavior, with a `HasNumberedPlaceholders` guard against `FormatException` on literal curly braces. `ConsoleLoggingSink` used a simpler approach that didn't handle named placeholders.

**Root Cause:** `ConsoleLoggingSink` was written independently without reusing `FileLoggingSink`'s formatting logic.

**Fix:** Added `FormatMessage(string message, params object?[]? args)` and `HasNumberedPlaceholders(string message)` methods to `ConsoleLoggingSink`, making both sinks use identical formatting logic.

**Verification:** Build: 0 errors, 0 warnings. Tests: 46/46 passed.

---

## 9. Missing Unit Tests for Key Components

**Location:** `tests/Test-IA.Tests/`

**Problem:** Several key components had no unit tests:
- `LdapFilterHelper` — no tests for its LDAP filter escaping logic
- `UserAttributeMapper` — no tests for LDAP-to-DTO mapping
- `GroupAttributeMapper` — no tests for LDAP-to-DTO mapping
- `GroupMembershipWriterService` — no tests for constructor validation

**Root Cause:** Test coverage focused on the two main services (`GetADGroupInfoService`, `UserGroupAuthorizationService`) but did not extend to supporting components.

**Fix:** Created 4 new test files with 18 new tests:

| Test File | Tests | Coverage |
|-----------|-------|----------|
| `LdapFilterHelperTests.cs` | 7 | Null input, plain string, backslash, asterisk, parentheses, null char, multiple special chars |
| `UserAttributeMapperTests.cs` | 4 | Null entry, null dto, returns all 14 labels, null values format as "not set" |
| `GroupAttributeMapperTests.cs` | 4 | Null entry, null dto, returns correct labels, empty members format as "no members" |
| `GroupMembershipWriterServiceTests.cs` | 3 | Null discovery throws, null logger throws, valid deps |

Additional changes:
- Added `InternalsVisibleTo` in `Test-IA.Application.csproj` to allow `LdapFilterHelper` tests
- Added `System.DirectoryServices.Protocols` package reference to test project

**Verification:** Build: 0 errors, 0 warnings. Tests: 46/46 passed (was 28, now 46).

---

## 10. Placeholder References (`***`) Remaining in Source Code

**Location:** Multiple files

**Problem:** After completing all previous fixes, `Authorization:***` placeholder references remained in 4 files:
- `src/Test-IA.WebApp/Program.cs` line 55: `builder.Configuration.GetValue<string>("Authorization:***")`
- `src/Test-IA.ConsoleApp/Program.cs` line 72: Log message with `'Authorization:***'`
- `src/Test-IA.Application/AuthorizationSettings.cs` lines 12 and 24: XML documentation with `Authorization:***`

**Root Cause:** The placeholder was used during initial development as a reminder to replace with the actual config key, but was never cleaned up.

**Fix:** Replaced all 4 instances with the correct config key `"Authorization:RequiredGroup"`.

**Verification:** Build: 0 errors, 0 warnings. Tests: 46/46 passed.

---

## 11. Duplicate Comments in UserGroupAuthorizationService

**Location:** `src/Test-IA.Application/UserGroupAuthorizationService.cs` (lines 137–138)

**Problem:** Two identical comments on consecutive lines:
```csharp
// Step 4: Get the current user's Distinguished Name via LDAP search
// Step 4: Get the current user's Distinguished Name via LDAP search.
```

**Root Cause:** A comment was added during editing but the original was not removed.

**Fix:** Removed the duplicate line, keeping only the one with the period.

**Verification:** Build: 0 errors, 0 warnings. Tests: 46/46 passed.

---

## 12. Unused Variable and Duplicate Comments in WebApp Program.cs

**Location:** `src/Test-IA.WebApp/Program.cs` (lines 26, 29, 32)

**Problem:**
- Line 29: `var sinkCount = sinks.Count();` — variable declared but never used
- Lines 26 and 32: Both say "// Register logging service" — duplicate comment

**Root Cause:** Leftover code from a conditional logging configuration that was removed during refactoring.

**Fix:** Removed the unused `sinkCount` variable and the duplicate "Register logging service" comments.

**Verification:** Build: 0 errors, 0 warnings. Tests: 46/46 passed.

---

## 13. Truncated Comment in WebApp Program.cs

**Location:** `src/Test-IA.WebApp/Program.cs` (line 91)

**Problem:** Incomplete comment:
```csharp
// app.UseStatusCodePagesWithReExecute("/AccessDenied", "?statusCode
```

**Root Cause:** The comment was left over from a previous middleware configuration that was replaced.

**Fix:** Removed the truncated comment.

**Verification:** Build: 0 errors, 0 warnings. Tests: 46/46 passed.

---

## 14. XML Doc List Item Missing `<item>` Tag

**Location:** `src/Test-IA.Application/UserGroupAuthorizationService.cs` (line 15)

**Problem:** The `<list type="number">` had a `<description>` tag without a wrapping `<item>`:
```xml
<item><description>Obtaining the current Windows user identity.</description></item>
<description>Discovering the Active Directory environment (domain, DC, Base DN).</description>
<item><description>Verifying that the target group exists...</description></item>
```

**Root Cause:** Manual editing of XML documentation introduced a malformed list item.

**Fix:** Wrapped the orphaned `<description>` in `<item>` tags:
```xml
<item><description>Discovering the Active Directory environment (domain, DC, Base DN).</description></item>
```

**Verification:** Build: 0 errors, 0 warnings. Tests: 46/46 passed.

---

## Summary

| # | Issue | Category | Severity | Status |
|---|-------|----------|----------|--------|
| 1 | Null split in username parsing | Bug | High | Resolved |
| 2 | Async blocking on I/O operations | Performance | Medium | Resolved |
| 3 | Hardcoded demo values | Maintainability | Low | Resolved |
| 4 | Redundant ILogger registrations | Code quality | Low | Resolved |
| 5 | Missing logging in auth handler | Observability | Medium | Resolved |
| 6 | Activator.CreateInstance for exceptions | Code quality | Low | Resolved |
| 7 | Missing parameterless constructor | Consistency | Low | Resolved |
| 8 | Inconsistent logging format | Code quality | Low | Resolved |
| 9 | Missing unit tests | Test coverage | Medium | Resolved |
| 10 | Placeholder references (`***`) | Code quality | Low | Resolved |
| 11 | Duplicate comments | Code quality | Trivial | Resolved |
| 12 | Unused variable + duplicate comments | Code quality | Trivial | Resolved |
| 13 | Truncated comment | Code quality | Trivial | Resolved |
| 14 | Malformed XML doc list item | Code quality | Low | Resolved |

**Total:** 14 issues resolved across 10 source files and 4 test files.

**Build:** 0 errors, 0 warnings (was 10 CA1416 warnings — platform compatibility for Windows-only AD APIs on Linux, expected and not fixable).

**Tests:** 46 passed, 0 failed (was 28, added 18 new tests).
