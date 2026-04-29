#!/usr/bin/env bash
#
# install-serena.sh — Install Serena (https://github.com/oraios/serena) and
# register it as an MCP server for Claude Code on macOS.
#
# Steps (per upstream docs):
#   1. Verify macOS and that the `claude` CLI is available.
#   2. Install `uv` (via Homebrew if available, otherwise Astral's installer).
#   3. uv tool install -p 3.13 serena-agent@latest --prerelease=allow
#   4. serena init
#   5. serena setup claude-code   (fallback: claude mcp add --scope user ...)
#   6. Verify registration.

set -euo pipefail

log()  { printf '\033[1;34m==>\033[0m %s\n' "$*"; }
warn() { printf '\033[1;33m!!\033[0m %s\n'  "$*" >&2; }
die()  { printf '\033[1;31mxx\033[0m %s\n'  "$*" >&2; exit 1; }

# 1. Sanity checks ------------------------------------------------------------

[[ "$(uname -s)" == "Darwin" ]] \
    || die "This script targets macOS (Darwin). Detected: $(uname -s)"

command -v claude >/dev/null 2>&1 \
    || die "Claude Code CLI ('claude') not found on PATH. Install it from https://docs.anthropic.com/claude/docs/claude-code first."

# 2. Install uv ---------------------------------------------------------------

if command -v uv >/dev/null 2>&1; then
    log "uv already installed: $(uv --version)"
else
    if command -v brew >/dev/null 2>&1; then
        log "Installing uv via Homebrew"
        brew install uv
    else
        log "Homebrew not found; installing uv via Astral installer"
        curl -LsSf https://astral.sh/uv/install.sh | sh
        # Astral installer drops uv in ~/.local/bin
        export PATH="$HOME/.local/bin:$PATH"
    fi
    command -v uv >/dev/null 2>&1 \
        || die "uv installation finished but 'uv' is still not on PATH. Open a new shell and re-run."
fi

# 3. Install Serena -----------------------------------------------------------

log "Installing serena-agent via uv (Python 3.13)"
uv tool install -p 3.13 serena-agent@latest --prerelease=allow

# Make sure uv's tool shims are wired into the user's shell for next time.
uv tool update-shell >/dev/null 2>&1 || true

if ! command -v serena >/dev/null 2>&1; then
    # uv places shims under ~/.local/bin by default; expose for current shell.
    export PATH="$HOME/.local/bin:$PATH"
fi
command -v serena >/dev/null 2>&1 \
    || die "'serena' is not on PATH after install. Restart your shell or add the uv tool bin dir to PATH, then re-run."

# 4. Initialise Serena --------------------------------------------------------

log "Initialising Serena (language-server backend)"
serena init

# 5. Register with Claude Code ------------------------------------------------

if claude mcp list 2>/dev/null | grep -q '^serena'; then
    log "Serena MCP server already registered with Claude Code"
else
    log "Registering Serena with Claude Code"
    if ! serena setup claude-code; then
        warn "'serena setup claude-code' failed; falling back to manual registration"
        claude mcp add --scope user serena -- \
            serena start-mcp-server --context claude-code --project-from-cwd
    fi
fi

# 6. Verify -------------------------------------------------------------------

log "Verifying installation"
claude mcp list | grep '^serena' || die "Serena registration not visible in 'claude mcp list'."
serena --version || true

cat <<'EOF'

Serena is installed and registered with Claude Code.

Next steps:
  - Open Claude Code in your project and run: /mcp
  - If Serena is slow to start, raise the timeout: export MCP_TIMEOUT=60000
  - See https://github.com/oraios/serena/blob/main/docs/02-usage/030_clients.md
    for hook configuration and the recommended system-prompt override.

EOF
