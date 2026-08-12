# Implementation plan

Living roadmap for building the guillemets engine against the `/specs`
fixture corpus. This file shrinks as milestones complete — it's *what's
left*, not a history of what's done. Agent/contributor working file, not
published documentation — see `README.md`/`docs/` for that. For *how*
it's built, see `docs/architecture.md`; for *how* to work (TDD
discipline, code style), see `CLAUDE.md`.

## Status

`dotnet test` is green: 114 passed, 13 skipped, 127 total, 0 failed.
Milestone 1 (`filter-syntax-redesign`) is mid-implementation — the
`: `/` | ` grammar, the global `\` escapes, the scoped `\n`/`\t`/`\|`
filter-value escapes, and every inline filter (`date`/`join`/`currency`/
`truncate`/`join last`) are live; only the block-footer pipeline remains,
listed in `SpecTests.cs`'s `IGNORED_FIXTURES`. Pluggable data sources
(JSON, POCO, Newtonsoft `JToken`), `tables`, and `inline-lists` are all
done — see `docs/architecture.md`.

## Remaining milestones

In priority order, matching disk order under `/specs`
(`variable-definitions` and `tables` are fully done, so the list picks up
after them) — except milestone 1, promoted to the top: it changes
already-shipped parsing behavior and every other milestone below depends
on its grammar.

1. `filter-syntax-redesign` — replace the shipped `(name = value)` filter
   grammar with the no-parens, no-`=`, pipe-style pipeline. Grammar and
   escaping rules are fully specified in `docs/specs.md` (Filters,
   Escaping) — that's the authoritative reference, not this file.
   Tokenizer and every inline filter (`date`, `join`, `currency`,
   `truncate`, `join last`) are implemented, including the scoped
   `\n`/`\t`/`\|` filter-value escapes (their own `EscapedToken` type,
   a `LiteralToken` subtype, keeps them distinguishable in `FilterParser`
   from ordinary unescaped text — see `docs/architecture.md`). Still
   pending:
   - A block-footer filter pipeline, accepting any registered filter (not
     just `join`) on the same grammar as the inline form — `BlockParser`
     currently has *no* footer-parsing at all (the old `(name = value)`
     footer support was deleted, not migrated), so this needs building
     from scratch. Telling a footer line apart from ordinary body text
     with no distinguishing lead token is a real parsing problem;
     `TokenCursor.Rewind`-based speculative parsing (see `CLAUDE.md`) is
     a legitimate option for it, not a smell to avoid.
   - How a filter learns whether it's running inline vs. as a block
     footer, for `join`'s bare-name default (`, ` inline, newline as a
     footer) — undecided; needed before `08-filters/004`/`005`.
2. `integration` — the full worked example, combining everything above.
   Already has dedicated, currently-`[Ignore]`d coverage in
   `JsonIntegrationTests`/`PocoIntegrationTests`/`JTokenIntegrationTests`
   — un-ignore all three once this milestone lands, and drop the
   `09-integration` exclusion note in `SpecTests.cs` if it's ever folded
   back into the generic sweep.
3. `errors` — currently 5 fixtures (`unclosed-guillemet`,
   `unclosed-block`, `mismatched-block-depth`, `literal-shares-close-line`,
   plus one retired alongside the old filter grammar). Add more error
   cases as new failure modes appear — extend `TemplateParseException`
   usage rather than introducing ad hoc exceptions.
4. `schema-localization` — true schema/localization remapping (business
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
- **Unresolved block name → falsy, not an error** — see
  `conditional-blocks/unresolved-property-no-else`.
- **Negating a non-last property-chain segment** (e.g. `people: !male:
  !parent`) is documented as unsupported (`docs/specs.md`, Negation), but
  isn't enforced yet — `PropertyChainNode.LastSegmentNegated` silently
  drops an earlier `!` instead of raising a `TemplateParseException`.
  Worth an `errors` fixture once someone decides it should actually fail
  loudly rather than stay silent.
