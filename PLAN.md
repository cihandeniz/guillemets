# Implementation plan

Living roadmap for building the guillemets engine against the `/specs` fixture
corpus. This file shrinks as milestones complete — it's *what's left*, not a
history of what's done. This is an agent/contributor working file, not published
documentation — see `README.md`/`docs/` for that. For *how* it's built, see
`docs/architecture.md`; for *how* to work (TDD discipline, code style), see
`CLAUDE.md`.

## Status

103 of 112 fixture cases pass (`dotnet test` is authoritative, via
`SpecTests.cs`). Remaining fixtures are listed in `SpecTests.cs`'s
`IGNORED_FIXTURES` set. Pluggable data sources (JSON, POCO, and Newtonsoft
`JToken`) are done — see `docs/architecture.md`. `tables` is done — see
`docs/architecture.md` for how `LoopBehavior` detects and renders one.

## Remaining milestones

In priority order, matching disk order under `/specs` (`variable-definitions`
and `tables` are fully done, so the list picks up after them) — except
milestone 1, promoted to the top: it changes already-shipped parsing
behavior and several other milestones/fixtures depend on its grammar.

1. `filter-syntax-redesign` — replace the shipped `(name = value)` filter
   grammar with a no-parens, no-`=`, pipe-style pipeline. Settled across
   design discussion; not yet written into `docs/specs.md` or any source
   file. The grammar:
   - `: ` (colon immediately followed by exactly one space) is a fixed,
     literal 2-character token — not "colon, then trim what follows." This
     now governs property-chain colons too (`company: name`), tightening
     today's "space after `:` is a SHOULD, unenforced" into a MUST
     everywhere `:` appears — a behavior change to already-shipped parsing,
     not only new grammar for filters.
   - ` | ` (space-pipe-space, exactly 3 characters, fixed/literal) is the
     pipeline-stage separator — chains a property chain into one or more
     filters, and chains filters into each other, e.g.:
     ```
     «company: employees: start date | date: dd/MM/yyyy | join last:  and  | join:, »
     ```
     Reuses the character templating engines already use for "apply a
     filter" (Django/Jinja/Liquid). It does share a character with the
     markdown-table row marker, but that's not an actual parsing conflict —
     table detection only ever looks at a loop body's very first literal,
     never inside a token's contents — just a minor readability overlap
     when a filter clause appears inside a table cell, accepted as a small
     cost for reusing a well-known convention.
   - `\` is a general escape character: `\X` makes the character right
     after it literal, for the rare case a value needs to contain `:`, `|`,
     `»`, etc.
   - No other implicit trimming at the grammar level — a filter's raw value
     is everything between its `: ` and the next ` | ` or the block/token's
     end, untouched. Individual filters may trim their own received args
     internally when incidental whitespace doesn't matter to them (`date`,
     `currency`, `length` should; `join`/`join last` must not, since
     whitespace is their actual payload).
   - New `join last` filter (a two-word name, matched as one token via
     `: `, not a second argument to `join`): merges the *last two* elements
     of the current list into one, joined by its value; fewer than 2
     elements is a no-op. `join` collapses the *entire* current list into a
     single string, joined by its value; 0 or 1 elements is a no-op. Both
     are genuinely sequential pipeline stages — order matters (`join last`
     before `join` produces the classic "a, b and c"; the reverse collapses
     first and leaves `join last` nothing to do) — not a paired
     configuration read together. The existing default inline-list
     auto-join (`, `, see Inline Lists) still applies as a fallback if the
     pipeline ends without fully collapsing the list to one string, so
     `join last` alone is enough for the common "a, b and c" case.
   - One grammar, used identically inline (`«expr | filter: arg»`) and as
     a block footer (the last line of a block body, before `»»`).

   Migration this invalidates or requires rewriting:
   - `05-variable-definitions/003-definition-list-separator` and
     `004-definition-list-separator-else` — currently *passing*; syntax
     changes, output doesn't.
   - `02-conditional-blocks/009-corrupted-filter-syntax-in-body` (error
     fixture) — its corruption (`(oops without equals`) stops meaning
     anything under a no-parens, no-`=` grammar; needs a new malformed
     example (e.g. a `:` with no following space where a value was
     expected, or a dangling `\` escape).
   - `10-errors/005-non-separator-filter` — the restriction it tests is
     removed entirely by this milestone; replace or remove the fixture.
   - `07-inline-lists/003-custom-separator` and `004-join-last`
     (currently `Ignore`d, not yet implemented) — rewrite to the new
     grammar and the `join`/`join last` design (`004-join-last` was
     built against an earlier "repeat the filter clause for a second value"
     idea that's now superseded).
   - `08-filters/001-date`, `002-currency`, `003-truncate-length`
     (currently `Ignore`d) — rewrite from `(date = dd/MM/yyyy)` etc. to
     `date: dd/MM/yyyy` etc.
   - Source: the tokenizer (recognize `: `, ` | `, and `\`-escapes as fixed
     lexical tokens, including wherever property-chain `:` is currently
     tokenized, to enforce the tightened `: ` MUST), `FilterParser` (full
     rewrite — no parens, no `=`), `BlockParser` (drop
     `ValidateIsSeparatorFilter`, update footer detection to the new
     grammar), `SeparatorFilter.cs` → `JoinFilter.cs` + new
     `JoinLastFilter.cs`, `FilterRegistry` registration names.
   - Docs: `docs/specs.md` (Filters section rewritten, Nested Property
     Access section's colon rule tightened, Loop Block with Separator
     section rewritten, Full Example's `(date = dd/MM/yyyy)` usage
     updated), `CLAUDE.md` (Core concepts' filter description),
     `docs/architecture.md` (`FilterParser` description).
2. `join-rename` — `separator` filter renamed to `join`. Applied as part of
   milestone 1's execution; tracked as its own entry since it was raised as
   a distinct decision.
3. `filter-pipelines` — a block footer accepts any registered filter, not
   only `join`, chained the same pipeline-style `|` grammar as the inline
   form: per-item filters (`length`, `currency`, `date`) map over every
   item in the list, list filters (`join`, `join last`) operate on the
   whole list. Drops the "blocks only accept the separator filter"
   restriction (`10-errors/005-non-separator-filter`, see milestone 1's
   migration list). Applied as part of milestone 1's execution; tracked as
   its own entry since it was raised as a distinct decision.
4. `inline-lists` (remainder) — `001-inline-scalar-list`/
   `002-inline-field-selection` need `VariableNode` to join an array's
   elements with the default `, ` separator when resolution yields a list
   (it currently renders the array's own `AsDisplayString()` instead, e.g.
   JSON's raw `["a","b"]`) — independent of milestone 1 above, since this is
   the no-filter default path.
5. `integration` — the full worked example, combining everything above.
   Already has dedicated, currently-`[Ignore]`d coverage in
   `JsonIntegrationTests`/`PocoIntegrationTests`/`JTokenIntegrationTests` —
   un-ignore all three once this milestone lands, and drop the
   `09-integration` exclusion note in `SpecTests.cs` if it's ever folded back
   into the generic sweep.
6. `errors` — currently 5 fixtures (`unclosed-guillemet`, `unclosed-block`,
   `mismatched-block-depth`, `literal-shares-close-line`,
   `non-separator-filter` — the last one is retired by milestone 3 above).
   Add more error cases as new failure modes appear — extend
   `TemplateParseException` usage rather than introducing ad hoc exceptions.
7. `schema-localization` — true schema/localization remapping (business term ≠
   property name), per "Schema & Localization" in `docs/specs.md`: a mapping
   table (`Localized Term = template token = PropertyName`) resolved
   case-insensitively against the default language, for the cases where
   direct PascalCase-of-space-words resolution via Humanizer doesn't already
   match. No `/specs` fixture group exists for this yet — add one (new
   numbered group, e.g. `12-schema-localization`) as part of this milestone,
   test-first, rather than bolting the feature on without fixture coverage.
   Needs a design decision, before writing fixtures, on where the mapping
   table itself is supplied from (a data source alongside the render call? a
   separate file/format?) since nothing in the engine's public API accepts
   one today — see `docs/architecture.md`.

## Known v1 scope decisions (not gaps to "fix" without discussion)

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
