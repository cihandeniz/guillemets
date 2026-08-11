# Implementation plan

Living roadmap for building the guillemets engine against the `/specs` fixture
corpus. This file shrinks as milestones complete — it's *what's left*, not a
history of what's done. This is an agent/contributor working file, not published
documentation — see `README.md`/`docs/` for that. For *how* it's built, see
`docs/architecture.md`; for *how* to work (TDD discipline, code style), see
`CLAUDE.md`.

## Status

36 of 45 fixtures pass (`dotnet test` is authoritative, via `SpecTests.cs`).
Remaining fixtures are listed in `SpecTests.cs`'s `IGNORED_FIXTURES` set.
Pluggable data sources (JSON, POCO, and Newtonsoft `JToken`) are done — see
`docs/architecture.md`.

## Remaining milestones

In fixture-group order (see `/specs`, simplest → most complex; group folders are
numbered on disk purely for sort order — referred to here by name only). One
exception: `filters` is numbered last on disk (`08-filters`, after
`variable-definitions`/`tables`/`inline-lists`) but has to be *implemented*
first, because `definition-list-separator` (the one remaining
`variable-definitions` fixture) needs `(separator = , )` filter parsing — leave
that single fixture for last within the `variable-definitions` milestone rather
than reordering the milestones themselves.

1. `filters` — `date`/`currency`/`length`. First milestone that needs typed
   (`DateTime`/`decimal`) access rather than just display strings/booleans —
   this is where the deferred `IDataSource` typed-access question (see
   `docs/architecture.md`) gets decided, test-first.
2. `variable-definitions` — `definition-boolean` and `definition-object` done;
   `definition-list-separator` needs `(separator = , )` filter parsing from
   the milestone above, so do it last.
3. `tables` — should mostly fall out of the above once blocks exist; confirm
   rather than build new.
4. `inline-lists` — field-selection projection and custom `(separator)` already
   work for `variables/nested-property-chained-list`-style cases; confirm the
   remaining fixtures, particularly the loop-with-separator form.
5. `integration` — the full worked example, combining everything above. Already
   has dedicated, currently-`[Ignore]`d coverage in
   `JsonIntegrationTests`/`PocoIntegrationTests`/`JTokenIntegrationTests` —
   un-ignore all three once this milestone lands, and drop the `09-integration`
   exclusion note in `SpecTests.cs` if it's ever folded back into the generic
   sweep.
6. `errors` — currently 3 fixtures (`unclosed-guillemet`, `unclosed-block`,
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
