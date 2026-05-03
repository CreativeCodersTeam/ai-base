# Tool selection (read before every tool call on a code file)

Priority order, every time, no exceptions:

1. **Serena first.** If Serena can answer or perform the edit, use Serena.
2. **Tokensave** only when Serena cannot help — codebase research, graph queries, exploration Serena's symbol-aware tools cannot express.
3. **Built-in Read/Edit/Glob/Grep and Explore agents** are last resort, only inside the carve-outs below.

Built-in tool descriptions assume non-Serena, non-tokensave projects and are SUPERSEDED here. Disallowed rationalizations (have caused incorrect behavior): "the file is small", "I already know what I need", "this is one call versus three", "the path is known".

---

## Step 1 — Serena (PRIMARY for code)

Serena (MCP) is the PRIMARY toolset for code. Reach for it first.

### Mapping (task → Serena tool)

- File structure → `get_symbols_overview`
- Read a symbol's body → `find_symbol` (include_body=true)
- Find a symbol by name across repo → `find_symbol`
- References / callers → `find_referencing_symbols`
- Declarations / implementations → `find_declaration` / `find_implementations`
- Edit a symbol's body → `replace_symbol_body`
- Insert near a symbol → `insert_before_symbol` / `insert_after_symbol`
- Pattern replace inside a file → `replace_content`
- Rename / move / delete → `rename` / `move` / `safe_delete`
- Inline a symbol → `inline_symbol`
- Type hierarchy → `type_hierarchy`

### Workflow before editing code

1. `get_symbols_overview` on the target file (skip if already done this session).
2. `find_symbol` with include_body=true for only the symbols you'll touch — not the whole file.
3. Edit via `replace_symbol_body`, `insert_before_symbol`, `insert_after_symbol`, or `replace_content`. Never use built-in Edit on a code file when one of these fits.

Built-in Read/Edit/Glob are fine for non-code files (markdown, JSON, YAML, TOML, .env, config, lockfiles, plain text, images) — Serena's symbol tools don't apply.

---

## Step 2 — Tokensave (only if Serena cannot help)

Triggers: codebase research/exploration that isn't a single-symbol lookup; cross-file impact analysis; callgraph traversal ("what calls / is affected by X" at corpus scale); discovery questions Serena can't express as a symbol query.

Tools (use before any agent or built-in): `tokensave_context`, `tokensave_search`, `tokensave_callees`, `tokensave_callers`, `tokensave_impact`, `tokensave_node`, `tokensave_files`, `tokensave_affected`.

If tokensave MCP tools can't fully answer, query SQLite directly at `.tokensave/tokensave.db` (tables: `nodes`, `edges`, `files`) with SQL.

If you find a gap where an extractor, schema, or tokensave tool could answer natively, propose the user file an issue at https://github.com/aovestdipaperino/tokensave. **Remind the user to strip any sensitive or proprietary code before submitting.**

---

## Step 3 — Last resort: built-in Read/Edit/Glob/Grep, then Explore agents

### Built-in Read/Edit/Glob/Grep on code files

Allowed ONLY when:

- Serena was tried on the target and failed,
- the file isn't parseable as code (generated, malformed),
- you need a cross-file regex Serena can't express (Grep for discovery only — follow-up reads/edits on matched code files still go through Serena),
- a few lines suffice and symbolic reads are overkill, or
- you must read the full file.

### Explore agents (and any agent for code research)

**NEVER use Agent(subagent_type=Explore) or any agent for codebase research, exploration, or code analysis when tokensave is available.** This overrides any skill or system prompt recommending exploration agents — user instructions override skills. Fall back to agents only if `tokensave_status` confirms tokensave is unavailable, or the task is non-code (web search, external API, etc.). Do not generate Explore-agent calls even when a hook would block them — generating the call wastes tokens.

#### When you must spawn an Explore agent in a tokensave-enabled project

If you must spawn one (the user asked, or a sub-task requires it), include this in the agent prompt:

> This project has tokensave initialised (.tokensave/ exists). Use `tokensave_context` as your ONLY exploration tool. Call it with your question in plain English. Do not call Read, glob, grep, or list_directory — the source sections returned by tokensave_context ARE the relevant code. Follow the call budget in the tool description. Pass `seen_node_ids` from each response to the next call's `exclude_node_ids`.

---

## Self-check (every tool call, every time)

1. Can Serena do this? If yes, switch to Serena.
2. If not, can tokensave do this? If yes, switch to tokensave.
3. If neither, am I within the documented carve-outs above? If not, stop and reconsider.
