#!/usr/bin/env bash
#
# setup-claude.sh — One-shot, idempotent setup of the local Claude Code
# environment on macOS.
#
# Steps:
#   1. Install (or upgrade) tokensave via the aovestdipaperino Homebrew tap
#      and register it with Claude Code.
#   2. Upgrade (or install) Serena via upgrade-serena.sh.
#   3. Install Claude Code plugins via install-plugins.sh.
#   4. Copy res/statusline-command.sh to ~/.claude/statusline-command.sh.
#   5. Copy res/serena-tokensave-claude.md to ~/.claude/CLAUDE.md
#      (one-time .bak of any pre-existing, divergent file).
#
# Re-running the script is safe: every step is a no-op when already in the
# desired state. No file mode bits are ever changed.

set -euo pipefail

log()  { printf '\033[1;34m==>\033[0m %s\n' "$*"; }
warn() { printf '\033[1;33m!!\033[0m %s\n'  "$*" >&2; }
die()  { printf '\033[1;31mxx\033[0m %s\n'  "$*" >&2; exit 1; }

SCRIPT_DIR=$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)
RES_DIR="$SCRIPT_DIR/res"
CLAUDE_HOME="${CLAUDE_HOME:-$HOME/.claude}"

TOKENSAVE_TAP="aovestdipaperino/tap"
TOKENSAVE_FORMULA="${TOKENSAVE_TAP}/tokensave"

# 0. Sanity ------------------------------------------------------------------

[[ "$(uname -s)" == "Darwin" ]] \
    || die "This script targets macOS (Darwin). Detected: $(uname -s)"

command -v claude >/dev/null 2>&1 \
    || die "Claude Code CLI ('claude') not found on PATH. Install it from https://docs.anthropic.com/claude/docs/claude-code first."

command -v brew >/dev/null 2>&1 \
    || die "Homebrew ('brew') not found on PATH. Install it from https://brew.sh first."

mkdir -p "$CLAUDE_HOME"

# 1. Tokensave ---------------------------------------------------------------

log "Step 1/5: tokensave"

if command -v tokensave >/dev/null 2>&1; then
    log "tokensave already installed: $(tokensave --version 2>/dev/null || echo 'unknown version')"
    brew upgrade "$TOKENSAVE_FORMULA" 2>/dev/null \
        || log "tokensave already at the latest version"
else
    log "Tapping $TOKENSAVE_TAP"
    brew tap "$TOKENSAVE_TAP" 2>/dev/null \
        || log "tap $TOKENSAVE_TAP already added"
    log "Installing $TOKENSAVE_FORMULA"
    brew install "$TOKENSAVE_FORMULA"
fi

command -v tokensave >/dev/null 2>&1 \
    || die "'tokensave' is not on PATH after install. Restart your shell, then re-run."

log "Registering tokensave with Claude Code (idempotent)"
tokensave install --agent claude-code \
    || warn "'tokensave install --agent claude-code' returned non-zero — re-check with 'tokensave doctor'"

# 2. Serena ------------------------------------------------------------------

log "Step 2/5: Serena (upgrade or install)"
bash "$SCRIPT_DIR/upgrade-serena.sh"

# 3. Claude plugins ----------------------------------------------------------

log "Step 3/5: Claude Code plugins"
bash "$SCRIPT_DIR/install-plugins.sh"

# 4. Statusline --------------------------------------------------------------

log "Step 4/5: statusline command"
STATUSLINE_SRC="$RES_DIR/statusline-command.sh"
STATUSLINE_DST="$CLAUDE_HOME/statusline-command.sh"

[[ -f "$STATUSLINE_SRC" ]] || die "Source missing: $STATUSLINE_SRC"

if [[ -f "$STATUSLINE_DST" ]] && cmp -s "$STATUSLINE_SRC" "$STATUSLINE_DST"; then
    log "$STATUSLINE_DST already up to date"
else
    log "Copying statusline-command.sh -> $STATUSLINE_DST"
    cp "$STATUSLINE_SRC" "$STATUSLINE_DST"
fi

# 5. Global CLAUDE.md --------------------------------------------------------

log "Step 5/5: global CLAUDE.md"
CLAUDEMD_SRC="$RES_DIR/serena-tokensave-claude.md"
CLAUDEMD_DST="$CLAUDE_HOME/CLAUDE.md"
CLAUDEMD_BAK="$CLAUDEMD_DST.bak"

[[ -f "$CLAUDEMD_SRC" ]] || die "Source missing: $CLAUDEMD_SRC"

if [[ -f "$CLAUDEMD_DST" ]] && cmp -s "$CLAUDEMD_SRC" "$CLAUDEMD_DST"; then
    log "$CLAUDEMD_DST already up to date"
else
    if [[ -f "$CLAUDEMD_DST" && ! -f "$CLAUDEMD_BAK" ]]; then
        log "Backing up existing CLAUDE.md -> $CLAUDEMD_BAK (one-time)"
        cp "$CLAUDEMD_DST" "$CLAUDEMD_BAK"
    fi
    log "Copying serena-tokensave-claude.md -> $CLAUDEMD_DST"
    cp "$CLAUDEMD_SRC" "$CLAUDEMD_DST"
fi

# Summary --------------------------------------------------------------------

log "Setup complete. Current MCP servers:"
claude mcp list || true

log "Installed Claude Code plugins:"
claude plugin list || true

cat <<EOF

Done. Re-running 'bash claude-code/setup-claude.sh' is safe and idempotent.

Notes:
  - Statusline:   $STATUSLINE_DST
  - Global rules: $CLAUDEMD_DST
  - Previous CLAUDE.md (if any): $CLAUDEMD_BAK
EOF
