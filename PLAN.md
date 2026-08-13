# Implementation plan

Living roadmap for building the guillemets engine against the `/specs`
fixture corpus. This file shrinks as milestones complete — it's *what's
left*, not a history of what's done. Agent/contributor working file, not
published documentation — see `README.md`/`docs/` for that. For *how*
it's built, see `docs/architecture.md`; for *how* to work (TDD
discipline, code style), see `CLAUDE.md`.

## Status

`dotnet test` is green: 160 passed, 12 skipped, 0 failed. Every milestone
through `glossary-localization` is done, including `upper`/`lower` as two
new language-guaranteed filters alongside `join`/`join last`. See
`docs/architecture.md` for how the engine is built and `docs/specs.md`/
`docs/implementations/dotnet.md` for current behavior — no open
ambiguities remain outside scope navigation (below).

Scope navigation (`.: `/`..: `) has its spec text and its full fixture set
under `specs/14-scope-navigation` written and checked in (12 cases, listed
in `SpecTests.cs`'s `IGNORED_FIXTURES` per the redesign-checkpoint process
in `CLAUDE.md`) — this is the checkpoint-for-review point; engine work
hasn't started.

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
   Navigation in `docs/specs.md` — the design questions PLAN.md used to
   list here are answered there now). Spec text and the full fixture
   set are done and checked in (`specs/14-scope-navigation`, 12 cases,
   all listed in `SpecTests.cs`'s `IGNORED_FIXTURES`) — this was the
   redesign checkpoint per `CLAUDE.md`; engine work starts next,
   one fixture at a time, back to the normal TDD loop. Implementation
   sketch, so a cold start doesn't have to re-derive it from the
   tokenizer:
   - Two new tokens, `.: ` and `..: ` (`Symbols.cs` needs a `DOT`
     constant), each implementing `ITextToken` like `ColonToken`/
     `BareColonToken` already do — so `.: `/`..: ` appearing in plain
     prose (outside a property chain) still round-trips as literal
     text via `TextParser`, and writing either without the trailing
     space isn't recognized as a navigator at all, same rule as `: `.
   - `PropertyChainParser` recognizes these only when they appear
     before any segment has been added to the chain being built —
     once at the very start of `Parse`, and again right after an
     `EqualsToken` is consumed (a variable definition's `expr` is
     itself a fresh chain). Zero or more `..: ` followed by at most
     one `.: ` before the first real segment; a navigator token seen
     anywhere else in the chain is the `.: ..: name`/`.: .: name`
     parse error (`specs/14-scope-navigation/011`/`012`).
   - Climbing past the outermost scope is deliberately *not* a parse
     error (revised from the original design) — it resolves to
     nothing at render time, same as any other chain that can't find
     its property, the way `?.` in C# short-circuits a chain once one
     link is null (`specs/14-scope-navigation/009`/`010`). So there's
     no need for `PropertyChainParser` to know the current
     block-nesting depth at parse time — a `..: ` climb is purely a
     render-time `Scope.Parent` walk that stops (yielding null/falsy)
     once it runs out of parents, regardless of how many `..: `
     markers the author wrote.
2. Rehumanize `docs/specs.md` and the other published docs (`README.md`,
   `docs/architecture.md`, `docs/implementations/dotnet.md`,
   `docs/README.md`) — a readability/tone pass, not a correctness one,
   after this session's many incremental edits.
