# dotnet-reviewer Skill — Design Spec

**Date:** 2026-04-30
**Status:** Design approved, ready for implementation planning
**Skill location:** `src/skills/csharp/dotnet-reviewer/`

## Purpose

A Copilot-compatible Agent Skill that performs structured .NET code reviews on either uncommitted working-tree changes or all committed changes on the current feature branch (relative to `main`). Activates **only on explicit name** (`dotnet-reviewer`, `dotnet code review`, `dotnet review`) — no generic review-phrase triggers.

Targets **.NET 10+ only**. The skill detects the concrete .NET version in use (via `global.json` and `*.csproj`) and applies version-aware checks. Earlier .NET versions are out of scope; the skill aborts with a clear message if it detects an SDK below 10.

## Activation & Invocation

- The skill never activates on generic review requests. The `description` frontmatter lists the explicit trigger phrases above; reviewers for other languages must not be hijacked.
- All skill content (frontmatter, body, references, scripts) is written in **English**.
- Default report language is **English**. The user may request another language at invocation; the skill honors that for the report only.

## Interactive Prompt (Step 1 of every run)

The skill begins by asking the user three things:

1. **Mode** — `uncommitted` (working tree vs. HEAD) or `branch` (committed changes on current branch vs. `main`).
2. **Tools** — for each of `build`, `format`, `test`: enable Y/N. Default is N for all three.
3. **Report language override** — optional; default English.

`main` is the fixed baseline for `branch` mode. If `main` does not exist, the skill aborts.

## High-Level Flow

```
explicit invocation
   → interactive prompt (mode, tools, language)
   → detect-dotnet-version.sh        (fail-fast if SDK < 10)
   → collect-diff.sh --mode <m>      (applies exclusions)
   → if loc>2000 OR files>50: ask user to choose strategy B|C|D
   → run-checks.sh (selected tools)  (build|format|test, JSON output)
   → review against version-aware checklists in references/
   → render report per references/report-format.md
   → write to docs/reviews/YYYY-MM-DD-<branch>-<mode>.md  (no auto-commit)
   → output: file path + 1-line summary
```

## Component Layout

```
src/skills/csharp/dotnet-reviewer/
├── SKILL.md                       # English; ~150–250 lines
├── LICENSE.txt                    # Apache 2.0
├── scripts/
│   ├── detect-dotnet-version.sh
│   ├── collect-diff.sh
│   └── run-checks.sh
├── references/
│   ├── severity-taxonomy.md
│   ├── report-format.md
│   ├── review-checklist-net10.md
│   ├── review-checklist-security.md
│   ├── review-checklist-performance.md
│   ├── review-checklist-architecture.md
│   └── review-checklist-code-quality.md
└── tests/
    ├── README.md
    ├── run-tests.sh
    ├── fixtures/
    │   ├── repo-net10/
    │   ├── repo-net8/
    │   ├── repo-no-git/
    │   ├── repo-malformed-csproj/
    │   ├── repo-large-diff/
    │   └── repo-empty-diff/
    ├── unit/
    │   ├── test-detect-version.sh
    │   ├── test-collect-diff.sh
    │   └── test-run-checks.sh
    └── integration/
        └── test-skill-flow.md
```

### SKILL.md (orchestrator)

- Frontmatter: `name: dotnet-reviewer`, `description` listing explicit triggers and stating "activates only on explicit name", `license: Complete terms in LICENSE.txt`.
- Sections: **When to Use** (with negative list), **Prerequisites** (`git`, `dotnet ≥ 10`, `bash`), **Workflow** (the 8-step decision tree), **Output Contract**, **Resource Index**.
- Kept under 500 lines; substance lives in `references/`.

### scripts/

All scripts: `--help`, deterministic exit codes, JSON to stdout, errors to stderr, no credential handling, no destructive operations.

| Script | Inputs | Output (JSON) | Responsibility |
|---|---|---|---|
| `detect-dotnet-version.sh` | repo root | `{sdk, target_frameworks[], project_files[]}` | Parse `global.json` + every `<TargetFramework(s)>` from `*.csproj`. Fail-fast (exit 4) if SDK < 10. |
| `collect-diff.sh` | `--mode uncommitted\|branch`, `--baseline main` | `{loc, files, file_list[], diff}` | Collect unified diff. For `branch` mode: `git diff main...HEAD`. Apply exclusions (see below). |
| `run-checks.sh` | `--build`, `--format`, `--test` (combinable) | `{build:{ok,warnings[],errors[]}, format:{ok,violations[]}, test:{ok,failed[],duration}}` | Run requested tools with deterministic flags; structured results, never raw logs. |

**Exclusions in `collect-diff.sh`:**
- Whatever `.gitignore` already excludes (gitignored files don't appear in `git diff` anyway, but the script never re-includes them).
- Plus: `*.min.js`, anything under `wwwroot/lib/**`.
- EF migrations and generated `.g.cs` files **are reviewed** (not excluded).

### references/

| File | Content |
|---|---|
| `severity-taxonomy.md` | Critical / Major / Minor / Suggestion / Nitpick — definitions + examples. Area tags: Security, Performance, Architecture, Code-Quality, Tests, .NET-Idioms. |
| `report-format.md` | Detailed Executive Summary schema (counts per severity, top-3 risks, changed LOC, scope description); finding schema `[Severity][Area] file:line` + description + recommendation + fix code block; report file naming. |
| `review-checklist-net10.md` | Version-specific items for .NET 10: Primary Constructors, Collection Expressions, Required Members, new LINQ APIs, etc. Selected by `detect-dotnet-version.sh` output. Future versions get their own `review-checklist-net<N>.md` file without SKILL.md changes. |
| `review-checklist-security.md` | Input validation, authn/authz, SQL-injection, secrets, OWASP mapping. |
| `review-checklist-performance.md` | Allocations, async/await anti-patterns, EF Core query pitfalls, Span/Memory usage, hot-path heuristics. |
| `review-checklist-architecture.md` | Layer violations, dependency direction, SOLID, DI misuse, pattern consistency. |
| `review-checklist-code-quality.md` | Naming, complexity, nullability, IDisposable, test-coverage hints. |

## Severity & Report Format

**Severity scale:** Critical / Major / Minor / Suggestion / Nitpick.
**Area tags:** Security, Performance, Architecture, Code-Quality, Tests, .NET-Idioms.

**Finding format:**

```
[Major][Security] src/Api/UserService.cs:42
<one-line description>

<recommendation paragraph>

```csharp
// fix suggestion (manual application — no auto-patch)
...
```
```

**Executive Summary** (detailed, top of every report):
- Counts per severity
- Top-3 risks (one line each)
- Changed lines of code, file count
- Review scope description (mode used, tools run, baseline, exclusions applied, .NET version detected)

**Severity mapping for tool outputs:**
- `dotnet build` errors → Critical
- `dotnet build` warnings → Minor
- `dotnet test` failures → **Critical**
- `dotnet format` violations → **Suggestion**

**Report path:** `docs/reviews/YYYY-MM-DD-<branch>-<mode>.md`. Never overwrite — append `-2`, `-3`, … if collision. The skill creates `docs/reviews/` if missing. **Never auto-commits.** User decides whether to commit.

## Large-Diff Handling

Threshold: **LOC > 2000 OR files > 50**.

When tripped, the skill asks the user to pick:
- **(B) Review everything** — warn about token cost in report header.
- **(C) Prioritize** — critical files first (heuristics: `*Service.cs`, `*Controller.cs`, files without sibling test files, security-related namespaces); rest summarized.
- **(D) Chunk file-by-file** — review per file, report grouped by file.

If C is chosen but no critical files can be identified, fall back to D and note the fallback in the report.

## Error Handling

### Script-level (deterministic, fail-fast)

| Condition | Exit | Behavior |
|---|---|---|
| Not a git repo | 2 | Skill aborts, informs user. |
| `main` missing (branch mode) | 3 | Skill aborts. |
| .NET SDK < 10 or not found | 4 | Skill aborts with detected version. |
| Malformed `global.json` / `*.csproj` | 5 | Skill lists offending file, asks user whether to proceed without version-awareness. |
| Empty diff | 0 (with empty payload) | Skill reports "no changes to review", exits cleanly. |
| `dotnet build` failure | 0 | JSON has `build.ok=false` + errors; review continues, errors become Critical findings. |
| `dotnet test` failure | 0 | JSON has `test.ok=false` + failures; failures become Critical findings. |
| `dotnet format` violations | 0 | JSON has `format.ok=false` + violations; violations become Suggestion findings. |

### Skill-level (graceful degradation)

| Condition | Behavior |
|---|---|
| Selected tool not installed | Log "X not available, skipping"; continue. |
| Strategy B chosen on huge diff | Warn about token load in report header. |
| Strategy C: no critical files identified | Fall back to D, note transparently in report. |
| Report path collision | Append numeric suffix; never overwrite. |
| `docs/reviews/` missing | Create it. |
| No `dotnet` toolchain at all | Run in pure-analysis mode; silently disable build/format/test, note in report. |

### Things the skill never does
- No auto-retries (deterministic failure surface).
- No destructive operations as "fixes" (no `git reset`, no deletions).
- No secrets in logs/reports (scripts never `cat .env`, never `printenv`).
- No `--no-verify`, no hook bypassing.

### Input safety
- User input (mode, tool flags) validated against a whitelist.
- File paths from diff are passed via arrays + `--`, never via shell substitution.
- No `eval`, no dynamic sourcing.

## Testing Strategy

**Unit tests (Bash, no framework dependencies):**
- Per script: happy path, every exit code, JSON shape assertions, edge cases.
- `dotnet` invocations in `run-checks.sh` tested via `PATH`-override mock that emits predictable outputs.

**Fixtures:** Six minimal git repos covering happy path (.NET 10), pre-10 SDK rejection, missing git, malformed csproj, large diff, empty diff.

**Smoke test (manual, documented):** `tests/integration/test-skill-flow.md` is a 10–15 step checklist a human runs end-to-end (explicit-name activation, prompt flow, report write, report format correctness). No LLM-in-loop CI — too non-deterministic and costly.

**Out of scope:**
- Review substance itself (what findings the LLM extracts) — not deterministic; we rely on human-reviewable checklists in `references/`.
- `dotnet` toolchain behavior — Microsoft's responsibility.

**Acceptance:**
- `bash tests/run-tests.sh` passes on macOS and Linux without `dotnet` SDK installed (mocks).
- Smoke checklist walked through once before the skill ships.

## Out of Scope (explicit)

- Auto-patching or auto-committing the report.
- Cross-repo reviews.
- Non-.NET code (TypeScript, Python, etc. — only mentioned, never reviewed).
- IDE integration.
- Automated LLM-driven review-quality testing.
- .NET versions below 10.

## Open Items for Implementation Plan

- Concrete content of each `review-checklist-*.md` (substance lives there; structure already fixed by `report-format.md`).
- Exact Bash assertion helper functions for `tests/run-tests.sh`.
- Distribution mechanism from `src/skills/csharp/dotnet-reviewer/` to a consumable install location (`.github/skills/` or `~/.github/skills/`) — currently treated as separate concern; this skill source lives at `src/skills/csharp/dotnet-reviewer/`.
