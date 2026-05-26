---
name: dotnet-implementer
description: >
  Iterative implementation workflow for .NET/C# requirements. Use when asked to
  implement a feature, user story, requirement, or change request in a .NET project.
  Guides through 5 phases: requirement review, implementation planning,
  sub-agent-driven implementation (code, tests, documentation), code review with
  rework loop, and final summary. Directly invokes the appropriate dotnet-*
  skills at each phase. Never commits code — the user always commits manually.
allowed-tools: Read Grep Glob Edit Create Task
---

# dotnet-implementer — Iterative .NET Requirement Implementation Flow

A structured workflow for implementing requirements end-to-end in .NET / C#
projects. Covers production code, tests, and documentation in every cycle.

> **CRITICAL RULE — NO COMMITS:** You must NEVER commit code or create git
> commits. The user always commits manually. If asked to commit, skip that
> request and inform the user that committing is their responsibility.

> **CRITICAL RULE — USE THE DOTNET SKILLS:** Never implement, test, document,
> or review with only built-in knowledge. The dotnet-* skills listed below are
> mandatory inputs to the corresponding phases. Invoke them via the `Skill`
> tool — listing or paraphrasing them does NOT count.

> **CRITICAL RULE — USER URGENCY DOES NOT WAIVE BINDINGS.** Phrases like
> "skip the ceremony", "just bang it out", "I'm in a hurry", "no need for the
> full workflow", "this is trivial", "no tests needed", or any deadline
> pressure are NOT permission to skip Step 0 invocations or any required
> binding. They are exactly the conditions this skill exists to survive.
> Acknowledge the deadline, then run the workflow at speed — do not collapse
> it. The only way to lawfully shrink a binding's work is via an `n/a` mark
> backed by an OBJECTIVE technical reason (see the `n/a` rules below); user
> preference, pressure, or speed are never valid `n/a` reasons.

## Flow Overview

```
Phase 1: Requirement Review
    ↓
Phase 2: Implementation Plan
    ↓
Phase 3: Implementation (Sub-Agents) ◄──┐
    ↓                                    │
Phase 4: Review (Sub-Agent)              │
    ↓ (rework needed?)──────────────────►┘
    ↓ (all good)
Phase 5: Summary
```

## Skill Map — Fixed Bindings

This workflow is .NET-only, so the skills it depends on are fixed. No
discovery, no slots. Each binding below is mandatory whenever the matching
work happens in the task.

| Binding | Skill(s)               | Used in |
|---------|------------------------|---------|
| Implementation — general C# / DI / Options / config / modern C# idioms | `dotnet-fundamentals`  | Phase 3 (always) |
| Implementation — ASP.NET Core (controllers, minimal APIs, middleware, auth, OpenAPI) | `dotnet-aspnet`        | Phase 3 (when ASP.NET Core is touched) |
| Implementation — EF Core (DbContext, entities, LINQ, migrations) | `dotnet-ef-core`       | Phase 3 (when EF Core is touched) |
| Implementation — SDK / client library generation | `dotnet-sdk-builder`   | Phase 3 (when building a typed SDK / HTTP client) |
| API surface lookup / NuGet & platform API inspection | `dotnet-inspect`       | Phase 1, Phase 3 (whenever an external/platform API is involved) |
| Tests | `dotnet-tester`        | Phase 3 (always, when code is written or changed) |
| Documentation (XML doc comments) | `dotnet-xmldocs`       | Phase 3 (always, for public API additions/changes) |
| Code review | `dotnet-reviewer`      | Phase 4 (always) |
| Build / dependencies / packages | `dotnet-nuget-manager` | Phase 3 (any change to `<PackageReference>`, `<PackageVersion>`, `Directory.Packages.props`, or any package add/remove/version edit — direct `*.csproj` editing of package lines is NOT a workaround) |
| Router (when the right .NET sub-skill is not obvious) | `dotnet`               | Any phase, as a tie-breaker |

**Invocation rule:** A binding is fulfilled only when its skill has been
invoked via the `Skill` tool in this conversation, for this task. Listing it,
paraphrasing it, or relying on prior knowledge does NOT count. The same rule
applies whether you do the work yourself or dispatch a sub-agent —
self-execution does not waive the invocation, and a sub-agent's invocation
does not waive it for the main agent's own follow-up edits.

**Multiple bindings per task:** If a task touches ASP.NET Core, writes tests,
and adds XML docs, that's three bindings — invoke `dotnet-aspnet`,
`dotnet-tester`, and `dotnet-xmldocs` independently. Never collapse them.
Invocations from a previous task do NOT carry over; re-invoke per task.

## Phase 1 — Requirement Review

Analyze the requirement before any code is written:

1. Read and understand the requirement thoroughly.
2. Identify acceptance criteria (explicit and implicit).
3. Clarify ambiguities — ask the user targeted questions.
4. Identify affected projects (`*.csproj`), components, files, and modules.
5. Check for existing tests, documentation, and related code.
6. Identify which dotnet-* skills the work will touch (see *Skill Map*).
   Use `dotnet-inspect` here whenever the requirement involves an
   external/platform API whose surface you need to verify.
7. Review `CLAUDE.md`, `AGENTS.md`, and any `.editorconfig` /
   `Directory.Build.props` / `Directory.Packages.props` files in the
   solution and follow the conventions found there.

**Output:** Confirmed understanding of the requirement, resolved ambiguities,
identified scope, and the list of dotnet-* skills required for the work.

## Phase 2 — Implementation Plan

Create a structured plan with trackable tasks:

1. Break the requirement into discrete implementation tasks.
2. Each task MUST include all three aspects:
   - **Production code** changes
   - **Test** additions or updates (via `dotnet-tester`)
   - **Documentation** updates (XML docs via `dotnet-xmldocs`; READMEs / docs
     as applicable)
3. **Each task MUST publish a Skill-prerequisite checklist** listing every
   dotnet-* skill it depends on (from Phase 1). The checklist is part of the
   plan text the user sees; it is not optional, and it is not collapsed even
   when a task touches every binding. Format:

   ```
   Task #N: <subject>
     Skill prerequisites (Step 0 of Phase 3):
       [ ] dotnet-fundamentals
       [ ] dotnet-aspnet          (controllers + middleware)
       [ ] dotnet-tester
       [ ] dotnet-xmldocs
       [ ] dotnet-nuget-manager   (adds Microsoft.Extensions.Http)
   ```

   When Step 0 fires in Phase 3, each `[ ]` is replaced with `[x]`. A skill
   that legitimately does not apply to a task is marked `n/a` with a one-line
   reason.

   **`n/a` criteria — strict.** A binding may be marked `n/a` ONLY when an
   objective technical fact makes the work impossible or empty for this task.
   Valid `n/a` reasons cite the codebase, not the user:

   - ✅ `n/a — task touches no public API members (internal sealed class)` for `dotnet-xmldocs`
   - ✅ `n/a — no NuGet packages added/removed/version-changed in this task` for `dotnet-nuget-manager`
   - ✅ `n/a — task is pure deletion of dead code with no behavior change` for `dotnet-tester`
   - ❌ NOT valid: `n/a — user said no tests`
   - ❌ NOT valid: `n/a — too small to test`
   - ❌ NOT valid: `n/a — trivial endpoint`
   - ❌ NOT valid: `n/a — no test project in repo` (creating the test project IS part of the task; bootstrap is not a free pass)
   - ❌ NOT valid: `n/a — logging/config wiring, no testable seam` (write at minimum an integration smoke test through `WebApplicationFactory` or equivalent; if even that is genuinely impossible, document WHY in code-referenced terms, not as a preference)

   If you cannot write the `n/a` reason in the form "no <artifact> exists in
   this task because <code-referenced fact>", the binding is NOT `n/a` —
   invoke the skill and do the work.

   **No avoidance-driven `n/a`.** You may not restructure the
   implementation specifically to escape a binding. If the natural form of
   the change introduces a public extension method, write the extension —
   you do not get to inline it into top-level statements just to mark
   `dotnet-xmldocs` as `n/a`. Same for: collapsing a service into private
   inline code to dodge tests, hand-rolling an HTTP client to dodge
   `dotnet-sdk-builder`, or hiding logic behind `internal` to dodge XML
   docs. The binding follows the natural shape of the work, not a shape
   chosen to minimise bindings.

   **Conditional bindings require diff-evidence to be `n/a`.** For bindings
   that are conditional on the work (e.g. `dotnet-ef-core`, `dotnet-sdk-builder`,
   `dotnet-inspect`), an `n/a` mark must cite what was actually checked,
   not assumed. Example: `n/a — scanned changed files; no DbContext,
   IQueryable, or migration changes` for `dotnet-ef-core`. A bare "task does not
   touch EF Core" is not enough — say how you confirmed it.
4. Define task dependencies (what must be done first).
5. Identify tasks that can be parallelized via sub-agents.

**Output:** Task list with dependencies and a per-task Skill-prerequisite
checklist. A task missing its checklist is not a valid plan entry.

## Phase 3 — Implementation

Execute tasks using sub-agents for parallel work where possible:

1. For each task (or group of independent tasks):
   - **Step 0 — invoke every required dotnet-* skill.** Before any `Write`,
     `Edit`, or code-producing `Bash` call for this task, call the `Skill`
     tool **once per binding** the task is assigned to. If the task touches
     production code + tests + docs, that is at minimum three calls
     (`dotnet-fundamentals` + `dotnet-tester` + `dotnet-xmldocs`), plus any
     stack-specific skill (`dotnet-aspnet`, `dotnet-ef-core`, `dotnet-sdk-builder`)
     and `dotnet-nuget-manager` if packages change. One invocation never substitutes
     for another. Wait for each skill's content to load and follow its
     workflow when producing the corresponding artifact. Skill invocations
     made for a previous task do NOT carry over; re-invoke per task.
   - Delegate to sub-agents where parallelism helps, passing the relevant
     dotnet-* skills as context. Sub-agent dispatch is **in addition to** —
     not instead of — Step 0: the main agent still needs the skill workflow
     loaded for any follow-up edits it makes itself.
   - Implement production code using `dotnet-fundamentals` plus any
     stack-specific skill (`dotnet-aspnet` for ASP.NET Core, `dotnet-ef-core` for
     EF Core, `dotnet-sdk-builder` for SDK / typed HTTP clients).
   - Write or update tests using `dotnet-tester`.
   - Add or update XML documentation using `dotnet-xmldocs` for every public
     API addition or change.
   - When packages are added, removed, or have versions changed, use
     `dotnet-nuget-manager` (enforces `dotnet` CLI, supports `Directory.Packages.props`,
     `dotnet list package --outdated`, etc.).
2. Run existing tests, linters, and the build to verify changes don't break
   anything. Use `dotnet-tester` for the test workflow and `dotnet-nuget-manager` for
   restore / package-related concerns.
3. Track task completion status. Only mark a task `completed` once every
   required binding has been invoked AND the corresponding artifact (code /
   tests / docs / package update) has been produced or explicitly recorded as
   `n/a` for this task.

**Important:** Respect existing project conventions, patterns, and tooling
(see `.editorconfig`, `Directory.Build.props`, `Directory.Packages.props`,
`CLAUDE.md`).

**Artifact-substance bar.** Invoking a skill is necessary but not
sufficient — the produced artifact must reflect the skill's guidance:

- A `dotnet-tester` test that asserts nothing meaningful (e.g.
  `Assert.True(true);`, a `WebApplicationFactory` smoke test that creates a
  client and asserts nothing about the new behavior) does NOT satisfy the
  binding. The test must assert at least one behavior introduced or
  affected by the change in this task.
- A `dotnet-xmldocs` invocation that produces empty `<summary />` tags or
  copies the member name into the summary does NOT satisfy the binding.
  Summaries must describe behavior; `<param>`, `<returns>`, and
  `<exception>` tags must be present where applicable.
- A `dotnet-nuget-manager` invocation followed by hand-editing
  `<PackageReference>` lines anyway does NOT satisfy the binding — use the
  `dotnet` CLI as the skill prescribes.
- A `dotnet-reviewer` invocation that does not produce a Markdown report
  under `docs/reviews/` does NOT satisfy the binding.

If an artifact does not meet the substance bar, the binding is `[!]`, not
`[x]`, even though `Skill(...)` fired.

### Worked example — beginning a task with multiple bindings

Task #N: "Add a `GET /users/{id}` endpoint that loads the user via EF Core,
returns a DTO, is covered by tests, and is documented."

Bindings: `dotnet-fundamentals`, `dotnet-aspnet`, `dotnet-ef-core`, `dotnet-tester`,
`dotnet-xmldocs`.

Correct opening of the task:

```
TaskUpdate(taskId=N, status=in_progress)
Skill(skill="dotnet-fundamentals")  ← DI, Options, modern C#
Skill(skill="dotnet-aspnet")        ← controller / minimal API + ProblemDetails
Skill(skill="dotnet-ef-core")              ← DbContext usage, async query
Skill(skill="dotnet-tester")        ← xUnit + FakeItEasy + AwesomeAssertions
Skill(skill="dotnet-xmldocs")       ← XML doc comments on public members
# ...follow each invoked skill's workflow...
Write(file_path=".../UsersController.cs", content=...)
Write(file_path=".../UsersControllerTests.cs", content=...)
Edit(file_path=".../UsersService.cs", ...)
```

Notes:

- Five bindings → five `Skill(...)` calls. No collapsing.
- If you dispatch a sub-agent for any binding, the `Skill(...)` call still
  happens here in the main conversation first, and the skill name is also
  passed to the sub-agent prompt.
- Omitting any `Skill(...)` line above is the exact failure mode this section
  exists to prevent.

## Phase 4 — Review

Run a thorough code review using a sub-agent:

1. Launch a code-review sub-agent that uses `dotnet-reviewer`. This skill
   produces a Markdown report under `docs/reviews/` with severity-tagged
   findings. Invoke `dotnet-reviewer` via the `Skill` tool in the main
   conversation **and** pass it to the sub-agent prompt.
2. The review checks for:
   - Correctness and completeness against the requirement
   - Test coverage for new/changed code
   - Documentation accuracy (XML docs included)
   - Code quality, potential bugs, security issues, and .NET idioms
3. Evaluate review findings:
   - **Rework needed:** Create new tasks for findings and return to **Phase 3**.
     Each new task gets its own Skill-prerequisite checklist (Phase 2 rules
     apply).
   - **All good:** Proceed to **Phase 5**.

## Phase 5 — Summary

Provide a comprehensive summary of all work done:

1. List all files created or modified.
2. Describe what was implemented and why.
3. List all tests added or updated.
4. List all documentation changes (XML docs, READMEs, etc.).
5. Note any decisions made during implementation.
6. Highlight anything the user should review before committing.
7. **Publish the Skill-invocation log.** For every task in the plan,
   reproduce the Skill-prerequisite checklist from Phase 2 with each entry
   resolved. Each line MUST carry one of three states, with evidence:

   - `[x] <skill> — invoked at <evidence>` — `<evidence>` is a verifiable
     pointer such as "turn N", "before Write of `<file>`", or the exact
     `Skill(skill="…")` call. Generic phrases like "considered" or "applied"
     are NOT acceptable evidence.
   - `[n/a] <skill> — did not apply (<reason>)` — only valid when the binding
     legitimately did not apply to this task.
   - `[!] <skill> — NOT invoked` — a workflow violation. **`[!]` is NOT a
     shipping path.** It is an emergency disclosure, not an escape hatch.
     Whenever this state appears you MUST:
     (a) name it explicitly here as a violation,
     (b) mark the affected task as **INCOMPLETE** in your prose summary (do
         not call the work "done" — the user is reading this to decide
         whether to commit, and `[!]` means "do not commit yet"),
     (c) STOP the workflow and offer to re-enter Phase 3 immediately to
         re-run the missing skill's workflow on the produced artifact, and
     (d) make the recommendation actionable: name the exact `Skill(...)`
         call and the file(s) that still need its workflow applied.
     An agent that ships `[!]` and stops is shipping broken work. Phase 5
     does not "complete" while any `[!]` is present.

   The log is mandatory even when every binding was invoked correctly — it
   is the audit trail for the user.

   **No back-dated evidence.** `[x]` evidence must be a real, ordered turn
   that the user can verify against the transcript. If the `Skill(...)` call
   came AFTER the artifact was written, that is `[!]`, not `[x]` — invocation
   order matters because the skill's workflow is supposed to shape the
   artifact, not certify it after the fact.

> **Reminder:** The user will commit the changes themselves. Do NOT create any
> commits.

## Red Flags — Do Not Skip the dotnet-* Skills

If you catch yourself thinking any of these, STOP and invoke the required
skill:

| Rationalization                                                                                   | Reality                                                                                                                                                                                                                                                                    |
|---------------------------------------------------------------------------------------------------|----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| "I already know C# / ASP.NET / EF Core well"                                                      | The skill encodes project-specific conventions and modern .NET idioms you may not be applying. Invoke it.                                                                                                                                                                  |
| "It's a tiny change, no skill needed"                                                             | Small changes still follow the stack's patterns and XML-doc rules. Invoke it.                                                                                                                                                                                              |
| "I'll just use built-in knowledge for the test"                                                   | `dotnet-tester` is mandatory for tests — xUnit + FakeItEasy + AwesomeAssertions conventions live there.                                                                                                                                                                    |
| "XML docs are obvious"                                                                            | `dotnet-xmldocs` exists for a reason. Public API additions/changes go through it.                                                                                                                                                                                          |
| "I can review the code myself"                                                                    | Phase 4 is `dotnet-reviewer`. Self-review does not produce the severity-tagged Markdown report.                                                                                                                                                                            |
| "I'll add the NuGet package by editing the csproj"                                                | Package changes go through `dotnet-nuget-manager` (uses `dotnet` CLI, respects `Directory.Packages.props`).                                                                                                                                                                |
| "The sub-agent will figure it out"                                                                | Sub-agents are stateless — pass them the required dotnet-* skills explicitly.                                                                                                                                                                                              |
| "I already listed the skill in the plan"                                                          | A listed skill is not an invoked skill. The binding is fulfilled only when the `Skill` tool has fired in this conversation for this task.                                                                                                                                  |
| "I'll just do this small bit myself instead of dispatching"                                       | Self-execution does NOT waive the binding. Invoke the skill first, then write.                                                                                                                                                                                             |
| "One skill covers everything in this task"                                                        | Each binding is its own workflow. Implementation, testing, documentation, build, and review are independent — never collapse them.                                                                                                                                         |
| "The skill was invoked in the previous task"                                                      | Invocations do NOT carry over between tasks. Re-invoke at the start of every task the binding applies to.                                                                                                                                                                  |
| "I'll pick the right .NET skill from memory"                                                      | If the right sub-skill is not obvious, invoke `dotnet` first — it routes to the correct specialized skill.                                                                                                                                                                 |
| "User said 'skip the ceremony' / 'just bang it out' / 'I'm in a hurry'"                           | User urgency does not waive bindings. See the CRITICAL RULE at the top. Acknowledge the deadline, then run the workflow at speed — do not collapse it.                                                                                                                     |
| "User said 'no tests'"                                                                            | User preference is not a valid `n/a` reason. Either an objective technical reason for `n/a` exists in the code, or `dotnet-tester` is required. If the user genuinely wants to skip tests, name it as a violation and let them decide, do not silently absorb it as `n/a`. |
| "I'll mark `dotnet-tester` n/a — it's just config wiring / a one-liner / a lambda"                | Not valid. See `n/a` criteria in Phase 2: `n/a` requires a code-referenced technical fact, not subjective triviality. Lambda endpoints, config wiring, and small changes still get at least an integration smoke test.                                                     |
| "No test project exists, so tests are n/a"                                                        | Not valid. Creating the test project is part of the task. Missing infrastructure is not a free pass.                                                                                                                                                                       |
| "I'll just edit `<PackageReference>` in the csproj — that's not really 'adding a package'"        | It IS, and the Skill Map row for `dotnet-nuget-manager` explicitly covers any `<PackageReference>` / `<PackageVersion>` / `Directory.Packages.props` change. Direct csproj package edits are the loophole this row exists to close.                                        |
| "I'll ship with `[!] NOT invoked` and a recommended follow-up pass"                               | `[!]` is NOT a shipping path. It marks the task INCOMPLETE. Re-enter Phase 3 and run the missing skill's workflow before Phase 5 completes.                                                                                                                                |
| "The sub-agent invoked the skill — that covers my own follow-up edits"                            | It does not. Sub-agent invocations do not waive the main agent's Step 0 for its own Write/Edit calls. Re-invoke before your own follow-up edits, every time.                                                                                                               |
| "I can write the `Skill(...)` call in the Phase 5 log even if I made it after the Write"          | No. Evidence must be a real ordered turn. Invocation-after-artifact is `[!]`, not `[x]` — the skill is supposed to shape the artifact, not rubber-stamp it.                                                                                                                |
| "External API is well-known, no need for `dotnet-inspect`"                                        | If "well-known" means you remember it from training, invoke `dotnet-inspect` — versions drift, APIs change, training data is stale. The skill is fast; the bug from a hallucinated signature is not.                                                                       |
| "I'll just inline this so there's no public extension and `dotnet-xmldocs` becomes n/a"           | Avoidance-driven `n/a` is forbidden. See Phase 2 `n/a` criteria — the binding follows the natural shape of the work, not a shape chosen to minimise bindings. If a public extension method is the natural form, write it and document it.                                  |
| "I'll write a `WebApplicationFactory` test that asserts nothing — that satisfies `dotnet-tester`" | It does not. See the Artifact-substance bar in Phase 3 — a test that asserts nothing about the change is `[!]`, not `[x]`.                                                                                                                                                 |
| "I'll invoke `dotnet-nuget-manager` then hand-edit the csproj anyway"                             | Skill invocation without following the skill's CLI workflow does not satisfy the binding. See the Artifact-substance bar.                                                                                                                                                  |
| "I'll batch all five `Skill(...)` calls up front, then write everything"                          | Bulk loading is fine for the invocation order, but each skill's workflow must actually shape the corresponding artifact. If you batched, you must afterwards re-check each artifact against each loaded skill's guidance before declaring `[x]`.                           |
| "Phase 1 is just clarification — I can speed-run it"                                              | Phase 1 is where ambiguities surface. A speed-run that produces no clarifying questions on an ambiguous requirement is a failure mode the user will pay for later. If the requirement is truly unambiguous, say so explicitly; do not skip the check.                      |

---

For detailed guidance on each phase, see
[references/REFERENCE.md](references/REFERENCE.md).
