# Implementation plan

Living roadmap for building the guillemets engine against the `/specs`
fixture corpus. This file shrinks as milestones complete — it's *what's
left*, not a history of what's done. Agent/contributor working file, not
published documentation — see `README.md`/`docs/` for that. For *how*
it's built, see `docs/architecture.md`; for *how* to work (TDD
discipline, code style), see `CLAUDE.md`.

## Status

`dotnet test` is green: 159 passed, 0 skipped, 0 failed. Every milestone
through `glossary-localization` is done, including `upper`/`lower` as two
new language-guaranteed filters alongside `join`/`join last`. See
`docs/architecture.md` for how the engine is built and `docs/specs.md`/
`docs/implementations/dotnet.md` for current behavior — no open
ambiguities remain.

## Remaining milestones

In priority order, matching disk order under `/specs`
(`variable-definitions`, `tables`, `filter-syntax-redesign`,
`glossary-localization`, and `integration` are fully done, so the list
picks up after them; `errors` has no further known gaps — add a fixture
directly, per `CLAUDE.md`, whenever a new failure mode turns up rather
than tracking it here).

1. Explicit scope-navigation syntax — `.: name` for "this scope only,"
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
   - stopped during creating specs under .specs.md/Scope Navigation
   - will continue to write specs and then create spec cases
2. Rehumanize `docs/specs.md` and the other published docs (`README.md`,
   `docs/architecture.md`, `docs/implementations/dotnet.md`,
   `docs/README.md`) — a readability/tone pass, not a correctness one,
   after this session's many incremental edits.
