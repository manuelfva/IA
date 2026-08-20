# Repository Architecture

## Context

- Languages: C#
- Architecture: Clean Architecture
- Domain: Active Directory (AD DS and AD LDS)
- Dependency Injection is mandatory

---

## Framework Requirements

- ALL .NET projects MUST target net10.0.
- New projects MUST use net10.0.
- Existing projects MUST NOT be downgraded.
- Multi-targeting is not allowed unless explicitly requested.
- Examples, snippets, and generated code MUST assume .NET 10 APIs and language features when applicable.
- In the event of a conflict with cline rules, the local rules shall take priority for this project over the global rules.

---

## C# Version Requirements

- Use modern .NET 10 and C# features when they improve readability.
- Prefer collection expressions where appropriate.
- Prefer primary constructors when appropriate.
- Prefer modern pattern matching.
- Prefer native framework functionality over third-party libraries.

---

## High-Level Structure

- The repository contains multiple projects with single responsibility
- Each project MUST belong to a clearly defined domain:
  - AD DS
  - AD LDS
- Shared logic MUST be extracted into reusable libraries

---

## Layered Architecture

The following layers MUST be respected:

- Domain
  - Business rules and entities
  - NO external dependencies

- Application
  - Use cases and orchestration
  - Depends only on Domain

- Infrastructure
  - LDAP / Active Directory access
  - External integrations
  - Implements interfaces

- Presentation (if applicable)
  - Controllers / APIs / scripts
  - MUST NOT contain business logic

---

## LDAP / Active Directory Rules

- ALL LDAP access MUST be centralized in Infrastructure
- MUST NOT duplicate LDAP connection logic
- MUST reuse existing services before creating new ones
- MUST differentiate implementations:
  - AD DS services
  - AD LDS services
- MUST NOT mix AD DS and AD LDS logic

---

## External Connection Reuse and Caching

- Connections to external repositories and services MUST be reused whenever the underlying client supports connection reuse.
- This rule applies to LDAP / Active Directory, databases, HTTP services, and other external resources.
- The first operation for a given endpoint and credential context MAY establish the connection or create the client.
- Subsequent operations MUST reuse the existing cached connection or client instead of creating a new connection.
- Connection and client management MUST be centralized in the Infrastructure layer.
- Business and Application layers MUST NOT create, cache, or dispose external connections directly.
- Cached resources MUST be keyed by all values that define their security and connectivity context, including, where applicable:
  - Server or host
  - Port
  - Protocol or transport security
  - Tenant or domain
  - Authentication identity or credential context
  - Database or repository name
- Credentials, passwords, access tokens, and sensitive connection data MUST NOT be used as plain-text cache keys or written to logs.
- Cache implementations MUST be thread-safe and MUST prevent multiple concurrent requests from creating duplicate connections for the same cache key.
- The cache MUST support expiration, invalidation, and controlled recreation after connection failures.
- A failed or invalid connection MUST NOT remain usable in the cache.
- Transient failures MAY trigger reconnection according to the resilience policy defined by the Application layer.
- Infrastructure MUST NOT perform unbounded automatic retries.
- Cached connections and clients MUST be disposed gracefully when they expire, are invalidated, or when the application shuts down.
- Connection reuse MUST NOT compromise isolation between different servers, tenants, domains, databases, or authentication contexts.
- The implementation MUST expose abstractions through interfaces, for example:
  - `ILdapConnectionFactory`
  - `IDatabaseConnectionFactory`
  - `IExternalClientCache`
- The implementation MUST provide metrics or logs that distinguish:
  - Cache hit
  - Cache miss
  - New connection created
  - Connection invalidated
  - Connection recreated after failure
- Logs MUST include non-sensitive endpoint information and MUST NOT include credentials, passwords, tokens, or PII.
- Tests MUST verify:
  - The first request creates the connection.
  - Subsequent requests reuse the cached connection.
  - Concurrent requests do not create duplicate connections.
  - Invalid or failed connections are removed and recreated.
  - Different connection contexts do not share the same cached resource.
---

### LDAP Connection Reuse

- LDAP connection creation MUST be handled by a centralized `ILdapConnectionFactory`.
- The factory MUST first retrieve a valid connection from the cache.
- If no valid connection exists, the factory MUST create, configure, authenticate, and cache one connection for the requested context.
- LDAP connections MUST be invalidated when authentication fails, the server closes the connection, or the connection becomes unusable.
- The factory MUST NOT create a new LDAP connection for every repository operation.
- AD DS and AD LDS connections MUST use separate cache contexts and MUST NOT be mixed.
- LDAP searches and modifications MUST reuse the cached connection whenever the connection is valid.

---

## Reusability

- ALWAYS check for existing services before creating new code
- SHARED logic MUST be abstracted into common services or libraries
- DUPLICATION is strictly forbidden

---

## Interfaces and Contracts

- ALL external access MUST be defined via interfaces
  - Example: `IUserQueryService`, `IGroupMembershipWriter`, `IComputerAccountCreator`
- Implementations MUST be replaceable
- Code MUST depend on abstractions, not concrete classes
- Abstractions shared between AD DS and AD LDS MUST reside in `Domain/Common` with separate implementations in `Infrastructure/ADDS` and `Infrastructure/ADLDS`

---

## Attribute Mapping

- Attribute mapping MUST use the `IAttributeMapper` pattern.
- Static dictionaries, static classes, hardcoded attribute maps, or inline attribute-name mappings MUST NOT be used.
- `IAttributeMapper` MUST define the abstraction for mapping domain properties to directory-specific LDAP attributes and vice versa.
- Attribute mapper implementations MUST be provided through Dependency Injection.
- AD DS and AD LDS MUST have separate `IAttributeMapper` implementations when their schemas or attribute names differ.
- Attribute mapping MUST NOT be embedded directly in services, repositories, or LDAP access code.
- Attribute names and mappings MUST be centralized within the corresponding `IAttributeMapper` implementation.
- New attribute mappings MUST extend the appropriate mapper rather than introducing new static dictionaries or duplicated mappings.

---

## Dependency Injection

- ALL services MUST be registered via DI
- MUST NOT instantiate dependencies manually (`new`)
- MUST favor constructor injection

---

## Separation of Concerns

- MUST NOT mix business logic with LDAP access
- MUST NOT access Infrastructure from Domain layer
- MUST NOT bypass Application layer

---

## AD DS vs AD LDS

- AD DS and AD LDS MUST be treated as separate domains
- MUST NOT assume shared schema or attributes
- MUST use dedicated services for each system
- Attribute mappings MUST be resolved through the appropriate `IAttributeMapper` implementation.
- Shared abstractions MUST be explicitly designed (as per Interfaces and Contracts section)

---

## Logging and Observability

- ALL LDAP interactions (connection attempts, searches, modifications) MUST be logged
- Use `ILogger<T>` injected via DI – never `Console.WriteLine` or `Debug.WriteLine`
- Log levels:
  - `Information`: successful operations, connection open/close
  - `Warning`: retries, fallback logic, deprecated attribute usage
  - `Error`: LDAP exceptions, authentication failures, timeouts
- Infrastructure layer logs MUST include enough context (e.g., server, operation type) but MUST NOT leak credentials or PII

---

## Error Handling and Resilience

- Domain layer defines custom exceptions inheriting from `DomainException` (e.g., `UserNotFoundException`, `LdapOperationFailedException`)
- Infrastructure layer MUST catch LDAP‑specific exceptions (`LdapException`, `DirectoryOperationException`, `AuthenticationException`) and wrap them into domain-friendly exceptions
- Application layer decides retry/fallback policies – Infrastructure never retries automatically unless explicitly configured
- All async LDAP operations MUST support cancellation via `CancellationToken`

---

## Configuration Management

- LDAP connection parameters (servers, ports, bind DN, base DNs) MUST come from `IConfiguration` (e.g., `appsettings.json`, environment variables)
- Hardcoded connection strings or credentials are FORBIDDEN
- Use `IOptions<T>` or `IOptionsSnapshot<T>` for strongly-typed settings

---

## Validation and Security

- Before any LDAP operation, validate:
  - Distinguished Names (DN) format – use `DistinguishedName` helper
  - Search filters – escape with `System.DirectoryServices.Protocols.SearchFilter.Escape`
  - Attribute names – against allowed lists when possible
- NEVER concatenate user input directly into LDAP filters (prevents LDAP injection)

---

## Testing Requirements

- Domain layer MUST have 100% unit test coverage for business rules (no external dependencies)
- Application layer MUST be tested with mocked interfaces of Infrastructure
- Infrastructure layer MUST have integration tests against a real (or test) AD/LDS instance
- PowerShell functions MUST have Pester tests

---

## Minimum Change Rule

A change is considered “minimal” if:
- It affects **≤3 files**
- It does **not break existing public contracts** (interfaces, method signatures, PowerShell function names)
- It **reuses or extends** existing classes/functions

If a change requires more than 3 files or introduces a new public abstraction, a written justification is MANDATORY in the pull request.

---

## Prohibitions

- NEVER duplicate LDAP logic
- NEVER bypass defined services
- NEVER mix AD DS and AD LDS implementations
- NEVER place LDAP logic outside Infrastructure
- NEVER create tightly coupled code
- NEVER swallow exceptions without logging
- NEVER hardcode credentials or connection strings

---

## Best Practices

- Prefer small, focused services
- Use async/await for I/O operations
- Validate inputs before LDAP operations
- Keep code testable and modular
- Use `CancellationToken` in all async methods
- Dispose `LdapConnection` and `DirectorySearcher` properly

---

## Repository Discovery

Before creating:

- Services
- Interfaces
- Repositories
- LDAP providers
- PowerShell modules

The existing repository MUST be searched for reusable implementations.

Creating duplicates is forbidden.

---

## Change Strategy

Order of preference:

1. Reuse existing implementation
2. Extend existing implementation
3. Introduce new implementation only if necessary

Every new abstraction must be justified.

## Existing Code First

Before creating:

- New services
- New interfaces
- New repositories
- New LDAP providers
- New PowerShell modules

The repository must be inspected for existing implementations.  
Existing code should be extended whenever reasonable.  
Creating parallel implementations is discouraged and must be justified.