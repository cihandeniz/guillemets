# guillemets

Project-specific conventions. General .NET/C# style and working habits are in
`.agents/generic.md` — read both.

## Project

Guillemets — a markdown-aware template engine for non-technical authors. `«»`
(U+00AB/U+00BB) are the sole delimiters, chosen for readability over
writability; they never collide with markdown. See `README.md`.

Doc ownership, in precedence order:

- `docs/specs.md` — the source of truth for behaviour, authoritative over this
  file. Runtime-agnostic: it defines the language itself, including the filter
  *mechanism* and the filters it guarantees (`join`, `join last`, `upper`,
  `lower`, `default`, `truncate`), but not what other filters exist or how they
  format.
- `docs/implementations/dotnet.md` — this .NET implementation's own behaviour
  on top of that (`date`/`currency`/`number`, plus .NET-specific notes). A
  port to another runtime gets its own file here, never edits to `specs.md`.
- `docs/architecture.md` — how the engine is built. See `.agents/generic.md`
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

A markdown construct a feature merely has to survive — a table, a blockquote —
is not a feature of its own and gets no group or top-level section. It earns one
basic case in `00-basics` proving it passes through, and beyond that every case
lives with the Guillemets feature it combines with, so a table inside a loop is
a loop case and a wrapped reference inside a quote is a variables case.
`docs/specs.md` mirrors that as subsections (`### As a Table`, `### In a
Blockquote` under Blocks; `### Quote Markers` under Whitespace). Without this
the corpus repeats every feature once per markdown construct.

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

A fixture covers a whole feature rather than one line of it — several related
cases merged into one template, each keeping its own name as a `##` heading
above the lines it contributed, so the name survives the merge and the expected
output reads as a labelled list. Every fixture opens with an `# H1` titling
itself, its own basename in Title Case (`001-simple-variable` → `# Simple
Variable`) — renaming a fixture means retitling it in the same edit, in both the
`.guil.md` and the `.md`. Three kinds of fixture go untitled, because a title
would change what they assert: `90-integration`, whose samples are whole
realistic documents where the `# H1` is content rather than a label; error
fixtures, whose message pins a line and column a title would shift; and a case
whose premise is that nothing precedes the block, like
`03-blocks/005-block-trim-at-template-edges`. Split only where the data would
conflict (two cases needing the same property to hold different values) or where
absence is the point and half the output would otherwise render empty.

Several cases share one template by giving it just the group number and
suffixing each case with a letter (`005-nested-blocks.guil.md` +
`005a-...`/`005b-...`); `SpecTests.cs` matches by leading digits.

The corpus reads front to back as a teaching order, so a fixture MUST NOT use
syntax no earlier group has introduced. When a case needs a later feature, it
moves to that feature's group rather than the group it thematically belongs to —
a filter case that needs a block is a block case. Filters sit early, right after
variables, because nearly everything else uses them.

Folders are numbered for sort order only — refer to fixtures by name in prose.
Feature groups run from `00-` up and `90-integration` stays last; a new group
appends at the next free number, and the gaps stay — including the ones left by
a dissolved group. `02-filters` holds only what `docs/specs.md` guarantees; a
case whose output depends on .NET formatting belongs in a unit test instead.
Errors live with the feature they break, `9xx-` prefixed so they sort last
within their group, and carry no `# H1` since their message pins a line and
column. `90-integration` is excluded from `SpecTests.cs` and driven by each data
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
  depth. Depth is counted in `Quote` tokens, so `>>` and `> >` are the same
  depth; a blank line keeps its own spelling minus a trailing space, and one the
  engine produces copies the block's opening spelling.
- **Inline lists**: scalar lists auto-join with `, `; override with
  `join`/`join last`.
- **Filters**: `name: value` chained with ` / `, no parens. Built-ins: `date`,
  `currency`, `join`, `join last`, `upper`, `lower`, `default`, `truncate`.

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

The general checklist is in `.agents/generic.md`. Here: `dotnet test`,
`__PR_DESC_UPDATE__.md`, `docs/architecture.md`, and this file.
