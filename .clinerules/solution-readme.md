This document defines how a README must be generated. It intentionally contains no project-specific assumptions. All project information must be discovered from the repository being documented.

# Prompt to Generate a Professional README.md

## Objective

You are an expert technical documentation assistant.

Generate a complete, production-ready `README.md` for the current repository.

The document must be accurate, concise, professional, written in US English, and immediately usable without manual editing.

Remember, every time the `README.md` file is updated you **MUST** update the `Last Updated` value too.

------------------------------------------------------------------------

# Repository Inspection Workflow

Execute the following steps **in order**.

1.  Determine the repository type.
2.  Inspect the solution and project structure.
3.  Detect the architecture.
4.  Inspect project references and dependencies.
5.  Inspect public APIs and services.
6.  Inspect configuration files.
7.  Inspect test projects.
8.  Inspect CI/CD configuration.
9.  Inspect repository documentation.
10. Generate the README.

Do not skip any step.

------------------------------------------------------------------------

# Repository Inspection Rules

Inspect, when available:

-   *.sln / *.slnx
-   \*.csproj
-   Directory.Build.props
-   Directory.Packages.props
-   global.json
-   NuGet.config
-   Source code
-   Unit tests
-   Requirements.md
-   workflow.md
-   architecture.md
-   conventions.md
-   README templates
-   Dockerfile
-   docker-compose.yml
-   GitHub Actions
-   Azure DevOps pipelines
-   Any additional project documentation.

Repository documentation has priority over inferred information.

Priority order:

1.  Requirements.md
2.  architecture.md
3.  workflow.md
4.  Source code
5.  Project files
6.  File names

------------------------------------------------------------------------

# General Generation Rules

-   Write everything in US English.
-   Never invent information.
-   Never speculate.
-   Only include information that can be verified.
-   If information cannot be determined:
    -   omit the section, or
    -   mark it as **Pending**.
-   Keep formatting consistent.
-   Prefer tables over long paragraphs.
-   Keep descriptions concise and technical.

------------------------------------------------------------------------

# Date and Time Requirements

When generating or updating the `Last Updated` value, the date and time MUST
represent the current date and time in the `Europe/Madrid` IANA time zone.

The mechanism used to obtain the value MUST be platform-independent and MUST
NOT depend on PowerShell, Windows-specific commands, or Windows-specific time
zone identifiers.

The time zone MUST be resolved explicitly using the IANA time zone identifier:

`Europe/Madrid`

The time zone rules MUST be applied automatically so that daylight saving
time is handled correctly.

The required display format is:

`dd/MM/yyyy HH:mm`

Example:

`11/08/2026 14:31`

The value MUST be obtained dynamically at generation time and MUST NOT be
hard-coded.

The generated README MUST use this value in the `Last Updated` section.

The implementation MUST NOT assume that the host system's local time zone is
`Europe/Madrid`. The current system time MUST be explicitly converted to the
`Europe/Madrid` time zone before formatting the value.

Do NOT use:

* PowerShell-specific commands.
* Windows-specific time zone identifiers such as `Romance Standard Time`.
* Hard-coded date or time values.
* The host system's local time without explicitly converting it to `Europe/Madrid`.

------------------------------------------------------------------------

# README Structure

Generate the following sections whenever applicable:

1.  Header
2.  Badges
3.  Table of Contents
4.  Overview
5.  Architecture
6.  Mermaid Diagram
7.  Layer Responsibilities
8.  Projects
9.  Public Services
10. Dependency Injection
11. Configuration
12. Getting Started
13. Testing
14. Coding Standards
15. Technology Stack
16. Repository Structure
17. Build Status
18. License
19. Last Updated

------------------------------------------------------------------------

# Header

Include:

-   Project title
-   Short description
-   Detected badges

Only include badges that can be verified.

------------------------------------------------------------------------

# Architecture

Automatically detect:

-   Clean Architecture
-   Layered
-   Onion
-   Hexagonal
-   Vertical Slice
-   Modular Monolith
-   Microservices

If unknown, generate a simplified dependency diagram.

------------------------------------------------------------------------

# Mermaid Rules

Use Mermaid.

Node identifiers MUST be different from displayed labels.

Never reuse a subgraph title as a node identifier.

------------------------------------------------------------------------

# Projects

Generate a table:

| Project \| Type \| Target Framework \| Description \| Depends On \|

Determine project types automatically.

------------------------------------------------------------------------

# Public API Rules

Document only publicly exposed functionality.

Do NOT document:

-   internal classes
-   private classes
-   helper classes
-   generated code
-   implementation details

unless necessary for understanding the architecture.

------------------------------------------------------------------------

# Dependency Injection

Describe:

-   Registration location
-   Service lifetimes
-   Main registrations

Only if detected.

------------------------------------------------------------------------

# Configuration

Document detected configuration sources:

-   appsettings.json
-   Environment Variables
-   User Secrets
-   Azure Key Vault
-   Docker
-   Other providers

------------------------------------------------------------------------

# Getting Started

Include:

-   Prerequisites
-   Clone (if repository URL is available)
-   Restore
-   Build
-   Run

Determine commands automatically.

Do not guess solution names.

------------------------------------------------------------------------

# Testing

Determine:

-   Test framework
-   Test command
-   Test categories

If execution results are available include:

-   Passed
-   Failed
-   Skipped
-   Duration

Otherwise:

> Tests pending execution.

Never invent statistics.

------------------------------------------------------------------------

# Coding Standards

Summarize detected conventions such as:

-   Nullable
-   File-scoped namespaces
-   EditorConfig
-   StyleCop
-   Roslyn analyzers

------------------------------------------------------------------------

# Technology Stack

Generate:

| Category \| Technology \|

Include only detected technologies.

------------------------------------------------------------------------

# Repository Structure

Generate a simplified directory tree reflecting the actual repository.

------------------------------------------------------------------------

# Build Status

Summarize CI/CD only if present.

------------------------------------------------------------------------

# Confidence Rule

Only include information that can be verified from the repository.

If confidence is low:

-   omit the section

or

-   mark it as Pending.

Never speculate.

------------------------------------------------------------------------

# Ignore Generated Code

Ignore:

-   obj/
-   bin/
-   Generated/
-   \*.g.cs
-   \*.Designer.cs
-   AssemblyInfo.cs

unless explicitly required.

------------------------------------------------------------------------

# README Scope

The README is an overview document.

Do not:

-   document every class
-   document every method
-   reproduce large portions of source code

Prefer links to additional documentation.

------------------------------------------------------------------------

# Validation Rules

Generate the README only after successful validation when applicable:

``` text
dotnet restore
dotnet build
dotnet test
dotnet run
```

If validation fails, explain which step failed instead of generating the
README.

------------------------------------------------------------------------

# Anti-Regeneration Rule

If an up-to-date README already exists and the project structure has not
changed, do not regenerate it unless explicitly requested.

------------------------------------------------------------------------

# Final Rule

The generated README must be complete, internally consistent,
professionally formatted, and ready to commit without manual editing.
