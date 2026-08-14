# Technical Context: Test-IA

## Technologies Used

| Category | Technology |
|----------|-----------|
| Language | C# |
| Framework | .NET 10.0 (`net10.0`) |
| Architecture | Clean Architecture |
| DI Container | Microsoft.Extensions.DependencyInjection |
| Logging | Microsoft.Extensions.Logging.Abstractions + Console |
| AD Access | System.DirectoryServices + System.DirectoryServices.Protocols |
| Web Framework | ASP.NET Core Razor Pages |
| Web UI | Glassmorphism + Aurora (CSS custom properties, backdrop-filter, @keyframes) |
| Unit Testing | xUnit 2.9.3 |
| Mocking | NSubstitute 6.2.0 |
| Assertions | FluentAssertions 8.10.0 |
| Test Runner | Microsoft.NET.Test.Sdk 17.14.1 |
| Test Coverage | coverlet.collector 6.0.4 |
| Test Coverage Reporter | xunit.runner.visualstudio 3.1.4 |
| Solution Format | `.slnx` |

## Development Setup

### Prerequisites

- Windows machine joined to an Active Directory domain (for runtime).
- .NET 10.0 SDK installed.
- Visual Studio Code or any text editor with C# support.

### Commands

```powershell
# Restore packages
dotnet restore

# Build all projects
dotnet build

# Run unit tests
dotnet test

# Run console application (requires domain-joined machine)
dotnet run --project src/Test-IA.ConsoleApp/Test-IA.ConsoleApp.csproj
```

## Project Dependencies

| Project | Depends On |
|---------|-----------|
| Test-IA.Domain | None |
| Test-IA.Application | Test-IA.Domain, Microsoft.Extensions.DependencyInjection.Abstractions, Microsoft.Extensions.Logging.Abstractions, System.DirectoryServices, System.DirectoryServices.Protocols |
| Test-IA.Logging | Microsoft.Extensions.Logging.Abstractions |
| Test-IA.ConsoleApp | Test-IA.Application, Test-IA.Logging, Microsoft.Extensions.DependencyInjection, Microsoft.Extensions.Logging.Console |
| Test-IA.WebApp | Test-IA.Application, Test-IA.Logging, Microsoft.Extensions.DependencyInjection, Microsoft.Extensions.Hosting |
| Test-IA.Tests | Test-IA.Domain, Test-IA.Application, Test-IA.Logging, xUnit, NSubstitute, FluentAssertions |

## NuGet Packages (Version 10.0.11 unless noted)

- `Microsoft.Extensions.DependencyInjection.Abstractions` 10.0.11
- `Microsoft.Extensions.Logging.Abstractions` 10.0.11
- `Microsoft.Extensions.DependencyInjection` 10.0.11
- `Microsoft.Extensions.Logging.Console` 10.0.11
- `System.DirectoryServices` 10.0.11
- `System.DirectoryServices.Protocols` 10.0.11
- `xunit` 2.9.3
- `xunit.runner.visualstudio` 3.1.4
- `Microsoft.NET.Test.Sdk` 17.14.1
- `NSubstitute` 6.2.0
- `FluentAssertions` 8.10.0
- `coverlet.collector` 6.0.4

## Technical Constraints

- **Target Framework**: `net10.0` (no multi-targeting).
- **Nullable Reference Types**: Enabled in all projects.
- **Implicit Usings**: Enabled in all projects.
- **File-scoped namespaces**: Used throughout.
- **No `Console.WriteLine`**: All output via `ILoggerService`.
- **No static AD configuration**: Domain, DC, Base DN, credentials must never be hard-coded.
- **No fallback on discovery failure**: Must fail clearly with descriptive error.
- **XML Documentation**: All public members must have `///` comments.

## LDAP-Specific Technical Details

### Discovery APIs

- `System.DirectoryServices.ActiveDirectory.Domain.GetCurrentDomain()` — determines the current domain.
- `System.DirectoryServices.DirectoryEntry("LDAP://RootDSE")` — obtains DC DNS host name and naming context.
- `System.DirectoryServices.Protocols.LdapConnection` — creates LDAP connections with `AuthType.Negotiate`.
- `System.DirectoryServices.Protocols.SearchRequest` — performs LDAP searches.

### Authentication

- `AuthType.Negotiate` — uses Windows Integrated Authentication (Kerberos preferred, NTLM fallback).
- No bind credentials — uses the current Windows user's security context.

### Search Filters

- User: `(&(objectCategory=person)(objectClass=user)(sAMAccountName={escaped}))`
- Group: `(&(objectCategory=group)(sAMAccountName={escaped}))`
- Member DN resolution: `(distinguishedName={escapedDN})` with `SearchScope.Subtree` to find `displayName` attribute

### Resource Disposal

- `LdapConnection` is disposed via `using` declarations.
- `DirectoryEntry` is disposed via `using` declarations.

## Known Issues

- None at this time.

## External Dependencies

- The application requires a **Windows machine joined to an Active Directory domain** to function. It will not work on a workgroup machine, Linux, or macOS.
- The application requires network access to at least one Domain Controller in the domain.
