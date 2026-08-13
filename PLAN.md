# Implementation plan

Living roadmap for building the guillemets engine against the `/specs`
fixture corpus. This file shrinks as milestones complete — it's *what's
left*, not a history of what's done. Agent/contributor working file, not
published documentation — see `README.md`/`docs/` for that. For *how*
it's built, see `docs/architecture.md`; for *how* to work (TDD
discipline, code style), see `CLAUDE.md`.

## Status

`dotnet test` is green: 146 passed, 4 skipped, 150 total, 0 failed. The 4
skipped are `glossary-localization` fixtures pending implementation (see
Remaining milestones, below).
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

1. `glossary-localization` — true glossary/localization remapping (business
   term ≠ property name), per "Glossary & Localization" in `docs/specs.md`
   and `docs/implementations/dotnet.md`. Design is settled: a glossary is
   `Term = PropertyName` rows, matched case-insensitively per segment,
   additive over direct resolution (a term with no entry falls back to
   PascalCase-of-space-words as today). Supplied at parse time —
   `Template.Create(template, glossary)` alongside the existing
   `configureFilters` overload — as an `IStringLocalizer`
   (`Microsoft.Extensions.Localization.Abstractions`): the resource key is
   the property name, its value (for whichever culture is ambient) is the
   term. Because the actual lookup happens during `Render`, not `Create`,
   each render re-resolves against `CultureInfo.CurrentUICulture` on the
   calling thread at that moment — no separate culture parameter on
   `Render`. The `/specs/13-glossary-localization` fixture group exists
   (`001-basic-mapping`, `002-fallback-not-in-glossary`,
   `003-case-insensitive-glossary-match`, `004-nested-chain-per-segment`,
   `005-block-header`, each with a `.en.json` sidecar — a JSON object of
   `"PropertyName": "Term"` entries, the same key/value direction
   `IStringLocalizer.GetAllStrings()` returns) and is red against the
   current engine except `002` (whose expected behavior needs no glossary
   at all) — the other four are listed in `SpecTests.cs`'s
   `IGNORED_FIXTURES`. Still to do:
   - Add the `Microsoft.Extensions.Localization.Abstractions` package
     reference.
   - Implement glossary resolution in `PropertyResolver`/`Scope`, threaded
     through a new `Template.Create` overload.
   - Wire `SpecTests.cs` to load a case's `.<culture>.json` sidecar(s) (if
     present) into a fake `IStringLocalizer` and pass it to
     `Template.Create`, then un-ignore the four fixtures one at a time,
     TDD-style.
   - A small dedicated test suite (like each data source's own
     integration tests) exercising actual `CultureInfo.CurrentUICulture`
     switching between renders of the same parsed `Template` — the flat
     fixture corpus doesn't model an ambient culture change well, so this
     stays outside `/specs`.
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
