# CLAUDE.md

## Project

Guillemets — a logicless, markdown-aware template engine for non-technical
authors. `«»` (U+00AB/U+00BB) are the sole delimiters, chosen because they
never collide with markdown and are easy to type via AltGr (e.g. Turkish
keyboards).

**SPECS.md is the source of truth for behavior**, authoritative over this
file — resolve spec ambiguities there alongside the code change, don't
just patch around them.

**Cold start?** Read `PLAN.md` first for implementation status and
remaining milestones. This file is the durable *how to work here*;
`PLAN.md` is the living *what's done and what's next*.

## Stack

C#/.NET, targeting `net10.0`. Layout:
- `/src/Guillemets` — the class library. `/test/Guillemets.Tests` — NUnit
  test project.
- `/specs` — the fixture corpus, the acceptance contract. Don't edit
  fixtures to make a test pass; if one looks wrong, fix it deliberately
  and say why. Each case is a flat file triple sharing a basename in a
  numbered group folder: `.guil.md`/`.json`/`.md` (template/data/expected
  output) for success, or `.guil.md`/`.json`/`.error` (expected exception
  message) for cases that must throw `TemplateParseException`. Several
  cases can share one template by giving the template just the group
  number and suffixing each case's `.json`/`.md` with a letter
  (`005-nested-blocks.guil.md` + `005a-...`/`005b-...`); `FixtureTests.cs`
  matches a case to its template by leading digits. Group folders are
  numbered on disk for sort order only — refer to fixtures by name in
  prose, not number.
- `Guillemets.slnx` at repo root (.NET 10's default `dotnet new sln`
  format).
- Central package management: `Directory.Packages.props` (versions) +
  `Directory.Build.props` (shared `TargetFramework`/`LangVersion`/
  `Nullable`/etc.), both at repo root.
- Assertions use **Shouldly**, not NUnit's `Assert.That`.
  PascalCase-of-space-words resolution uses **Humanizer.Core**'s
  `.Dehumanize()`, not hand-rolled splitting.

## Core concepts

(Full detail in SPECS.md — this is a map, not a replacement.)

- **Delimiters**: `«»`. A single `«»` is always an inline variable/token
  (may span multiple lines, normalized to single spaces); a run of two or
  more (`««`, `«««`, ...) always opens a block, closed at the same depth.
  Beyond depth 2 the extra depth is cosmetic (nesting readability only) —
  unexercised by any fixture using a genuinely deeper inner delimiter.
  Nesting itself works today (`conditional-blocks/nested-blocks`), via the
  parser's own recursive-descent call stack, not depth-tracking on tokens.
- **Property access**: `:` drills into objects and projects over lists
  (`.Select()`); chained across lists it flattens (`.SelectMany()`).
- **Blocks**: `««name` ... `»»`. Behavior is inferred from the resolved
  type of `name` — boolean → if, list → loop, object → scope. No
  keywords, same syntax for all three. Variable lookup falls back to
  enclosing scopes.
- **Else**: `--` on its own line splits truthy/falsy (or non-null/null)
  branches inside a block.
- **Magic loop variables**: `«first»`, `«last»`; `!` negates any boolean.
- **Variable definitions**: `««name = expr` ... `»»` captures a block's
  rendered output (or resolved value) into `name` for reuse below, under
  the same type-inferred if/loop/scope rules.
- **Tables**: a block may open/close with a leading/trailing `|` so it
  stays valid inside a markdown table row.
- **Inline lists**: scalar lists auto-join with `, `; override via
  `(separator = ...)`, usable inline or as the last line of a loop block.
- **Parameters**: `(name = value)` inside a token, resolved before the
  outer expression evaluates. Built-ins: `format`, `currency`, `length`,
  `separator`.

## Localization / naming

Templates are authored with natural, space-separated words — the
author's business vocabulary. Models are defined by developers in
PascalCase/camelCase — the developer's code vocabulary. Where the two
don't match (e.g. "quote no" vs. `OfferNo`), a schema mapping bridges
them: `Localized Term = template token = PropertyName`, resolved
case-insensitively against the default language. See "Schema &
Localization" in SPECS.md.

## C# code style

- `using` directives sorted alphabetically (no special-casing `System.*`);
  `using static` directives form their own group below, separated by a
  blank line.
- Never write `private` explicitly — it's the default.
- Keep whitespace between statements minimal — no blank-line padding
  between unrelated statements.
- One type per file.
- Prefer polymorphic dispatch (base type + virtual/abstract method, or a
  strategy class per type) over a `switch`/pattern-match implementing
  per-type behavior inline — the behavior must live in its own class, not
  in the switch arms. A `switch` that only *selects* between
  already-implemented strategies is fine. Doesn't apply to a genuinely
  stateful, sequential parser walking a token stream — that's normal
  parser-writing.
- Don't call `new SomeType(...)` inside a constructor body unless
  `SomeType` is a DTO or `record`. Real dependencies are
  constructor-injected and wired up at the composition root
  (`TemplateEngine.Render`, `Tokenizer.Tokenize()`).
- Never write `sealed` — explicit house style; types stay open for
  inheritance even with no current subtypes.
- Naming (see `.editorconfig`): private instance fields are `_camelCase`;
  any `static` field, regardless of accessibility, is `SCREAMING_CASE` (a
  custom rule, since standard "static fields start uppercase" conventions
  would otherwise conflict with the private-field rule).
- Write small, single-purpose methods from the start, not as a later
  cleanup pass — factor out a repeated multi-line sequence immediately.
  Prefer a plain private method over a local function closing over
  another method's locals.
- Give a stateful, sequential scanner (a cursor, a parser) private
  instance fields only for state that must persist *across* separate
  method calls (e.g. `TokenCursor._position`, since `Parser` drives it
  call by call). When one method owns an entire scan start to finish,
  plain locals are the better fit (e.g. `Tokenizer.Tokenize()`).
- Avoid tuples/small one-off DTOs used purely to shuttle two or three
  values between methods. A success/failure method returns `bool`
  (mutating instance state as a side effect); a method that needs to hand
  back one meaningful value returns that value directly, typed
  explicitly.
- Never use the `!` null-forgiving operator — it silences the compiler
  instead of resolving the issue, defeating the point of `Nullable`
  (`Directory.Build.props`). Follow the house nullable guide:
  <https://github.com/mouseless/learn-dotnet/blob/main/nullable-usage/README.md>.
  When a value is nullable by type but a real invariant guarantees it
  isn't null at some point, use `?? throw new
  InvalidOperationException(...)` instead — it fails loudly at the point
  of use if the invariant is ever broken, rather than risking a
  `NullReferenceException` downstream.

## Working on this repo

- Run `dotnet test` from the repo root for the full fixture suite — each
  fixture becomes one NUnit test case, named by its relative path under
  `/specs`.
- Engine work proceeds fixture-group by fixture-group, simplest → most
  complex — implement one group's mechanic, confirm `dotnet test` flips
  exactly that group green with no regressions, then move on.
- **TDD, one fixture at a time.** Pick the smallest next fixture, write
  only the minimal code to pass it, run the full suite to confirm no
  regressions — then actually refactor (correct layering, remove
  duplication, apply the style rules above) rather than leaving cleanup
  for later. Report and let the fixture's author/reviewer weigh in
  before moving to the next one.
- **No failing tests at commit time.** Unimplemented fixtures are listed
  in `FixtureTests.cs`'s `IGNORED_FIXTURES` set (`Ignored`, never
  `Failed`) — remove a fixture's name once its case goes green. When a
  fixture is deliberately left unimplemented, leave a comment above its
  entry explaining what's undecided.
- **The build enforces style, not just `dotnet format`.**
  `Directory.Build.props` sets `EnforceCodeStyleInBuild`/
  `TreatWarningsAsErrors`, so `dotnet build`/`dotnet test` fail on any
  `.editorconfig` violation or compiler warning.
- Known flaky build issue: `MSB3374` (can't set last-write-time on an
  `obj/**/*.Up2Date` file) — not a real problem, just retry once.

## Parking (ending a session)

When the user says they're "parking" (wrapping up for the day):

1. Run `dotnet test`, confirm all-green — flag clearly if not; don't park
   on red.
2. Update `PLAN.md`: refresh status/fixture count, move completed work
   out of "Remaining milestones," add new "Known v1 scope decisions."
3. Update `CLAUDE.md` with any durable convention/rule/decision from this
   session — these two files are what survive to a cold start elsewhere;
   nothing load-bearing should live only in chat history.
4. Remind the user of uncommitted changes — don't run git yourself; just
   point out what's pending.
5. Give a short summary: what's done, what's next, anything to
   double-check.

## Git

Never run `git` commands in this repo — the user handles git themselves.
Give them the exact command to run and wait.
