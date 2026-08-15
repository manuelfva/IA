# C# Coding Guidelines for Continue

## Priority

1.  Existing project architecture and patterns
2.  Security and correctness
3.  These coding rules
4.  Style preferences

## Assistant Behavior

-   Preserve existing project structure and conventions.
-   Make minimal focused changes.
-   Do not refactor unrelated code.
-   Do not invent APIs, classes, methods, packages, or dependencies.
-   Ask for clarification when requirements are ambiguous.
-   Prefer existing internal examples over generic examples.

## Language & Style

-   Use file-scoped namespaces.
-   Enable nullable reference types.
-   Use ArgumentNullException.ThrowIfNull() for parameter validation.
-   Prefer record types for immutable DTOs and value objects.
-   Use readonly struct for small immutable value types.
-   Avoid var when the type is not obvious from the right-hand side.
-   Prefer readable and explicit code.

## Namespace Consistency

- Always use the same namespace for interfaces and their implementations across layers.
- For example, if `IStringConcat` is defined in `TestIA.Domain.Interfaces`, then `StringConcatService` in `TestIA.Application.Services` must include `using TestIA.Domain.Interfaces;`.
- Do not assume that types are in the global namespace or in the same namespace as the consuming project.
- When creating a new file, explicitly add the required `using` directives for all external types.

## Async Patterns

-   Prefer true async APIs for I/O operations when available.
-   Avoid async void except event handlers.
-   Use CancellationToken as the last parameter in async methods.
-   Never use .Result or .Wait() in async code.
-   Do not wrap synchronous I/O in Task.Run unless there is a clear
    reason.

## Disposal & Resources

-   Dispose IDisposable and IAsyncDisposable resources.
-   Prefer using declarations and await using.
-   Do not cache disposable operation-specific objects.
-   Do not share mutable resources without a safe lifetime strategy.

## Null Handling

-   Enable nullable reference types in all projects.
-   Use nullable annotations correctly.
-   Avoid returning null collections.
-   Prefer `Array.Empty<T>()` or `Enumerable.Empty<T>()`.

## Exceptions

-   Keep domain exceptions meaningful and specific.
-   Infrastructure exceptions must be wrapped before crossing
    application boundaries.
-   Never swallow exceptions silently.
-   Preserve the original exception when wrapping.

## LDAP / Directory Services

-   Keep LDAP implementation details inside infrastructure layers.
-   Do not expose LDAP-specific exceptions to application layers.
-   Dispose LDAP resources correctly.
-   Do not hardcode LDAP servers, credentials, or configuration.
-   Reuse LDAP helpers only through approved shared patterns.

## Dependency Injection

-   Use appropriate service lifetimes.
-   Do not capture scoped services in singleton services.
-   Prefer constructor injection.
-   Do not add dependencies without approval.
  
**Package verification**: Before writing any code that references `IServiceCollection` or `ServiceCollection`, ensure the `Microsoft.Extensions.DependencyInjection.Abstractions` package is installed. If not, add it using `dotnet add`. This check is mandatory before creating any DI-related code.

