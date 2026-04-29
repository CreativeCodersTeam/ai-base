#!/usr/bin/env bash
#
# setup-serena-hooks.sh — Idempotent installer that merges every hook block
# from claude-code/res/serena-hooks.json into ~/.claude/settings.json.
#
# Behaviour:
#   * For each event (PreToolUse, SessionStart, Stop, ...) in the source
#     file, missing blocks/commands are prepended so serena's hooks fire
#     first within their event category.
#   * A block already containing the exact same command is left untouched —
#     re-running the script is a no-op.
#   * Existing unrelated hooks (tokensave, GitKraken, ...) are preserved in
#     their original relative order.
#   * The first run that actually mutates settings.json creates a one-time
#     settings.json.bak.

set -euo pipefail

log()  { printf '\033[1;34m==>\033[0m %s\n' "$*"; }
warn() { printf '\033[1;33m!!\033[0m %s\n'  "$*" >&2; }
die()  { printf '\033[1;31mxx\033[0m %s\n'  "$*" >&2; exit 1; }

SCRIPT_DIR=$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)
SRC="$SCRIPT_DIR/res/serena-hooks.json"
CLAUDE_HOME="${CLAUDE_HOME:-$HOME/.claude}"
DST="$CLAUDE_HOME/settings.json"
BAK="$DST.bak"

command -v jq >/dev/null 2>&1 \
    || die "'jq' not found on PATH. Install it (e.g. 'brew install jq') and re-run."

[[ -f "$SRC" ]] || die "Source missing: $SRC"

mkdir -p "$CLAUDE_HOME"

# Use an empty object as the merge base when settings.json does not yet exist.
CUR_INPUT="$DST"
TMP_EMPTY=""
if [[ ! -f "$DST" ]]; then
    TMP_EMPTY=$(mktemp)
    printf '{}\n' > "$TMP_EMPTY"
    CUR_INPUT="$TMP_EMPTY"
fi

# Validate the existing settings.json before touching anything.
jq empty "$CUR_INPUT" \
    || die "Existing $DST is not valid JSON. Aborting."
jq empty "$SRC" \
    || die "Source $SRC is not valid JSON. Aborting."

TMP_OUT=$(mktemp)
cleanup() { rm -f "$TMP_OUT" "$TMP_EMPTY"; }
trap cleanup EXIT

jq -n \
    --slurpfile cur "$CUR_INPUT" \
    --slurpfile add "$SRC" '
    # True iff the command/type pair already appears anywhere in $blocks.
    def hook_present($blocks; $h):
        any($blocks[]?.hooks[]?; .type == $h.type and .command == $h.command);

    # Build the list of blocks to prepend for one event, in source order.
    # Each source block contributes only the hooks not yet present in
    # $existing; blocks whose commands are all already present are dropped.
    def new_blocks($existing; $src_blocks):
        [ $src_blocks[]
          | . as $b
          | { matcher: (.matcher // ""),
              hooks:  [ .hooks[] | select(hook_present($existing; .) | not) ] }
          | select(.hooks | length > 0)
        ];

    ($cur[0] // {}) as $base
    | reduce ($add[0].hooks | to_entries[]) as $evt
        ($base;
         (.hooks[$evt.key] // []) as $existing
         | .hooks[$evt.key] = new_blocks($existing; $evt.value) + $existing)
' > "$TMP_OUT"

# If nothing changed, drop the tmp file and report a no-op.
if [[ -f "$DST" ]] && cmp -s "$TMP_OUT" "$DST"; then
    log "$DST already contains every serena hook — nothing to do."
    exit 0
fi

# One-time backup before the first mutating write.
if [[ -f "$DST" && ! -f "$BAK" ]]; then
    log "Backing up existing settings.json -> $BAK (one-time)"
    cp "$DST" "$BAK"
fi

mv "$TMP_OUT" "$DST"
trap - EXIT
rm -f "$TMP_EMPTY"

log "Merged serena hooks into $DST"

# Brief summary of which events the source file touches.
EVENTS=$(jq -r '.hooks | keys | join(", ")' "$SRC")
log "Events handled: $EVENTS"
