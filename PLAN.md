# Implementation plan

Living roadmap for building the guillemets engine against the `/specs` fixture
corpus. This file shrinks as milestones complete — it's *what's left*, not a
history of what's done. This is an agent/contributor working file, not published
documentation — see `README.md`/`docs/` for that. For *how* it's built, see
`docs/architecture.md`; for *how* to work (TDD discipline, code style), see
`CLAUDE.md`.

## Status

98 of 108 fixture cases pass (`dotnet test` is authoritative, via
`SpecTests.cs`). Remaining fixtures are listed in `SpecTests.cs`'s
`IGNORED_FIXTURES` set. Pluggable data sources (JSON, POCO, and Newtonsoft
`JToken`) are done — see `docs/architecture.md`.

## Remaining milestones

In priority order (not disk order — group folders are numbered purely for
sort order, referred to here by name only). `variable-definitions` is fully
done. `filters` was originally first but has been pushed to last: it's the
most speculative milestone (typed `IDataSource` access is still undecided),
so the simpler, better-understood milestones go first.

1. `tables` — should mostly fall out of blocks already existing; confirm
   rather than build new.
2. `inline-lists` — `001-inline-scalar-list`/`002-inline-field-selection`
   need `VariableNode` to join an array's elements with the default `, `
   separator when resolution yields a list (it currently renders the array's
   own `AsDisplayString()` instead, e.g. JSON's raw `["a","b"]`);
   `003-custom-separator` needs the inline `(separator = ...)` parsing from
   the `filters` milestone below, so it can't finish until that lands.
3. `integration` — the full worked example, combining everything above.
   Already has dedicated, currently-`[Ignore]`d coverage in
   `JsonIntegrationTests`/`PocoIntegrationTests`/`JTokenIntegrationTests` —
   un-ignore all three once this milestone lands, and drop the
   `09-integration` exclusion note in `SpecTests.cs` if it's ever folded back
   into the generic sweep.
4. `errors` — currently 5 fixtures (`unclosed-guillemet`, `unclosed-block`,
   `mismatched-block-depth`, `literal-shares-close-line`,
   `non-separator-filter`). Add more error cases as new failure modes
   appear — extend `TemplateParseException` usage rather than introducing ad
   hoc exceptions.
5. `filters` — `date`/`currency`/`length`, plus the *inline* `(name = value)`
   form attached directly inside `«...»` after a property chain
   (`«quote: tags (separator = ; )»`, `«date (date = dd/MM/yyyy)»`) —
   `07-inline-lists/003-custom-separator` needs this too, since it's the same
   attachment point. The block-footer form of `(separator = ...)` is already
   done (`BlockParser`/`FilterParser`, see `docs/architecture.md`) — this
   milestone is only about the inline form. First milestone that needs typed
   (`DateTime`/`decimal`) access rather than just display strings/booleans —
   this is where the deferred `IDataSource` typed-access question (see
   `docs/architecture.md`) gets decided, test-first.

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
