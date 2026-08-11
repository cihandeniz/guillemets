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

## Remaining milestones

In fixture-group order (see `/specs`, simplest → most complex; group folders are
numbered on disk purely for sort order — referred to here by name only).
`variable-definitions` is fully done, including `definition-list-separator`:
`BlockParser` now recognizes a `(separator = ...)` line as the line
immediately before a block's own `»»` — whichever branch that falls in, the
truthy body when there's no `~`, or the falsy body when there is one (`~`
itself is never adjacent to the separator; it stays on its own line like any
other block) — and pulls it out as `BlockNode.Separator`, which
`LoopBehavior` uses to join each iteration's trimmed render instead of
concatenating them raw.
`05-variable-definitions/004-definition-list-separator-else` covers both
branches. That footer form is the only `(name = value)` parsing that exists
so far — the *inline* form, attached directly inside `«...»`
after a property chain (`«quote: tags (separator = ; )»`,
`«date (date = dd/MM/yyyy)»`), is still unimplemented and is what the
`filters` milestone below has to add.

1. `filters` — `date`/`currency`/`length`, plus inline `(separator = ...)` on a
   bare variable/property chain (`07-inline-lists/003-custom-separator` needs
   this too, since it's the same inline attachment point). First milestone
   that needs typed (`DateTime`/`decimal`) access rather than just display
   strings/booleans — this is where the deferred `IDataSource` typed-access
   question (see `docs/architecture.md`) gets decided, test-first.
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
