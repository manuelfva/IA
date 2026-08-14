# Project Brief: Test-IA

## Overview

**Test-IA** is a .NET 10.0 console application that demonstrates real Active Directory Domain Services (AD DS) access using LDAP. It performs dynamic domain discovery, connects via Windows Integrated Authentication, and retrieves user and group information from the domain.

## Core Requirements

1. **Dynamic AD Discovery**: The application MUST discover the Active Directory domain, Domain Controller, and LDAP Base DN dynamically at runtime. No static configuration of any AD connection parameters is allowed.

2. **Two Core Services**:
   - `GetADUserInfo`: Retrieves user attributes (`displayName`, `employeeID`, `mail`, `userPrincipalName`) by `samAccountName`.
   - `GetADGroupInfo`: Retrieves group attributes (`displayName`, `member`) by `samAccountName`.

3. **Real LDAP Connection**: Uses `System.DirectoryServices.Protocols` for actual LDAP operations against the on-premises AD DS directory. No Microsoft Graph, Entra ID, or static test data.

4. **Windows Integrated Authentication**: Uses the current Windows security context (Kerberos/Negotiate). No credentials are stored, transmitted, or prompted.

5. **Clean Architecture**: Strict separation between Domain (interfaces), Application (implementations), Logging (abstraction), and ConsoleApp (composition root).

6. **Logging Abstraction**: All output goes through `ILoggerService` wrapping `Microsoft.Extensions.Logging.ILogger`. Direct console output is strictly forbidden.

7. **Unit Testability**: Tests must not require a live AD DS environment. The `ADDomainDiscoveryService` is designed as a virtual method to allow test overrides.

## Sample Data

- User `samAccountName`: `MFVA649T`
- Group `samAccountName`: `employees of MADRID`

## Project Structure

```
src/
  Test-IA.Domain/        # Interfaces (IGetADUserInfo, IGetADGroupInfo), DTOs (UserDto, GroupDto), DomainException
  Test-IA.Application/   # Service implementations, ADDomainDiscoveryService, LdapFilterHelper
  Test-IA.Logging/       # ILoggerService interface and LoggingService implementation
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
