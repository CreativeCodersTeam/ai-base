# Gherkin/BDD Skills Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add two language-agnostic Agent Skills to the repo — `gherkin-bdd` (authoring Gherkin + implementing BDD tests) and `gherkin-bdd-reviewer` (reviewing BDD tests against `gherkin-bdd`'s standards).

**Architecture:** Both skills live under `src/skills/general/`. `gherkin-bdd` owns the canonical rule set, framework references (Reqnroll/.NET, Cucumber-JVM/Java, Cucumber.js/TS), and a rule-precedence policy (repo conventions override built-in rules). `gherkin-bdd-reviewer` has no own rules — it invokes `gherkin-bdd` via the Skill tool, inherits the precedence, and emits a severity-tagged Markdown report under `docs/reviews/`.

**Tech Stack:** Markdown skill files (`SKILL.md` + `references/` + `assets/`). Verification via shell checks (frontmatter, line count, Gherkin validity, relative paths).

**Spec:** `docs/superpowers/specs/2026-06-22-gherkin-bdd-skill-design.md`

---

## File Structure

```
src/skills/general/gherkin-bdd/
├── SKILL.md                      # Task 1 — core Gherkin + precedence + workflow + detection
├── references/
│   ├── gherkin-style.md          # Task 2
│   ├── reqnroll-dotnet.md        # Task 3
│   ├── cucumber-jvm.md           # Task 4
│   └── cucumber-js.md            # Task 5
└── assets/
    └── example.feature           # Task 6

src/skills/general/gherkin-bdd-reviewer/
└── SKILL.md                      # Task 7
```

Final task (Task 8) runs full validation across both skills.

**Note on "tests":** These are markdown skill files, so verification is structural (frontmatter validity, `name` format, line-count guideline, relative paths, valid Gherkin) rather than unit tests. Each task ends with a concrete verification command and a commit.

---

### Task 1: `gherkin-bdd` SKILL.md

**Files:**
- Create: `src/skills/general/gherkin-bdd/SKILL.md`

- [ ] **Step 1: Create the SKILL.md with full content**

Create `src/skills/general/gherkin-bdd/SKILL.md` with exactly this content:

````markdown
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
````

- [ ] **Step 2: Verify frontmatter and structure**

Run:
```bash
head -3 src/skills/general/gherkin-bdd/SKILL.md | grep -q "^name: gherkin-bdd$" && \
grep -q "^description: " src/skills/general/gherkin-bdd/SKILL.md && \
echo "frontmatter OK"
```
Expected: prints `frontmatter OK`

- [ ] **Step 3: Verify line-count guideline (< 200 lines)**

Run:
```bash
wc -l < src/skills/general/gherkin-bdd/SKILL.md
```
Expected: a number below 200.

- [ ] **Step 4: Commit**

```bash
git add src/skills/general/gherkin-bdd/SKILL.md
git commit -m "feat: add gherkin-bdd authoring skill SKILL.md"
```

---

### Task 2: `references/gherkin-style.md`

**Files:**
- Create: `src/skills/general/gherkin-bdd/references/gherkin-style.md`

- [ ] **Step 1: Create the reference with full content**

Create `src/skills/general/gherkin-bdd/references/gherkin-style.md`:

````markdown
# Gherkin Style Guide

In-depth authoring rules. Subordinate to repo-local conventions (see SKILL.md →
Rule Precedence).

## Declarative vs. Imperative

Imperative (avoid):
```gherkin
Scenario: Login
  Given I open "/login"
  When I type "alice" into "#username"
  And I type "secret" into "#password"
  And I click "#submit"
  Then I see "Welcome"
```

Declarative (prefer):
```gherkin
Scenario: Returning user signs in
  Given Alice has a registered account
  When she signs in with valid credentials
  Then she sees her dashboard
```

Declarative steps survive UI changes and read as business behavior.

## One Behavior, One `When`

Each scenario describes a single behavior with exactly one `When`. If you need a
second `When`, that is a second scenario.

Bad:
```gherkin
Scenario: Manage cart
  When the user adds an item
  Then the cart shows 1 item
  When the user removes the item
  Then the cart is empty
```

Good — split into two scenarios, each with one `When`.

## Scenario Outline

Use outlines to remove duplication across data variations:
```gherkin
Scenario Outline: Password strength
  Given a registration form
  When the user submits the password "<password>"
  Then the strength is shown as "<strength>"

  Examples:
    | password    | strength |
    | abc         | weak     |
    | Abcd1234    | medium   |
    | Abcd1234!@# | strong   |
```

Do not use an outline for a single row — use a plain `Scenario`.

## Background

`Background` is for setup shared by **every** scenario in the file. If only some
scenarios need it, move it into those scenarios (or split the file). Keep Background
short (1–3 steps) and free of assertions.

## Tags

- Use tags for selection/filtering only (`@smoke`, `@regression`, `@wip`).
- Mark work-in-progress with `@wip` and exclude it from CI runs.
- Remove tags no runner or filter uses.

## Step Reuse

Parameterize instead of duplicating:
```gherkin
# Instead of two steps:
#   Given the cart contains a book
#   Given the cart contains a pen
Given the cart contains a "book"
Given the cart contains a "pen"
```
One step definition with a string argument serves both.

## Naming

- `Feature:` names the capability ("Checkout", not "CheckoutTests").
- `Scenario:` names the behavior/outcome, not the mechanics.
- File name mirrors the feature (`checkout.feature`).
````

- [ ] **Step 2: Verify the file parses as Markdown with fenced Gherkin**

Run:
```bash
grep -c '```gherkin' src/skills/general/gherkin-bdd/references/gherkin-style.md
```
Expected: a number ≥ 4.

- [ ] **Step 3: Commit**

```bash
git add src/skills/general/gherkin-bdd/references/gherkin-style.md
git commit -m "docs: add gherkin style reference for gherkin-bdd skill"
```

---

### Task 3: `references/reqnroll-dotnet.md`

**Files:**
- Create: `src/skills/general/gherkin-bdd/references/reqnroll-dotnet.md`

- [ ] **Step 1: Create the reference with full content**

Create `src/skills/general/gherkin-bdd/references/reqnroll-dotnet.md`:

````markdown
# Reqnroll (.NET) Implementation

Reqnroll is the actively maintained successor to SpecFlow. Step-definition syntax is
the same; attributes live in the `Reqnroll` namespace.

## Setup

Add packages to the test project (`.csproj`):
```xml
<ItemGroup>
  <PackageReference Include="Reqnroll.xUnit" Version="2.*" />
  <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.*" />
  <PackageReference Include="xunit" Version="2.*" />
  <PackageReference Include="xunit.runner.visualstudio" Version="2.*" />
</ItemGroup>
```
Place `.feature` files in the test project; their build action is `Reqnroll feature`
(set automatically by the package).

## Step Definitions

A `[Binding]` class holds the steps. Attributes use regex or cucumber expressions.
```csharp
using Reqnroll;

[Binding]
public class CheckoutSteps
{
    private readonly Cart _cart = new();

    [Given(@"the cart contains a ""(.*)""")]
    public void GivenTheCartContainsA(string item) => _cart.Add(item);

    [When(@"the user checks out")]
    public void WhenTheUserChecksOut() => _cart.Checkout();

    [Then(@"the order total is (\d+)")]
    public void ThenTheOrderTotalIs(int total) =>
        Assert.Equal(total, _cart.Total);
}
```

## Sharing State

Use constructor injection (Reqnroll's context injection) to share state between
binding classes — one instance per scenario:
```csharp
public class CheckoutSteps
{
    private readonly Cart _cart;
    public CheckoutSteps(Cart cart) => _cart = cart; // Cart resolved per scenario
}
```

## Hooks

```csharp
[Binding]
public class Hooks
{
    [BeforeScenario]
    public void BeforeScenario() { /* arrange */ }

    [AfterScenario]
    public void AfterScenario() { /* cleanup */ }
}
```

## Run

```bash
dotnet test
# Filter by tag:
dotnet test --filter "Category=smoke"   # @smoke tag maps to xUnit trait "Category"
```
Expected: test output lists each scenario as a passing test.
````

- [ ] **Step 2: Verify framework markers present**

Run:
```bash
grep -q "\[Binding\]" src/skills/general/gherkin-bdd/references/reqnroll-dotnet.md && \
grep -q "dotnet test" src/skills/general/gherkin-bdd/references/reqnroll-dotnet.md && \
echo "reqnroll ref OK"
```
Expected: prints `reqnroll ref OK`

- [ ] **Step 3: Commit**

```bash
git add src/skills/general/gherkin-bdd/references/reqnroll-dotnet.md
git commit -m "docs: add Reqnroll (.NET) reference for gherkin-bdd skill"
```

---

### Task 4: `references/cucumber-jvm.md`

**Files:**
- Create: `src/skills/general/gherkin-bdd/references/cucumber-jvm.md`

- [ ] **Step 1: Create the reference with full content**

Create `src/skills/general/gherkin-bdd/references/cucumber-jvm.md`:

````markdown
# Cucumber-JVM (Java) Implementation

## Setup (Maven)

```xml
<dependencies>
  <dependency>
    <groupId>io.cucumber</groupId>
    <artifactId>cucumber-java</artifactId>
    <version>7.18.0</version>
    <scope>test</scope>
  </dependency>
  <dependency>
    <groupId>io.cucumber</groupId>
    <artifactId>cucumber-junit-platform-engine</artifactId>
    <version>7.18.0</version>
    <scope>test</scope>
  </dependency>
  <dependency>
    <groupId>org.junit.platform</groupId>
    <artifactId>junit-platform-suite</artifactId>
    <version>1.10.2</version>
    <scope>test</scope>
  </dependency>
</dependencies>
```

Place `.feature` files under `src/test/resources/` and glue (step) code under
`src/test/java/`.

## Runner (JUnit Platform Suite)

```java
import org.junit.platform.suite.api.*;
import static io.cucumber.junit.platform.engine.Constants.*;

@Suite
@IncludeEngines("cucumber")
@SelectClasspathResource("features")
@ConfigurationParameter(key = GLUE_PROPERTY_NAME, value = "com.example.steps")
public class RunCucumberTest {}
```

## Step Definitions (glue)

```java
import io.cucumber.java.en.*;
import static org.junit.jupiter.api.Assertions.assertEquals;

public class CheckoutSteps {
    private final Cart cart = new Cart();

    @Given("the cart contains a {string}")
    public void cartContains(String item) { cart.add(item); }

    @When("the user checks out")
    public void checkout() { cart.checkout(); }

    @Then("the order total is {int}")
    public void orderTotal(int total) { assertEquals(total, cart.total()); }
}
```

## Hooks

```java
import io.cucumber.java.*;

public class Hooks {
    @Before public void before() { /* arrange */ }
    @After  public void after()  { /* cleanup */ }
}
```

## Run

```bash
mvn test
# Filter by tag:
mvn test -Dcucumber.filter.tags="@smoke"
```
Expected: surefire reports each scenario as a passing test.
````

- [ ] **Step 2: Verify framework markers present**

Run:
```bash
grep -q "io.cucumber" src/skills/general/gherkin-bdd/references/cucumber-jvm.md && \
grep -q "mvn test" src/skills/general/gherkin-bdd/references/cucumber-jvm.md && \
echo "cucumber-jvm ref OK"
```
Expected: prints `cucumber-jvm ref OK`

- [ ] **Step 3: Commit**

```bash
git add src/skills/general/gherkin-bdd/references/cucumber-jvm.md
git commit -m "docs: add Cucumber-JVM (Java) reference for gherkin-bdd skill"
```

---

### Task 5: `references/cucumber-js.md`

**Files:**
- Create: `src/skills/general/gherkin-bdd/references/cucumber-js.md`

- [ ] **Step 1: Create the reference with full content**

Create `src/skills/general/gherkin-bdd/references/cucumber-js.md`:

````markdown
# Cucumber.js (TypeScript) Implementation

## Setup

```bash
npm install --save-dev @cucumber/cucumber ts-node typescript
```

Layout:
```
features/
  checkout.feature
features/step_definitions/
  checkout.steps.ts
features/support/
  world.ts
```

## Config (`cucumber.js` in project root)

```javascript
module.exports = {
  default: {
    requireModule: ['ts-node/register'],
    require: ['features/**/*.ts'],
    paths: ['features/**/*.feature'],
  },
};
```

## Step Definitions

```typescript
import { Given, When, Then } from '@cucumber/cucumber';
import assert from 'node:assert';
import { CartWorld } from './../support/world';

Given('the cart contains a {string}', function (this: CartWorld, item: string) {
  this.cart.add(item);
});

When('the user checks out', function (this: CartWorld) {
  this.cart.checkout();
});

Then('the order total is {int}', function (this: CartWorld, total: number) {
  assert.strictEqual(this.cart.total, total);
});
```

## World (per-scenario state)

```typescript
import { setWorldConstructor } from '@cucumber/cucumber';
import { Cart } from '../../src/cart';

export class CartWorld {
  cart = new Cart();
}
setWorldConstructor(CartWorld);
```

## Hooks

```typescript
import { Before, After } from '@cucumber/cucumber';

Before(function () { /* arrange */ });
After(function () { /* cleanup */ });
```

## Run

```bash
npx cucumber-js
# Filter by tag:
npx cucumber-js --tags "@smoke"
```
Expected: each scenario reported as passing.
````

- [ ] **Step 2: Verify framework markers present**

Run:
```bash
grep -q "@cucumber/cucumber" src/skills/general/gherkin-bdd/references/cucumber-js.md && \
grep -q "npx cucumber-js" src/skills/general/gherkin-bdd/references/cucumber-js.md && \
echo "cucumber-js ref OK"
```
Expected: prints `cucumber-js ref OK`

- [ ] **Step 3: Commit**

```bash
git add src/skills/general/gherkin-bdd/references/cucumber-js.md
git commit -m "docs: add Cucumber.js (TypeScript) reference for gherkin-bdd skill"
```

---

### Task 6: `assets/example.feature`

**Files:**
- Create: `src/skills/general/gherkin-bdd/assets/example.feature`

- [ ] **Step 1: Create the example feature file**

Create `src/skills/general/gherkin-bdd/assets/example.feature`:

```gherkin
# language: en
@checkout
Feature: Checkout
  As a shopper
  I want to pay for the items in my cart
  So that I receive my order

  Background:
    Given the store sells "book" for 10

  Scenario: Pay for a single item
    Given the cart contains a "book"
    When the user checks out
    Then the order total is 10

  @discount
  Scenario Outline: Bulk discount is applied
    Given the cart contains <count> copies of "book"
    When the user checks out
    Then the order total is <total>

    Examples:
      | count | total |
      | 1     | 10    |
      | 5     | 45    |
      | 10    | 80    |
```

- [ ] **Step 2: Verify it is valid Gherkin**

Run:
```bash
npx --yes @cucumber/gherkin-utils@latest >/dev/null 2>&1; \
grep -Eq '^Feature:' src/skills/general/gherkin-bdd/assets/example.feature && \
grep -Eq '^[[:space:]]+Scenario( Outline)?:' src/skills/general/gherkin-bdd/assets/example.feature && \
grep -Eq '^[[:space:]]+Examples:' src/skills/general/gherkin-bdd/assets/example.feature && \
echo "gherkin shape OK"
```
Expected: prints `gherkin shape OK` (structural check; the npx call is best-effort and may be skipped offline).

- [ ] **Step 3: Commit**

```bash
git add src/skills/general/gherkin-bdd/assets/example.feature
git commit -m "docs: add example.feature asset for gherkin-bdd skill"
```

---

### Task 7: `gherkin-bdd-reviewer` SKILL.md

**Files:**
- Create: `src/skills/general/gherkin-bdd-reviewer/SKILL.md`

- [ ] **Step 1: Create the reviewer SKILL.md with full content**

Create `src/skills/general/gherkin-bdd-reviewer/SKILL.md`:

````markdown
---
name: gherkin-bdd-reviewer
description: Reviews existing Gherkin feature files and BDD step definitions against Gherkin best practices and anti-patterns. Use when asked to review .feature files, audit BDD scenarios, check Given/When/Then quality, or assess step-definition reuse and binding correctness (Reqnroll, Cucumber-JVM, Cucumber.js). Produces a severity-tagged Markdown report under docs/reviews/. Must NOT activate on generic "review my code" requests. For writing or implementing BDD tests, use gherkin-bdd instead.
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

## Load Standards First

This skill owns **no rules of its own**. Before reviewing:

1. Invoke the `gherkin-bdd` skill (via the Skill tool) to load the canonical Gherkin
   best-practice rule set and the framework reference for the project under review.
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

## Report Output

Write a Markdown report to `docs/reviews/<YYYY-MM-DD>-bdd-review.md` with:

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
````

- [ ] **Step 2: Verify frontmatter, name, and non-activation guardrail**

Run:
```bash
head -3 src/skills/general/gherkin-bdd-reviewer/SKILL.md | grep -q "^name: gherkin-bdd-reviewer$" && \
grep -q "Must NOT activate" src/skills/general/gherkin-bdd-reviewer/SKILL.md && \
grep -q "gherkin-bdd" src/skills/general/gherkin-bdd-reviewer/SKILL.md && \
echo "reviewer SKILL OK"
```
Expected: prints `reviewer SKILL OK`

- [ ] **Step 3: Commit**

```bash
git add src/skills/general/gherkin-bdd-reviewer/SKILL.md
git commit -m "feat: add gherkin-bdd-reviewer skill"
```

---

### Task 8: Validate both skills

**Files:**
- No new files; verification only.

- [ ] **Step 1: Validate frontmatter + name format across both skills**

Run:
```bash
for f in src/skills/general/gherkin-bdd/SKILL.md src/skills/general/gherkin-bdd-reviewer/SKILL.md; do
  echo "== $f =="
  awk 'NR==1&&$0=="---"{ok=1} END{exit !ok}' "$f" && echo "frontmatter fence OK"
  grep -E "^name: [a-z0-9-]+$" "$f"
  grep -E "^description: .{40,}" "$f" >/dev/null && echo "description length OK"
done
```
Expected: each file prints `frontmatter fence OK`, a lowercase-hyphen `name:` line, and `description length OK`.

- [ ] **Step 2: Confirm no absolute paths in resource references**

Run:
```bash
grep -RnE '\]\((/|[A-Za-z]:\\)' src/skills/general/gherkin-bdd* || echo "no absolute resource paths"
```
Expected: prints `no absolute resource paths`.

---

## Self-Review Notes

- **Spec coverage:** authoring skill (Tasks 1–6), framework references for all three
  ecosystems (Tasks 3–5), rule precedence (Task 1 §Rule Precedence), reviewer skill
  with Skill-tool invocation + inherited precedence + severity report (Task 7),
  validation (Task 8). Python deliberately excluded; no LICENSE.txt — matches
  spec scope.
- **Naming consistency:** skill names `gherkin-bdd` and `gherkin-bdd-reviewer` and the
  reference filenames are used identically across all tasks and cross-references.
- **No placeholders:** every file's full content is inlined; verification commands
  have concrete expected output.
```
