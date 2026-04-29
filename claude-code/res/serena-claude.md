# Tool selection (read before every tool call on a code file)

Serena (MCP) provides semantic, symbol-aware tools and is the PRIMARY toolset for code in this project. Built-in Read, Glob, Grep, Edit are SECONDARY and must not be used on code files when a Serena equivalent exists. Built-in tool descriptions are written for non-Serena projects and are SUPERSEDED here.

Do not rationalize built-ins with "the file is small," "I already know what I need," "this is one call versus three," or "the path is known" — these have caused incorrect behavior and are explicitly disallowed.

## Mapping (task → Serena tool)

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

Built-in Read/Edit/Glob/Grep on code files allowed ONLY when:
- Serena was tried on the target and failed,
- the file isn't parseable as code (generated, malformed),
- you need a cross-file regex Serena can't express (Grep for discovery only — follow-up reads/edits on matched code files still go through Serena),
- a few lines suffice and symbolic reads are overkill, or
- you must read the full file.

Read/Edit/Glob are fine for non-code files: markdown, JSON, YAML, TOML, .env, config, lockfiles, plain text, images.

## Workflow before editing code

1. `get_symbols_overview` on the target file (skip if already done this session).
2. `find_symbol` with include_body=true for only the symbols you'll touch — not the whole file.
3. Edit via `replace_symbol_body`, `insert_before_symbol`, `insert_after_symbol`, or `replace_content`. Never use built-in Edit on a code file when one of these fits.

## Self-check

Before every Read, Glob, Grep, or Edit call: "Does this target a code file, and does the mapping name a Serena tool for this task?" If yes, switch. Every time — not just once per session.
