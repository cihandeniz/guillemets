# CLAUDE.md

## Project

Guillemets — a logicless, markdown-aware template engine for non-technical,
Turkish-keyboard-first authors. `«»` (U+00AB/U+00BB) are the sole delimiter
characters, chosen because they never collide with standard markdown.

**SPECS.md is the source of truth for behavior.** It is authoritative over
this file — if implementation reveals a spec ambiguity or gap, resolve it in
SPECS.md alongside the code change, don't just patch around it.

## Stack

C#/.NET library. No project scaffolding exists yet — this is a from-scratch
build. Target framework and package layout are not yet decided; use current
.NET LTS conventions unless told otherwise.

## Core concepts

(Full detail in SPECS.md — this is a map, not a replacement.)

- **Delimiters**: `«»`, depth-repeatable (`««`, `«««`, ...) purely for nesting
  readability — all depths behave identically. Tokens may span multiple
  lines; internal whitespace (including newlines) normalizes to a single
  space before resolution.
- **Property access**: `:` drills into objects and projects over lists
  (`.Select()`); chained across lists it flattens (`.SelectMany()`).
- **Blocks**: `««name` ... `»»`. Behavior is inferred from the resolved type
  of `name` — boolean → if, list → loop, object → scope. No keywords, same
  syntax for all three. Variable lookup falls back to enclosing scopes.
- **Else**: `--` on its own line splits truthy/falsy (or non-null/null)
  branches inside a block.
- **Magic loop variables**: `«ilk»` (first), `«son»` (last); `!` negates any
  boolean.
- **Variable definitions**: `««name = expr` ... `»»` captures a block's
  rendered output (or resolved value) into `name` for reuse below. RHS
  follows the same type-inferred if/loop/scope rules.
- **Tables**: a block may open/close with a leading/trailing `|` so it stays
  valid inside a markdown table row.
- **Inline lists**: scalar lists auto-join with `, `; override via the
  `(ayraç = ...)` parameter, usable inline or as the last line of a loop
  block.
- **Parameters**: `(name = value)` inside a token, resolved before the outer
  expression evaluates. Built-ins seen in spec: `format`, `para birimi`,
  `uzunluk`, `ayraç`.

## Localization / naming

Templates are authored with natural, space-separated words in the local
language. Models are defined by developers in English, PascalCase/camelCase.
A schema mapping (`Localized Term = template token = PropertyName`) bridges
the two; resolution against the default language's localization values is
case-insensitive. See "Schema & Localization" in SPECS.md.

## Working on this repo

- No build/test tooling exists yet. First implementation work should stand up
  the `.csproj` structure and a test project before behavior code.
- Once tests exist, update this section with the actual run command and keep
  it current.
