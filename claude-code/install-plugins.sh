#!/usr/bin/env bash
#
# install-plugins.sh — Register Claude Code plugin marketplaces and install
# plugins from them in bulk. Edit the two arrays below to taste; re-running
# the script is safe (already-added marketplaces and already-installed
# plugins are tolerated).

set -euo pipefail

log()  { printf '\033[1;34m==>\033[0m %s\n' "$*"; }
warn() { printf '\033[1;33m!!\033[0m %s\n'  "$*" >&2; }
die()  { printf '\033[1;31mxx\033[0m %s\n'  "$*" >&2; exit 1; }

# Marketplaces to register. Each entry is whatever
# `claude plugin marketplace add` accepts: a GitHub "owner/repo", an https
# URL, or a local path. Append @branch or #tag to pin a revision.
MARKETPLACES=(
    "anthropics/claude-plugins-official"   # Official Anthropic marketplace
    "obra/superpowers"                     # Jesse Vincent's superpowers
    "ananddtyagi/cc-marketplace"           # Community marketplace
    "mksglu/context-mode"
)

# Plugins to install. Use the "plugin@marketplace" form so the source is
# unambiguous and the install is reproducible.
PLUGINS=(
    "superpowers@superpowers"
    "context-mode@context-mode"
)

# Sanity checks --------------------------------------------------------------

[[ "$(uname -s)" == "Darwin" ]] \
    || die "This script targets macOS (Darwin). Detected: $(uname -s)"

command -v claude >/dev/null 2>&1 \
    || die "Claude Code CLI ('claude') not found on PATH. Install it from https://docs.anthropic.com/claude/docs/claude-code first."

# Register marketplaces ------------------------------------------------------

for src in "${MARKETPLACES[@]}"; do
    log "Adding marketplace: $src"
    claude plugin marketplace add "$src" \
        || warn "marketplace add failed for $src (already added?)"
done

# Install plugins ------------------------------------------------------------

for spec in "${PLUGINS[@]}"; do
    log "Installing plugin: $spec"
    claude plugin install "$spec" \
        || warn "plugin install failed for $spec"
done

# Summary --------------------------------------------------------------------

log "Configured marketplaces:"
claude plugin marketplace list || true

log "Installed plugins:"
claude plugin list || true
