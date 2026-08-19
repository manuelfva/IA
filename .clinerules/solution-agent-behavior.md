## User-Controlled Persistent Actions

The following actions are strictly user-controlled and MUST NOT be performed automatically:

1. Memory Bank updates
   - Do NOT create, modify, update, or regenerate any Memory Bank files automatically.
   - Do NOT update the Memory Bank as part of completing a task.
   - Only update the Memory Bank when the user explicitly requests it.

2. README.md
   - Do NOT create, modify, or update README.md automatically.
   - Do NOT consider README.md updates to be an implicit part of completing a task.
   - Only modify README.md when the user explicitly requests it.

3. Git staging
   - Do NOT run `git add`, `git stage`, or any equivalent Git staging operation automatically.
   - Never stage files as part of completing a task.
   - Only stage changes when the user explicitly requests it.

4. Git commits
   - Do NOT run `git commit` automatically.
   - Never create a commit as part of completing a task.
   - Only create a commit when the user explicitly requests it.

### Explicit User Authorization

An explicit user request is required before performing any of the actions above.

Examples of explicit authorization include:
- "Update the Memory Bank."
- "Update README.md."
- "Stage the changes."
- "Run git add."
- "Commit the changes."
- "Create a commit with the following message: ..."

Statements such as:
- "Complete the task."
- "Finish the implementation."
- "Clean everything up."
- "Finalize the work."
- "Prepare the project."
- "Make sure everything is up to date."

MUST NOT be interpreted as authorization to perform any of the restricted actions.

### When Authorization Is Not Explicit

If any of these actions appears necessary or desirable, DO NOT perform it automatically.

Instead:
1. Complete all other authorized work.
2. Leave the restricted action untouched.
3. Inform the user that the action was intentionally not performed.
4. Ask the user whether they want you to perform it.

The absence of an explicit prohibition in the user's current request MUST NOT be interpreted as permission.

### Git Repository Safety

Git read-only operations are allowed, including:
- `git status`
- `git diff`
- `git log`
- `git show`
- `git branch --show-current`

Git state-changing operations require explicit user authorization, including:
- `git add`
- `git stage`
- `git commit`
- `git push`
- `git pull`
- `git merge`
- `git rebase`
- `git reset`
- `git restore`
- `git checkout` when it modifies the working tree or switches branches

Never perform these operations automatically as part of a task.

### Priority

These rules take precedence over any default workflow, task-completion behavior, project convention, Memory Bank workflow, Git workflow, or inferred best practice that would otherwise cause these actions to be performed automatically.