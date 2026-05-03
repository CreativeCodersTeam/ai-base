#!/usr/bin/env bash
# Claude Code statusLine command
# Mirrors the zsh PROMPT style: git branch (orange bg) + cwd (light blue bg)
# Appends: model + ctx%, 5h-session limit, 7d-weekly limit (when available)

input=$(cat)

cwd=$(echo "$input" | jq -r '.workspace.current_dir // .cwd // empty')
model=$(echo "$input" | jq -r '.model.display_name // empty')
git_branch=$(echo "$input" | jq -r '.workspace.git_worktree // empty')
context_pct=$(echo "$input" | jq -r '.context_window.used_percentage // empty')
# Effort level — present only when model supports reasoning effort
effort_level=$(echo "$input" | jq -r '.effort.level // empty')
# Thinking fallback — show only when thinking is on but no effort level present
thinking_enabled=$(echo "$input" | jq -r '.thinking.enabled // empty')

# Rate limit fields — only present for Claude.ai subscribers after first API call
five_h_pct=$(echo "$input" | jq -r '.rate_limits.five_hour.used_percentage // empty')
five_h_resets=$(echo "$input" | jq -r '.rate_limits.five_hour.resets_at // empty')
seven_d_pct=$(echo "$input" | jq -r '.rate_limits.seven_day.used_percentage // empty')
seven_d_resets=$(echo "$input" | jq -r '.rate_limits.seven_day.resets_at // empty')

# Shorten cwd: replace $HOME with ~
home="$HOME"
short_cwd="${cwd/#$home/~}"

# ANSI color codes
RESET='\033[0m'
# Orange background (#D75F00 = 202), white fg
ORANGE_BG='\033[48;5;202m'
WHITE_FG='\033[97m'
# Light blue background (#ADD8E6 approx = 153), black fg
LBLUE_BG='\033[48;5;153m'
BLACK_FG='\033[30m'
# Dim for model/context info
DIM='\033[2m'
# Progress-bar tint colours (256-colour)
COLOR_GREEN='\033[38;5;70m'   # green  — usage <50 %
COLOR_YELLOW='\033[38;5;220m' # yellow — usage 50–80 %
COLOR_RED='\033[38;5;160m'    # red    — usage >80 %

# Helper: format seconds-until-epoch as "Xh Ym" or "Ym" or "now"
format_countdown() {
  local resets_at="$1"
  local now
  now=$(date +%s)
  local diff=$(( resets_at - now ))
  if [ "$diff" -le 0 ]; then
    echo "now"
  elif [ "$diff" -lt 3600 ]; then
    echo "$(( diff / 60 ))m"
  else
    local h=$(( diff / 3600 ))
    local m=$(( (diff % 3600) / 60 ))
    if [ "$m" -eq 0 ]; then
      echo "${h}h"
    else
      echo "${h}h ${m}m"
    fi
  fi
}

# Helper: render a 10-cell Unicode progress bar with threshold tinting
# Usage: make_bar <integer_percentage_0_to_100>
# Returns the bar string including colour + reset codes
make_bar() {
  local pct="$1"
  local total=10
  local filled=$(( pct * total / 100 ))
  [ "$filled" -gt "$total" ] && filled=$total
  local empty=$(( total - filled ))

  # Pick colour by threshold
  local bar_color
  if [ "$pct" -ge 80 ]; then
    bar_color="$COLOR_RED"
  elif [ "$pct" -ge 50 ]; then
    bar_color="$COLOR_YELLOW"
  else
    bar_color="$COLOR_GREEN"
  fi

  # Build filled and empty strings
  local bar_filled=""
  local i
  for (( i=0; i<filled; i++ )); do bar_filled+="█"; done
  local bar_empty=""
  for (( i=0; i<empty; i++ )); do bar_empty+="░"; done

  printf "${bar_color}${bar_filled}${DIM}${bar_empty}${RESET}"
}

# Try to get git branch from cwd if not provided by JSON
if [ -z "$git_branch" ]; then
  git_branch=$(git -C "$cwd" --no-optional-locks symbolic-ref --short HEAD 2>/dev/null)
fi

output=""

# Git branch segment (orange bg, white text) — only if in a git repo
if [ -n "$git_branch" ]; then
  output+=$(printf "${ORANGE_BG}${WHITE_FG}(%s)${RESET}" "$git_branch")
fi

# CWD segment (light blue bg, black text)
output+=$(printf "${LBLUE_BG}${BLACK_FG} %s${RESET}" "$short_cwd")

# Model + context usage (dimmed)
if [ -n "$model" ]; then
  # Build model label: "Claude Sonnet 4.6 (high)" or "Claude Sonnet 4.6 (thinking)" or just "Claude Sonnet 4.6"
  model_label="${model}"
  if [ -n "$effort_level" ]; then
    model_label="${model_label} (${effort_level})"
  elif [ "$thinking_enabled" = "true" ]; then
    model_label="${model_label} (thinking)"
  fi
  extra="${model_label}"
  if [ -n "$context_pct" ]; then
    extra="${extra} | ctx: $(printf '%.0f' "$context_pct")%"
  fi
  output+=$(printf " ${DIM}[%s]${RESET}" "$extra")
fi

# Empty 10-cell placeholder bar (dimmed) for when no usage data is available
empty_bar=$(printf "${DIM}░░░░░░░░░░${RESET}")

# 5-hour session limit — show real data when present, placeholder otherwise
if [ -n "$five_h_pct" ]; then
  pct_int=$(printf '%.0f' "$five_h_pct")
  bar=$(make_bar "$pct_int")
  if [ -n "$five_h_resets" ]; then
    countdown=$(format_countdown "$five_h_resets")
    output+=$(printf " ${DIM}[5h ${RESET}%s${DIM} %d%% · %s]${RESET}" "$bar" "$pct_int" "$countdown")
  else
    output+=$(printf " ${DIM}[5h ${RESET}%s${DIM} %d%%]${RESET}" "$bar" "$pct_int")
  fi
else
  output+=$(printf " ${DIM}[5h ${RESET}%s${DIM} --%%]${RESET}" "$empty_bar")
fi

# 7-day weekly limit — show real data when present, placeholder otherwise
if [ -n "$seven_d_pct" ]; then
  pct_int=$(printf '%.0f' "$seven_d_pct")
  bar=$(make_bar "$pct_int")
  if [ -n "$seven_d_resets" ]; then
    countdown=$(format_countdown "$seven_d_resets")
    output+=$(printf " ${DIM}[7d ${RESET}%s${DIM} %d%% · %s]${RESET}" "$bar" "$pct_int" "$countdown")
  else
    output+=$(printf " ${DIM}[7d ${RESET}%s${DIM} %d%%]${RESET}" "$bar" "$pct_int")
  fi
else
  output+=$(printf " ${DIM}[7d ${RESET}%s${DIM} --%%]${RESET}" "$empty_bar")
fi

printf "%s" "$output"
