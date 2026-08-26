# Project Brief: Test-IA

## Overview

**Test-IA** is a .NET 10.0 application with ConsoleApp and WebApp presentation layers that demonstrates real Active Directory Domain Services (AD DS) access using LDAP. It performs dynamic domain discovery, connects via Windows Integrated Authentication, and retrieves and manages user and group information from the domain.

## Core Requirements

1. **Dynamic AD Discovery**: The application MUST discover the Active Directory domain, Domain Controller, and LDAP Base DN dynamically at runtime. No static configuration of any AD connection parameters is allowed.

2. **Core Services**:
   - `GetADUserInfo`: Retrieves user attributes (`displayName`, `employeeID`, `mail`, `userPrincipalName`, etc.) by `samAccountName`.
   - `GetADGroupInfo`: Retrieves group attributes (`displayName`, `member`) by `samAccountName`.
   - `IGroupMembershipWriter`: Add/remove members from AD groups via LDAP modify.
   - `IUserWriter`: Update AD user attributes via LDAP modify.
   - `IUserGroupAuthorizationService`: Check if current user is member of configured AD group.

3. **Real LDAP Connection**: Uses `System.DirectoryServices.Protocols` for actual LDAP operations against the on-premises AD DS directory. No Microsoft Graph, Entra ID, or static test data.

4. **Windows Integrated Authentication**: Uses the current Windows security context (Kerberos/Negotiate). No credentials are stored, transmitted, or prompted.

5. **Clean Architecture**: Strict separation between Domain (interfaces, DTOs, exceptions), Application (implementations, mappers, helpers), Logging (abstraction), and Presentation layers (ConsoleApp, WebApp).

6. **Logging Abstraction**: All output goes through `ILoggerService` wrapping `Microsoft.Extensions.Logging.ILogger`. Direct console output is strictly forbidden. Extensible sink pattern with `ILoggingSink` interface for pluggable output targets.

7. **Unit Testability**: Tests must not require a live AD DS environment. The `ADDomainDiscoveryService` is designed as a virtual method to allow test overrides.

## Sample Data

- User `samAccountName`: `MFVA649T` (configurable via `Demo:UserSamAccountName` in appsettings.json)
- Group `samAccountName`: `employees of MADRID` (configurable via `Demo:GroupSamAccountName` in appsettings.json)

## Project Structure

```
src/
  Test-IA.Domain/        # Interfaces, DTOs (record types), Domain Exceptions
  Test-IA.Application/   # Service implementations, mappers, helpers, DI extensions
  Test-IA.Logging/       # ILoggerService, ILoggingSink, ConsoleLoggingSink, FileLoggingSink
  Test-IA.ConsoleApp/    # Composition root (Program.cs), demonstrates real service execution
  Test-IA.WebApp/        # ASP.NET Core Razor Pages web application (Glassmorphism + Aurora UI)
tests/
  Test-IA.Tests/         # xUnit tests with NSubstitute and FluentAssertions
```

## Key Constraints

- Target framework: `.NET 10.0` (`net10.0`)
- No hard-coded LDAP parameters (domain, DC, Base DN, credentials)
- No fallback to static configuration if discovery fails
- All public members MUST have XML documentation
- Dependency Injection via `Microsoft.Extensions.DependencyInjection`
- Solution format: `.slnx`
- All demo values are configurable via `appsettings.json` (no hardcoded values)
