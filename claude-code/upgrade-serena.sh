#!/usr/bin/env bash
#
# upgrade-serena.sh — Upgrade Serena if already installed; otherwise delegate
# to install-serena.sh.
#
# Considered "installed" only when both:
#   - 'serena' is on PATH, AND
#   - Claude Code lists 'serena' as a registered MCP server.

set -euo pipefail

log()  { printf '\033[1;34m==>\033[0m %s\n' "$*"; }
warn() { printf '\033[1;33m!!\033[0m %s\n'  "$*" >&2; }

SCRIPT_DIR=$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)
INSTALL_SCRIPT="$SCRIPT_DIR/install-serena.sh"

serena_installed=false
if command -v serena >/dev/null 2>&1 \
   && command -v claude  >/dev/null 2>&1 \
   && claude mcp list 2>/dev/null | grep -q '^serena'; then
    serena_installed=true
fi

if ! $serena_installed; then
    log "Serena is not fully installed — delegating to install-serena.sh"
    exec bash "$INSTALL_SCRIPT" "$@"
fi

log "Serena is installed; upgrading"

# Opportunistically refresh uv itself (no-op if already current).
brew upgrade uv 2>/dev/null || uv self update 2>/dev/null || true

# Preferred path: in-place upgrade.
if ! uv tool upgrade serena-agent --prerelease=allow; then
    warn "'uv tool upgrade' failed — falling back to clean reinstall"
    uv tool install -p 3.13 serena-agent@latest --prerelease=allow --reinstall
fi

log "Upgrade complete"
serena --version || true

cat <<'EOF'

Restart any active Claude Code sessions to pick up the upgraded Serena MCP server.

EOF
