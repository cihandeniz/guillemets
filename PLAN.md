# Implementation plan

Living roadmap for building the guillemets engine against the `/specs`
fixture corpus. This file shrinks as milestones complete — it's *what's
left*, not a history of what's done. Agent/contributor working file, not
published documentation — see `README.md`/`docs/` for that. For *how*
it's built, see `docs/architecture.md`; for *how* to work (TDD
discipline, code style), see `CLAUDE.md`.

## Status

`dotnet test` is green: 141 passed, 0 skipped, 141 total, 0 failed.
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
`integration` are fully done, so the list picks up after them; `errors`
has no further known gaps — add a fixture directly, per `CLAUDE.md`,
whenever a new failure mode turns up rather than tracking it here).

1. `schema-localization` — true schema/localization remapping (business
   term ≠ property name), per "Schema & Localization" in `docs/specs.md`:
   a mapping table (`Localized Term = template token = PropertyName`)
   resolved case-insensitively against the default language, for cases
   where direct PascalCase-of-space-words resolution via Humanizer
   doesn't already match. No `/specs` fixture group exists for this yet —
   add one, test-first. Needs a design decision, before writing fixtures,
   on where the mapping table itself is supplied from (a data source
   alongside the render call? a separate file/format?) since nothing in
   the engine's public API accepts one today.

## Spec hardening backlog

From a time-boxed spec/coverage review. Everything found is resolved
except three ambiguities below, each still needing a decision before a
wording fix (none are known bugs — just unstated behavior). Along the
way this surfaced and fixed three real bugs, each now locked in by a
fixture: `SymbolTree.ExtendMatch`/`CloseToken` mishandling a `»»` run
not followed by another `»` or a newline; inline resolution of a
list-projected boolean chain (e.g. `«items: active»`) leaking raw
`True`/`False` instead of filtering, per `PropertyResolver.Resolve`;
and `TryResolveArrayItems` crashing on a loop header chain that
flattens through two list levels (e.g. `quotes: prices`) instead of
merging them into one loop.

1. Table "footer" (trailing rows) vs. filter "Block Footer" — unclear
   if/how the two interact within one loop block.
2. Nested-loop `first`/`last` shadowing is unaddressed.
3. `first`/`last` availability after "Filtering Out Items in Lists"
   collapses a loop down to a single scoped item is unaddressed.
