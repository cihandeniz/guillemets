# guillemets

Project-specific conventions. General .NET/C# style and working habits
are in `.claude/generic.md` — read both.

## Project

Guillemets — a markdown-aware template engine for non-technical authors. `«»`
(U+00AB/U+00BB) are the sole delimiters, chosen because they never collide with
markdown and are easy to type via AltGr (e.g. Turkish keyboards).

**`docs/specs.md` is the source of truth for behavior**, authoritative over this
file — resolve spec ambiguities there alongside the code change, don't just
patch around them. It's runtime-agnostic: it defines the template language
itself, including the filter *mechanism* and the `join`/`join last` filters
it guarantees, but deliberately not other filters (what exists, how each
formats output), since those wrap whatever the host runtime provides.
`docs/implementations/dotnet.md` is the source of truth for this .NET
implementation's own behavior on top of that (its own filters, such as
`date`/`currency`/`truncate`, plus any .NET-specific notes on `join`/
`join last`) — same rule applies, resolve ambiguities there. A port to
another runtime gets its own file under `docs/implementations/`, not edits
to `specs.md` or this one.

**`docs/specs.md`/`docs/implementations/dotnet.md` prose style**: every
paragraph that introduces a new concept gets a worked `markdown` example
(template → output) right there, not just a description — added
throughout during this project's readability pass, so keep doing it for
new sections. A MUST-rule or a gotcha that's easy to get wrong (a fixed
token needing exact spacing, a navigator ordering constraint, a filter's
value not being trimmed) gets a GFM alert blockquote (`> [!NOTE]`/
`[!TIP]`/`[!WARNING]`/`[!IMPORTANT]`) instead of being buried inline in
a paragraph. In `README.md` specifically, an *actual rendered output*
example is shown as live markdown (bold text, a real table) rather than
inside a fenced snippet, with the exact raw text preserved separately in
a `<details><summary>Raw output</summary>` block for anyone who wants
the literal characters.

When a new template-language feature raises a "what if X doesn't exist /
goes too far" question (see the general rule in `.claude/generic.md`),
check first whether resolving to nothing at render time (falsy, like
C#'s `?.`) already matches how the rest of the language treats missing
data (drilling into a null object, or a chain whose property doesn't
exist anywhere, already resolves to nothing — see Nested Property Access
and Resolving the Block Name in `docs/specs.md`) before reaching for a
`TemplateParseException`.

**Cold start?** Read `PLAN.md` first for implementation status and remaining
milestones, then `docs/architecture.md` for how the engine is actually built.
`IDEAS.md` holds speculative, non-committed notes — things worth
remembering, not things anyone's decided to build; unlike `PLAN.md` it
only grows, and only loses an entry once it's promoted into `PLAN.md`
as a real milestone or deliberately rejected.

This file, `.claude/generic.md`, `PLAN.md`, and `IDEAS.md` are
agent/contributor working files, not published documentation — none
should be linked from `README.md` or anything under `/docs`. The
published docs are `README.md` (basic) and `/docs` (`specs.md`,
`architecture.md`, `implementations/dotnet.md` and any future
per-runtime sibling — lowercase, no reference back to these working
files).

## Stack

C#/.NET, targeting `net10.0`. Layout:
- `/src/Guillemets` — the class library. `/test/Guillemets.Tests` — NUnit test
  project.
- `/specs` — the fixture corpus, the acceptance contract. Don't edit fixtures to
  make a test pass; if one looks wrong, fix it deliberately and say why. If
  satisfying a fixture demands disproportionate parser/engine complexity, check
  whether the fixture's *template* (not just its data/expected output) is shaped
  awkwardly before adding permanent special-casing — the same capability can
  often be exercised with a more natural template, and that's usually the better
  fix. Each case is a flat file pair or triple sharing a basename in a numbered
  group folder: `.guil.md`/`.md` (template/expected output) for success, or
  `.guil.md`/`.error` (expected exception message) for cases that must throw
  `TemplateParseException`, plus an optional `.json` data file — omit it and the
  case renders against `{}`, which is the common case for anything that doesn't
  touch data (parse-error cases, plain literal text). `glossary-localization`
  cases take a further optional `.<culture>.json` sidecar per language (e.g.
  `.en.json`, `.tr.json` — a JSON object of `"PropertyName": "Term"` entries,
  the same key/value direction `IStringLocalizer.GetAllStrings()` returns)
  alongside the `.json` data file, following the same per-exact-case (not
  shared-by-leading-digit) convention as `.json` — omit every culture and the
  case renders with no glossary at all. Several cases can share
  one template by giving the template just the group number and suffixing each
  case's `.md`/`.error` (and `.json`, if present) with a letter
  (`005-nested-blocks.guil.md` + `005a-...`/`005b-...`); `SpecTests.cs`
  matches a case to its template by leading digits. Group folders are numbered
  on disk for sort order only — refer to fixtures by name in prose, not number.
  `08-filters` only holds cases for the mechanism `docs/specs.md` actually
  guarantees (`join`/`join last`/`upper`/`lower`/`default`); a case whose
  expected output depends on `date`/`currency`/`truncate`'s exact .NET
  formatting belongs in a `test/Guillemets.Tests/*.cs` unit test instead
  (see `FilterFormattingTests.cs`/`FilterCultureTests.cs`), same as any
  other .NET-implementation-specific behavior — not the runtime-agnostic
  `/specs` corpus. `09-integration` is excluded from `SpecTests.cs`'s own
  discovery sweep entirely — it's exercised directly by each data source's
  own `*IntegrationTests.cs` instead.
- `Guillemets.slnx` at repo root (.NET 10's default `dotnet new sln` format).
- Central package management: `Directory.Packages.props` (versions) +
  `Directory.Build.props` (shared `TargetFramework`/`LangVersion`/
  `Nullable`/etc.), both at repo root.
- Assertions use **Shouldly**, not NUnit's `Assert.That`.
  PascalCase-of-space-words resolution uses **Humanizer.Core**'s
  `.Dehumanize()`, not hand-rolled splitting.

Exploit C# namespace lookup (see `.claude/generic.md`) deliberately for a
public extension method meant to be broadly discoverable: `Template`'s
`Render`/`RenderObject` extensions (`JsonElementExtensions.cs`/
`PocoExtensions.cs`/`JTokenExtensions.cs`, one per adapter folder under
`/src/Guillemets/Data`) live in the bare root `Guillemets` namespace, *not*
nested under their adapter's own namespace (`Guillemets.Data.Json`/
`Guillemets.Data.Poco`/`Guillemets.Data.Newtonsoft`, where the adapter types
`JsonElementDataSource`/`PocoDataSource`/`JTokenDataSource` themselves stay) —
so any consumer who already has `using Guillemets;` for `Template` gets the
extension methods for free, no extra `using` needed. `Render(JsonElement)` and
`Render(JToken)` are plain overloads of one name — their parameter types are
concrete and unrelated, so there's no ambiguity. `RenderObject(object)` keeps
its own name instead of also being called `Render`: `object` is broad enough
that folding it into the same overload set would blur which one a call
actually hits. A future adapter should follow the same split: adapter type in
its own `Guillemets.Data.X` namespace, extension method named `Render` in the
bare root `Guillemets` namespace if its parameter is a concrete type. All
adapters — including Newtonsoft's `JTokenDataSource` — live in the one core
`Guillemets` package; there's no per-adapter sibling project (decided when
`JTokenDataSource` was added: one package is simpler while there's only a
handful of adapters, and `Newtonsoft.Json` isn't a heavy dependency to carry).

## Core concepts

(Full detail in `docs/specs.md` — this is a map, not a replacement.)

- **Delimiters**: `«»`. A single `«»` is an inline variable/token; a run of two
  or more (`««`, `«««`, ...) opens a block, closed by the exact same run length
  or `TemplateParseException` is thrown — depth beyond 2 is cosmetic (fixtures
  go one guillemet deeper per nesting level, for readability, not because the
  parser requires it), validated via
  `OpenBlockToken.Depth`/`CloseBlockToken.Depth` in `Parser.ParseBlock`.
- **Property access**: `:` drills into objects and projects over lists
  (`.Select()`); chained across lists it flattens (`.SelectMany()`).
- **Scope navigation**: `.: name` pins resolution to the current scope only,
  skipping magic-var shadowing; `..: name` climbs to the enclosing scope,
  chainable (`..: ..: name`) and composable with `.: ` (`..: .: name`).
- **Blocks**: `««name` ... `»»`. Behavior is inferred from the resolved type of
  `name` — boolean → if, list → loop, object → scope. No keywords, same syntax
  for all three. Variable lookup falls back to enclosing scopes.
- **Else**: `~` on its own line splits truthy/falsy (or non-null/null) branches
  inside a block.
- **Magic loop variables**: `«first»`, `«last»`; `!` negates any boolean.
- **Variable definitions**: `««name = expr` ... `»»` captures a block's rendered
  output (or resolved value) into `name` for reuse below, under the same
  type-inferred if/loop/scope rules.
- **Tables**: a block may open/close with a leading/trailing `|` so it stays
  valid inside a markdown table row.
- **Inline lists**: scalar lists auto-join with `, `; override via the
  `join`/`join last` filters, usable inline or as the last line of a loop
  block.
- **Filters**: `name: value` chained with ` | ` after a property chain or
  another filter, no parens — `«expr | filter: value»`. `: ` (colon+space)
  is a fixed token, same as property access; nothing after it is trimmed.
  `\` escapes a reserved character. Built-ins: `date`, `currency`, `truncate`,
  `join`, `join last`, `upper`, `lower`, `default`. New built-in filter names
  should read as verbs (an action performed on a value) rather than nouns —
  `truncate`, not `length` — but this is a default, not absolute: a short,
  conventional name matching what other templating engines call the same
  operation can win, as `upper`/`lower` did over `uppercase`/`lowercase`.
  Don't relitigate `date`/`currency`, already-accepted names that read as
  nouns.

## Localization / naming

Templates are authored with natural, space-separated words — the author's
business vocabulary. Models are defined by developers in PascalCase/camelCase —
the developer's code vocabulary. Direct resolution (PascalCase-of-space-words
via Humanizer's `.Dehumanize()`) already bridges the two whenever they agree
case-insensitively. Where they don't (e.g. "quote no" vs. `OfferNo`), a
glossary bridges the rest: `Term = PropertyName` rows, matched
case-insensitively, additive over direct resolution rather than replacing it —
a term with no entry still falls back to direct resolution. See "Glossary &
Localization" in `docs/specs.md`.

`SpecTests.cs` builds its `IStringLocalizer` from a case's `.<culture>.json`
sidecar via `FakeStringLocalizer`, a minimal in-memory implementation.
`GlossaryResourceIntegrationTests.cs` separately exercises a real
`.restext`-backed `IStringLocalizer` — `Resources/Glossary.restext` plus a
same-named empty marker type in `Resources/Glossary.cs` (needed so
`ResourceManagerStringLocalizerFactory.Create(Type)` can locate the
compiled resource by namespace/name convention) — confirming the feature
also works against the real ASP.NET Core localization stack, not just the
fake.

## Working on this repo

General working habits (TDD ordering, migration audits, the "reviewed"
workflow, redesign checkpoints, no-failing-tests-at-commit-time, build
style enforcement) are in `.claude/generic.md` — this section is just
how they apply here.

- `make init` (alias `make fix-owners` — same recipe, reach for whichever
  name fits: initial sandbox setup or a later ownership fix) downloads
  `setup-claudedev-sandbox.sh` from `cihandeniz/config-files` into the
  gitignored `.tmp/scripts/` on first use (cached after that — delete
  `.tmp/` to force a re-download) and runs it with `sudo`. The script
  itself lives outside this repo now; don't recreate `scripts/` here.
- Run `dotnet test` from the repo root for the full fixture suite — each fixture
  becomes one NUnit test case, named by its relative path under `/specs`.
- Engine work proceeds fixture-group by fixture-group, simplest → most complex —
  implement one group's mechanic, confirm `dotnet test` flips exactly that group
  green with no regressions, then move on. The generic "one test case at a
  time" TDD loop applies per fixture within that group.
- The redesign-checkpoint and no-failing-tests-at-commit-time rules use
  `SpecTests.cs`'s `IGNORED_FIXTURES` set (`Ignored`, never `Failed`) as
  their concrete mechanism — remove a fixture's name once its case goes
  green.

## Parking (ending a session)

Follow the general checklist in `.claude/generic.md`. Here, that means:
`dotnet test` for step 1; `PLAN.md`'s "Remaining milestones" for step 2;
`docs/architecture.md` for step 3; `.claude/specific.md` (this file) for
step 4, unless the learning isn't guillemets-specific, in which case
`.claude/generic.md` instead.
