# CLAUDE.md

## Project

Guillemets — a logicless, markdown-aware template engine for non-technical
authors. `«»` (U+00AB/U+00BB) are the sole delimiter characters, chosen
because they never collide with standard markdown and are easy to type on
keyboard layouts that expose them via AltGr (e.g. Turkish).

**SPECS.md is the source of truth for behavior.** It is authoritative over
this file — if implementation reveals a spec ambiguity or gap, resolve it in
SPECS.md alongside the code change, don't just patch around it.

**Cold start?** Read `PLAN.md` first — it has the current implementation
status, architecture as actually built, and the remaining milestones. This
file (`CLAUDE.md`) is the durable *how to work here*; `PLAN.md` is the
living *what's done and what's next*.

## Stack

C#/.NET library, targeting `net10.0` (current LTS). Layout:
- `/src/Guillemets` — the class library.
- `/test/Guillemets.Tests` — NUnit test project.
- `/specs` — the fixture corpus, the acceptance contract. Don't edit
  fixtures to make a test pass; if one looks wrong, fix it deliberately
  and say why. Fixtures are flat sibling files sharing a number-prefixed
  basename within each numbered group folder (e.g.
  `02-conditional-blocks/001-boolean-true-no-else.guil.md` +
  `.json` + `.md`) — not one subdirectory per case. Two triple shapes:
  `.guil.md`/`.json`/`.md` (template / data / expected rendered output)
  for success cases, or `.guil.md`/`.json`/`.error` (template / data /
  expected exception message) for cases that must throw
  `TemplateParseException` — see `10-errors/`.
- `Guillemets.slnx` at repo root ties both projects together (.NET 10
  defaults `dotnet new sln` to the newer XML solution format).
- Central package management: `Directory.Packages.props` (all `PackageVersion`
  entries, `ManagePackageVersionsCentrally=true`) and `Directory.Build.props`
  (shared `TargetFramework`/`LangVersion`/`ImplicitUsings`/`Nullable`), both at
  repo root. Individual `.csproj` files hold only what's unique to them.
- Assertions use **Shouldly** (`actual.ShouldBe(expected)`), not NUnit's
  `Assert.That`. PascalCase-of-space-words token resolution uses
  **Humanizer.Core**'s `.Dehumanize()`, not hand-rolled string splitting.

## Core concepts

(Full detail in SPECS.md — this is a map, not a replacement.)

- **Delimiters**: `«»`. Depth is what distinguishes an inline variable from a
  block: a single `«»` is always an inline variable/token (may still span
  multiple lines — see below); a run of two or more (`««`, `«««`, ...) always
  opens a block, and its close must use the same depth. Beyond depth 2, the
  extra depth is purely for nesting readability (e.g. a block nested inside
  another block) and behaves identically to `««`/`»»` for parsing purposes —
  no fixture nests blocks yet, so this is currently unexercised. Tokens may
  span multiple lines; internal whitespace (including newlines) normalizes to
  a single space before resolution.
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
- One class per file (one type per file in practice — e.g. `Ast/`, `Tokens/`,
  `Renderers/` each hold one file per type/class).
- Prefer polymorphic dispatch (a base type with an abstract/virtual method,
  each subtype overriding it — or a separate strategy class per type) over a
  `switch`/pattern-match that implements per-type behavior inline. A `switch`
  that merely *selects* which already-implemented strategy instance to hand
  off to (see `TemplateEngine.Renderer`) is fine — the behavior itself must
  live in its own class, not in the switch arms. This does not apply to a
  genuinely stateful, sequential parser walking a token stream (see
  `Parser.cs`) — that's normal parser-writing, not the anti-pattern this rule
  targets.

## Working on this repo

- Run `dotnet test` from the repo root to run the full fixture suite. Each
  fixture under `/specs` becomes one NUnit test case, named by its relative
  path (e.g. `02-conditional-blocks/003-else-truthy`), via `FixtureTests.cs`
  in the test project.
- Engine work proceeds fixture-group by fixture-group (see the numbered
  `/specs` subfolders, ordered simplest → most complex) — implement one
  group's mechanic, confirm `dotnet test` flips exactly that group green
  with no regressions, then move to the next.
- **TDD, one fixture at a time.** Pick the next single fixture (smallest
  next), write only the minimal code to make it pass, run the full suite to
  confirm no regressions — then actually perform a reasonable refactor pass
  (correct layering, remove duplication, apply the style rules above) rather
  than leaving cleanup for later. Report the result and let the fixture's
  author/reviewer weigh in before moving to the next one.
- **No failing tests at commit time.** Fixtures not yet implemented are
  listed in `FixtureTests.cs`'s `IgnoredFixtures` set and show as
  `Ignored`/`Skipped`, never `Failed` — `dotnet test` should always report
  zero failures. Remove a fixture's name from that set once its case goes
  green; the set is empty once the engine fully implements the spec.
- Known flaky build issue: `dotnet build`/`dotnet test` occasionally fails
  with `MSB3374` (can't set last-write-time on an `obj/**/*.Up2Date` file).
  Not a real permission problem — just retry the command once and it clears.

## Parking (ending a session)

When the user says they're "parking" (their term for wrapping up for the
day), do this before ending the turn:

1. Run `dotnet test` and confirm it's all-green (zero `Failed`) — flag it
   clearly if it isn't; don't park on red.
2. Update `PLAN.md`: refresh the status line/fixture count, move anything
   completed this session out of "Remaining milestones" (or note partial
   progress), add any newly-surfaced "Known v1 scope decisions."
3. Update `CLAUDE.md` itself with any durable convention, rule, or
   architecture decision that came up this session and isn't reflected here
   yet — this file plus `PLAN.md` are what survive to a cold start on
   another machine or conversation; don't let anything load-bearing live
   only in this session's chat history.
4. Remind the user of uncommitted changes — don't run git yourself (see the
   no-git rule below); just point out what's pending.
5. Give a short summary: what's done, what's next, anything to double-check.

## Git

Never run `git` commands (status, add, commit, mv, etc.) in this repo —
the user handles git themselves. Give them the exact command to run and
wait, rather than invoking it.
