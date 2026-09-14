# guillemets

Project-specific conventions. General .NET/C# style and working habits are in
`.claude/generic.md` — read both.

## Project

Guillemets — a markdown-aware template engine for non-technical authors. `«»`
(U+00AB/U+00BB) are the sole delimiters, chosen for readability over
writability; they never collide with markdown. See `README.md`.

Doc ownership, in precedence order:

- `docs/specs.md` — the source of truth for behaviour, authoritative over this
  file. Runtime-agnostic: it defines the language itself, including the filter
  *mechanism* and the `join`/`join last` filters it guarantees, but not what
  other filters exist or how they format.
- `docs/implementations/dotnet.md` — this .NET implementation's own behaviour
  on top of that (`date`/`currency`/`truncate`, plus .NET-specific notes). A
  port to another runtime gets its own file here, never edits to `specs.md`.
- `docs/architecture.md` — how the engine is built. See `.claude/generic.md`
  for what belongs there. High-level only: meta entities and the parse/render
  flow, never a feature's details.
- `docs/symbols.md` — the concrete symbol table and its trie diagram. Lives
  apart from `architecture.md` so adding a symbol doesn't churn that doc.
- `docs/cheatsheet.md` — one example per feature, no prose. Generated examples
  were verified against the engine; keep it that way when adding a feature.

Resolve a spec ambiguity in the owning doc alongside the code change; don't
patch around it. Section order in `docs/specs.md` follows the `/specs` folder
groups, so a fixture group and a spec section map one to one — note that its
full example contains `##` headings inside a fence, so anything walking that
file's structure has to track fences rather than grep for `^## `.

Published-doc prose style: every paragraph introducing a concept gets a worked
`markdown` example (template → output) right there. A MUST-rule or an
easy-to-get-wrong gotcha gets a GFM alert (`> [!NOTE]`/`[!TIP]`/`[!WARNING]`/
`[!IMPORTANT]`) rather than being buried inline. `README.md` additionally shows
rendered output as live markdown, with the literal characters preserved in a
`<details><summary>Raw output</summary>` block.

When a new language feature raises a "what if X doesn't exist" question, check
whether resolving to nothing at render time already matches how the rest of the
language treats missing data before reaching for a `TemplateParseException`.

Adding a symbol? Check it against markdown rendering of the *template* itself,
not just the output — a `.guil.md` gets read on GitHub, so a marker that pairs
into emphasis/strikethrough or opens a fence would defeat the point of `«»`.
`pandoc -f gfm -t html` over the fixtures, with a known-positive control,
settles it.

**Cold start**: find the PR for the current branch, then read
`docs/architecture.md`. The canonical repo is `mouseless/guillemets` — PRs and
issues live there regardless of what a local `origin` points to.

## Stack

C#/.NET, `net10.0`. `/src/Guillemets` is the library, `/test/Guillemets.Tests`
the NUnit project, `Guillemets.slnx` the solution. Central package management
via `Directory.Packages.props` + `Directory.Build.props`. Assertions use
Shouldly; PascalCase-of-space-words uses Humanizer's `.Dehumanize()`.

`/src` exploits C# namespace lookup for `Template`'s extensions:
`JsonElementExtensions.cs`/`PocoExtensions.cs`/`JTokenExtensions.cs` live in the
bare `Guillemets` namespace, not their adapter's own, so `using Guillemets;`
pulls them in. `RenderObject` keeps its own name since `object` is too broad to
overload on. All adapters ship in one package.

### The `/specs` corpus

The acceptance contract. Don't edit a fixture to make a test pass; if one looks
wrong, fix it deliberately and say why. If satisfying a fixture demands
disproportionate engine complexity, check whether its *template* is shaped
awkwardly before adding permanent special-casing.

Each case is a flat file group sharing a basename inside a numbered folder:

- `.guil.md` + `.md` — template and expected output.
- `.guil.md` + `.error` — expected `TemplateParseException` message.
- `.json` — optional data; omit it and the case renders against `{}`.
- `.<culture>.json` — optional glossary sidecar, per exact case.

Several cases share one template by giving it just the group number and
suffixing each case with a letter (`005-nested-blocks.guil.md` +
`005a-...`/`005b-...`); `SpecTests.cs` matches by leading digits.

Folders are numbered for sort order only — refer to fixtures by name in prose.
Feature groups run from `00-` up and `99-errors` stays last; a new group
appends at the next free number, nothing is renumbered, and the gap before 99
stays. `08-filters` holds only what `docs/specs.md` guarantees; a case whose
output depends on .NET formatting belongs in a unit test instead.
`09-integration` is excluded from `SpecTests.cs` and driven by each data
source's own `*IntegrationTests.cs`.

## Core concepts

A map only — `docs/specs.md` is the contract, and its rules are easy to
re-derive wrongly from the code alone. Go there before changing behaviour.

- **Delimiters**: `«»`. One is an inline variable; a run of two or more opens a
  block, closed by the same run length. Depth beyond 2 is cosmetic.
- **Property access**: `:` drills into objects and projects over lists;
  chained across lists it flattens.
- **Scope navigation**: `.: name` pins to the current scope, `..: name` climbs;
  both chainable and composable.
- **Blocks**: `««name` ... `»»`, behaviour inferred from the resolved type —
  boolean → if, list → loop, object → scope. No keywords. Lookup falls back to
  enclosing scopes.
- **Else**: `~` alone on a line splits the branches.
- **Whitespace**: blank lines around markers are required and partly swallowed;
  `««~`/`~»»` trim at render time only; a hard-wrapped `«...»` treats one
  newline as the space a symbol needs, two as a paragraph break.
- **Magic loop variables**: `«first»`, `«last»`; `!` negates any boolean.
- **Variable definitions**: `««name = expr` ... `»»` captures rendered output
  for reuse below.
- **Tables**: a block may open/close with a leading/trailing `|` to stay valid
  in a markdown table row.
- **Blockquotes**: any block works inside `>`-prefixed lines, at any nesting
  depth. Depth is counted in `Quote` tokens, so `>>` and `> >` are the same;
  blank lines render a canonical marker, one `>` per level.
- **Inline lists**: scalar lists auto-join with `, `; override with
  `join`/`join last`.
- **Filters**: `name: value` chained with ` / `, no parens. Built-ins: `date`,
  `currency`, `truncate`, `join`, `join last`, `upper`, `lower`, `default`.

New built-in filter names should read as verbs (`truncate`, not `length`), but
a short conventional name other engines share can win, as `upper`/`lower` did.
`date`/`currency` are settled; don't relitigate them.

## Localization / naming

Authors write natural space-separated words; models are PascalCase. Direct
resolution via `.Dehumanize()` bridges them whenever they agree
case-insensitively. Where they don't ("quote no" vs `OfferNo`), a glossary of
`Term = PropertyName` rows is matched case-insensitively and is *additive* — a
term with no entry still falls back to direct resolution.

`SpecTests.cs` builds its `IStringLocalizer` from a case's `.<culture>.json` via
`FakeStringLocalizer`. `GlossaryResourceIntegrationTests.cs` separately
exercises a real `.restext`-backed localizer — `Resources/Glossary.restext`
plus a same-named empty marker type, needed so
`ResourceManagerStringLocalizerFactory.Create(Type)` can locate the resource by
convention.

## Working on this repo

- Run `dotnet test` from the repo root; each fixture becomes one NUnit case
  named by its path under `/specs`.
- `SpecTests.cs`'s `IGNORED_FIXTURES` is the concrete mechanism for the
  redesign-checkpoint and no-failing-tests rules. Remove a name once it's
  green.
- Work fixture-group by fixture-group, simplest first: implement one group's
  mechanic, confirm `dotnet test` flips exactly that group green, move on.
- `make init` (alias `make fix-owners`) downloads a setup script from
  `cihandeniz/config-files`, pinned by commit and verified against a SHA256,
  and runs it with `sudo`. Bumping the pin means updating both Makefile
  variables — the checksum exists to catch a pin bumped without review.
- CI is modeled on `mouseless/baked`, adapted for this repo's single-package
  shape. Coverage goes through `coverlet.collector` with `test/runsettings.xml`
  — baked's `dotnet test --coverage` flags need Microsoft.Testing.Platform,
  which this classic VSTest + `NUnit3TestAdapter` host doesn't support. Check
  the test host before copying more from baked.

## Parking

The general checklist is in `.claude/generic.md`. Here: `dotnet test`,
`__PR_DESC_UPDATE__.md`, `docs/architecture.md`, and this file.
