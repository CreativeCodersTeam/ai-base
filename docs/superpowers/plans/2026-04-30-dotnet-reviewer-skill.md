# dotnet-reviewer Skill Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a Copilot-compatible Agent Skill at `src/skills/csharp/dotnet-reviewer/` that reviews .NET 10+ code on uncommitted or branch-committed changes, activated only by explicit name, producing a structured Markdown report under `docs/reviews/`.

**Architecture:** SKILL.md orchestrates an interactive flow that calls three Bash helper scripts (version detection, diff collection, optional tool checks), reviews against versioned checklists in `references/`, and writes a report. Scripts emit JSON; tests use `jq` to assert shape. `dotnet` calls are mockable via `PATH` override.

**Tech Stack:** Bash (POSIX-friendly where possible, `/usr/bin/env bash` shebang), `git`, `jq` (test-time only), `dotnet ≥ 10` SDK (runtime), Markdown.

**Spec:** `docs/superpowers/specs/2026-04-30-dotnet-reviewer-skill-design.md`

---

## File Structure

```
src/skills/csharp/dotnet-reviewer/
├── SKILL.md                          # English orchestrator (~200 lines)
├── LICENSE.txt                        # Apache 2.0
├── scripts/
│   ├── detect-dotnet-version.sh       # JSON: {sdk, target_frameworks, project_files}
│   ├── collect-diff.sh                # JSON: {loc, files, file_list, diff}
│   └── run-checks.sh                  # JSON: {build, format, test}
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
    ├── run-tests.sh                   # entry point + assert helpers
    ├── helpers.sh                     # sourced by unit tests
    ├── fixtures/
    │   ├── make-fixtures.sh           # rebuilds all six repos
    │   ├── repo-net10/                # SDK 10, valid csproj, small diff
    │   ├── repo-net8/                 # SDK 8 → expects exit 4
    │   ├── repo-no-git/               # plain dir → expects exit 2
    │   ├── repo-malformed-csproj/     # broken XML → expects exit 5
    │   ├── repo-large-diff/           # >2000 LOC, >50 files
    │   └── repo-empty-diff/           # clean working tree
    ├── unit/
    │   ├── test-detect-version.sh
    │   ├── test-collect-diff.sh
    │   └── test-run-checks.sh
    └── integration/
        └── test-skill-flow.md         # manual smoke checklist
```

---

## Task 1: Skill Skeleton & License

**Files:**
- Create: `src/skills/csharp/dotnet-reviewer/LICENSE.txt`
- Create: `src/skills/csharp/dotnet-reviewer/SKILL.md` (placeholder)
- Create: `src/skills/csharp/dotnet-reviewer/.gitkeep` files for empty dirs (or use `mkdir -p` and rely on `.gitignore` not excluding)

- [ ] **Step 1: Create directory tree**

```bash
mkdir -p src/skills/csharp/dotnet-reviewer/{scripts,references,tests/{fixtures,unit,integration}}
```

- [ ] **Step 2: Download Apache 2.0 license text**

```bash
curl -sSL https://www.apache.org/licenses/LICENSE-2.0.txt \
  -o src/skills/csharp/dotnet-reviewer/LICENSE.txt
```

Verify file is non-empty:

```bash
test -s src/skills/csharp/dotnet-reviewer/LICENSE.txt && echo OK
```

Expected: `OK`

- [ ] **Step 3: Create placeholder SKILL.md**

Write to `src/skills/csharp/dotnet-reviewer/SKILL.md`:

```markdown
---
name: dotnet-reviewer
description: PLACEHOLDER — overwritten by Task 17.
license: Complete terms in LICENSE.txt
---

# Placeholder
This file will be replaced by Task 17 (SKILL.md authoring) once scripts and references are in place.
```

- [ ] **Step 4: Commit**

```bash
git add src/skills/csharp/dotnet-reviewer/
git commit -m "feat(dotnet-reviewer): scaffold skill directory + license"
```

---

## Task 2: Test Harness & Assertion Helpers

**Files:**
- Create: `src/skills/csharp/dotnet-reviewer/tests/helpers.sh`
- Create: `src/skills/csharp/dotnet-reviewer/tests/run-tests.sh`

- [ ] **Step 1: Write `tests/helpers.sh` — assertion library**

```bash
#!/usr/bin/env bash
# Source from unit tests. Provides: assert_eq, assert_ne, assert_exit, assert_json_eq, fail.

set -u

_ASSERT_PASSED=0
_ASSERT_FAILED=0
_FAILED_NAMES=()

_red()   { printf '\033[31m%s\033[0m' "$1"; }
_green() { printf '\033[32m%s\033[0m' "$1"; }

assert_eq() {
  local actual=$1 expected=$2 name=$3
  if [[ "$actual" == "$expected" ]]; then
    _ASSERT_PASSED=$((_ASSERT_PASSED+1))
    printf '  %s %s\n' "$(_green PASS)" "$name"
  else
    _ASSERT_FAILED=$((_ASSERT_FAILED+1))
    _FAILED_NAMES+=("$name")
    printf '  %s %s\n    expected: %q\n    actual:   %q\n' \
      "$(_red FAIL)" "$name" "$expected" "$actual"
  fi
}

assert_exit() {
  local actual=$1 expected=$2 name=$3
  assert_eq "$actual" "$expected" "$name (exit code)"
}

assert_json_eq() {
  local json=$1 jq_path=$2 expected=$3 name=$4
  local actual
  actual=$(printf '%s' "$json" | jq -r "$jq_path" 2>/dev/null || echo "<jq-error>")
  assert_eq "$actual" "$expected" "$name ($jq_path)"
}

fail() {
  _ASSERT_FAILED=$((_ASSERT_FAILED+1))
  _FAILED_NAMES+=("$1")
  printf '  %s %s\n' "$(_red FAIL)" "$1"
}

summary() {
  printf '\n%d passed, %d failed\n' "$_ASSERT_PASSED" "$_ASSERT_FAILED"
  if (( _ASSERT_FAILED > 0 )); then
    printf 'Failed:\n'
    for n in "${_FAILED_NAMES[@]}"; do printf '  - %s\n' "$n"; done
    return 1
  fi
}
```

- [ ] **Step 2: Write `tests/run-tests.sh` — runner**

```bash
#!/usr/bin/env bash
# Runs all unit tests. Builds fixtures first if missing.

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
SKILL_DIR="$(cd "$SCRIPT_DIR/.." && pwd)"

if ! command -v jq >/dev/null 2>&1; then
  echo "ERROR: jq is required for tests" >&2
  exit 1
fi

if [[ ! -d "$SCRIPT_DIR/fixtures/repo-net10/.git" ]]; then
  echo "Building fixtures..."
  bash "$SCRIPT_DIR/fixtures/make-fixtures.sh"
fi

EXIT=0
for t in "$SCRIPT_DIR/unit"/test-*.sh; do
  [[ -f "$t" ]] || continue
  echo
  echo "=== $(basename "$t") ==="
  if ! SKILL_DIR="$SKILL_DIR" bash "$t"; then
    EXIT=1
  fi
done

exit "$EXIT"
```

- [ ] **Step 3: Smoke-run the harness**

Run:

```bash
bash src/skills/csharp/dotnet-reviewer/tests/run-tests.sh
```

Expected: fails because `make-fixtures.sh` doesn't exist yet — **this is fine for now**, confirms the harness is wired.

- [ ] **Step 4: Commit**

```bash
git add src/skills/csharp/dotnet-reviewer/tests/helpers.sh \
        src/skills/csharp/dotnet-reviewer/tests/run-tests.sh
git commit -m "feat(dotnet-reviewer): add test harness and assertion helpers"
```

---

## Task 3: Test Fixtures

**Files:**
- Create: `src/skills/csharp/dotnet-reviewer/tests/fixtures/make-fixtures.sh`

- [ ] **Step 1: Write `make-fixtures.sh`**

This is one script that generates all six fixture repos idempotently.

```bash
#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

reset_dir() {
  local d=$1
  rm -rf "$d"
  mkdir -p "$d"
}

git_init_at() {
  ( cd "$1" && git init -q -b main && \
    git config user.email "test@example.com" && \
    git config user.name "test" )
}

make_csproj() {
  local path=$1 tfm=$2
  cat > "$path" <<EOF
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>$tfm</TargetFramework>
  </PropertyGroup>
</Project>
EOF
}

make_global_json() {
  local path=$1 ver=$2
  cat > "$path" <<EOF
{
  "sdk": {
    "version": "$ver"
  }
}
EOF
}

# 1. repo-net10 — happy path
NET10="$ROOT/repo-net10"
reset_dir "$NET10"
git_init_at "$NET10"
make_global_json "$NET10/global.json" "10.0.100"
make_csproj "$NET10/App.csproj" "net10.0"
mkdir -p "$NET10/src" "$NET10/wwwroot/lib"
echo 'public class Hello { public string Name => "world"; }' > "$NET10/src/Hello.cs"
echo 'minified' > "$NET10/static.min.js"
echo 'vendor' > "$NET10/wwwroot/lib/vendor.js"
( cd "$NET10" && git add -A && git commit -q -m "initial" )
# Add an uncommitted change for uncommitted-mode tests
echo 'public class New { }' > "$NET10/src/New.cs"

# 2. repo-net8 — pre-10 SDK
NET8="$ROOT/repo-net8"
reset_dir "$NET8"
git_init_at "$NET8"
make_global_json "$NET8/global.json" "8.0.100"
make_csproj "$NET8/App.csproj" "net8.0"
( cd "$NET8" && git add -A && git commit -q -m "initial" )

# 3. repo-no-git — not a git repo
NOGIT="$ROOT/repo-no-git"
reset_dir "$NOGIT"
make_csproj "$NOGIT/App.csproj" "net10.0"

# 4. repo-malformed-csproj
MALF="$ROOT/repo-malformed-csproj"
reset_dir "$MALF"
git_init_at "$MALF"
cat > "$MALF/App.csproj" <<'EOF'
<Project Sdk="Microsoft.NET.Sdk"
  <PropertyGroup>
    <TargetFramework>net10.0
EOF
( cd "$MALF" && git add -A && git commit -q -m "broken" )

# 5. repo-large-diff — >2000 LOC, >50 files
LARGE="$ROOT/repo-large-diff"
reset_dir "$LARGE"
git_init_at "$LARGE"
make_global_json "$LARGE/global.json" "10.0.100"
make_csproj "$LARGE/App.csproj" "net10.0"
mkdir -p "$LARGE/src"
echo 'placeholder' > "$LARGE/src/seed.cs"
( cd "$LARGE" && git add -A && git commit -q -m "initial" )
# Generate 60 files with ~50 lines each → ~3000 LOC uncommitted
for i in $(seq 1 60); do
  {
    printf 'public class C%d {\n' "$i"
    for j in $(seq 1 48); do printf '    public int P%d => %d;\n' "$j" "$j"; done
    printf '}\n'
  } > "$LARGE/src/C${i}.cs"
done

# 6. repo-empty-diff — clean working tree
EMPTY="$ROOT/repo-empty-diff"
reset_dir "$EMPTY"
git_init_at "$EMPTY"
make_global_json "$EMPTY/global.json" "10.0.100"
make_csproj "$EMPTY/App.csproj" "net10.0"
echo 'public class Empty { }' > "$EMPTY/Empty.cs"
( cd "$EMPTY" && git add -A && git commit -q -m "initial" )

echo "Fixtures built under $ROOT"
```

- [ ] **Step 2: Run it**

```bash
bash src/skills/csharp/dotnet-reviewer/tests/fixtures/make-fixtures.sh
```

Expected: prints `Fixtures built under …`. Verify directories exist:

```bash
ls src/skills/csharp/dotnet-reviewer/tests/fixtures/
```

Expected: `make-fixtures.sh`, `repo-empty-diff`, `repo-large-diff`, `repo-malformed-csproj`, `repo-net10`, `repo-net8`, `repo-no-git`.

- [ ] **Step 3: Add fixtures to .gitignore**

Generated fixtures are rebuilt by `make-fixtures.sh` — don't commit the repos themselves, only the generator. Append to root `.gitignore`:

```
src/skills/csharp/dotnet-reviewer/tests/fixtures/repo-*/
```

- [ ] **Step 4: Commit**

```bash
git add src/skills/csharp/dotnet-reviewer/tests/fixtures/make-fixtures.sh .gitignore
git commit -m "feat(dotnet-reviewer): add fixture generator for unit tests"
```

---

## Task 4: detect-dotnet-version.sh — Tests

**Files:**
- Create: `src/skills/csharp/dotnet-reviewer/tests/unit/test-detect-version.sh`

- [ ] **Step 1: Write the failing tests**

```bash
#!/usr/bin/env bash
set -u
TEST_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
SKILL_DIR="${SKILL_DIR:-$(cd "$TEST_DIR/../.." && pwd)}"
FIX="$SKILL_DIR/tests/fixtures"
SCRIPT="$SKILL_DIR/scripts/detect-dotnet-version.sh"
# shellcheck source=../helpers.sh
source "$SKILL_DIR/tests/helpers.sh"

# 1. Happy path: net10
out=$(bash "$SCRIPT" --repo-root "$FIX/repo-net10" 2>/dev/null); rc=$?
assert_exit "$rc" 0 "net10 exit code"
assert_json_eq "$out" '.sdk' "10.0.100" "net10 sdk"
assert_json_eq "$out" '.target_frameworks[0]' "net10.0" "net10 tfm"
assert_json_eq "$out" '.project_files | length' "1" "net10 project_files count"

# 2. Pre-10 SDK rejected
out=$(bash "$SCRIPT" --repo-root "$FIX/repo-net8" 2>/dev/null); rc=$?
assert_exit "$rc" 4 "net8 exit code"

# 3. Malformed csproj
out=$(bash "$SCRIPT" --repo-root "$FIX/repo-malformed-csproj" 2>/dev/null); rc=$?
assert_exit "$rc" 5 "malformed csproj exit code"

# 4. --help works
out=$(bash "$SCRIPT" --help 2>&1); rc=$?
assert_exit "$rc" 0 "--help exit code"
[[ "$out" == *"detect-dotnet-version"* ]] && \
  assert_eq "ok" "ok" "--help text mentions script name" || \
  fail "--help text mentions script name"

summary
```

- [ ] **Step 2: Run tests — expect failures**

```bash
bash src/skills/csharp/dotnet-reviewer/tests/unit/test-detect-version.sh
```

Expected: all assertions FAIL because `scripts/detect-dotnet-version.sh` doesn't exist yet.

---

## Task 5: detect-dotnet-version.sh — Implementation

**Files:**
- Create: `src/skills/csharp/dotnet-reviewer/scripts/detect-dotnet-version.sh`

- [ ] **Step 1: Write the script**

```bash
#!/usr/bin/env bash
# detect-dotnet-version.sh — Detect .NET SDK and target frameworks in a repo.
# Outputs JSON: {sdk, target_frameworks[], project_files[]}
# Exit codes: 0 ok, 1 usage, 4 SDK<10, 5 malformed project file.

set -u

usage() {
  cat <<EOF
detect-dotnet-version.sh — detect .NET SDK and target frameworks

Usage: detect-dotnet-version.sh --repo-root <path>
       detect-dotnet-version.sh --help

Exit codes:
  0  success
  1  usage error
  4  SDK below 10 or no .NET version detected
  5  malformed global.json or *.csproj
EOF
}

REPO_ROOT=""
while (( $# > 0 )); do
  case "$1" in
    --repo-root) REPO_ROOT=${2:-}; shift 2 ;;
    --help|-h)   usage; exit 0 ;;
    *) echo "unknown arg: $1" >&2; usage >&2; exit 1 ;;
  esac
done

[[ -n "$REPO_ROOT" ]] || { echo "missing --repo-root" >&2; usage >&2; exit 1; }
[[ -d "$REPO_ROOT" ]] || { echo "not a directory: $REPO_ROOT" >&2; exit 1; }

# --- parse global.json (optional) ---
SDK="unknown"
if [[ -f "$REPO_ROOT/global.json" ]]; then
  SDK=$(grep -oE '"version"[[:space:]]*:[[:space:]]*"[^"]+"' "$REPO_ROOT/global.json" \
        | head -n1 | sed -E 's/.*"version"[[:space:]]*:[[:space:]]*"([^"]+)".*/\1/')
  if [[ -z "$SDK" ]]; then
    echo "malformed global.json: $REPO_ROOT/global.json" >&2
    exit 5
  fi
fi

# --- find all *.csproj and extract <TargetFramework(s)> ---
project_files_json="[]"
tfms_json="[]"
mapfile -t projects < <(find "$REPO_ROOT" -type f -name '*.csproj' -not -path '*/bin/*' -not -path '*/obj/*' 2>/dev/null | sort)

if (( ${#projects[@]} == 0 )); then
  echo "no *.csproj found under $REPO_ROOT" >&2
  exit 4
fi

declare -a all_tfms=()
declare -a rel_paths=()

for p in "${projects[@]}"; do
  rel=${p#"$REPO_ROOT/"}
  rel_paths+=("$rel")
  # crude check for malformed XML: must contain </Project>
  if ! grep -q '</Project>' "$p"; then
    echo "malformed csproj: $p" >&2
    exit 5
  fi
  # extract single or plural element
  tfm_line=$(grep -oE '<TargetFramework[s]?>[^<]+</TargetFramework[s]?>' "$p" | head -n1 || true)
  if [[ -z "$tfm_line" ]]; then
    echo "no TargetFramework in: $p" >&2
    exit 5
  fi
  tfm_value=$(printf '%s' "$tfm_line" | sed -E 's|<TargetFrameworks?>([^<]+)</TargetFrameworks?>|\1|')
  IFS=';' read -r -a parts <<<"$tfm_value"
  for t in "${parts[@]}"; do
    [[ -n "$t" ]] && all_tfms+=("$t")
  done
done

# --- enforce .NET 10+ ---
ok=0
for t in "${all_tfms[@]}"; do
  if [[ "$t" =~ ^net([0-9]+)\.[0-9]+$ ]]; then
    major=${BASH_REMATCH[1]}
    if (( major >= 10 )); then ok=1; break; fi
  fi
done

if (( ok == 0 )); then
  echo "no target framework >= net10.0 detected; found: ${all_tfms[*]}" >&2
  exit 4
fi

# --- emit JSON (manual, jq-free) ---
json_array() {
  local first=1
  printf '['
  for v in "$@"; do
    (( first )) || printf ','
    first=0
    printf '"%s"' "${v//\"/\\\"}"
  done
  printf ']'
}

tfms_json=$(json_array "${all_tfms[@]}")
project_files_json=$(json_array "${rel_paths[@]}")

printf '{"sdk":"%s","target_frameworks":%s,"project_files":%s}\n' \
  "$SDK" "$tfms_json" "$project_files_json"
```

- [ ] **Step 2: Run the tests — expect pass**

```bash
bash src/skills/csharp/dotnet-reviewer/tests/unit/test-detect-version.sh
```

Expected: all assertions PASS, `summary` prints `5 passed, 0 failed` (or similar non-zero passed, 0 failed).

- [ ] **Step 3: Commit**

```bash
git add src/skills/csharp/dotnet-reviewer/scripts/detect-dotnet-version.sh \
        src/skills/csharp/dotnet-reviewer/tests/unit/test-detect-version.sh
git commit -m "feat(dotnet-reviewer): add detect-dotnet-version.sh + tests"
```

---

## Task 6: collect-diff.sh — Tests

**Files:**
- Create: `src/skills/csharp/dotnet-reviewer/tests/unit/test-collect-diff.sh`

- [ ] **Step 1: Write the failing tests**

```bash
#!/usr/bin/env bash
set -u
TEST_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
SKILL_DIR="${SKILL_DIR:-$(cd "$TEST_DIR/../.." && pwd)}"
FIX="$SKILL_DIR/tests/fixtures"
SCRIPT="$SKILL_DIR/scripts/collect-diff.sh"
source "$SKILL_DIR/tests/helpers.sh"

# 1. uncommitted mode on net10 — picks up New.cs, excludes *.min.js and wwwroot/lib
out=$(bash "$SCRIPT" --repo-root "$FIX/repo-net10" --mode uncommitted 2>/dev/null); rc=$?
assert_exit "$rc" 0 "uncommitted exit code"
assert_json_eq "$out" '.files' "1" "uncommitted file count"
file_list=$(printf '%s' "$out" | jq -r '.file_list | join(",")')
[[ "$file_list" == "src/New.cs" ]] && \
  assert_eq ok ok "uncommitted file_list is src/New.cs" || \
  fail "uncommitted file_list is src/New.cs (got: $file_list)"

# 2. branch mode on empty-diff — no changes vs main
out=$(bash "$SCRIPT" --repo-root "$FIX/repo-empty-diff" --mode branch 2>/dev/null); rc=$?
assert_exit "$rc" 0 "empty-diff branch exit code"
assert_json_eq "$out" '.files' "0" "empty-diff file count"
assert_json_eq "$out" '.diff' "" "empty-diff payload empty"

# 3. not a git repo
out=$(bash "$SCRIPT" --repo-root "$FIX/repo-no-git" --mode uncommitted 2>/dev/null); rc=$?
assert_exit "$rc" 2 "no-git exit code"

# 4. branch mode but main missing — simulate by deleting branch
T=$(mktemp -d); cp -R "$FIX/repo-empty-diff"/. "$T"/
( cd "$T" && git branch -m main feature && git checkout -q -b feature2 )
out=$(bash "$SCRIPT" --repo-root "$T" --mode branch 2>/dev/null); rc=$?
assert_exit "$rc" 3 "missing-main exit code"
rm -rf "$T"

# 5. large-diff repo — counts >2000 LOC, >50 files
out=$(bash "$SCRIPT" --repo-root "$FIX/repo-large-diff" --mode uncommitted 2>/dev/null); rc=$?
assert_exit "$rc" 0 "large-diff exit code"
loc=$(printf '%s' "$out" | jq '.loc')
files=$(printf '%s' "$out" | jq '.files')
(( loc > 2000 )) && assert_eq ok ok "large-diff loc>2000 (got $loc)" || fail "large-diff loc>2000 (got $loc)"
(( files > 50 )) && assert_eq ok ok "large-diff files>50 (got $files)" || fail "large-diff files>50 (got $files)"

# 6. --help
out=$(bash "$SCRIPT" --help 2>&1); rc=$?
assert_exit "$rc" 0 "--help exit"

summary
```

- [ ] **Step 2: Run tests — expect failures**

```bash
bash src/skills/csharp/dotnet-reviewer/tests/unit/test-collect-diff.sh
```

Expected: all FAIL (script missing).

---

## Task 7: collect-diff.sh — Implementation

**Files:**
- Create: `src/skills/csharp/dotnet-reviewer/scripts/collect-diff.sh`

- [ ] **Step 1: Write the script**

```bash
#!/usr/bin/env bash
# collect-diff.sh — collect a unified diff in uncommitted or branch mode.
# Output JSON: {loc, files, file_list, diff}
# Exit codes: 0 ok (incl. empty diff), 1 usage, 2 not git, 3 baseline missing.

set -u

usage() {
  cat <<EOF
collect-diff.sh — collect a unified diff with exclusions applied

Usage:
  collect-diff.sh --repo-root <path> --mode uncommitted|branch [--baseline main]
  collect-diff.sh --help

Exclusions:
  - .gitignore (implicit via git diff)
  - *.min.js
  - wwwroot/lib/**

Exit codes:
  0  success (empty diff is success)
  1  usage error
  2  not a git repository
  3  baseline branch not found (branch mode only)
EOF
}

REPO_ROOT="" MODE="" BASELINE="main"
while (( $# > 0 )); do
  case "$1" in
    --repo-root) REPO_ROOT=${2:-}; shift 2 ;;
    --mode)      MODE=${2:-};      shift 2 ;;
    --baseline)  BASELINE=${2:-};  shift 2 ;;
    --help|-h)   usage; exit 0 ;;
    *) echo "unknown arg: $1" >&2; usage >&2; exit 1 ;;
  esac
done

[[ -n "$REPO_ROOT" && -n "$MODE" ]] || { usage >&2; exit 1; }
case "$MODE" in uncommitted|branch) ;; *) echo "invalid --mode: $MODE" >&2; exit 1 ;; esac
[[ -d "$REPO_ROOT/.git" ]] || { echo "not a git repository: $REPO_ROOT" >&2; exit 2; }

cd "$REPO_ROOT"

if [[ "$MODE" == "branch" ]]; then
  if ! git rev-parse --verify --quiet "$BASELINE" >/dev/null; then
    echo "baseline branch not found: $BASELINE" >&2
    exit 3
  fi
fi

# pathspec exclusions on top of .gitignore
EXCLUDES=(
  ':(exclude)*.min.js'
  ':(exclude,glob)wwwroot/lib/**'
)

if [[ "$MODE" == "uncommitted" ]]; then
  # working-tree changes vs HEAD (staged + unstaged + untracked)
  # Use --no-renames for stable file_list; include untracked via git status.
  diff_payload=$(git diff HEAD --no-renames -- . "${EXCLUDES[@]}")
  # Append untracked files as added-from-empty diffs
  while IFS= read -r f; do
    [[ -z "$f" ]] && continue
    diff_payload+=$'\n'$(git diff --no-index --no-renames /dev/null "$f" 2>/dev/null || true)
  done < <(git ls-files --others --exclude-standard -- . "${EXCLUDES[@]}")
else
  diff_payload=$(git diff "$BASELINE"...HEAD --no-renames -- . "${EXCLUDES[@]}")
fi

# Count files & LOC from the diff
files=0
loc=0
declare -a file_list=()
while IFS= read -r line; do
  case "$line" in
    "+++ b/"*) f=${line#+++ b/}; [[ "$f" != "/dev/null" ]] && { file_list+=("$f"); files=$((files+1)); } ;;
    "+"*|"-"*)
      # ignore the +++/--- header lines (already matched above)
      [[ "$line" == "+++ "* || "$line" == "--- "* ]] || loc=$((loc+1))
      ;;
  esac
done <<< "$diff_payload"

# emit JSON (escape diff payload for JSON string)
json_escape() {
  python3 -c 'import json,sys; print(json.dumps(sys.stdin.read()), end="")' <<<"$1"
}

list_json() {
  local first=1
  printf '['
  for v in "$@"; do
    (( first )) || printf ','
    first=0
    printf '"%s"' "${v//\"/\\\"}"
  done
  printf ']'
}

diff_json=$(json_escape "$diff_payload")
fl_json=$(list_json "${file_list[@]}")

printf '{"loc":%d,"files":%d,"file_list":%s,"diff":%s}\n' \
  "$loc" "$files" "$fl_json" "$diff_json"
```

- [ ] **Step 2: Run tests — expect pass**

```bash
bash src/skills/csharp/dotnet-reviewer/tests/unit/test-collect-diff.sh
```

Expected: all pass.

- [ ] **Step 3: Commit**

```bash
git add src/skills/csharp/dotnet-reviewer/scripts/collect-diff.sh \
        src/skills/csharp/dotnet-reviewer/tests/unit/test-collect-diff.sh
git commit -m "feat(dotnet-reviewer): add collect-diff.sh + tests"
```

---

## Task 8: run-checks.sh — Tests (with mocked dotnet)

**Files:**
- Create: `src/skills/csharp/dotnet-reviewer/tests/unit/test-run-checks.sh`
- Create: `src/skills/csharp/dotnet-reviewer/tests/unit/mock-dotnet/dotnet`

- [ ] **Step 1: Create mock `dotnet`**

Write to `src/skills/csharp/dotnet-reviewer/tests/unit/mock-dotnet/dotnet`:

```bash
#!/usr/bin/env bash
# Mock dotnet. Behavior controlled via MOCK_DOTNET_MODE env var.
# Modes:
#   build-ok       — exit 0, no warnings
#   build-warn     — exit 0, prints one warning
#   build-fail     — exit 1, prints one error
#   format-clean   — exit 0
#   format-dirty   — exit 2, prints one violation
#   test-pass      — exit 0
#   test-fail      — exit 1, prints "Failed: 1"
case "${MOCK_DOTNET_MODE:-}" in
  build-ok)     exit 0 ;;
  build-warn)   echo "src/Foo.cs(10,5): warning CS0168: variable declared but never used"; exit 0 ;;
  build-fail)   echo "src/Foo.cs(12,1): error CS1002: ; expected"; exit 1 ;;
  format-clean) exit 0 ;;
  format-dirty) echo "src/Foo.cs(20,1): IDE0005 Using directive is unnecessary."; exit 2 ;;
  test-pass)    echo "Passed: 5"; exit 0 ;;
  test-fail)    echo "Failed: 1"; exit 1 ;;
  *) echo "mock dotnet: unknown mode '${MOCK_DOTNET_MODE:-}'" >&2; exit 99 ;;
esac
```

Make it discoverable when on PATH (no chmod — invoke via `bash` in tests; the shebang exists for completeness, but tests use `PATH=mock-dotnet:$PATH` *after* placing a wrapper symlink, OR run via `bash`). To keep it simple: **put a wrapper that the tests invoke directly.** Update the script header note: tests source `run-checks.sh` with `DOTNET_BIN` env var instead.

**Decision:** rather than fight `PATH` + chmod, `run-checks.sh` will accept `DOTNET_BIN` env override (default `dotnet`). This makes mocking a one-liner: `DOTNET_BIN="bash $MOCK ..."`.

- [ ] **Step 2: Write tests using `DOTNET_BIN`**

```bash
#!/usr/bin/env bash
set -u
TEST_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
SKILL_DIR="${SKILL_DIR:-$(cd "$TEST_DIR/../.." && pwd)}"
FIX="$SKILL_DIR/tests/fixtures"
SCRIPT="$SKILL_DIR/scripts/run-checks.sh"
MOCK="$SKILL_DIR/tests/unit/mock-dotnet/dotnet"
source "$SKILL_DIR/tests/helpers.sh"

run_with_mode() {
  local mode=$1; shift
  MOCK_DOTNET_MODE="$mode" DOTNET_BIN="bash $MOCK" \
    bash "$SCRIPT" --repo-root "$FIX/repo-net10" "$@"
}

# build only — happy
out=$(run_with_mode build-ok --build); rc=$?
assert_exit "$rc" 0 "build-ok exit"
assert_json_eq "$out" '.build.ok' "true" "build-ok flag"
assert_json_eq "$out" '.format' "null" "format omitted when not requested"

# build only — fail
out=$(run_with_mode build-fail --build); rc=$?
assert_exit "$rc" 0 "build-fail wrapper exit (still 0)"
assert_json_eq "$out" '.build.ok' "false" "build-fail flag"
errs=$(printf '%s' "$out" | jq '.build.errors | length')
(( errs >= 1 )) && assert_eq ok ok "build-fail produces error" || fail "build-fail produces error"

# format dirty
out=$(run_with_mode format-dirty --format); rc=$?
assert_exit "$rc" 0 "format-dirty wrapper exit"
assert_json_eq "$out" '.format.ok' "false" "format-dirty flag"

# test fail
out=$(run_with_mode test-fail --test); rc=$?
assert_exit "$rc" 0 "test-fail wrapper exit"
assert_json_eq "$out" '.test.ok' "false" "test-fail flag"

# combined: build + format + test, all good
out=$(MOCK_DOTNET_MODE=build-ok DOTNET_BIN="bash $MOCK" \
      bash "$SCRIPT" --repo-root "$FIX/repo-net10" --build --format --test 2>/dev/null) || true
# can't have multi-mode mock in one run; test just asserts JSON has all three keys present (each runs the same mock)
assert_json_eq "$out" '.build.ok' "true" "combined build present"
assert_json_eq "$out" '(.format != null)' "true" "combined format present"
assert_json_eq "$out" '(.test != null)' "true" "combined test present"

# --help
out=$(bash "$SCRIPT" --help 2>&1); rc=$?
assert_exit "$rc" 0 "--help exit"

summary
```

- [ ] **Step 3: Run tests — expect failures**

```bash
bash src/skills/csharp/dotnet-reviewer/tests/unit/test-run-checks.sh
```

Expected: FAIL (script missing).

---

## Task 9: run-checks.sh — Implementation

**Files:**
- Create: `src/skills/csharp/dotnet-reviewer/scripts/run-checks.sh`

- [ ] **Step 1: Write the script**

```bash
#!/usr/bin/env bash
# run-checks.sh — run optional dotnet build|format|test, emit structured JSON.
# Output JSON: {build:{ok,warnings[],errors[]}|null, format:{ok,violations[]}|null, test:{ok,failed[],duration}|null}
# Always exits 0 (tool failures are reported in JSON, not via exit code) unless usage error.
# DOTNET_BIN env overrides `dotnet` (used by tests).

set -u

usage() {
  cat <<EOF
run-checks.sh — run dotnet build/format/test and emit structured JSON

Usage:
  run-checks.sh --repo-root <path> [--build] [--format] [--test]
  run-checks.sh --help

Each requested check runs independently; failures are reported in JSON, not via exit code.
Set DOTNET_BIN to override the dotnet binary path (used by tests).
EOF
}

REPO_ROOT=""; DO_BUILD=0; DO_FORMAT=0; DO_TEST=0
while (( $# > 0 )); do
  case "$1" in
    --repo-root) REPO_ROOT=${2:-}; shift 2 ;;
    --build)  DO_BUILD=1; shift ;;
    --format) DO_FORMAT=1; shift ;;
    --test)   DO_TEST=1; shift ;;
    --help|-h) usage; exit 0 ;;
    *) echo "unknown arg: $1" >&2; usage >&2; exit 1 ;;
  esac
done

[[ -n "$REPO_ROOT" && -d "$REPO_ROOT" ]] || { usage >&2; exit 1; }

DOTNET=${DOTNET_BIN:-dotnet}

run_dotnet() {
  # captures stdout+stderr into the named variable, returns the exit code
  local _out_var=$1; shift
  local _tmp; _tmp=$(mktemp)
  local _rc=0
  # shellcheck disable=SC2086
  ( cd "$REPO_ROOT" && $DOTNET "$@" ) >"$_tmp" 2>&1 || _rc=$?
  printf -v "$_out_var" '%s' "$(cat "$_tmp")"
  rm -f "$_tmp"
  return "$_rc"
}

json_string() {
  python3 -c 'import json,sys; print(json.dumps(sys.stdin.read()), end="")' <<<"$1"
}

json_string_array() {
  local first=1
  printf '['
  while IFS= read -r line; do
    [[ -z "$line" ]] && continue
    (( first )) || printf ','
    first=0
    printf '%s' "$(json_string "$line")"
  done <<<"$1"
  printf ']'
}

BUILD_JSON="null"
if (( DO_BUILD )); then
  out=""
  run_dotnet out build --nologo --verbosity minimal || true
  warnings=$(grep -E ': warning [A-Z]+[0-9]+' <<<"$out" || true)
  errors=$(grep -E ': error [A-Z]+[0-9]+' <<<"$out" || true)
  ok="true"; [[ -n "$errors" ]] && ok="false"
  BUILD_JSON=$(printf '{"ok":%s,"warnings":%s,"errors":%s}' \
    "$ok" \
    "$(json_string_array "$warnings")" \
    "$(json_string_array "$errors")")
fi

FORMAT_JSON="null"
if (( DO_FORMAT )); then
  out=""
  rc=0
  run_dotnet out format --verify-no-changes --no-restore || rc=$?
  violations=$(grep -E '\([0-9]+,[0-9]+\)' <<<"$out" || true)
  ok="true"; (( rc != 0 )) && ok="false"
  FORMAT_JSON=$(printf '{"ok":%s,"violations":%s}' \
    "$ok" "$(json_string_array "$violations")")
fi

TEST_JSON="null"
if (( DO_TEST )); then
  out=""
  rc=0
  start=$(date +%s)
  run_dotnet out test --nologo --verbosity minimal || rc=$?
  end=$(date +%s)
  failed=$(grep -E '^Failed:' <<<"$out" || true)
  ok="true"; (( rc != 0 )) && ok="false"
  TEST_JSON=$(printf '{"ok":%s,"failed":%s,"duration":%d}' \
    "$ok" "$(json_string_array "$failed")" "$((end - start))")
fi

printf '{"build":%s,"format":%s,"test":%s}\n' "$BUILD_JSON" "$FORMAT_JSON" "$TEST_JSON"
```

- [ ] **Step 2: Run tests — expect pass**

```bash
bash src/skills/csharp/dotnet-reviewer/tests/unit/test-run-checks.sh
```

Expected: all pass.

- [ ] **Step 3: Commit**

```bash
git add src/skills/csharp/dotnet-reviewer/scripts/run-checks.sh \
        src/skills/csharp/dotnet-reviewer/tests/unit/test-run-checks.sh \
        src/skills/csharp/dotnet-reviewer/tests/unit/mock-dotnet/dotnet
git commit -m "feat(dotnet-reviewer): add run-checks.sh + mocked tests"
```

---

## Task 10: references/severity-taxonomy.md

**Files:**
- Create: `src/skills/csharp/dotnet-reviewer/references/severity-taxonomy.md`

- [ ] **Step 1: Write the file**

```markdown
# Severity Taxonomy and Area Tags

Every finding is tagged `[Severity][Area]` followed by `path:line`.

## Severity Levels

| Severity | When to use | Examples |
|---|---|---|
| **Critical** | Ship-blocker. Production correctness, security, data loss, or a failing test. | SQL injection, unhandled `null` deref on hot path, failing test, build error. |
| **Major** | Will hurt users or maintainers but not a ship-blocker. | Missing input validation on a public API, race condition under load, broken contract with caller. |
| **Minor** | Real issue, low impact, deserves a fix in this PR. | Build warning, sloppy exception handling, missing log context, dead code. |
| **Suggestion** | Improvement worth considering. Author can accept or reject. | Refactor opportunity, alternative idiom, better naming, format violation. |
| **Nitpick** | Cosmetic. Author should ignore unless trivial. | Whitespace, comment phrasing, minor style preference. |

## Area Tags

Pick the **dominant** concern. If two apply, pick the higher-severity area.

| Tag | Scope |
|---|---|
| `Security` | Authn/authz, input validation, secrets, injection, deserialization, crypto, OWASP. |
| `Performance` | Allocations, async/await misuse, EF query shape, hot-path heuristics, Span/Memory. |
| `Architecture` | Layer/dependency direction, SOLID, DI misuse, pattern consistency, coupling. |
| `Code-Quality` | Naming, complexity, nullability, IDisposable, dead code, exception strategy. |
| `Tests` | Missing coverage, flaky tests, weak assertions, test maintenance smell. |
| `.NET-Idioms` | Version-specific idioms (Primary Ctors, Collection Expressions, Required Members, …). |

## Mapping from Tool Outputs

| Tool finding | Severity | Area |
|---|---|---|
| `dotnet build` error | Critical | Code-Quality (or context-driven) |
| `dotnet build` warning | Minor | Code-Quality |
| `dotnet test` failure | Critical | Tests |
| `dotnet format` violation | Suggestion | Code-Quality |

## Examples

```
[Critical][Security] src/Api/UserController.cs:42
User input is concatenated directly into a SQL string.

Use parameterized queries via `FromSqlInterpolated` or EF Core's LINQ.

```csharp
// before
var users = ctx.Users.FromSqlRaw($"SELECT * FROM Users WHERE Name = '{name}'");

// after
var users = ctx.Users.FromSqlInterpolated($"SELECT * FROM Users WHERE Name = {name}");
```
```
```

- [ ] **Step 2: Commit**

```bash
git add src/skills/csharp/dotnet-reviewer/references/severity-taxonomy.md
git commit -m "docs(dotnet-reviewer): add severity taxonomy reference"
```

---

## Task 11: references/report-format.md

**Files:**
- Create: `src/skills/csharp/dotnet-reviewer/references/report-format.md`

- [ ] **Step 1: Write the file**

````markdown
# Report Format

The reviewer writes one Markdown file to `docs/reviews/YYYY-MM-DD-<branch>-<mode>.md`.
Never overwrite — append `-2`, `-3`, … on collision. Never auto-commit.

## Required Sections

1. Title + metadata block
2. Executive Summary (detailed)
3. Findings, ordered by severity desc, then by file path
4. Tool Output Appendix (if any tool ran)

## Skeleton

```markdown
# .NET Code Review — <branch> (<mode>)

**Date:** YYYY-MM-DD
**Mode:** uncommitted | branch (vs. main)
**Detected SDK:** 10.0.x
**Target Framework(s):** net10.0, …
**Tools run:** build=Y/N · format=Y/N · test=Y/N
**Exclusions:** .gitignore, *.min.js, wwwroot/lib/**
**Review strategy:** full | prioritized | chunked
**Diff size:** <files> files, <loc> changed LOC

## Executive Summary

| Severity | Count |
|---|---|
| Critical | N |
| Major | N |
| Minor | N |
| Suggestion | N |
| Nitpick | N |

**Top risks:**
1. <one line>
2. <one line>
3. <one line>

**Overall:** <1–2 sentence assessment>

## Findings

### [Critical][Security] src/Api/UserController.cs:42
<one-line description>

<recommendation paragraph>

```csharp
// fix suggestion
```

### [Major][Performance] src/Data/Repo.cs:88
…

## Tool Output Appendix

### dotnet build
- 0 errors, 2 warnings (see findings above for warnings folded in).

### dotnet test
- 12 passed, 0 failed (duration: 4s).

### dotnet format
- Skipped.
```

## Rules

- **Every finding MUST include a fix suggestion** as a code block. If the fix is structural (no single-snippet rewrite), describe the steps in prose and provide the most-affected snippet.
- Do not paste raw diff content. Reference `path:line` instead.
- File paths are repo-relative.
- Group findings by severity desc, ties broken by file path asc.
- Tool warnings/errors that are already covered by a hand-written finding should not be duplicated in the appendix — note "folded into findings".
- If strategy `chunked` was used, group findings under `## Findings — <file>` subsections.
- If strategy `prioritized` was used, list deferred files at the end as `## Files Not Reviewed in Detail`.
````

- [ ] **Step 2: Commit**

```bash
git add src/skills/csharp/dotnet-reviewer/references/report-format.md
git commit -m "docs(dotnet-reviewer): add report format reference"
```

---

## Task 12: references/review-checklist-net10.md

**Files:**
- Create: `src/skills/csharp/dotnet-reviewer/references/review-checklist-net10.md`

- [ ] **Step 1: Write the file**

```markdown
# Review Checklist — .NET 10

Apply when `detect-dotnet-version.sh` reports `target_frameworks` containing `net10.0`.

## Language Idioms

- **Primary constructors** — prefer over redundant private fields when the parameter is used directly. Flag: legacy `ctor + private readonly field` pattern in new code.
- **Collection expressions** — `[1, 2, 3]` over `new[] { 1, 2, 3 }` and `new List<int> { 1, 2, 3 }`. Flag: verbose collection initialization.
- **Required members** — `required` modifier replaces hand-rolled validation in constructors. Flag: throws in constructor for missing init-only properties.
- **`field` keyword** — auto-property backing-field access (preview in 9, stable in 10). Flag: unnecessary backing field declarations.
- **Pattern matching** — list patterns, relational patterns. Flag: chained `if (x.Length > 0 && x[0] == …)`.
- **`init` and `required` together** — for immutable POCOs.

## API Idioms

- `System.Threading.Lock` (new lock type) over `object`-based `lock` for new code.
- `Random.Shared` for non-cryptographic randomness — never `new Random()` in hot path.
- `TimeProvider` for testable time — flag direct `DateTime.UtcNow` in code that should be testable.
- `JsonSerializerContext` (source-gen) over reflection-based `JsonSerializer` on hot paths.

## Project Configuration

- `<Nullable>enable</Nullable>` MUST be on. Flag projects without it.
- `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>` recommended for libraries.
- `<LangVersion>` should not be pinned below the SDK's default unless a comment explains why.
- `ImplicitUsings` enabled — flag stale top-of-file using directives that are already implicit.

## Things That Are Still Wrong

These are not new in .NET 10 but are still common:

- `.Result` / `.Wait()` on `Task` — sync-over-async deadlock risk.
- `async void` outside event handlers.
- `IEnumerable<T>` enumerated multiple times when the source is a generator.
- `string` concatenation in loops where `StringBuilder` or `string.Create` fits.
```

- [ ] **Step 2: Commit**

```bash
git add src/skills/csharp/dotnet-reviewer/references/review-checklist-net10.md
git commit -m "docs(dotnet-reviewer): add .NET 10 checklist"
```

---

## Task 13: references/review-checklist-security.md

**Files:**
- Create: `src/skills/csharp/dotnet-reviewer/references/review-checklist-security.md`

- [ ] **Step 1: Write the file**

```markdown
# Review Checklist — Security

## Input Validation

- All public-API entry points (controllers, minimal-API handlers, gRPC, message handlers) validate input.
- DTOs use `[Required]`, `[Range]`, `[StringLength]`, or FluentValidation rules. Flag DTOs without any validation.
- Reject untrusted input before it reaches the data layer. No ad-hoc string parsing in business logic.

## Injection

- **SQL:** parameterized queries via EF Core LINQ, `FromSqlInterpolated`, or `DbCommand` with parameters. Flag: `FromSqlRaw` with interpolation, raw `SqlCommand` with concatenation.
- **Command/Process:** `Process.Start` with user-controlled args — flag any concatenation; require `ProcessStartInfo` with `ArgumentList`.
- **LDAP/XPath:** sanitized filter construction.
- **Path traversal:** `Path.Combine` does not protect — validate that the resolved absolute path is under the intended root.

## Authentication / Authorization

- `[Authorize]` (or equivalent) on every non-anonymous endpoint. Flag controllers without `[Authorize]` at class level when most actions need auth.
- Role/policy checks match the intent (no `[Authorize(Roles = "Admin")]` for user-scoped data; use resource-based authorization).
- No "fallback to anonymous" branches in middleware.

## Secrets and Credentials

- No secrets in source: API keys, connection strings with passwords, JWT keys.
- `appsettings.*.json` with secrets must be `.gitignore`-d; production uses a secret manager.
- Logs do not include `request.Headers`, `Request.Body`, or full URLs containing tokens.
- `HttpClient` does not blindly trust certs (`ServerCertificateCustomValidationCallback` should not return `true`).

## Deserialization & Serialization

- `JsonSerializer` does not deserialize untrusted JSON to `object` or `dynamic`.
- `BinaryFormatter` is forbidden — flag any usage.
- XML deserialization disables DTD processing (`XmlReaderSettings.DtdProcessing = Prohibit`).

## Cryptography

- No `MD5` or `SHA1` for security purposes. Use `SHA256+`.
- No `RandomNumberGenerator.Create()` followed by predictable seeding — use `RandomNumberGenerator.GetBytes()`.
- AES with `CipherMode.ECB` is forbidden. Prefer authenticated modes (`AesGcm`).
- No hard-coded IVs or keys.

## Web App Specific

- CSRF protection on state-changing endpoints (cookie auth + non-GET).
- Anti-forgery tokens for forms.
- CORS is restrictive — `AllowAnyOrigin` only on read-only public endpoints.
- HSTS, secure cookies, `SameSite=Lax` minimum.
- Output encoding — Razor auto-encodes; `Html.Raw` is a code smell that needs justification.

## OWASP Mapping

When reporting, reference the OWASP Top 10 category in parentheses:
- A01 Broken Access Control
- A02 Cryptographic Failures
- A03 Injection
- A04 Insecure Design
- A05 Security Misconfiguration
- A07 Identification & Auth Failures
- A08 Software & Data Integrity Failures
- A09 Security Logging Failures
- A10 SSRF
```

- [ ] **Step 2: Commit**

```bash
git add src/skills/csharp/dotnet-reviewer/references/review-checklist-security.md
git commit -m "docs(dotnet-reviewer): add security checklist"
```

---

## Task 14: references/review-checklist-performance.md

**Files:**
- Create: `src/skills/csharp/dotnet-reviewer/references/review-checklist-performance.md`

- [ ] **Step 1: Write the file**

```markdown
# Review Checklist — Performance

## async/await

- No `.Result`, `.Wait()`, `GetAwaiter().GetResult()` in async code paths.
- `async void` only on event handlers.
- `Task.Run` not used to "fake async" over CPU-bound work that already runs on a worker thread (e.g., inside an existing async pipeline).
- `ConfigureAwait(false)` on library code (not application code in modern ASP.NET).
- `ValueTask` for hot paths that frequently complete synchronously; do not consume `ValueTask` more than once.
- `IAsyncEnumerable<T>` for streaming; flag `List<T>` accumulation when callers can stream.

## Allocations

- `string` concatenation in loops → `StringBuilder` or `string.Create`.
- `string.Format` with hot-path frequency → interpolated handlers.
- LINQ `ToList()` / `ToArray()` immediately followed by another enumeration — extra allocation for nothing.
- `params object[]` on hot paths — allocates per call. Use overloads.
- Closures capturing `this` in hot lambdas — flag if measurably hot.
- Boxing: `int` → `object`, generic constraints missing.

## Span / Memory

- Parsing/slicing strings: `ReadOnlySpan<char>` over `Substring`.
- File / network buffers: `Memory<byte>` / `IBufferWriter<byte>` over `byte[]`.
- `stackalloc` for small fixed buffers in hot paths (with bounds check).

## EF Core

- N+1 queries — flag `foreach (entity) { ctx.Related.Where(...) }` patterns.
- Missing `.AsNoTracking()` on read-only queries.
- `Include` chains pulling unused columns — projection (`Select`) preferred for read paths.
- Filters applied client-side after `.ToList()` — push to SQL.
- `ChangeTracker` not cleared on long-lived contexts.
- Missing indexes on filtered/joined columns (call out in review when obvious from query shape).

## Hot-Path Heuristics

A method is "hot" if any of:
- It's on a request path of an HTTP server.
- It's inside a `for` / `while` over a user-sized collection.
- It's called from `Hosted` / `BackgroundService` loops.
- A comment or naming suggests perf-sensitive use ("inner loop", "hot", "fast path").

## Concurrency

- Shared mutable state without synchronization (lock, `Interlocked`, immutable patterns).
- `ConcurrentDictionary` misuse — `GetOrAdd` with allocating factory called on every hit.
- `SemaphoreSlim` not awaited in `using` / `try/finally`.
- `Task.WhenAll` with thousands of tasks → bound concurrency (`Parallel.ForEachAsync` with `MaxDegreeOfParallelism`).
```

- [ ] **Step 2: Commit**

```bash
git add src/skills/csharp/dotnet-reviewer/references/review-checklist-performance.md
git commit -m "docs(dotnet-reviewer): add performance checklist"
```

---

## Task 15: references/review-checklist-architecture.md

**Files:**
- Create: `src/skills/csharp/dotnet-reviewer/references/review-checklist-architecture.md`

- [ ] **Step 1: Write the file**

```markdown
# Review Checklist — Architecture

## Layering

- Domain references no infrastructure (no EF Core attributes on domain entities; no `HttpClient` in domain).
- Application layer orchestrates — does not reach into UI/infrastructure types directly.
- Controllers / minimal-API handlers stay thin: parse, dispatch to application service, map result.
- Infrastructure adapters implement domain/application interfaces, not the other way around.

## Dependency Direction

- Outer rings depend on inner rings. Flag any inner-ring file with a `using` of an outer-ring namespace.
- Solution structure should make this provable; if it doesn't (single project, mixed namespaces), call it out.

## SOLID

- **SRP:** classes that change for multiple reasons. Public surface that mixes orchestration with persistence.
- **OCP:** strategy patterns where `if (type == X) … else if (type == Y) …` is repeated across files.
- **LSP:** subclass that throws `NotSupportedException` on a base member.
- **ISP:** "fat" interfaces with one consumer per method. Split or use mark-only interfaces.
- **DIP:** new-ing dependencies inside a class instead of injecting (except for value objects).

## Dependency Injection

- Lifetimes: avoid `Singleton` capturing `Scoped` (e.g., `DbContext` in a `Singleton` cache).
- Factory delegates over `IServiceProvider` parameters in constructors.
- No `BuildServiceProvider()` in `Configure`/`ConfigureServices` — that creates a second container.
- Validation of options on startup (`ValidateOnStart`).

## Pattern Consistency

- New code matches the existing project pattern. If this file uses CQRS handlers and you added a controller method that bypasses them, flag it.
- Naming consistency: `XxxService` vs. `XxxRepository` vs. `XxxHandler` — pick one and stay consistent within a bounded context.
- New pattern introductions (e.g., switching from MediatR to direct calls) need an architectural rationale; flag if introduced silently.

## Module Boundaries

- Each project (.csproj) has a clear purpose. Flag projects whose name and contents have drifted.
- Public API of a project is intentional — `internal` over `public` unless cross-project consumption is required.
- `InternalsVisibleTo` only for tests; flag if used to share types with non-test projects.
```

- [ ] **Step 2: Commit**

```bash
git add src/skills/csharp/dotnet-reviewer/references/review-checklist-architecture.md
git commit -m "docs(dotnet-reviewer): add architecture checklist"
```

---

## Task 16: references/review-checklist-code-quality.md

**Files:**
- Create: `src/skills/csharp/dotnet-reviewer/references/review-checklist-code-quality.md`

- [ ] **Step 1: Write the file**

```markdown
# Review Checklist — Code Quality

## Naming

- Methods are verbs. Properties are nouns. Async methods end in `Async`.
- No Hungarian notation (`strName`, `iCount`).
- Booleans read as questions: `IsValid`, `HasItems`, `CanRetry`.
- Acronyms: `Url`, `Id`, `Api` (PascalCase per .NET conventions, not `URL`/`ID`/`API`).
- Type parameters: `T`, or `TKey`/`TValue` — descriptive when more than one.

## Nullability

- `<Nullable>enable</Nullable>` is on (project-level check).
- No `!` (null-forgiving operator) without a comment explaining why.
- No `#nullable disable` regions in new code.
- API surface honors nullability annotations — methods returning `T?` actually return null in some path.

## Complexity

- Methods over ~30 lines or with 4+ levels of nesting deserve a closer look.
- A `switch` with more than ~7 arms over the same value usually wants polymorphism or a lookup table.
- Boolean parameters often hide two methods — flag and suggest splitting.

## Exception Handling

- Catch the specific exception type, not `Exception`. `catch (Exception)` requires justification.
- Never swallow exceptions silently. Logging counts only if the log line carries enough context to diagnose.
- Don't use exceptions for control flow.
- Wrap third-party exceptions at the boundary; do not leak them into domain code.

## IDisposable / IAsyncDisposable

- `using` (or `using` declarations) for every `IDisposable` you create.
- `IDisposable` types as fields require the owning class to also be disposable.
- `IAsyncDisposable` consumed with `await using`, not `using`.

## Dead Code

- Unused `using` directives → format check should catch; flag if format check is off.
- Unreachable code (`return` followed by statements).
- Unused private members. Public unused members may be API for a consumer — leave alone unless clearly orphaned.

## Comments and Docs

- Public API has XML docs, especially for libraries.
- Comments explain *why*, not *what*. Flag comments that restate the code.
- TODOs without a ticket reference are a smell; flag.

## Tests (cross-cutting)

- New public behavior has at least one test.
- Tests assert behavior, not implementation (no over-mocking).
- Names describe the scenario: `Method_Condition_Expected`.
- Arrange/Act/Assert layout is visible.
- No conditional logic in test bodies (`if`/`for` inside tests).
- Edge cases: null, empty collection, boundary values.

## Logging

- Structured logging (named placeholders), not string concatenation.
- Log level matches consequence: `Error` for exceptions reaching the boundary, `Warning` for recoverable degraded paths, `Information` for state transitions, `Debug` for detail.
- No PII in logs (email, user names, full request bodies) without a redaction strategy.
```

- [ ] **Step 2: Commit**

```bash
git add src/skills/csharp/dotnet-reviewer/references/review-checklist-code-quality.md
git commit -m "docs(dotnet-reviewer): add code-quality checklist"
```

---

## Task 17: SKILL.md (Orchestrator)

**Files:**
- Modify: `src/skills/csharp/dotnet-reviewer/SKILL.md`

- [ ] **Step 1: Replace the placeholder with the real SKILL.md**

```markdown
---
name: dotnet-reviewer
description: Performs structured code reviews on .NET 10+ projects. Activates ONLY on explicit name — use the phrases "dotnet-reviewer", "dotnet code review", or "dotnet review". Reviews either uncommitted working-tree changes or committed changes on the current feature branch (vs. main). Produces a Markdown report under docs/reviews/ with severity-tagged findings ([Critical|Major|Minor|Suggestion|Nitpick][Security|Performance|Architecture|Code-Quality|Tests|.NET-Idioms]) and fix suggestions. Must NOT activate on generic "review my code" requests; other-language reviewers must not be hijacked.
license: Complete terms in LICENSE.txt
---

# dotnet-reviewer

Structured code review for .NET 10+ projects. The skill is invoked by explicit name only and produces a Markdown report.

## When to Use This Skill

Use ONLY when the user invokes one of:
- `dotnet-reviewer`
- `dotnet code review`
- `dotnet review`

Do NOT activate on generic phrases like "review my code", "can you check this PR", "look at my changes". Those go to other reviewers (or to no skill at all).

The user may add language preferences (e.g., "in German") — apply that to the report only. The skill itself remains in English.

## Prerequisites

- `git` repo with `main` branch (for branch mode).
- `dotnet ≥ 10` SDK if any of build/format/test will run.
- `bash` 4+ available.
- `python3` available (used by scripts for safe JSON encoding).

## Workflow

Follow these steps in order.

### Step 1 — Interactive prompt

Ask the user three things:

1. **Mode:** `uncommitted` (working-tree vs HEAD, includes staged/unstaged/untracked) or `branch` (current branch vs `main`).
2. **Tools:** for each of `build`, `format`, `test` — yes or no. Default no for all three.
3. **Report language:** default English. If they want another language, capture it.

Validate inputs against the whitelist. Re-prompt on invalid input.

### Step 2 — Detect .NET version

Run `scripts/detect-dotnet-version.sh --repo-root <repo>`.

- Exit 0: parse JSON `{sdk, target_frameworks, project_files}`. Pick the highest `net<N>.0` from `target_frameworks` to drive checklist selection.
- Exit 4 (SDK < 10 or none): abort. Tell the user "this skill targets .NET 10+; detected `<X>`."
- Exit 5 (malformed): show offending file. Ask the user whether to proceed without version-awareness. If yes, fall back to general checklists only.
- Exit 2 (not a directory) or 1 (usage): bug — report and abort.

### Step 3 — Collect diff

Run `scripts/collect-diff.sh --repo-root <repo> --mode <mode> --baseline main`.

- Exit 0 with `files == 0`: report "no changes to review" and exit.
- Exit 0 with `files > 0`: continue.
- Exit 2: not a git repo — abort.
- Exit 3 (branch mode, missing `main`): abort, tell user.

### Step 4 — Large-diff strategy gate

If `loc > 2000` OR `files > 50`, ask the user to choose:

- **(B) Review everything** — note token cost in report header.
- **(C) Prioritize** — review files matching `*Service.cs`, `*Controller.cs`, files without sibling `*.Tests/*Tests.cs` first; summarize the rest.
- **(D) Chunk file-by-file** — review each file independently; group findings by file.

If C is chosen but no files match the priority heuristics, fall back to D and note the fallback transparently in the report.

### Step 5 — Run requested tool checks

For each tool the user selected, invoke `scripts/run-checks.sh --repo-root <repo>` with the appropriate flag(s). Parse JSON.

If a tool isn't installed, the script reports the failure inside the JSON — log "X not available, skipping" and continue. Don't abort.

### Step 6 — Review

Walk the diff against:
1. The version-specific checklist (`references/review-checklist-net<N>.md`).
2. `references/review-checklist-security.md`.
3. `references/review-checklist-performance.md`.
4. `references/review-checklist-architecture.md`.
5. `references/review-checklist-code-quality.md`.

Fold tool findings into the issue list using the severity mapping defined in `references/severity-taxonomy.md`:
- `dotnet build` errors → Critical
- `dotnet build` warnings → Minor
- `dotnet test` failures → Critical
- `dotnet format` violations → Suggestion

Each finding MUST include a fix suggestion as a code block (`csharp` fenced) — no auto-patching.

### Step 7 — Render report

Generate the report following `references/report-format.md` exactly:
- Title + metadata block
- Detailed Executive Summary (counts, top-3 risks, LOC, scope)
- Findings ordered by severity desc, then file path asc
- Tool Output Appendix

### Step 8 — Write report

Path: `docs/reviews/YYYY-MM-DD-<branch>-<mode>.md`. Branch name is sanitized (replace `/` with `-`).

If the path exists, append `-2`, `-3`, … until unique. Create `docs/reviews/` if missing. **Never auto-commit. Never overwrite.**

Output to chat: the file path and a one-line summary (e.g., `"Wrote review with 2 Critical, 5 Major findings to docs/reviews/…"`).

## Output Contract

- Single Markdown file under `docs/reviews/`.
- Format strictly per `references/report-format.md`.
- Severity and area tags from `references/severity-taxonomy.md`.

## Resource Index

- `scripts/detect-dotnet-version.sh` — SDK / target framework detection
- `scripts/collect-diff.sh` — diff collection with exclusions
- `scripts/run-checks.sh` — optional dotnet build/format/test
- `references/severity-taxonomy.md`
- `references/report-format.md`
- `references/review-checklist-net10.md`
- `references/review-checklist-security.md`
- `references/review-checklist-performance.md`
- `references/review-checklist-architecture.md`
- `references/review-checklist-code-quality.md`

## Things This Skill Never Does

- Auto-patches or auto-commits the report.
- Reviews non-.NET code.
- Bypasses git hooks (`--no-verify`, `--no-gpg-sign`).
- Runs destructive operations as "fixes" (no `git reset`, no deletions).
- Includes secrets in logs or the report.
- Reviews .NET versions below 10 — aborts with a clear message.
```

- [ ] **Step 2: Commit**

```bash
git add src/skills/csharp/dotnet-reviewer/SKILL.md
git commit -m "feat(dotnet-reviewer): write SKILL.md orchestrator"
```

---

## Task 18: tests/integration/test-skill-flow.md (Smoke Checklist)

**Files:**
- Create: `src/skills/csharp/dotnet-reviewer/tests/integration/test-skill-flow.md`

- [ ] **Step 1: Write the file**

```markdown
# Smoke Test — End-to-End Skill Flow

Manual checklist a human walks through once before declaring the skill ready.
Time budget: ~15 minutes. Use `tests/fixtures/repo-net10` as the working directory.

## Prerequisites

- [ ] `bash tests/run-tests.sh` passes (all unit tests green).
- [ ] `dotnet --version` reports 10.x (or use the mocked path on a machine without SDK).
- [ ] You have a Copilot-compatible client that loads skills from this directory.

## Activation

- [ ] Type a generic prompt like "please review my changes" — skill MUST NOT activate.
- [ ] Type "use dotnet-reviewer to review my changes" — skill activates.
- [ ] Type "dotnet code review" — skill activates.
- [ ] Type "dotnet review please" — skill activates.

## Interactive Prompt

- [ ] Skill asks for mode (uncommitted | branch).
- [ ] Skill asks build/format/test (defaults to all No).
- [ ] Skill asks for report language (defaults to English).
- [ ] Invalid mode input is re-prompted, not silently accepted.

## Version Detection

- [ ] On `repo-net10`: SDK detected as `10.0.100`, target framework `net10.0`.
- [ ] On `repo-net8`: skill aborts with "this skill targets .NET 10+".
- [ ] On `repo-malformed-csproj`: skill prompts whether to proceed without version awareness.

## Diff Collection

- [ ] Uncommitted mode picks up `src/New.cs` (the unstaged file).
- [ ] `static.min.js` and `wwwroot/lib/vendor.js` are excluded.
- [ ] Branch mode against missing `main` aborts with a clear message.
- [ ] Empty-diff repo reports "no changes to review" and exits.

## Large-Diff Strategy Gate

- [ ] On `repo-large-diff`, skill detects > 2000 LOC / > 50 files and offers B/C/D.
- [ ] Choosing C: report includes prioritized files first, "Files Not Reviewed in Detail" section at the end.
- [ ] Choosing D: report is grouped by file under `## Findings — <file>` subheaders.
- [ ] Choosing B: header notes "full review under high token cost".

## Tool Integration (with real `dotnet` 10)

- [ ] Build only: report appendix lists `dotnet build` summary.
- [ ] Format only: violations appear as `Suggestion` findings.
- [ ] Test only: failures appear as `Critical` findings.

## Report

- [ ] Report file written to `docs/reviews/YYYY-MM-DD-<branch>-<mode>.md`.
- [ ] No auto-commit happens.
- [ ] Re-running creates `*-2.md` (no overwrite).
- [ ] Executive Summary contains: counts table, top-3 risks, LOC + file count, scope description.
- [ ] At least one finding includes a `csharp` fenced code block as a fix suggestion.
- [ ] Severity ordering: Critical → Major → Minor → Suggestion → Nitpick.
- [ ] Chat output is one file path + one-line summary, nothing else.

## Failure Modes

- [ ] Killing the skill mid-run leaves no half-written report.
- [ ] Running with `dotnet` not on `PATH` and all tool flags off: review still produced.
- [ ] Running with secrets in environment: secrets do not appear in report or logs.

## Sign-off

- [ ] Walked through by: __________
- [ ] Date: __________
- [ ] Notes: __________
```

- [ ] **Step 2: Commit**

```bash
git add src/skills/csharp/dotnet-reviewer/tests/integration/test-skill-flow.md
git commit -m "docs(dotnet-reviewer): add manual smoke checklist"
```

---

## Task 19: tests/README.md

**Files:**
- Create: `src/skills/csharp/dotnet-reviewer/tests/README.md`

- [ ] **Step 1: Write the file**

```markdown
# Tests

## Layout

- `helpers.sh` — assertion helpers sourced by every unit test.
- `run-tests.sh` — entry point. Builds fixtures if missing, runs every `unit/test-*.sh`.
- `fixtures/make-fixtures.sh` — generates the six fixture repos. Idempotent.
- `unit/test-*.sh` — one file per script under test.
- `unit/mock-dotnet/dotnet` — mock `dotnet` binary, behavior controlled via `MOCK_DOTNET_MODE`.
- `integration/test-skill-flow.md` — manual smoke checklist; not run by `run-tests.sh`.

## Running

```bash
# all unit tests (rebuilds fixtures on first run)
bash tests/run-tests.sh

# one test file
bash tests/unit/test-detect-version.sh

# rebuild fixtures from scratch
rm -rf tests/fixtures/repo-*
bash tests/fixtures/make-fixtures.sh
```

## Dependencies

- `bash` 4+
- `git`
- `jq`
- `python3` (used by `collect-diff.sh` and `run-checks.sh` for JSON escaping)

`dotnet` SDK is **not** required to run unit tests — `run-checks.sh` is exercised against the mock binary.

## Conventions

- Each test calls `summary` last; the function exits non-zero if any assertion failed.
- Tests do not modify the fixture repos. If a test needs to mutate a repo, it copies it to `mktemp` first.
- New scripts → new `unit/test-<name>.sh` + new fixtures only if existing ones don't fit.
```

- [ ] **Step 2: Commit**

```bash
git add src/skills/csharp/dotnet-reviewer/tests/README.md
git commit -m "docs(dotnet-reviewer): add tests/README"
```

---

## Task 20: Final Sanity Run

- [ ] **Step 1: Run the full unit-test suite**

```bash
bash src/skills/csharp/dotnet-reviewer/tests/run-tests.sh
```

Expected: every `test-*.sh` reports `N passed, 0 failed`.

- [ ] **Step 2: Lint shell scripts (best-effort)**

```bash
command -v shellcheck >/dev/null && \
  shellcheck src/skills/csharp/dotnet-reviewer/scripts/*.sh \
             src/skills/csharp/dotnet-reviewer/tests/*.sh \
             src/skills/csharp/dotnet-reviewer/tests/unit/*.sh \
             src/skills/csharp/dotnet-reviewer/tests/fixtures/*.sh \
  || echo "shellcheck not installed — skipped"
```

If `shellcheck` flags anything, fix and re-commit per finding.

- [ ] **Step 3: Verify SKILL.md frontmatter**

```bash
head -5 src/skills/csharp/dotnet-reviewer/SKILL.md
```

Expected: starts with `---`, contains `name: dotnet-reviewer`, `description:` is one paragraph (no line breaks), ends with `---`.

- [ ] **Step 4: Verify directory tree**

```bash
find src/skills/csharp/dotnet-reviewer -type f -not -path '*/repo-*' | sort
```

Expected files (no fixture-generated repos):

```
src/skills/csharp/dotnet-reviewer/LICENSE.txt
src/skills/csharp/dotnet-reviewer/SKILL.md
src/skills/csharp/dotnet-reviewer/references/report-format.md
src/skills/csharp/dotnet-reviewer/references/review-checklist-architecture.md
src/skills/csharp/dotnet-reviewer/references/review-checklist-code-quality.md
src/skills/csharp/dotnet-reviewer/references/review-checklist-net10.md
src/skills/csharp/dotnet-reviewer/references/review-checklist-performance.md
src/skills/csharp/dotnet-reviewer/references/review-checklist-security.md
src/skills/csharp/dotnet-reviewer/references/severity-taxonomy.md
src/skills/csharp/dotnet-reviewer/scripts/collect-diff.sh
src/skills/csharp/dotnet-reviewer/scripts/detect-dotnet-version.sh
src/skills/csharp/dotnet-reviewer/scripts/run-checks.sh
src/skills/csharp/dotnet-reviewer/tests/README.md
src/skills/csharp/dotnet-reviewer/tests/fixtures/make-fixtures.sh
src/skills/csharp/dotnet-reviewer/tests/helpers.sh
src/skills/csharp/dotnet-reviewer/tests/integration/test-skill-flow.md
src/skills/csharp/dotnet-reviewer/tests/run-tests.sh
src/skills/csharp/dotnet-reviewer/tests/unit/mock-dotnet/dotnet
src/skills/csharp/dotnet-reviewer/tests/unit/test-collect-diff.sh
src/skills/csharp/dotnet-reviewer/tests/unit/test-detect-version.sh
src/skills/csharp/dotnet-reviewer/tests/unit/test-run-checks.sh
```

- [ ] **Step 5: Walk the manual smoke checklist**

Open `src/skills/csharp/dotnet-reviewer/tests/integration/test-skill-flow.md` and check every item.

- [ ] **Step 6: Final commit (if anything changed)**

```bash
git status
# if dirty:
git add -A
git commit -m "chore(dotnet-reviewer): final sanity fixes"
```

---

## Spec Coverage Self-Review

| Spec section | Covered by task |
|---|---|
| Skill location `src/skills/csharp/dotnet-reviewer/` | Task 1 |
| English content | Tasks 10–17 (all in English) |
| Activation only on explicit name | Task 17 (SKILL.md description + When to Use) |
| Interactive prompt (mode, tools, language) | Task 17 (Step 1 of workflow) |
| Fixed `main` baseline | Task 7 (`--baseline main` default) |
| .NET 10+ enforcement, version awareness | Tasks 4–5, Task 12 (net10 checklist), Task 17 (Step 2) |
| Exclusions: .gitignore + `*.min.js` + `wwwroot/lib/**` | Tasks 6–7 (pathspec excludes) |
| Diff threshold 2000 LOC / 50 files → B/C/D prompt | Task 17 (Step 4) |
| Tools optional, asked in prompt | Tasks 8–9, Task 17 (Step 1, Step 5) |
| Severity scale + area tags | Task 10 |
| Severity mapping (test → Critical, format → Suggestion, build err → Critical, build warn → Minor) | Task 10 + Task 17 (Step 6) |
| Detailed Executive Summary | Task 11 |
| Fix suggestions per finding | Task 11 (rule "every finding MUST include a fix suggestion") |
| Report path with collision avoidance, no auto-commit | Task 17 (Step 8) |
| Error handling per spec table | Tasks 5, 7, 9 (script exit codes) + Task 17 (orchestrator response per code) |
| Input safety (whitelist, no eval) | Scripts use case statements; no eval anywhere |
| Six fixture repos | Task 3 |
| Bash-only unit tests | Tasks 4, 6, 8 + Task 2 |
| Mocked `dotnet` via `DOTNET_BIN` | Tasks 8–9 |
| Manual smoke checklist | Task 18 |
| Acceptance: `bash tests/run-tests.sh` green without dotnet SDK | Task 20 Step 1 |

No gaps identified. No placeholders in code or commands. Function/file names consistent across tasks.
