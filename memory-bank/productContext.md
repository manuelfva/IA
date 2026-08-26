# Product Context: Test-IA

## Why This Project Exists

This project exists to demonstrate a production-ready approach to accessing on-premises Active Directory Domain Services (AD DS) from a .NET application. It serves as:

1. **A reference implementation** for how to perform dynamic AD discovery and LDAP operations without hard-coded connection parameters.
2. **A learning tool** showing Clean Architecture applied to Active Directory access.
3. **A validation exercise** proving that a .NET application can connect to AD DS using Windows Integrated Authentication with zero static configuration.

## Problems It Solves

- **Hard-coded LDAP connections**: Traditional approaches require hard-coding Domain Controller hostnames, Base DNs, and LDAP URLs. This project eliminates that entirely through dynamic discovery.
- **Credential management**: Eliminates the need to store or manage LDAP bind credentials by using the current Windows security context.
- **Tight coupling**: Separates LDAP access logic from business logic through Clean Architecture, making the system testable and maintainable.
- **LDAP injection**: Provides a safe `LdapFilterHelper` for escaping user input before use in LDAP filters.
- **AD group management**: Allows administrators to add/remove users from AD groups programmatically.
- **User attribute updates**: Supports updating AD user attributes (info, mobile, street address, city, state, postal code, department, title, phone) via LDAP modify operations.

## How It Should Work

### User Experience Flow

#### Console Application

1. The user runs the console application on a domain-joined Windows machine.
2. The application silently discovers the Active Directory environment using Windows APIs.
3. It connects to a Domain Controller via LDAP using Windows Integrated Authentication.
4. It performs authorization check against the configured AD group (`Authorization.RequiredGroup` from `appsettings.json`).
5. If authorized, it performs two searches:
   - Looks up a user by `samAccountName` and displays their attributes.
   - Looks up a group by `samAccountName` and displays their members.
6. All output is structured through the logging abstraction (Console provider in this case).
7. If the machine is not domain-joined or discovery fails, the application fails with a clear error message.

#### Web Application

1. The user opens the WebApp in a browser (HTTP or HTTPS).
2. Windows Authentication (Kerberos/NTLM) authenticates the user automatically.
3. The page displays three sections: Users (search + update), Groups (search + membership management), and an Access Denied page for unauthorized users.
4. User search: Enter `samAccountName` → displays all 14 LDAP attributes dynamically.
5. User update: Enter `samAccountName` + modify any of 10 attributes → updates the user in AD.
6. Group search: Enter `samAccountName` → displays group info + resolved member display names.
7. Add member: Enter group + member `samAccountName` → adds user to group via LDAP modify.
8. Remove member: Enter group + member `samAccountName` → removes user from group via LDAP modify.
9. All results are rendered back into the page and displayed in glass-morphism styled panels.
10. Errors are displayed in a glass-morphism alert panel with structured logging.

### Expected Behavior

- **Success**: User and group information is retrieved and displayed with structured logging.
- **User not found**: A `UserNotFoundException` is caught and logged as an error.
- **Group not found**: A `GroupNotFoundException` is caught and logged as an error.
- **Not domain-joined**: A `DomainException` is thrown with a clear message and logged.
- **No Domain Controller**: A `DomainException` is thrown and logged.
- **LDAP connection failure**: A `DomainException` wrapping the underlying `LdapException` is thrown and logged.
- **Not a member of required group**: User receives 401 Unauthorized (WebApp) or application exits with error (ConsoleApp).
- **Group does not exist in AD**: `MissingGroupException` is thrown with descriptive message.

## User Experience Goals

- **Zero configuration**: The user should not need to provide any Active Directory connection parameters.
- **Clear error messages**: Every failure mode should produce a descriptive error that helps diagnose the problem.
- **No credential prompts**: The application should work silently without asking for credentials.
- **Structured output**: All results should be displayed in a readable, consistent format.
- **Configurable**: All values (required group, demo user/group) are configurable via `appsettings.json`.
- **Extensible logging**: Pluggable logging sinks (file, console, and future database/EventLog sinks).
- **Secure**: No sensitive data exposed to unauthorized users (AccessDenied page is standalone, generic message).
