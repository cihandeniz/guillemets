# Implementation plan

Living roadmap for building the guillemets engine against the `/specs` fixture
corpus. This file shrinks as milestones complete — it's *what's left*, not a
history of what's done. This is an agent/contributor working file, not published
documentation — see `README.md`/`docs/` for that. For *how* it's built, see
`docs/architecture.md`; for *how* to work (TDD discipline, code style), see
`CLAUDE.md`.

## Status

39 of 46 fixtures pass (`dotnet test` is authoritative, via `SpecTests.cs`).
Remaining fixtures are listed in `SpecTests.cs`'s `IGNORED_FIXTURES` set.
Pluggable data sources (JSON, POCO, and Newtonsoft `JToken`) are done — see
`docs/architecture.md`.

## Refactor backlog

Surfaced during an architecture review ahead of the `filters` milestone.
None of these block starting `filters`, but the milestone will make each one
worse if left alone — resolve alongside the fixture work they touch, not as
a separate cleanup pass.

- **`BlockParser.TryParseSeparatorFooter` hardcodes the filter name
  `"separator"`** (`filter.Name != "separator"`) to decide whether a parsed
  `(name = value)` belongs to the block footer. `date`/`currency`/`length`
  will need the same attach-and-validate shape in a different position
  (inline after a property chain) — decide whether "which filter names are
  legal where" moves to a shared spot before it's copy-pasted a third time.
- **No shared shape for "this node has filters attached."** `BlockNode`
  carries a bespoke `Separator` field (`string?`) fed straight to
  `LoopBehavior`; `VariableNode` has no filter field at all yet. Once
  `VariableNode` needs `(date = ...)`/`(currency = ...)`/`(length = ...)`,
  and `BlockNode` already has `(separator = ...)`, that's two node types
  independently inventing "optional filter payload." Consider a shared
  `IReadOnlyList<FilterNode>` both hold, with one "apply filters to a
  value/rendered string" step.
- **No concept of *applying* a filter yet, only parsing one.**
  `FilterNode.Render` throws by design — today's only consumer
  (`separator`) reads `.Name`/`.Value` directly and never renders it.
  `date`/`currency`/`length` need real behavior: given a resolved
  `IDataSource` and a filter value string, produce a formatted string. Per
  house style (polymorphic dispatch over switch-on-type), this likely wants
  an `IFilter`-per-filter-name strategy (`DateFilter`, `CurrencyFilter`,
  `LengthFilter`), not a branch inside `VariableNode.Render`. This is also
  where the deferred `IDataSource` typed-access question (no `AsDateTime()`/
  `AsDecimal()` yet) has to actually get resolved.
- **`PropertyResolver.Resolve(IDataSource, PropertyChain)`'s return value
  conflates two meanings.** `IEnumerable<IDataSource>` means "several
  scalar results from projecting through a list" in one caller
  (`«quotes: prices: amount»`) and "one value that happens to itself be an
  array" in another (`«tags»`) — nothing today distinguishes them except
  inspecting `.Kind` on each yielded item, which nothing does. This is
  likely a prerequisite for the `inline-lists` milestone, not just cleanup
  — it's why `«tags»` still renders JSON's raw `["a","b"]` instead of
  joining it. `PropertyResolver.cs` already carries a `// TODO REFACTOR`
  marker on this.
- **`PropertyResolver.Resolve` is overloaded twice with different
  semantics** — `Resolve(Scope, PropertyChain)` (scope-aware, handles
  magic variables/captured variables/enclosing-scope fallback) vs.
  `Resolve(IDataSource, PropertyChain)` (a plain scope-free walker) — both
  `public` under the same name. New callers (filters resolving a value
  before formatting it) will have to guess which one they want. Worth a
  rename when this file is next touched.

## Remaining milestones

In fixture-group order (see `/specs`, simplest → most complex; group folders are
numbered on disk purely for sort order — referred to here by name only).
`variable-definitions` is fully done.

1. `filters` — `date`/`currency`/`length`, plus the *inline* `(name = value)`
   form attached directly inside `«...»` after a property chain
   (`«quote: tags (separator = ; )»`, `«date (date = dd/MM/yyyy)»`) —
   `07-inline-lists/003-custom-separator` needs this too, since it's the same
   attachment point. The block-footer form of `(separator = ...)` is already
   done (`BlockParser`/`FilterParser`, see `docs/architecture.md`) — this
   milestone is only about the inline form. First milestone that needs typed
   (`DateTime`/`decimal`) access rather than just display strings/booleans —
   this is where the deferred `IDataSource` typed-access question (see
   `docs/architecture.md`) gets decided, test-first.
2. `tables` — should mostly fall out of blocks already existing; confirm rather
   than build new.
3. `inline-lists` — `001-inline-scalar-list`/`002-inline-field-selection` need
   `VariableNode` to join an array's elements with the default `, ` separator
   when resolution yields a list (it currently renders the array's own
   `AsDisplayString()` instead, e.g. JSON's raw `["a","b"]`);
   `003-custom-separator` needs the inline `(separator = ...)` parsing from the
   milestone above.
4. `integration` — the full worked example, combining everything above. Already
   has dedicated, currently-`[Ignore]`d coverage in
   `JsonIntegrationTests`/`PocoIntegrationTests`/`JTokenIntegrationTests` —
   un-ignore all three once this milestone lands, and drop the `09-integration`
   exclusion note in `SpecTests.cs` if it's ever folded back into the generic
   sweep.
5. `errors` — currently 3 fixtures (`unclosed-guillemet`, `unclosed-block`,
   `mismatched-block-depth`). Add more error cases as new failure modes appear —
   extend `TemplateParseException` usage rather than introducing ad hoc
   exceptions.

## Known v1 scope decisions (not gaps to "fix" without discussion)

- **True schema/localization remapping** (business term ≠ property name) is out
  of scope — only direct PascalCase-of-space-words resolution via Humanizer is
  implemented.
- **Currency/date/truncation formatting** in the `filters`/ `integration`
  fixtures matches the fixtures as authored, not an independently pinned spec —
  don't "correct" it without discussion.
- **Unresolved block name → falsy, not an error** — see
  `conditional-blocks/unresolved-property-no-else`.
- **Negating a non-last property-chain segment** (e.g. `people: !male: !parent`)
  is documented as unsupported (`docs/specs.md`, Negation), but isn't enforced
  yet — `PropertyChain.LastSegmentNegated` silently drops an earlier `!` instead
  of raising a `TemplateParseException`. Worth an `errors` fixture once someone
  decides it should actually fail loudly rather than stay silent.
