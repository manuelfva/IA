# Test-IA Solution Requirements

Create a .NET solution named **Test-IA** that strictly complies with all architecture, coding, workflow, and testing rules defined in the `./` folder of this repository.

**Before starting, apply `.clinerules\solution-workflow.md` exactly as specified: search for existing implementations, reuse or extend them, and keep changes to the minimum required.**

---

## Functional Requirements

The solution must expose two services that implement the following:

### 1. GetADUserInfo

- Connect using a **real LDAP connection** to the Active Directory domain to which the local Windows machine belongs and search for a domain user.
- The Active Directory domain, Domain Controller, LDAP server, LDAP Base DN, and other LDAP connection parameters MUST be discovered dynamically. They MUST NOT be statically configured.
- Receives one string (`string`) containing the value of the user's Active Directory attribute `samAccountName`.
- Returns the values of the following Active Directory user attributes:
  - `displayName`
  - `employeeID`
  - `mail`
  - `userPrincipalName`

### 2. GetADGroupInfo

- Connect using a **real LDAP connection** to the Active Directory domain to which the local Windows machine belongs and search for a domain group.
- The Active Directory domain, Domain Controller, LDAP server, LDAP Base DN, and other LDAP connection parameters MUST be discovered dynamically. They MUST NOT be statically configured.
- Receives one string (`string`) containing the value of the Active Directory group attribute `samAccountName`.
- Returns the following Active Directory group attributes:
  - `displayName`
  - `member`

These services must be defined as **interfaces** in the Domain layer and **implemented** in the Application layer.

---

## Console Application Requirements

The solution MUST also contain a console application.

The `Main` method MUST demonstrate the real execution of both services:

1. Call `GetADUserInfo` using the sample `samAccountName`:
   - `MFVA649T`
2. Call `GetADGroupInfo` using the sample `samAccountName`:
   - `employees of MADRID`
3. Display the returned results through the required logging abstraction.

A console application that only registers services without actually calling them is incomplete.

The console application is expected to execute against the Active Directory environment of the Windows machine on which it is run.

---

## Logging Requirements

The solution MUST include a logging abstraction layer:

- **Test-IA.Logging** project MUST expose an `ILoggerService` interface that wraps `Microsoft.Extensions.Logging.ILogger`.
- The `ILoggerService` MUST provide:
  - `LogInformation(string message, params object[] args)`
  - `LogWarning(string message, params object[] args)`
  - `LogError(string message, params object[] args)`
- All console output MUST be performed through this logging abstraction.
- The use of `Console.WriteLine`, `Console.Write`, or any direct console output is strictly forbidden.
- The ConsoleApp MUST inject `ILoggerService` and use it for all output.

---

# Active Directory Connection and Authentication Requirements

## Domain Membership

The application MUST assume that the Windows machine where the application runs is joined to an **Active Directory Domain Services (AD DS)** domain.

The application MUST determine the Active Directory environment from the local Windows machine at runtime.

The application MUST NOT require the user to manually provide Active Directory connection information.

---

## No Static LDAP Configuration

The application MUST NOT require, store, hard-code, or receive as user input any of the following Active Directory or LDAP connection parameters:

- Active Directory domain name
- Domain Controller hostname
- Domain Controller IP address
- LDAP server hostname
- LDAP server IP address
- LDAP Base DN
- LDAP connection URL
- LDAP port
- LDAP username
- LDAP password

These values MUST be discovered dynamically at runtime from the Active Directory environment of the local Windows machine.

The application MUST NOT contain environment-specific values such as:

```text
dc01.example.local
LDAP://dc01.example.local
DC=example,DC=local
192.168.1.10
```

or equivalent hard-coded values.

---

## Dynamic Active Directory Discovery

The implementation MUST perform the following discovery process at runtime:

1. Determine the Active Directory domain to which the local computer belongs using the appropriate Windows/.NET Active Directory APIs.
2. Dynamically discover an available Domain Controller for that domain using Active Directory domain discovery mechanisms.
3. Obtain the LDAP naming context/Base DN dynamically from Active Directory rather than constructing or configuring it statically.
4. Build the LDAP connection information from the dynamically discovered Active Directory information.
5. Connect to Active Directory using Windows Integrated Authentication.
6. Use the security context of the Windows user running the application.
7. Do not prompt the user for Active Directory credentials.
8. Do not store, transmit, or manage an Active Directory username or password.
9. Do not assume a specific Domain Controller, domain name, IP address, LDAP URL, or Base DN.
10. The implementation MUST continue to work when the Active Directory domain contains multiple Domain Controllers.
11. The implementation MUST NOT depend on a particular Domain Controller hostname.
12. The implementation MUST obtain the naming context dynamically, preferably from the Active Directory RootDSE `defaultNamingContext`, rather than deriving it from a configured value.

The implementation should use the .NET/Windows Active Directory APIs appropriate for domain discovery, such as the APIs provided by `System.DirectoryServices.ActiveDirectory`.

---

## Domain Controller Discovery

The implementation MUST use Active Directory's domain discovery mechanisms to locate a suitable Domain Controller.

It MUST NOT use:

- a configured Domain Controller hostname;
- a configured IP address;
- a DNS name stored in `appsettings.json`;
- an environment variable containing a Domain Controller name;
- a command-line argument containing a Domain Controller name.

The selected Domain Controller MAY vary between executions depending on the Active Directory environment.

If the discovered Domain Controller becomes unavailable, the application should report a clear error. The implementation should not silently fall back to a statically configured server.

---

## LDAP Authentication

The preferred authentication mechanism is:

**Windows Integrated Authentication using the security context of the currently logged-on Windows user.**

The implementation MUST use integrated authentication rather than explicit LDAP credentials.

The implementation should use **Kerberos when it is available through the normal Active Directory integrated authentication mechanisms**. The application itself must not implement the Kerberos protocol.

The implementation should use the Windows/.NET LDAP authentication mechanism that allows Windows to negotiate the appropriate integrated authentication protocol, such as `Negotiate`.

The application MUST:

- use the current Windows security context;
- avoid asking the user for credentials;
- avoid storing credentials;
- avoid transmitting credentials as application-managed values.

The application MUST NOT:

- use anonymous LDAP authentication;
- use simple LDAP authentication with a username and password;
- require a configured service account;
- require a configured domain account;
- store passwords in source code, configuration files, environment variables, or command-line arguments.

---

## LDAP Connection Technology

The implementation MUST use a real LDAP connection and LDAP search operations.

The preferred .NET implementation should use the standard Windows/.NET LDAP and Active Directory APIs, including where appropriate:

- `System.DirectoryServices.ActiveDirectory` for Active Directory domain and Domain Controller discovery.
- `System.DirectoryServices.Protocols` for LDAP connections and LDAP searches.

For LDAP authentication, use the Windows integrated authentication mechanism (`Negotiate`) rather than explicit credentials.

The implementation MUST NOT replace LDAP access with:

- Microsoft Graph;
- Microsoft Entra ID APIs;
- local Windows user APIs;
- registry-based configuration;
- static test data;
- hard-coded Active Directory objects.

The application is specifically required to demonstrate access to the on-premises **AD DS LDAP directory**.

---

## LDAP Naming Context Discovery

The LDAP Base DN MUST be discovered dynamically.

The implementation MUST NOT hard-code values such as:

```text
DC=example,DC=local
```

After discovering a suitable Domain Controller, the implementation should query the LDAP RootDSE and obtain the `defaultNamingContext` attribute.

The resulting naming context MUST be used as the search base for the user and group searches.

This ensures that the application does not depend on a specific Active Directory domain name or DNS namespace.

---

## LDAP Search Requirements

### User search

The user search MUST use the dynamically discovered naming context and search for:

```text
(&(objectCategory=person)(objectClass=user)(sAMAccountName={input}))
```

The search MUST retrieve:

- `displayName`
- `employeeID`
- `mail`
- `userPrincipalName`

The input value MUST be handled safely and MUST NOT be concatenated into an LDAP filter without appropriate escaping.

### Group search

The group search MUST use the dynamically discovered naming context and search for:

```text
(&(objectCategory=group)(sAMAccountName={input}))
```

The search MUST retrieve:

- `displayName`
- `member`

The input value MUST be handled safely and MUST NOT be concatenated into an LDAP filter without appropriate escaping.

---

## Active Directory Environment Failure

The application MUST fail with a clear and descriptive error when:

- the local machine is not joined to an Active Directory domain;
- the domain cannot be determined;
- a suitable Domain Controller cannot be discovered;
- the LDAP RootDSE cannot be queried;
- the `defaultNamingContext` cannot be obtained;
- the LDAP connection cannot be established;
- integrated authentication fails;
- the requested user cannot be found;
- the requested group cannot be found.

Errors MUST be reported through the logging abstraction.

The application MUST NOT expose passwords, authentication tokens, or other sensitive authentication information in logs.

---

## Secure LDAP and Compatibility

The primary requirement is reliable Windows Integrated Authentication against the domain's AD DS LDAP service.

The implementation MUST NOT require LDAPS/certificate configuration as a prerequisite for the basic application to work in a standard domain-joined Windows environment.

If secure LDAP (LDAPS) is available and required by the target Active Directory security policy, the implementation should be capable of supporting it without introducing statically configured server information.

The implementation MUST respect the security policies enforced by the Active Directory environment and MUST NOT disable LDAP security mechanisms merely to make authentication succeed.

---

## Architecture for Active Directory Access

The Active Directory implementation MUST preserve the Clean Architecture separation.

The Domain layer MUST NOT contain:

- LDAP implementation details;
- Windows-specific Active Directory APIs;
- Domain Controller discovery code;
- LDAP connection objects;
- authentication credentials.

The Domain layer contains the service contracts/interfaces.

The Application layer contains the implementations and may contain the infrastructure-facing Active Directory components required to satisfy the functional requirements.

The implementation should separate the following responsibilities where practical:

1. Active Directory environment discovery.
2. Domain Controller discovery.
3. LDAP naming-context discovery.
4. LDAP connection creation.
5. User search.
6. Group search.

This separation is intended to keep the LDAP discovery and connection logic independently testable and prevent the service implementations from becoming tightly coupled to environment-specific configuration.

---

## Code Documentation Requirements

All public types, methods, properties, and interfaces MUST include XML documentation comments (`///`) that describe their purpose, parameters, and return values.

This applies to:

- Interfaces and their members in **Test-IA.Domain**.
- Public classes and methods in **Test-IA.Application**.
- Public classes and methods in **Test-IA.Logging**.
- The `Main` method and any helper methods in **Test-IA.ConsoleApp**.
- Test classes and test methods in **Test-IA.Tests**.

Inline comments (`//`) are optional and should be used only to explain non-obvious logic or workarounds.

Avoid redundant comments that merely repeat the code.

Example:

```csharp
/// <summary>
/// Concatenates two strings and returns the result.
/// </summary>
/// <param name="a">The first string.</param>
/// <param name="b">The second string.</param>
/// <returns>The concatenated string.</returns>
string Concat(string a, string b);
```
---

### Last Updated Date and Time requirements

When creating or updating `README.md`, the `Last Updated` value MUST contain
the current date and time at the moment the README is generated or updated.

The date and time MUST represent the current date and time in the Europe/Madrid IANA time zone.

The required display format is:

`dd/MM/yyyy HH:mm`

Example:

`11/08/2026 14:31`

The value MUST be obtained dynamically at generation time and MUST NOT be
hard-coded.

The generated `README.md` MUST use the obtained value in the `Last Updated`
field.

The implementation MUST explicitly convert the current time to the
`Europe/Madrid` time zone. It MUST NOT assume that the host system's local
time zone is `Europe/Madrid`.

The mechanism used to obtain the value MUST be platform-independent and MUST
NOT depend on PowerShell, Windows-specific commands, or Windows-specific time
zone identifiers.

Use the IANA time zone identifier `Europe/Madrid` so that daylight saving time
is handled automatically.

---

## NuGet Package Requirements

### Test-IA.Application

The project MUST reference:

- `Microsoft.Extensions.DependencyInjection.Abstractions`
- `System.DirectoryServices`
- `System.DirectoryServices.Protocols`

### Test-IA.ConsoleApp

The project MUST reference:

- `Microsoft.Extensions.DependencyInjection`

### Test-IA.Logging

The project MUST reference:

- `Microsoft.Extensions.Logging.Abstractions`

The Active Directory packages MUST be explicitly declared rather than relying on accidental transitive dependencies.

### Commands to run before coding

Execute the following commands in order:

```powershell
dotnet add src/Test-IA.Application/Test-IA.Application.csproj package Microsoft.Extensions.DependencyInjection.Abstractions
dotnet add src/Test-IA.Application/Test-IA.Application.csproj package System.DirectoryServices
dotnet add src/Test-IA.Application/Test-IA.Application.csproj package System.DirectoryServices.Protocols
dotnet add src/Test-IA.ConsoleApp/Test-IA.ConsoleApp.csproj package Microsoft.Extensions.DependencyInjection
dotnet add src/Test-IA.Logging/Test-IA.Logging.csproj package Microsoft.Extensions.Logging.Abstractions
dotnet restore
```

---

## Solution Structure (Clean Architecture)

Create the following projects and folders:

- **Test-IA.Domain**
  - Interfaces:
    - `IGetADUserInfo`
    - `IGetADGroupInfo`
  - Pure business/domain contracts only.

- **Test-IA.Application**
  - Implementations of the services.
  - Active Directory discovery and LDAP infrastructure required by the application.
  - No hard-coded domain-specific connection information.

- **Test-IA.Logging**
  - Logging infrastructure wrapping `Microsoft.Extensions.Logging`.

- **Test-IA.ConsoleApp**
  - Console application with explicit `Main` method.
  - The `Program` class and its `Main` method MUST include XML comments.
  - Uses dependency injection to consume the services.
  - Executes real Active Directory examples:
    - **GetADUserInfo**: Search the user with `samAccountName` `MFVA649T`.
    - **GetADGroupInfo**: Search the group with `samAccountName` `employees of MADRID`.
  - Displays results through `ILoggerService`.

- **Test-IA.Tests**
  - xUnit tests for both services and the logging project.
  - Tests MUST be designed so that unit tests do not require access to a live Active Directory environment.
  - Active Directory integration behavior should be isolated behind appropriate abstractions so the core service logic can be tested independently.
  - If integration tests against a real AD DS environment are implemented, they MUST be clearly identified as integration tests and MUST NOT contain hard-coded domain-specific connection parameters.

---

## Dependency Injection Configuration

- The **composition root** is `Program.cs` in the console app.
- All service implementations (`GetADUserInfo`, `GetADGroupInfo`) MUST be registered there.
- If `Application` or other projects need to expose registration helpers, they may reference `Microsoft.Extensions.DependencyInjection.Abstractions` and provide `IServiceCollection` extension methods if they follow Clean Architecture principles.
- The console app is the only project that fully builds and uses the service provider to run the real execution examples.
- Active Directory discovery and LDAP infrastructure components MUST be registered through dependency injection.
- The implementation MUST NOT create hidden global/static LDAP connections.
- LDAP connections and related disposable resources MUST be disposed correctly.

---

## Project References and Packages

| Project | References/Packages |
|---|---|
| Test-IA.Domain | No infrastructure dependencies |
| Test-IA.Application | Test-IA.Domain, `Microsoft.Extensions.DependencyInjection.Abstractions`, `System.DirectoryServices`, `System.DirectoryServices.Protocols` |
| Test-IA.Logging | `Microsoft.Extensions.Logging.Abstractions` |
| Test-IA.ConsoleApp | Test-IA.Application, Test-IA.Logging, `Microsoft.Extensions.DependencyInjection` |
| Test-IA.Tests | Test-IA.Application, Test-IA.Domain, Test-IA.Logging, xUnit |

---

## Technical Constraints

- **Framework**: Use **.NET 10.0** (`net10.0`). Verify availability with `dotnet --list-sdks`.
- The application is intended to execute on a **Windows machine that is a member of an Active Directory Domain Services domain**.
- **Solution format**: Use the new `.slnx` format (preferred for cleaner diffs and readability). Generate it with `dotnet new sln --format slnx`.
- **Terminal**: PowerShell. Do **not** use `&&` as command separator. Use `;` or execute commands sequentially.
- **dotnet new**: Always use `--force` to avoid interactive prompts.
- **Console app**:
  - Must have an explicit `Main` method (`--use-program-main`).
  - Do not use `Console.ReadKey()`, `Console.ReadLine()` or similar blocking calls that fail in non-interactive execution.
  - Do not retrieve LDAP connection parameters statically.
  - Do not use configuration files, environment variables, command-line arguments, or source-code constants for the Active Directory domain, Domain Controller, LDAP URL, LDAP Base DN, username, or password.
  - Discover the Active Directory environment dynamically from the Windows machine at runtime.
- **Dependency Injection**: Any project that needs it may reference `Microsoft.Extensions.DependencyInjection` or its abstractions. The composition root is the console application (`Program.cs`).
- **Code Documentation**: ALL public members MUST have XML comments (`///`). This is a mandatory deliverable.
- **Logging**: ALL output MUST be performed through the `ILoggerService` abstraction. The use of `Console.WriteLine`, `Console.Write`, or any direct console output is strictly forbidden.
- **Security**:
  - Never log credentials or authentication tokens.
  - Never store domain credentials.
  - Never use anonymous LDAP authentication.
  - Never use static domain-specific LDAP configuration.
- Do not compile until all code files are written and references are in place.
- Remove automatically generated files such as `Class1.cs` or `UnitTest1.cs`.
- Follow the workflow: validation must succeed before generating documentation.
- Projects MUST be placed in `src/` for Domain, Application, Logging, and ConsoleApp, and in `tests/` for Tests.

---

## Validation Requirements

Before generating the README, the implementation MUST be validated.

The validation MUST confirm:

1. `dotnet restore` succeeds.
2. `dotnet build` succeeds without errors.
3. `dotnet test` succeeds.
4. The console application executes successfully on a domain-joined Windows machine with access to AD DS.
5. The application discovers the Active Directory domain dynamically.
6. The application discovers a suitable Domain Controller dynamically.
7. The application obtains the LDAP naming context dynamically.
8. No Active Directory connection parameters are statically configured.
9. Windows Integrated Authentication is used.
10. No username or password is requested or stored.
11. `GetADUserInfo` performs a real LDAP search.
12. `GetADGroupInfo` performs a real LDAP search.
13. LDAP search inputs are safely escaped.
14. No direct console output is used.
15. All public members have XML documentation.
16. LDAP and Active Directory resources are disposed correctly.
17. Unit tests do not depend on a live AD DS environment.
18. The `.gitignore` file has been created correctly for all the projects in the solution.

If the real execution fails because the current machine is not domain-joined or cannot reach Active Directory, report the precise reason instead of replacing the real LDAP implementation with mocked or static data.

---

## Anti-Regression Requirements

The implementation MUST NOT introduce any fallback that violates the Active Directory requirements.

In particular, if dynamic Active Directory discovery fails, the application MUST NOT silently fall back to:

- a hard-coded Domain Controller;
- a hard-coded domain name;
- a hard-coded LDAP URL;
- a hard-coded Base DN;
- anonymous LDAP authentication;
- a configured username/password.

The application should fail clearly and report the discovery or connection problem through the logging abstraction.

---

## Anti-Regeneration Rule

After generating the README.md, do not edit it again unless the user explicitly requests a change or the project structure has changed.

If asked to "generate documentation" and the README already exists and is up-to-date, skip the generation and report that the documentation is already current.

---

## Documentation

After successful validation, including code documentation completeness, generate a professional `README.md` at the repository root using `.clinerules/solution-readme.md` as the formatting reference.

The `README.md` MUST reflect the final verified state of the solution.
