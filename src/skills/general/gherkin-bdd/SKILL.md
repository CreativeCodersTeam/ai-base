---
name: gherkin-bdd
description: Authoring Gherkin feature files and implementing BDD tests. Use when asked to write .feature files, Given/When/Then scenarios, scenario outlines, BDD specs, or to implement step definitions/bindings and wire up a BDD runner (Reqnroll/SpecFlow, Cucumber-JVM, Cucumber.js). Covers Gherkin syntax, best practices, anti-patterns, and framework-specific step implementation. For reviewing existing BDD tests, use gherkin-bdd-reviewer instead.
---

# Gherkin & BDD Skill

Author well-structured Gherkin `.feature` files and turn them into runnable BDD
tests (step definitions + runner) for Reqnroll (.NET), Cucumber-JVM (Java), or
Cucumber.js (TypeScript).

## When to Use This Skill

- Writing or refining `.feature` files, scenarios, or scenario outlines.
- Translating acceptance criteria / requirements into Given-When-Then specs.
- Implementing step definitions / bindings and wiring up a BDD test runner.
- Running and verifying a BDD suite.

Do **not** use this skill for reviewing/auditing existing BDD tests — use
`gherkin-bdd-reviewer` for that.

## Rule Precedence (read first)

Repo-local Gherkin/BDD conventions **always override** this skill's built-in rules
when they conflict. The built-in rules are the fallback for anything the repo does
not specify. Before authoring or implementing, scan the repo and adopt existing
conventions. Check these sources (first match wins, in order):

1. Project instruction files: `CLAUDE.md`, `AGENTS.md`,
   `.github/copilot-instructions.md`, and any `*.md` style/contribution guide that
   mentions Gherkin or BDD.
2. Linter/formatter config: `.gherkin-lintrc` / `gherkin-lint` config, `.editorconfig`
   rules for `*.feature`.
3. Existing `.feature` files and step-definition folders — infer the established
   style (spoken language tag, file/scenario naming, step phrasing, tag taxonomy)
   and follow it.
4. Framework config that constrains conventions: `reqnroll.json`/`specflow.json`,
   `cucumber.js`/`cucumber.json`, `@CucumberOptions`.

When a repo convention is silent on a point, fall back to the rules below. When the
repo conflicts with a rule below, follow the repo and note the override to the user.

## Gherkin Core

A feature file uses these keywords:

- `Feature:` — the capability under test (one per file). Optional free-text
  description lines follow.
- `Scenario:` — one concrete example of behavior.
- `Scenario Outline:` + `Examples:` — a parameterized scenario run once per data row,
  using `<placeholder>` tokens.
- `Background:` — steps run before every scenario in the file (shared setup only).
- `Given` (context/preconditions), `When` (the single action under test),
  `Then` (observable outcome). `And` / `But` continue the previous keyword.
- Tags (`@smoke`, `@wip`) attach metadata to features/scenarios for filtering.

Write **declarative** business-language steps ("Given the user has an active
account"), not **imperative** UI steps ("Given I click the #login button"). Keep the
solution domain out of the spec.

## Best Practices & Anti-Patterns

This table is the **canonical fallback rule set** (subordinate to repo conventions
per *Rule Precedence*). See `references/gherkin-style.md` for depth and examples.

| Rule | Anti-pattern it prevents |
|---|---|
| One behavior per scenario | Multiple unrelated assertions in one scenario |
| Exactly one `When` per scenario | Chained actions hiding what's actually tested |
| Declarative, business-language steps | UI/implementation detail (clicks, selectors, SQL) |
| Reusable, parameterized steps | Near-duplicate steps differing only by a literal |
| `Background` only for shared setup | Background steps used by some scenarios only |
| Meaningful tags, no dead tags | Tag sprawl / tags no runner uses |
| Independent scenarios | Scenarios depending on execution order/shared state |

## Workflow

1. **Scan repo conventions** — apply *Rule Precedence* above.
2. **Write/refine the `.feature`** — apply Gherkin Core + Best Practices (or repo
   conventions where they differ). Use `assets/example.feature` as a template.
3. **Detect the stack** — see *Framework Detection* below.
4. **Load the matching framework reference** — read the relevant file under
   `references/` for setup, step-definition syntax, hooks, and the run command.
5. **Generate step definitions** — implement bindings for each step; reuse existing
   steps before adding new ones.
6. **Run the suite** — use the run command from the framework reference.
7. **Verify** — confirm scenarios pass; fix undefined/ambiguous steps.

## Framework Detection

| Signal | Framework | Reference |
|---|---|---|
| `.csproj` referencing `Reqnroll` or `SpecFlow` | Reqnroll (.NET) | `references/reqnroll-dotnet.md` |
| `pom.xml` / `build.gradle` with `io.cucumber` | Cucumber-JVM | `references/cucumber-jvm.md` |
| `package.json` with `@cucumber/cucumber` | Cucumber.js | `references/cucumber-js.md` |

If no BDD framework is present, recommend the conventional choice for the project's
language and follow the matching reference's setup section.
