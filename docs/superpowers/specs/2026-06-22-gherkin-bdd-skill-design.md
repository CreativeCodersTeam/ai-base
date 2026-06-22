# Design: Gherkin/BDD Agent Skills (two skills)

**Date:** 2026-06-22
**Status:** Approved (design) — pending spec review
**Location in repo:** `src/skills/general/`

## Purpose

Two complementary, language-agnostic Agent Skills:

1. **`gherkin-bdd`** — *authoring* skill. Helps users write Gherkin `.feature` files
   and implement runnable BDD tests (step definitions + runner) from them. Owns the
   canonical Gherkin/BDD standards (syntax, best practices, anti-patterns) and the
   framework-specific implementation references.

2. **`gherkin-bdd-reviewer`** — *review* skill. Reviews existing Gherkin feature
   files and BDD step definitions against the standards owned by `gherkin-bdd`,
   and produces a structured Markdown report with severity-tagged findings.

The standards live in **one place** (`gherkin-bdd`). The reviewer invokes the
authoring skill to load those standards, then applies them as review criteria —
so the rules never drift between the two skills.

## Scope

In scope:
- Gherkin authoring: `Feature`, `Scenario`, `Background`, `Scenario Outline`,
  `Examples`, tags, `Given`/`When`/`Then`/`And`/`But`.
- Gherkin best practices and anti-patterns (declarative vs. imperative, one behavior
  per scenario, reusable steps, avoiding UI-coupled steps).
- BDD test implementation across three ecosystems:
  - Reqnroll (.NET / C#)
  - Cucumber-JVM (Java + JUnit)
  - Cucumber.js (TypeScript / JavaScript)
- Stack detection (which project files indicate which framework) and routing to the
  correct reference.
- Running the BDD suite and verifying results (authoring skill).
- Structured review of feature files and step definitions with a Markdown report
  (review skill).

Out of scope (YAGNI):
- Python (`behave`) — explicitly excluded.
- Generating application/production code; the skills target specs and test glue only.
- Bundled LICENSE.txt (kept consistent with existing `general/` skills, which have none).

---

## Skill 1: `gherkin-bdd` (authoring)

### Discovery (frontmatter)

```yaml
---
name: gherkin-bdd
description: Authoring Gherkin feature files and implementing BDD tests. Use when
  asked to write .feature files, Given/When/Then scenarios, scenario outlines, BDD
  specs, or to implement step definitions/bindings and wire up a BDD runner
  (Reqnroll/SpecFlow, Cucumber-JVM, Cucumber.js). Covers Gherkin syntax, best
  practices, anti-patterns, and framework-specific step implementation. For
  reviewing existing BDD tests, use gherkin-bdd-reviewer instead.
---
```

### Structure

```
src/skills/general/gherkin-bdd/
├── SKILL.md                      # < 200 lines: core Gherkin + workflow + detection
├── references/
│   ├── gherkin-style.md          # In-depth style rules, good vs. bad examples
│   ├── reqnroll-dotnet.md        # Setup, [Binding], step defs, hooks, runner
│   ├── cucumber-jvm.md           # Glue code, @Given/@When/@Then, JUnit runner
│   └── cucumber-js.md            # Cucumber.js/TS, World, hooks, config
└── assets/
    └── example.feature           # Reference feature file used as-is
```

### SKILL.md sections

1. **Title + overview** — one-paragraph summary of what the skill enables.
2. **When to Use This Skill** — trigger scenarios reinforcing the description.
3. **Gherkin Core** — structural keywords and Given-When-Then rules; declarative
   vs. imperative guidance.
4. **Best Practices & Anti-Patterns** — compact table (one behavior per scenario,
   single `When`, reusable/parameterized steps, no UI-implementation detail in steps,
   business language, tag discipline). This table is the **canonical rule set** the
   reviewer consumes. Links to `references/gherkin-style.md` for depth.
5. **Workflow** — numbered: (1) write/refine `.feature`, (2) detect stack,
   (3) load matching framework reference, (4) generate step definitions,
   (5) run the suite, (6) verify output. Each step references the relevant doc.
6. **Framework Detection** — table mapping project signals to reference:

   | Signal | Framework | Reference |
   |---|---|---|
   | `.csproj` with `Reqnroll`/`SpecFlow` package | Reqnroll (.NET) | `references/reqnroll-dotnet.md` |
   | `pom.xml`/`build.gradle` with `io.cucumber` | Cucumber-JVM | `references/cucumber-jvm.md` |
   | `package.json` with `@cucumber/cucumber` | Cucumber.js | `references/cucumber-js.md` |

   If no BDD framework is present, recommend the conventional choice for the detected
   language and link the matching reference for setup.

### references/ content (loaded on demand)

- **gherkin-style.md** — extended authoring rules, paired good/bad examples, scenario
  outline patterns, tag taxonomy, Background usage guidance.
- **reqnroll-dotnet.md** — NuGet packages, project setup, `[Binding]` classes,
  `[Given]/[When]/[Then]` attributes, step argument transforms, hooks
  (`[BeforeScenario]` etc.), `dotnet test` execution.
- **cucumber-jvm.md** — Maven/Gradle deps, glue package layout, annotated step
  methods, `@CucumberOptions`/JUnit Platform runner, hooks, `mvn test` execution.
- **cucumber-js.md** — npm deps, `cucumber.js`/`cucumber.json` config, step
  definitions in TS, the World object, hooks, `npx cucumber-js` execution.

### assets/

- **example.feature** — a small, well-formed feature file (declarative style,
  scenario outline, tags) used verbatim as a teaching/reference template.

---

## Skill 2: `gherkin-bdd-reviewer` (review)

### Discovery (frontmatter)

```yaml
---
name: gherkin-bdd-reviewer
description: Reviews existing Gherkin feature files and BDD step definitions against
  Gherkin best practices and anti-patterns. Use when asked to review .feature files,
  audit BDD scenarios, check Given/When/Then quality, or assess step-definition
  reuse and binding correctness (Reqnroll, Cucumber-JVM, Cucumber.js). Produces a
  severity-tagged Markdown report. Must NOT activate on generic "review my code"
  requests. For writing or implementing BDD tests, use gherkin-bdd instead.
---
```

### How it uses `gherkin-bdd`

The reviewer does **not** duplicate the rules. Its SKILL.md instructs the agent to
**invoke the `gherkin-bdd` skill** (via the Skill tool) to load the canonical Gherkin
standards and the relevant framework reference, then evaluate the target files
against them. This single-sources the standards: editing `gherkin-bdd` automatically
changes what the reviewer enforces.

### Structure

```
src/skills/general/gherkin-bdd-reviewer/
└── SKILL.md                      # Review workflow, severity scheme, report format
```

No own `references/` — the standards come from `gherkin-bdd`.

### SKILL.md sections

1. **Title + overview.**
2. **When to Use This Skill** — review/audit triggers; explicit non-activation on
   generic "review my code" (mirrors `dotnet-reviewer` guardrail).
3. **Load Standards** — first step: invoke `gherkin-bdd` to obtain the best-practice
   rule set and detect/route to the framework reference for the project under review.
4. **Review Workflow** — (1) locate `.feature` files and their step definitions,
   (2) check each against the authoring rules (declarative style, single behavior,
   step reuse, business language, tags, Background misuse), (3) check step-definition
   bindings for correctness, undefined/duplicate/ambiguous steps, and dead steps,
   (4) compile findings.
5. **Severity & Categories** — finding tags modeled on `dotnet-reviewer`:
   `[Critical|Major|Minor|Suggestion|Nitpick]` × `[Gherkin-Style|Step-Defs|Coverage|Maintainability]`.
6. **Report Output** — write a Markdown report under `docs/reviews/` with
   severity-tagged findings, file/line references, and concrete fix suggestions.

---

## Design Rationale

- **Two skills, one rule set.** Authoring and reviewing are distinct user intents and
  warrant separate discovery hits. Keeping the rules only in `gherkin-bdd` and having
  the reviewer invoke it prevents standards drift.
- **Single skill per ecosystem avoided.** One authoring skill with framework
  references keeps framework detail out of context until progressive loading needs it.
- **`general/` category.** The Gherkin core is language-neutral; only the
  implementation references are framework-bound, and they live in `references/`.
- **Reviewer follows repo convention.** Severity-tagged Markdown report under
  `docs/reviews/`, with a non-activation guardrail, matching `dotnet-reviewer`.

## Success Criteria

- `gherkin-bdd` loads on prompts about writing Gherkin/`.feature`/BDD/step
  definitions; produces valid Gherkin and correct, runnable step definitions for the
  detected framework, and runs the suite to verify.
- `gherkin-bdd-reviewer` loads on review/audit prompts (not generic code review),
  invokes `gherkin-bdd` for standards, and emits a severity-tagged Markdown report
  under `docs/reviews/`.
- Both SKILL.md files stay under the 500-line guideline (authoring target < 200);
  large detail lives in `references/`.
- Both pass the repo's skill validation checklist (valid frontmatter,
  lowercase-hyphen name, relative resource paths, no secrets).

## Validation

- Manual review of both SKILL.md files against the project `CLAUDE.md` checklist.
- Sanity-check each reference against current framework conventions (Reqnroll
  attributes, Cucumber-JVM annotations, Cucumber.js config).
- Confirm `example.feature` parses as valid Gherkin.
- Confirm the reviewer's "Load Standards" step correctly references `gherkin-bdd`
  by name and that the two skills' descriptions cross-link without overlapping
  triggers.
