# Implementation plan

Living roadmap for building the guillemets engine against the `/specs`
fixture corpus. This file shrinks as milestones complete — it's *what's
left*, not a history of what's done. Agent/contributor working file, not
published documentation — see `README.md`/`docs/` for that. For *how*
it's built, see `docs/architecture.md`; for *how* to work (TDD
discipline, code style), see `CLAUDE.md`.

## Status

`dotnet test` is green: 163 passed, 10 skipped, 0 failed. Every milestone
through `glossary-localization` is done, including `upper`/`lower` as two
new language-guaranteed filters alongside `join`/`join last`. See
`docs/architecture.md` for how the engine is built and `docs/specs.md`/
`docs/implementations/dotnet.md` for current behavior — no open
ambiguities remain outside scope navigation (below).

Scope navigation (`.: `/`..: `) has its spec text and fixture set checked
in under `specs/14-scope-navigation` (13 cases). `.: ` (This Scope Only)
is implemented — its 3 fixtures pass, including that it skips a defined
variable (see Variable Definitions in `docs/specs.md`) of the same name,
not just magic vars and enclosing-scope fallback. The 10 remaining
fixtures (`..: ` climbing, plus the two `.: `-must-be-last error cases,
which need `..: ` to exist to even be reachable) are still in
`SpecTests.cs`'s `IGNORED_FIXTURES`.

## Remaining milestones

In priority order, matching disk order under `/specs`
(`variable-definitions`, `tables`, `filter-syntax-redesign`,
`glossary-localization`, and `integration` are fully done, so the list
picks up after them; `errors` has no further known gaps — add a fixture
directly, per `CLAUDE.md`, whenever a new failure mode turns up rather
than tracking it here).

1. Explicit scope-navigation syntax — `.: name` for "this scope only,"
   bypassing magic-var shadowing, and `..: name` for "climb to the
   parent scope," chainable and composable with `.: ` (see Scope
   Navigation in `docs/specs.md`). `.: ` is done:
   `LocalScopeToken`/`Tokens.LocalScope` (`Symbols.cs` matches
   `[DOT, COLON, SPACE]`), `PropertyChainNode.ThisScopeOnly` set by
   `PropertyChainParser.ParseLeadingNavigator` before the main parse
   loop starts, and `PropertyResolver.Resolve` delegates to a nested
   `PropertyResolver.Resolution` (a Method Object — `scope`/
   `properties`/`_variables` become its fields, plus a back-reference
   to the outer `PropertyResolver` for the two operations that stay
   shared with `TryResolveArrayItems`: `Project` and
   `TryResolveFilteredItemScope`). `Resolution.Resolve()` itself never
   reads `ThisScopeOnly` — that's fully encapsulated in `TryMagic`/
   `TryDefinedVariable`/`TryFilteredItemScope`/`Owner`, each of which
   independently checks it against its own field. Remaining work is
   `..: ` (climbing) plus the two `.: `-must-be-last error cases, one
   fixture at a time:
   - A second token, `..: ` (`ParentScopeToken`/`Tokens.ParentScope`,
     `[DOT, DOT, COLON, SPACE]` in `Symbols.cs`), same `ITextToken`
     treatment as `.: ` so it round-trips as literal prose outside a
     property chain, and is invisible without its trailing space.
   - `PropertyChainNode` needs a climb count (e.g. `int ClimbLevels`)
     alongside `ThisScopeOnly`. `ParseLeadingNavigator` extends to
     consume zero or more leading `..: ` before the optional trailing
     `.: ` — a navigator token found anywhere else in the chain (i.e.
     after the first real segment, or after a `.: ` has already been
     consumed) is the `.: ..: name`/`.: .: name` parse error
     (`specs/14-scope-navigation/011`/`012`).
   - Climbing past the outermost scope is *not* a parse error — it
     resolves to nothing at render time, same as any other chain that
     can't find its property, the way `?.` in C# short-circuits a
     chain once one link is null (`specs/14-scope-navigation/009`/
     `010`; see also `specs/01-variables/007-nonexistent-nested-
     property`, which pins this same null-propagation behavior for a
     plain, non-navigator chain). So `PropertyChainParser` doesn't
     need to know the current block-nesting depth at parse time — the
     climb itself is another concern `Resolution` can own: walk
     `_scope.Parent` `ClimbLevels` times before consulting
     `ThisScopeOnly`/`FindOwner` (in `Owner`, and in whatever guards
     `TryMagic`/`TryDefinedVariable`/`TryFilteredItemScope`); if
     `Parent` is `null` before the walk completes, there's no scope
     left to resolve against, so the chain resolves to nothing (an
     `UndefinedDataSource`, matching how a chain through a missing
     property already resolves).
2. Rehumanize `docs/specs.md` and the other published docs (`README.md`,
   `docs/architecture.md`, `docs/implementations/dotnet.md`,
   `docs/README.md`) — a readability/tone pass, not a correctness one,
   after this session's many incremental edits.
