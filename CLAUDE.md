# CLAUDE.md

## Project

Guillemets — a logicless, markdown-aware template engine for non-technical
authors. `«»` (U+00AB/U+00BB) are the sole delimiter characters, chosen
because they never collide with standard markdown and are easy to type on
keyboard layouts that expose them via AltGr (e.g. Turkish).

**SPECS.md is the source of truth for behavior.** It is authoritative over
this file — if implementation reveals a spec ambiguity or gap, resolve it in
SPECS.md alongside the code change, don't just patch around it.

## Stack

C#/.NET library, targeting `net10.0` (current LTS). Layout:
- `/src/Guillemets` — the class library.
- `/test/Guillemets.Tests` — NUnit test project.
- `/specs` — the fixture corpus, the acceptance contract. Don't edit
  fixtures to make a test pass; if one looks wrong, fix it deliberately
  and say why. Two triple shapes: `.guil.md`/`.json`/`.md` (template /
  data / expected rendered output) for success cases, or `.guil.md`/
  `.json`/`.error` (template / data / expected exception message) for
  cases that must throw `TemplateParseException` — see `10-errors/`.
- `Guillemets.slnx` at repo root ties both projects together (.NET 10
  defaults `dotnet new sln` to the newer XML solution format).

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
- **Magic loop variables**: `«first»`, `«last»`; `!` negates any boolean.
- **Variable definitions**: `««name = expr` ... `»»` captures a block's
  rendered output (or resolved value) into `name` for reuse below. RHS
  follows the same type-inferred if/loop/scope rules.
- **Tables**: a block may open/close with a leading/trailing `|` so it stays
  valid inside a markdown table row.
- **Inline lists**: scalar lists auto-join with `, `; override via the
  `(separator = ...)` parameter, usable inline or as the last line of a loop
  block.
- **Parameters**: `(name = value)` inside a token, resolved before the outer
  expression evaluates. Built-ins: `format`, `currency`, `length`,
  `separator`.

## Localization / naming

Templates are authored with natural, space-separated words — the author's own
business vocabulary. Models are defined by developers in English,
PascalCase/camelCase — the developer's code vocabulary. The two don't always
match (e.g. a template author writes "quote no" but the developer named the
property `OfferNo`), so a schema mapping bridges them:
`Localized Term = template token = PropertyName`. Resolution against the
default language's localization values is case-insensitive. See
"Schema & Localization" in SPECS.md.

## C# code style

- `using` directives sorted alphabetically, no special-casing `System.*` to
  the top — it sorts wherever it falls alphabetically among the others.
- `using static` directives form their own group below the regular `using`
  directives, separated by a blank line. Group multiple `using static`
  directives together when there's more than one.
- Never write `private` explicitly — it's the default; only state
  accessibility when it's not the default (`public`, `internal`, etc.).
- Keep whitespace between statements minimal — don't pad method bodies with
  blank lines between unrelated statements.
- One class per file. Closely-related small data types (e.g. an AST's node
  interface + record types) may be grouped as nested types under one
  containing class instead of splitting into many trivial files.

## Working on this repo

- Run `dotnet test` from the repo root to run the full fixture suite. Each
  of the 28 fixtures under `/specs` becomes one NUnit test case, named by
  its relative path (e.g. `02-conditional-blocks/003-else-truthy`), via
  `FixtureTests.cs` in the test project.
- Engine work proceeds fixture-group by fixture-group (see the numbered
  `/specs` subfolders, ordered simplest → most complex) — implement one
  group's mechanic, confirm `dotnet test` flips exactly that group green
  with no regressions, then move to the next.
