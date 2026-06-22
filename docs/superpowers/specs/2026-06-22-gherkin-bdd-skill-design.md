# Design: `gherkin-bdd` Agent Skill

**Date:** 2026-06-22
**Status:** Approved (design) — pending spec review
**Location in repo:** `src/skills/general/gherkin-bdd/`

## Purpose

A language-agnostic Agent Skill that helps users **author Gherkin `.feature` files**
*and* **implement runnable BDD tests** from them. The skill teaches Gherkin syntax
and best practices, then detects the project's stack and guides framework-specific
step-definition implementation and test execution.

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
- Running the BDD suite and verifying results.

Out of scope (YAGNI):
- Python (`behave`) — explicitly excluded.
- Generating application/production code; the skill targets specs and test glue only.
- A bundled LICENSE.txt (kept consistent with existing `general/` skills, which have none).

## Skill Discovery (frontmatter)

```yaml
---
name: gherkin-bdd
description: Authoring Gherkin feature files and implementing BDD tests. Use when
  asked to write .feature files, Given/When/Then scenarios, scenario outlines, BDD
  specs, or to implement step definitions/bindings and wire up a BDD runner
  (Reqnroll/SpecFlow, Cucumber-JVM, Cucumber.js). Covers Gherkin syntax, best
  practices, anti-patterns, and framework-specific step implementation.
---
```

## Structure

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
3. **Gherkin Core** — the structural keywords and Given-When-Then rules; declarative
   vs. imperative guidance.
4. **Best Practices & Anti-Patterns** — compact table (one behavior per scenario,
   single `When`, reusable/parameterized steps, no UI-implementation detail in steps,
   business language, tag discipline). Links to `references/gherkin-style.md` for depth.
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

## Design Rationale

- **Single skill, not per-language skills.** One discovery hit for "Gherkin/BDD";
  framework-specific detail stays out of context until progressive loading pulls the
  relevant reference. Avoids 3 near-duplicate skills.
- **`general/` category.** The Gherkin authoring core is language-neutral; only the
  implementation references are framework-bound, and they live in `references/`.
- **Detection-then-route.** Keeps SKILL.md short and lets the agent pick the right
  implementation path from project evidence rather than asking the user.

## Success Criteria

- Loaded automatically when a prompt mentions Gherkin, `.feature`, Given/When/Then,
  BDD, step definitions, scenario outlines, or the named frameworks.
- Produces valid Gherkin following the documented best practices.
- Generates correct, runnable step definitions for whichever of the three frameworks
  the project uses, and runs the suite to verify.
- SKILL.md stays under the 500-line guideline (target < 200); large detail lives in
  `references/`.
- Passes the repo's skill validation checklist (valid frontmatter, lowercase-hyphen
  name, relative resource paths, no secrets).

## Validation

- Manual review of SKILL.md against the project `CLAUDE.md` validation checklist.
- Sanity-check each reference against current framework conventions (Reqnroll
  attributes, Cucumber-JVM annotations, Cucumber.js config).
- Confirm `example.feature` parses as valid Gherkin.
