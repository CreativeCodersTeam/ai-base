# dotnet-implementer Skill — Detailed Reference

This document provides in-depth guidance for each phase of the dotnet-implementer
workflow. It assumes a .NET / C# project; all skill bindings are fixed (no
discovery).

---

## Phase 1 — Requirement Review (Detail)

### Goal

Ensure a thorough understanding of the requirement before any implementation
begins. Prevent wasted effort from misunderstood requirements or unclear scope.

### Steps

#### 1.1 Read the Requirement

- Read the full requirement text (issue, user story, ticket, or user message).
- Identify the core objective: What should change? What is the expected outcome?

#### 1.2 Identify Acceptance Criteria

- Extract explicit acceptance criteria from the requirement.
- Derive implicit criteria from context (existing tests must still pass,
  existing API contracts must be honored, public API additions need XML docs,
  etc.).
- List edge cases that should be covered.

#### 1.3 Clarify Ambiguities

- If anything is unclear, ask the user targeted questions.
- Do NOT assume answers to ambiguous requirements — always ask.
- Common ambiguities to watch for:
  - Scope boundaries (what is in/out)
  - Error handling behavior (`ProblemDetails`? typed exceptions?)
  - Performance expectations
  - Backward compatibility requirements (API surface, EF Core migrations)
  - Target framework constraints

#### 1.4 Analyze the Codebase

- Identify the `*.csproj` files, projects, and components affected.
- Understand the existing architecture (hosts: ASP.NET Core, Worker Service,
  Console, MAUI; data layer: EF Core; cross-cutting: DI, Options, Serilog).
- Look for existing tests under `tests/` or `*.Tests` projects.
- Check for documentation (XML doc comments, READMEs) that needs updating.
- Inspect `Directory.Build.props`, `Directory.Packages.props`, `.editorconfig`,
  and any `nuget.config`.
- For external/platform APIs whose surface you need to verify, invoke
  `dotnet-inspect` (search for types, list APIs, diff versions, find
  extension methods).

#### 1.5 Identify Required dotnet-* Skills

This is the fixed skill map — not discovery. Read the requirement and list
which dotnet-* skills the work will involve:

- **Always present:** `dotnet-fundamentals` (DI, Options, modern C#),
  `dotnet-tester` (tests are required for code changes), `dotnet-xmldocs`
  (public API additions/changes).
- **Always for review:** `dotnet-reviewer` (Phase 4).
- **Conditional on the stack the requirement touches:**
  - `dotnet-aspnet` — controllers, minimal APIs, middleware, auth, OpenAPI,
    health checks, CORS, rate limiting, ProblemDetails.
  - `ef-core` — DbContext, entities, LINQ, migrations, repository patterns.
  - `dotnet-sdk-builder` — generating a typed SDK / typed HTTP client /
    client library.
- **Conditional on the work itself:**
  - `dotnet-inspect` — whenever an external/platform/NuGet API surface needs
    to be inspected, diffed, or located.
  - `nuget-manager` — whenever a NuGet package is added, removed, or has its
    version changed.
- **Tie-breaker:** `dotnet` — when the right specialized skill is not obvious.

Always check `CLAUDE.md`, `AGENTS.md`, and `.github/copilot-instructions.md`
for project conventions and coding standards.

### Output

Present the user with:

- A summary of the requirement in your own words.
- The identified acceptance criteria.
- Any clarifying questions (if applicable).
- The affected projects and areas of the codebase.
- The list of required dotnet-* skills for this work.

Wait for user confirmation before proceeding to Phase 2.

---

## Phase 2 — Implementation Plan (Detail)

### Goal

Create a clear, trackable plan that breaks the requirement into discrete tasks.
Each task should be self-contained and include code, tests, and documentation.

### Steps

#### 2.1 Define Tasks

- Break the requirement into the smallest reasonable tasks.
- Each task should be completable independently (or with clear dependencies).
- Use descriptive kebab-case IDs for task tracking.
- Every task description must include enough detail to execute without
  referring back to the plan.

#### 2.2 Task Structure

Each task MUST address these three aspects:

1. **Production Code:** What code changes are needed?
2. **Tests:** What tests must be written or updated? (via `dotnet-tester`)
3. **Documentation:** What documentation needs updating? XML doc comments via
   `dotnet-xmldocs` for any public API touch; READMEs / docs as applicable.
   Mark as "n/a — internal only" only when truly not applicable.

Each task MUST also record its **Skill prerequisites** — the dotnet-* skills
that the task will invoke in Phase 3. Use the binding list from Phase 1.5.

#### 2.3 Dependencies

- Identify which tasks depend on others.
- Tasks without dependencies can be parallelized.
- Common .NET dependency patterns:
  - Domain / entity types before EF Core configuration
  - EF Core configuration & migrations before repositories / services
  - Services before controllers / minimal API endpoints
  - DTO / contract types before consumers
  - Package changes (`nuget-manager`) before code that uses them

#### 2.4 Parallelization Strategy

- Group independent tasks for parallel execution via sub-agents.
- Database schema / migration tasks should not be parallelized with other
  EF Core work.
- Prefer smaller, focused sub-agent tasks over large monolithic ones.

### Output

Present the user with:

- The complete task list with descriptions and per-task Skill-prerequisite
  checklists.
- A dependency graph (which tasks block which).
- The planned execution order.
- Which tasks will be parallelized.

Wait for user confirmation before proceeding to Phase 3.

---

## Phase 3 — Implementation (Detail)

### Goal

Execute the implementation plan using sub-agents for efficient parallel work,
while ensuring every required dotnet-* skill is invoked before its
corresponding artifact is produced.

### Steps

#### 3.1 Task Execution

For each task (or parallel group of independent tasks):

1. **Update task status** to `in_progress`.
2. **Step 0 — invoke every required dotnet-* skill** for this task (see the
   task's Skill-prerequisite checklist). One `Skill(...)` call per binding.
   No collapsing.
3. **Choose the right sub-agent type:**
   - `Explore` — for codebase research and analysis.
   - `general-purpose` — for complex multi-step code changes.
   - A task / build runner — for running builds, tests, linters.
4. **Provide complete context** to the sub-agent:
   - What to implement
   - Which files / projects to modify
   - What tests to write
   - What conventions to follow (`.editorconfig`, `Directory.Build.props`,
     project's CLAUDE.md)
   - The dotnet-* skills assigned to this task — pass them explicitly, since
     sub-agents are stateless and cannot see the main agent's skill state.
5. **Review sub-agent output** before moving to the next task.
6. **Update task status** to `done` only after every binding has been invoked
   and every artifact (code / tests / docs / package update) is produced or
   marked `n/a`.

#### 3.2 Code Quality (Production Code)

- **Always invoke `dotnet-fundamentals`** before writing production C#.
  Apply DI, Options pattern, configuration patterns, `IOptions<T>`, primary
  constructors, required properties, nullable reference types, and
  `CancellationToken` propagation.
- **Invoke `dotnet-aspnet`** before writing ASP.NET Core code (controllers,
  minimal APIs, middleware, auth, ProblemDetails, OpenAPI, health checks,
  CORS, rate limiting).
- **Invoke `ef-core`** before writing EF Core code (DbContext, entities,
  configurations, LINQ, migrations, repositories).
- **Invoke `dotnet-sdk-builder`** when building a typed SDK or HTTP client.
- **Invoke `dotnet-inspect`** when you need to verify an external API surface
  before depending on it.
- Follow existing code style and conventions.
- Do not introduce new dependencies unless explicitly required.
- Keep changes minimal and focused on the requirement.
- Use existing patterns found in the codebase.

#### 3.3 Testing

- **Always invoke `dotnet-tester`** before writing or updating tests. The
  skill enforces xUnit + FakeItEasy + AwesomeAssertions conventions and uses
  a second agent to identify missing test cases.
- Write tests that cover the new/changed behavior.
- Ensure existing tests still pass.
- Cover edge cases identified in Phase 1.

#### 3.4 Documentation

- **Always invoke `dotnet-xmldocs`** before adding or updating XML doc
  comments on public APIs (`<summary>`, `<param>`, `<returns>`, `<exception>`,
  `<remarks>`, etc.).
- Update README or other docs if the change affects usage.
- Keep documentation changes in sync with code changes.

#### 3.5 Build & Dependencies

- **Invoke `nuget-manager`** whenever you add, remove, or change versions of
  NuGet packages. The skill enforces the `dotnet` CLI, supports
  `Directory.Packages.props` central versions, and provides verification
  workflows (`dotnet add/remove package`, `dotnet list package --outdated`,
  `dotnet restore`).
- After all tasks are complete:
  - Run `dotnet build` for the affected projects / solution.
  - Run `dotnet test` (via `dotnet-tester` workflow).
  - Run any linters / analyzers configured in the repo.
  - Fix any failures before proceeding.

### Important Constraints

- **NEVER commit code.** The user will commit when ready.
- **NEVER skip tests.** Every code change must have corresponding tests via
  `dotnet-tester`.
- **NEVER skip XML docs** on public API additions/changes. Use
  `dotnet-xmldocs`.
- **Respect existing patterns.** Do not refactor unrelated code.

---

## Phase 4 — Review (Detail)

### Goal

Ensure implementation quality through an automated code review before the
user commits. In .NET projects, `dotnet-reviewer` is the canonical reviewer.

### Steps

#### 4.1 Launch Code Review

Launch a code-review sub-agent with these instructions:

- Invoke `dotnet-reviewer` (also in the main conversation).
- `dotnet-reviewer` reviews uncommitted working-tree changes or committed
  changes on the current feature branch vs. `main`, and produces a Markdown
  report under `docs/reviews/` with severity-tagged findings
  (`[Critical|Major|Minor|Suggestion|Nitpick][Security|Performance|Architecture|Code-Quality|Tests|.NET-Idioms]`)
  and fix suggestions.
- Review ALL changes made during this implementation session.
- Compare changes against the original requirement and acceptance criteria.
- Focus on substantive issues only (not style or formatting handled by
  `.editorconfig`).

#### 4.2 Review Criteria

The review covers:

1. **Correctness:** Does the implementation satisfy the requirement and all
   acceptance criteria?
2. **Completeness:** Are there missing edge cases, error handling, or
   untested paths?
3. **Test Coverage:** Do tests adequately cover the new/changed code?
4. **Documentation:** Is XML documentation accurate and up to date?
5. **Code Quality:** Are there bugs, security issues, or logic errors?
6. **.NET Idioms:** DI lifetimes, async/await, `ConfigureAwait(false)` in
   libraries, nullable reference types, `Ensure.*` guards, primary
   constructors, etc.
7. **Consistency:** Does the code follow project conventions and patterns?

#### 4.3 Evaluate Findings

After the review:

- **No issues found:** Proceed to Phase 5.
- **Minor issues found:** Fix them directly (still invoking the required
  dotnet-* skills before edits), then proceed to Phase 5.
- **Significant issues found:**
  1. Create new tasks for each finding (each with its own Skill-prerequisite
     checklist).
  2. Return to Phase 3 to address them.
  3. After fixing, run Phase 4 again (rework loop).

#### 4.4 Rework Loop

The rework loop (Phase 3 → Phase 4) continues until:

- The review finds no significant issues, OR
- The user explicitly approves the current state.

There is no hard limit on rework iterations, but if the same issues recur
after 2 rework cycles, consult the user for guidance.

---

## Phase 5 — Summary (Detail)

### Goal

Provide a clear, comprehensive summary so the user knows exactly what was
done and can review before committing.

### Steps

#### 5.1 Change Summary

Create a structured summary containing:

1. **Requirement:** Brief restatement of the original requirement.
2. **Files Modified:** List all files that were created or changed.
3. **Implementation Details:** What was implemented and key design decisions.
4. **Tests Added/Updated:** List all test files and what they cover.
5. **Documentation Changes:** List all XML doc / README / docs updates.
6. **Package Changes:** Any NuGet packages added, removed, or version-changed
   (cross-reference `Directory.Packages.props` if used).
7. **Decisions Made:** Any design decisions or trade-offs during
   implementation.
8. **Review Notes:** Key points from `dotnet-reviewer` output (link to the
   report under `docs/reviews/`).
9. **Things to Check:** Anything the user should manually verify before
   committing.

#### 5.2 Skill-Invocation Log

Reproduce the Skill-prerequisite checklist from Phase 2 for every task, each
entry resolved as `[x] <skill> — invoked at <evidence>`, `[n/a] <skill> —
did not apply (<reason>)`, or `[!] <skill> — NOT invoked` (a workflow
violation that must be named, prevent task completion, and trigger a
follow-up pass).

#### 5.3 Final Reminder

End with a clear reminder:

> All changes are ready for your review. When you are satisfied, please commit
> the changes yourself. The AI will not create any commits.

---

## General Guidelines

### Sub-Agent Best Practices

- Always provide **complete context** to sub-agents (they are stateless).
- Use `Explore` agents only for non-code research (this repo prefers
  `tokensave` for code exploration where available — see project `CLAUDE.md`).
- Use a build/test runner for `dotnet build` and `dotnet test`.
- Launch independent tasks in **parallel** for efficiency.
- Always pass the relevant dotnet-* skills to the sub-agent prompt.

### Git and Commit Rules

- **NEVER** run `git commit`, `git add -A`/`git add .`, or any commit-related
  commands.
- Stage files by name only when the user explicitly asks for staging. Refuse
  to stage secret-like files (`.env`, `credentials.json`, `*.pem`); warn if
  the user insists.
- **NEVER** create branches, tags, or push to remotes unless explicitly asked.
- If the user or a tool requests a commit, **skip it** and inform the user:
  *"Committing is your responsibility. I've skipped this commit request."*
- You MAY use `git diff`, `git status`, `git log`, and other read-only git
  commands for analysis.

### Handling Project-Specific Conventions

- Always check for instruction files at the start (`CLAUDE.md`, `AGENTS.md`,
  `.github/copilot-instructions.md`).
- Honour `.editorconfig`, `Directory.Build.props`, and
  `Directory.Packages.props`.
- In **library** code, async calls use `.ConfigureAwait(false)`. In **tests**,
  do not use `.ConfigureAwait(false)` (per project convention).
- Never use `.GetAwaiter().GetResult()`, `.Result`, or `.Wait()` to block on
  async code — ask the user if there is no alternative.
- Use `Ensure.NotNull(...)` / `Ensure.IsNotNullOrEmpty(...)` /
  `Ensure.IsNotNullOrWhitespace(...)` from `CreativeCoders.Core` for argument
  guards in libraries (per project convention).

### Skill Bindings — Quick Reference

| Binding | Skill | Phase |
|---------|-------|-------|
| Core C# / DI / Options / modern C# | `dotnet-fundamentals` | 3.2 |
| ASP.NET Core | `dotnet-aspnet` | 3.2 |
| EF Core | `ef-core` | 3.2 |
| SDK / typed HTTP client | `dotnet-sdk-builder` | 3.2 |
| API surface lookup | `dotnet-inspect` | 1.4, 3.2 |
| Tests | `dotnet-tester` | 3.3 |
| XML documentation | `dotnet-xmldocs` | 3.4 |
| NuGet packages | `nuget-manager` | 3.5 |
| Code review | `dotnet-reviewer` | 4 |
| Router (tie-breaker) | `dotnet` | any |

**Rule:** A binding MUST be invoked (via the `Skill` tool, in this
conversation, for this task) before its corresponding artifact is produced.
A listed binding is not an invoked binding.
