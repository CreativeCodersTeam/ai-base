---
name: gherkin-bdd-reviewer
description: Reviews existing Gherkin feature files and BDD step definitions against Gherkin best practices and anti-patterns. Use when asked to review .feature files, audit BDD scenarios, check Given/When/Then quality, or assess step-definition reuse and binding correctness (Reqnroll, Cucumber-JVM, Cucumber.js). Produces a severity-tagged Markdown report (written under docs/reviews/ by default, or inline on request). Must NOT activate on generic "review my code" requests. For writing or implementing BDD tests, use gherkin-bdd instead.
license: MIT
---

# Gherkin & BDD Reviewer Skill

Reviews existing Gherkin feature files and their step definitions and produces a
structured, severity-tagged Markdown report.

## When to Use This Skill

- Reviewing or auditing `.feature` files or BDD scenarios.
- Checking Given/When/Then quality, scenario design, or tag usage.
- Assessing step-definition reuse and binding correctness.

Do **NOT** activate on a generic "review my code" request — that belongs to a code
review skill. Only activate for Gherkin/BDD-focused review. For writing or
implementing BDD tests, use `gherkin-bdd`.

For a **mixed request** (review existing scenarios *and* author new ones), review
first with this skill, then switch to `gherkin-bdd` for the authoring part.

## Load Standards First

This skill owns **no rules of its own**. Before reviewing:

1. Load the `gherkin-bdd` skill to get the canonical Gherkin best-practice rule set
   and the framework reference for the project under review. If your platform does not
   auto-load it, read the `gherkin-bdd` skill's `SKILL.md` and
   `references/gherkin-style.md` directly so you never review without the rule set.
2. Adopt `gherkin-bdd`'s **Rule Precedence**: judge against repo-local conventions
   first, and the built-in rules only as fallback. A scenario that breaks a built-in
   rule but matches an explicit repo convention is **not** a finding; breaking an
   established repo convention **is** a finding.

## Review Workflow

1. **Locate artifacts** — find `.feature` files and their step-definition folders.
2. **Detect the stack** — use `gherkin-bdd`'s Framework Detection to know which step
   syntax applies.
3. **Review feature files** against the loaded standards: declarative style, one
   behavior per scenario, single `When`, step reuse, business language, tag
   discipline, correct `Background` usage, scenario independence.
4. **Review step definitions**: undefined steps, ambiguous/duplicate step patterns,
   dead (unused) steps, incorrect bindings, leaked implementation detail.
5. **Compile findings** into the report.

## Severity & Categories

Tag each finding `[Severity][Category]`:

- Severity: `[Critical|Major|Minor|Suggestion|Nitpick]`
- Category: `[Gherkin-Style|Step-Defs|Coverage|Maintainability]`

Severity rubric (assign by impact, not by rule):

| Severity | Use for |
|---|---|
| `Critical` | The test is broken or misleading: undefined/ambiguous steps that fail or mis-bind, a scenario asserting the wrong outcome, scenarios coupled by shared state so results are non-deterministic. |
| `Major` | A core rule is violated in a way that undermines the spec's value: multiple behaviors / multiple `When` in one scenario, pervasive imperative/UI-detail steps, dead or duplicated step definitions. |
| `Minor` | A localized rule violation: a single chained step, one imperative step, a misused `Background` step. |
| `Suggestion` | A maintainability improvement that is not a violation: parameterizing near-duplicate steps, better naming. |
| `Nitpick` | Cosmetic: wording, ordering, optional tags. |

## Report Output

By default, write a Markdown report to `docs/reviews/<YYYY-MM-DD>-bdd-review.md`
(create the `docs/reviews/` directory if it does not exist). If the user asks for an
inline review or does not want a file written, emit the same report in the reply
instead. Either way the report contains:

- A one-paragraph summary (files reviewed, framework, overall health).
- A findings list; each finding has the `[Severity][Category]` tag, a
  `file:line` reference, the problem, and a concrete fix suggestion.
- A short "Conventions applied" note stating which repo conventions overrode
  built-in rules (or "none found — built-in rules used").

Example finding:
```
- [Major][Gherkin-Style] features/checkout.feature:14 — Scenario "Manage cart" has
  two `When` steps (add, then remove). Split into two scenarios, one behavior each.
```
