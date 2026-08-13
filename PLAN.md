# Implementation plan

Living roadmap for building the guillemets engine against the `/specs`
fixture corpus. This file shrinks as milestones complete — it's *what's
left*, not a history of what's done. Agent/contributor working file, not
published documentation — see `README.md`/`docs/` for that. For *how*
it's built, see `docs/architecture.md`; for *how* to work (TDD
discipline, code style), see `CLAUDE.md`.

## Status

`dotnet test` is green: 145 passed, 0 skipped, 145 total, 0 failed.
`filter-syntax-redesign` and `integration` are both fully done — the
`: `/` | ` grammar, the global `\` escapes, the scoped `\n`/`\t`/`\|`
filter-value escapes, every inline filter (`date`/`join`/`currency`/
`truncate`/`join last`), the block-footer pipeline (including `join`'s
context-dependent bare default, `, ` inline vs. newline in a footer),
and the full `001-customer-offer`/`002-almost-errors` worked examples
(across all three data sources) all pass. Pluggable data sources (JSON,
POCO, Newtonsoft `JToken`), `tables`, and `inline-lists` are all done —
see `docs/architecture.md`. A spec-hardening pass closed every known
ambiguity in `docs/specs.md` and fixed four real bugs it surfaced along
the way (`SymbolTree`/`CloseToken` mishandling a `»»` run not followed
by another `»` or a newline; inline resolution of a list-projected
boolean chain leaking raw `True`/`False` instead of filtering; a loop
header flattening through two list levels crashing instead of merging;
and a block-footer filter pipeline not requiring the same-line-as-`»»`
gluing the spec always assumed) — no open ambiguities remain.

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
2. Explicit scope-navigation syntax — `.: name` for "this scope only,"
   bypassing magic-var shadowing (`.: first` reaches the current
   scope's own `first` property even where the magic `«first»` would
   otherwise shadow it — see Magic Loop Variables in `docs/specs.md`),
   and `..: name` for "climb to the parent scope," chainable (`..: ..:
   name` climbs two levels). The two compose: `..: .: name` climbs one
   level, then applies `.: ` there, reaching that parent's own property
   with its magic var skipped too. Needs design work before fixtures:
   exact grammar for `.: `/`..: ` as property-chain-leading markers
   distinct from the existing `: ` property accessor, how far a `..: `
   chain can climb before erroring past the root scope, and how it
   interacts with `!` negation and filters.
3. Rehumanize `docs/specs.md` and the other published docs (`README.md`,
   `docs/architecture.md`, `docs/implementations/dotnet.md`,
   `docs/README.md`) — a readability/tone pass, not a correctness one,
   after this session's many incremental edits.
