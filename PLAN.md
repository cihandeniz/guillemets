# Implementation plan

Living roadmap for building the guillemets engine against the `/specs`
fixture corpus. This file shrinks as milestones complete — it's *what's
left*, not a history of what's done. Agent/contributor working file, not
published documentation — see `README.md`/`docs/` for that. For *how*
it's built, see `docs/architecture.md`; for *how* to work (TDD
discipline, code style), see `CLAUDE.md`.

## Status

`dotnet test` is green: 128 passed, 0 skipped, 128 total, 0 failed.
`filter-syntax-redesign` and `integration` are both fully done — the
`: `/` | ` grammar, the global `\` escapes, the scoped `\n`/`\t`/`\|`
filter-value escapes, every inline filter (`date`/`join`/`currency`/
`truncate`/`join last`), the block-footer pipeline (including `join`'s
context-dependent bare default, `, ` inline vs. newline in a footer),
and the full `001-customer-offer`/`002-almost-errors` worked examples
(across all three data sources) all pass. Pluggable data sources (JSON,
POCO, Newtonsoft `JToken`), `tables`, and `inline-lists` are all done —
see `docs/architecture.md`.

## Remaining milestones

In priority order, matching disk order under `/specs`
(`variable-definitions`, `tables`, `filter-syntax-redesign`, and
`integration` are fully done, so the list picks up after them).

1. `errors` — currently 6 fixtures (`unclosed-guillemet`,
   `unclosed-block`, `mismatched-block-depth`, `literal-shares-close-line`,
   `unclosed-block-dangling-filter-pipe`, plus one retired alongside the
   old filter grammar). Add more error cases as new failure modes
   appear — extend `TemplateParseException` usage rather than introducing
   ad hoc exceptions.
2. `schema-localization` — true schema/localization remapping (business
   term ≠ property name), per "Schema & Localization" in `docs/specs.md`:
   a mapping table (`Localized Term = template token = PropertyName`)
   resolved case-insensitively against the default language, for cases
   where direct PascalCase-of-space-words resolution via Humanizer
   doesn't already match. No `/specs` fixture group exists for this yet —
   add one, test-first. Needs a design decision, before writing fixtures,
   on where the mapping table itself is supplied from (a data source
   alongside the render call? a separate file/format?) since nothing in
   the engine's public API accepts one today.

## Known v1 scope decisions (not gaps to "fix" without discussion)

- **Currency/date/truncation formatting** in the `filters`/`integration`
  fixtures matches the fixtures as authored, not an independently pinned
  spec — don't "correct" it without discussion.
- **Unresolved block name → falsy, not an error** — per
  `conditional-blocks/unresolved-property-no-else` and
  `integration/002-almost-errors`. Holds both when the *container*
  resolves but is empty (e.g. an empty array whose items are never
  individually visited) and when the property is absent from the data
  entirely (`PropertyResolver.Project` uses `TryGetProperty`'s own
  `UndefinedDataSource` fallback instead of throwing).
- **Negating a non-last property-chain segment** (e.g. `people: !male:
  !parent`) is documented as unsupported (`docs/specs.md`, Negation), but
  isn't enforced yet — `PropertyChainNode.LastSegmentNegated` silently
  drops an earlier `!` instead of raising a `TemplateParseException`.
  Worth an `errors` fixture once someone decides it should actually fail
  loudly rather than stay silent.
