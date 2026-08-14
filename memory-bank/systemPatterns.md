# System Patterns: Test-IA

## Architecture

**Clean Architecture** with five layers:

```
┌─────────────────────────────────────────────────────────────┐
│               Test-IA.WebApp (Presentation)                 │
│  (Razor Pages, Glassmorphism + Aurora UI)                   │
├─────────────────────────────────────────────────────────────┤
│               Test-IA.ConsoleApp                            │
│  (Composition Root, DI registration, real execution)        │
├─────────────────────────────────────────────────────────────┤
│               Test-IA.Application                           │
│  (Service implementations, AD discovery, LDAP operations)   │
├─────────────────────────────────────────────────────────────┤
│               Test-IA.Domain                                │
│  (Interfaces, DTOs, Domain Exceptions)                      │
├─────────────────────────────────────────────────────────────┤
│               Test-IA.Logging                               │
│  (ILoggerService abstraction over Microsoft.Extensions.)    │
└─────────────────────────────────────────────────────────────┘
```

### Dependency Flow

```
WebApp → Application → Domain
ConsoleApp → Application → Domain
WebApp → Logging
ConsoleApp → Logging
Tests → Application, Domain, Logging
```

Domain has NO external dependencies. Application depends only on Domain (plus Microsoft.Extensions and System.DirectoryServices packages). Logging is standalone. WebApp and ConsoleApp are presentation layers that depend on Application and Logging.

## Key Technical Decisions

### 1. Dynamic Active Directory Discovery

The `ADDomainDiscoveryService` performs a three-step discovery process:

1. **Domain**: Uses `System.DirectoryServices.ActiveDirectory.Domain.GetCurrentDomain()` to determine the domain the machine is joined to.
2. **Domain Controller**: Uses `System.DirectoryServices.DirectoryEntry("LDAP://RootDSE")` to obtain the `dnsHostName` of the Domain Controller.
3. **Base DN**: Opens an LDAP connection to the discovered DC and queries RootDSE for the `defaultNamingContext` attribute.

This ensures zero static configuration of AD connection parameters.

### 2. Windows Integrated Authentication

All LDAP connections use `AuthType.Negotiate`, which allows Windows to negotiate the appropriate authentication protocol (Kerberos when available, NTLM as fallback). No credentials are stored or transmitted by the application.

### 3. LDAP Filter Escaping

The `LdapFilterHelper` class manually escapes special LDAP characters (`\`, `*`, `(`, `)`, `\0`) to prevent LDAP injection. It is used before constructing search filters from user input.

### 4. Service Abstraction

Service interfaces are defined in the Domain layer:
- `IGetADUserInfo` → `GetADUserInfoService`
- `IGetADGroupInfo` → `GetADGroupInfoService`

This allows the services to be replaced or mocked in tests.

### 5. Logging Abstraction

The `ILoggerService` interface wraps `Microsoft.Extensions.Logging.ILogger` to provide a consistent logging abstraction. This decouples the console app from the specific logging framework and allows for alternative logging implementations.

## Component Relationships

```
                    ┌──────────────────┐
                    │  Program.cs      │
                    │  (ConsoleApp)    │
                    └────────┬─────────┘
                             │ uses
              ┌──────────────┼──────────────┐
              │              │              │
              ▼              ▼              ▼
     ┌──────────────┐ ┌──────────────┐ ┌──────────────┐
     │ ServiceColl. │ │  Logging     │ │  ILogger     │
     │ Extensions   │ │  Service     │ │  (interface) │
     └──────┬───────┘ └──────┬───────┘ └──────────────┘
            │                │
            │ uses           │ uses
            ▼                ▼
     ┌──────────────┐ ┌──────────────┐
     │  ADDomain    │ │  Logging     │
     │  Discovery   │ │  Service     │
     └──────┬───────┘ └──────────────┘
            │
            │ uses
            ▼
     ┌──────────────┐     ┌──────────────┐
     │ GetADUser    │     │ GetADGroup   │
     │  Info Svc    │     │  Info Svc    │
     └──────┬───────┘     └──────┬───────┘
            │                     │
            │ depends on          │ depends on
            ▼                     ▼
     ┌──────────────────────────────────────┐
     │          Test-IA.Domain              │
     │  IGetADUserInfo, IGetADGroupInfo     │
     │  UserDto, GroupDto                   │
     │  DomainException, UserNotFoundEx     │
     │  GroupNotFoundEx                     │
     

```

## Critical Implementation Paths

### User Lookup Flow

1. `Program.Main` → resolves `IGetADUserInfo` from DI
2. `GetADUserInfoService.GetUser(samAccountName)` validates input
3. Calls `ADDomainDiscoveryService.Discover()` → gets domain, DC, Base DN
4. Creates `LdapConnection` to DC with `AuthType.Negotiate`
5. Escapes `samAccountName` via `LdapFilterHelper.Escape()`
6. Executes LDAP search: `(&(objectCategory=person)(objectClass=user)(sAMAccountName={escaped}))`
7. Extracts `displayName`, `employeeID`, `mail`, `userPrincipalName`
8. Returns `UserDto`

### Group Lookup Flow

1. `Program.Main` → resolves `IGetADGroupInfo` from DI
2. `GetADGroupInfoService.GetGroup(samAccountName)` validates input
3. Calls `ADDomainDiscoveryService.Discover()` → gets domain, DC, Base DN
4. Creates `LdapConnection` to DC with `AuthType.Negotiate`
5. Escapes `samAccountName` via `LdapFilterHelper.Escape()`
6. Executes LDAP search: `(&(objectCategory=group)(sAMAccountName={escaped}))`
7. Extracts `displayName`, `member` (as string array)
8. Returns `GroupDto`

## Design Patterns in Use

- **Dependency Injection**: All services registered via `IServiceCollection`. Composition root in `Program.cs`.
- **Service Collection Extensions**: `ServiceCollectionExtensions.AddTestIAServices()` provides a single registration method.
- **Record Types**: `UserDto` and `GroupDto` use C# record types for immutable data transfer.
- **Domain Exception Pattern**: Custom exceptions (`UserNotFoundException`, `GroupNotFoundException`) inheriting from `DomainException` for domain-specific error handling.
- **Facade Pattern**: `ADDomainDiscoveryService` encapsulates the complexity of domain, DC, and Base DN discovery behind a single `Discover()` method.
- **Helper/Utility Pattern**: `LdapFilterHelper` is a static utility class for LDAP filter escaping.
