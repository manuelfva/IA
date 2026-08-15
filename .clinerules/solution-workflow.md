# Repository Workflow

This workflow MUST be followed BEFORE generating any new code (C# or PowerShell) and before generating any project documentation.

The workflow must always move forward.

A completed step MUST NOT be repeated unless:
- a validation failure requires correction, or
- new repository information invalidates a previous decision.
- **Do not re-read or re-edit `README.md`** after it has been generated, unless the user explicitly requests it or the project structure changes. This is considered a loop and is prohibited.

Repeated reading, searching, or analysis without new information is prohibited.

This workflow is mandatory for all code changes and documentation generation.

------------------------------------------------------------------------

## Step 1: Load Repository Context (Once)

Before starting any task, load the required repository instructions.

Required files:

- `.cline/rules/solution-requirements.md`
- `.cline/rules/solution-architecture.md`
- `.cline/rules/solution-csharp.md`
- `.cline/rules/solution-conventions.md`
- `.cline/rules/solution-readme.md`
- `.cline/rules/solution-memory-bank.md`
- `workspace.code-workspace` (if present, used to detect project structure and build configuration)

These files MUST be read only once per task.

After all required files have been loaded successfully:

- Do not reload them.
- Do not repeat this step.
- Continue with **Step 2**.

If a required file is missing, continue with the available information
and report the missing file.

### Step 1.1 — Structural Files Loaded Once

The following files are considered **structural** and MUST be read only
once per session:

- Solution files (`.sln`, `.slnx`)
- Project files (`.csproj`, `.vbproj`)
- Package references (`packages.config`, `Directory.Packages.props`)

Once loaded, their content is immutable for the session. Do NOT re-read
them unless the user explicitly modifies them.

**Anti-re-read rule:** After loading the required files in step 1, do not invoke read_file for any of them during the rest of the task. If you need to recall their content, look for it in the conversation context. If the agent offers caching, trust that the content is already available.

------------------------------------------------------------------------

## Step 2: Search Before Create

Search is only required for the requested change.

Do NOT repeatedly search the repository for the same information.

### Search limit

For each requested feature:

- Maximum **3 targeted file reads** per feature.
- After a file is read once, cache its content. Do not read it again unless the task explicitly modifies it.
- Maximum 5 minutes.
- Stop after relevant results are found or no results exist.

If these limits are insufficient to determine the existence of a reusable implementation, you may extend the search **only with explicit justification**. In that case, record the justification and proceed.

The objective is reuse detection, not full repository discovery.

### Caching rule

Before reading any file, check if its content is already available from a
previous read in the current session. If yes, reuse the cached content.

### Required search locations

Search only:

- `src/`
- `tests/`
- `docs/`

Ignore:

- `.git/`
- `bin/`
- `obj`
- generated files
- package folders

### Completion rule

After completing the searches:

- Found implementation → reuse or extend.
- No implementation found → create new implementation.

Never restart the search phase unless new information changes the implementation decision.

------------------------------------------------------------------------

## Step 3: Reuse or Extend

Priority order:

1.  Reuse existing implementations.
2.  Extend existing abstractions when possible.
3.  Create new implementations only when justified.

New abstractions require written justification.

------------------------------------------------------------------------

## Step 4: Design the Change (Minimum Change Rule)

Before implementation, prepare a brief design description (can be mental, but must be clear):

- Prefer minimal changes.
- Avoid unnecessary file modifications.
- Preserve public contracts.
- Keep changes isolated to the correct layers.

If the design requires new abstractions, the justification from Step 3 must be included.

------------------------------------------------------------------------

## Step 5: Validate Architectural Compliance

Before coding verify:

-   Code belongs to the correct layer.
-   Dependencies follow architecture rules.
-   Configuration and logging rules are respected.
-   Inputs are validated.
-   Async patterns follow repository standards.
  
### Step 5.5 — Add Package Dependencies
Before writing any code that references external packages (e.g., Microsoft.Extensions.DependencyInjection.Abstractions), add the corresponding NuGet packages to the project(s) that require them. Use dotnet add <project> package <package> commands.
Do not proceed to writing implementation code until all packages are successfully added and restored.

### Step 5.6 — Validate Project References

Before writing any code that depends on another project (e.g., `Application` depends on `Domain`), ensure that the project reference is correctly added.

**Checklist:**
- [ ] The dependent project (e.g., `Test-IA.Application`) has a `<ProjectReference>` to the dependency project (e.g., `Test-IA.Domain`) in its `.csproj` file.
- [ ] The namespace of the referenced types matches the `using` directive in the consuming code.

**Action:**
- If the reference is missing, add it using `dotnet add <project> reference <dependency>`.
- Verify the namespace of the referenced interfaces/classes and use the correct `using` directive in the code.

Do not proceed to writing implementation code until project references are verified and correct.

------------------------------------------------------------------------

## Step 6: Implement and Test

Implementation must include validation:

-   Unit tests.
-   Integration tests where required.
-   Repository linters and analyzers.

**Incremental build check:**
- After creating each new file (or after creating all files for a project), run `dotnet build <project>.csproj` to ensure there are no compilation errors.
- If errors are found, fix them before proceeding to the next file or project.
- Do not wait until the final validation to discover reference or namespace errors.

------------------------------------------------------------------------

## Step 7: Validation Completion Rule (Principle)

Validation commands MUST NOT be executed repeatedly once they have passed.

A validation cycle consists of:

1. Clean (optional)
2. Build
3. Test
4. Execute application or required workflow

After a successful validation cycle:

- Record the result (e.g., in task context).
- Mark validation as completed.
- Proceed to the next step.

Do NOT rerun:

- `dotnet build`
- `dotnet test`
- `dotnet run`
- repository scans

unless:

- source code changes after validation,
- dependencies change,
- configuration changes,
- validation fails.

A successful validation result is considered final for the current task.

------------------------------------------------------------------------

## Step 8: Build and Execute Validation (Mandatory Gate Before Documentation)

This step enforces that validation is actually executed before moving to documentation.

**Do NOT create or modify README.md until all the following conditions are met:**

-   [ ] The complete solution builds successfully.
-   [ ] All projects compile without errors.
-   [ ] Automated tests pass successfully.
-   [ ] Required execution flows have been run successfully.
-   [ ] Runtime validation has completed.
-   [ ] No blocking errors remain.

### Execute the validation sequence now (once):

1. `dotnet clean` (only if required)
2. `dotnet build`
3. `dotnet test`
4. `dotnet run` or equivalent execution command

### Outcome:

**SUCCESS:**
- Validation completed.
- **Proceed directly to Step 9 (Documentation).**

**FAILURE:**
- Fix the issue.
- Restart validation from Step 6 (adjusting implementation).

Do not repeat successful validation commands.

------------------------------------------------------------------------

### Step 9: Generate Documentation (only one time)

Documentation generation is the final workflow activity and MUST be executed only once per task.

**The README is considered OUTDATED only if:**
- A new project is added or removed.
- A public interface or service contract changes (signature, name, parameters).
- Dependencies (NuGet packages, target framework) change.
- The solution structure (folders, projects) changes.
- The user explicitly requests a documentation update.

**The README is considered UP-TO-DATE if:**
- Only internal implementation details change (e.g., bug fixes, performance improvements, code style changes).
- Only test code changes.
- Only configuration files (e.g., `appsettings.json`) change.
- The build and test results remain the same.

**Action:**
1. Check if `README.md` exists.
2. If it exists, evaluate if it is outdated using the criteria above.
3. If it is up‑to‑date, **do not read, edit, or regenerate it**. Skip this step entirely.
4. If it is outdated or does not exist, generate it **once** following the `readme-rules.md` template.
5. After generation, **do not modify it again** during the same task, even if other code changes occur later.
6. **Never** update just the date without regenerating the full content.

Before generating, verify:
- [ ] Architecture is final.
- [ ] Dependencies and versions are final.
- [ ] Build commands are confirmed.
- [ ] Test results are available.
- [ ] Diagrams represent the final solution.
- [ ] No temporary code is documented.

The README generation process must use `solution-readme.md` as the formatting reference.

------------------------------------------------------------------------

## Step 10: Generate `.gitignore` file for the solution (Final Step)

Act as a DevOps expert. Your task is to generate the `.gitignore` file for the ROOT of this solution, which contains multiple projects of different types.

Follow this workflow strictly:

1. **MANDATORY RECURSIVE SCAN**:
   - Do NOT limit yourself to the current directory. Explore ALL subdirectories recursively.
   - Detect ALL technologies present by searching for these files at ANY level:
     - `.sln`, `.csproj`, `.vbproj`, `.fsproj` → .NET / C#.
     - `package.json` → Node.js (could be in `/ClientApp`, `/src/ui`, etc.).
     - `requirements.txt`, `pyproject.toml` → Python.
     - `Dockerfile`, `docker-compose.yml` → Docker.
     - `*.ps1`, `*.psm1`, `*.psd1` → PowerShell.
     - `pom.xml` → Java Maven, etc.
   - Make a list of ALL the technologies you find.

2. **INTELLIGENT BUILD & MERGE**:
   - Combine the official `.gitignore` rules for EVERY detected technology.
   - For .NET, ALWAYS ignore these at ANY level: `bin/`, `obj/`, `packages/`, `*.user`, `*.suo`, `.vs/`.
   - For Node.js, ignore ALL `node_modules/` folders wherever they appear (root or subfolders).
   - For PowerShell, ignore `*.ps1xml`, `*.clixml`, and `__PSScriptPolicyTest`.
   - If Docker is present, ignore `*.pid` and `.docker/` folders if they exist.
   - Ensure publication/build output folders like `publish/`, `artifacts/`, and `output/` are also excluded.

3. **PERMANENT GLOBAL RULES**:
   - Environment variables and secrets: `.env`, `.secrets`, `*.pem`, `*.key`.
   - Logs and temporary files: `*.log`, `*.tmp`, `*.cache`.
   - Operating system files: `.DS_Store`, `Thumbs.db`.
   - IDEs: Ignore `.vscode/` or `.idea/` ONLY if there is NO shared configuration file (like `.editorconfig`) in the repository; if in doubt, **omit them** to avoid wiping out team settings.

4. **OUTPUT FORMAT**:
   - Return ONLY the final `.gitignore` content inside a ```gitignore code block.
   - Right before the block, add a single line listing the technologies detected (e.g., "Detected: .NET 6, Node.js 18, PowerShell 7").
   - Do NOT add long explanations inside the code block; only brief inline comments if necessary.

5. **FALLBACK**:
   - If you only detect .NET and PowerShell, generate the classic `.gitignore` for them.
   - If you detect nothing, generate a basic global ignore and ask me which technologies I want to add.

Execute the recursive scan now and generate the file!

------------------------------------------------------------------------

