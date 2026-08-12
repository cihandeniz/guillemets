# Implementation plan

Living roadmap for building the guillemets engine against the `/specs` fixture
corpus. This file shrinks as milestones complete — it's *what's left*, not a
history of what's done. This is an agent/contributor working file, not published
documentation — see `README.md`/`docs/` for that. For *how* it's built, see
`docs/architecture.md`; for *how* to work (TDD discipline, code style), see
`CLAUDE.md`.

## Status

`dotnet test` is green: 101 passed, 26 skipped, 127 total, 0 failed.
Everything milestone 1's `filter-syntax-redesign` touches — new grammar,
new `join`/`join last`/escaping fixtures — is listed in `SpecTests.cs`'s
`IGNORED_FIXTURES`, since the engine still speaks the old `(name = value)`
grammar; see milestone 1 below for the full list and design. Fixtures come
out of that set one at a time as their case is implemented, per the usual
TDD loop (code, pass, refactor, review, repeat) — it's empty once the
engine is complete. Pluggable data sources (JSON, POCO, and Newtonsoft
`JToken`) are done — see `docs/architecture.md`. `tables` is done — see
`docs/architecture.md` for how `LoopBehavior` detects and renders one.

## Remaining milestones

In priority order, matching disk order under `/specs` (`variable-definitions`
and `tables` are fully done, so the list picks up after them) — except
milestone 1, promoted to the top: it changes already-shipped parsing
behavior and several other milestones/fixtures depend on its grammar.

1. `filter-syntax-redesign` — replace the shipped `(name = value)` filter
   grammar with a no-parens, no-`=`, pipe-style pipeline. Settled across
   design discussion and written into `docs/specs.md`; no source file
   changed yet (see status below). The grammar:
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
   - `\` is not a general "make anything literal" escape — only a fixed,
     small set of `\X` sequences are recognized; any other `\` is just a
     literal backslash, and whatever follows it is read normally. The
     guiding rule: only a character that *starts* an interpretation needs
     an escape route. `\«`, `\»`, and `\\` are recognized in ordinary
     template text — `«` always tries to open something, so it always
     needs one; `»` only needs one inside a block's body (an unescaped
     `»»` there would close the block early — outside any open block, `»`
     was already just text). `\|`, `\n`, and `\t` are recognized only
     while parsing a filter's value: `\|` for a literal `|` (a bare ` | `
     would otherwise end the value and start the next pipeline stage),
     `\n`/`\t` for an actual newline/tab, the only way to put one in a
     value confined to a single line. None of those three mean anything
     outside a filter's value. No `\:` — a filter clause only ever looks
     for the *first* `: `, so nothing after it is re-scanned for another
     one. Written up in `docs/specs.md` as its own "Escaping" section,
     right after Delimiters, with the filter-specific sequences
     cross-referenced from the Filters section rather than duplicated.
   - `IFilter.Apply` changes shape:
     `IReadOnlyList<string> Apply(IReadOnlyList<string> values, string? arg)`
     — returns a list of strings instead of one, since every filter is now
     just a list-transforming pipeline stage (`join`/`join last` shrink the
     list, others map 1:1); takes one nullable `string? arg` instead of
     `IReadOnlyList<string> args`, since a filter is single-valued now that
     `join`/`join last` are separate filters rather than one filter with
     multiple args. A filter's value is optional in the grammar (bare
     `join`, no `: value`) and falls back to a default — `join`'s default
     is `, ` inline, a newline as a block footer. Still undecided: exactly
     how `IFilter` learns which of those two contexts it's running in, to
     pick the right default when `arg` is `null` — a constructor parameter
     on the concrete filter, a second method, or something the caller
     resolves and always passes a non-null `arg`. Needs deciding before
     `JoinFilter` is implemented. No stated default for `join last` yet —
     only `join`'s was specified, so `join last` stays required for now.
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

   **Spec/test/doc redesign is done and reviewed; implementation hasn't
   started.** Every fixture touched by the new grammar was rewritten,
   confirmed red against the still-old engine, then moved into
   `IGNORED_FIXTURES` so `dotnet test` is green again — per the usual
   redesign → review → implement → review → refactor sequence, now at the
   "implement" step, one fixture at a time.

   Done:
   - `05-variable-definitions/003-definition-list-join` (renamed from
     `...-separator`) and `004-definition-list-join-else` (renamed) —
     were *passing*; syntax changed, output didn't.
   - `02-conditional-blocks/009-corrupted-filter-syntax-in-body` — no
     longer a success fixture. Its old corruption (`(oops without equals`)
     stopped meaning anything under a no-parens, no-`=` grammar; now an
     error fixture: a recognized filter name followed by `:` with no
     space (`join:oops`) throws, rather than silently falling back to
     literal text.
   - `10-errors/005-non-separator-filter` — deleted, the restriction it
     tested is gone. Replaced by
     `05-variable-definitions/006-definition-list-multi-filter-footer`,
     which exercises the capability the restriction used to forbid
     (`length` then `join` chained in a footer).
   - `07-inline-lists/003-custom-separator` and `004-join-last` (renamed
     from `004-last-separator`) — rewritten to the new grammar and the
     `join`/`join last` design (`004-join-last` was originally built
     against an earlier "repeat the filter clause for a second value"
     idea that's since been superseded).
   - `08-filters/001-date`, `002-currency`, `003-truncate-length` —
     rewritten from `(date = dd/MM/yyyy)` etc. to `date: dd/MM/yyyy` etc.
   - New fixtures for capabilities that had no coverage before:
     `08-filters/004-join-default-inline`/`005-join-default-block-footer`
     (bare `join`'s context-aware default), `006-join-escaped-newline`/
     `007-join-escaped-tab` (`\n`/`\t` in a filter value),
     `008-join-escaped-pipe` (`\|` in a filter value, proving it doesn't
     end the value early), `09-integration/002-almost-errors` (a battery
     of not-actually-errors in one document: null scope with else,
     empty-list loop with else, filtered-item scope with zero matches,
     filter-name-looking text that isn't in the footer position,
     `join`/`join last` on 0/1-item lists), and a new top-level
     `12-escaping` group covering the escape mechanism on its own terms:
     `001-guillemets-and-backslash` (moved from `00-basics/`
     `004-escaped-guillemets` — `\«`/`\»`/`\\` in plain text),
     `002-close-inside-block` (`\»` inside an actual block body — the one
     case that makes escaping `»` load-bearing rather than redundant),
     `003-unrecognized-sequence-stays-literal` (`\a`, not in the
     recognized set, stays as two literal characters — proves there's no
     general escape fallback), `004-control-escapes-outside-filter-value`
     (`\n`/`\t` stay literal outside a filter's value, confirming they're
     scoped, not global), `005-double-backslash-before-guillemet` (`\\«`
     collapses to one literal backslash and still opens normally,
     confirming composability).
   - `docs/specs.md`: new "Escaping" section (right after Delimiters);
     Nested Property Access's colon rule tightened to MUST; Filters
     section rewritten for the new grammar, `join`/`join last`, and
     default values; old Custom Separator/Last Separator/Loop Block with
     Separator subsections folded into Filters' new Block Footer
     subsection; Full Example's filter usage updated.
   - `CLAUDE.md`'s Core concepts' Filters/Inline lists bullets updated.
   - `SpecTests.cs`: dropped the now-dead `DummyFilter`/`ConfigureFilters`
     (only existed for the retired restriction test); `IGNORED_FIXTURES`
     trimmed to just the two `inline-lists` fixtures unrelated to this
     milestone, so everything else fails loudly instead of skipping.

   Still pending (implementation phase):
   - Source: the tokenizer (recognize `: `, ` | `, and the scoped `\`
     escapes above as fixed lexical tokens — including wherever
     property-chain `:` is currently tokenized, to enforce the tightened
     `: ` MUST),
     `FilterParser` (full rewrite — no parens, no `=`), `BlockParser`
     (drop `ValidateIsSeparatorFilter`, update footer detection),
     `IFilter` (new signature, see above), `SeparatorFilter.cs` →
     `JoinFilter.cs` + new `JoinLastFilter.cs`, `FilterRegistry`
     registration names.
   - Docs that describe real shipped code, deliberately left untouched
     until the source they describe actually changes: `docs/architecture.md`
     (`FilterParser`/`IFilter`/`SeparatorFilter` sections) and `README.md`
     (its "Custom filters" section shows the current `IFilter.Apply`
     signature).
2. `join-rename` — `separator` filter renamed to `join`. Applied as part of
   milestone 1's execution; tracked as its own entry since it was raised as
   a distinct decision.
3. `filter-pipelines` — a block footer accepts any registered filter, not
   only `join`, chained the same pipeline-style `|` grammar as the inline
   form: per-item filters (`length`, `currency`, `date`) map over every
   item in the list, list filters (`join`, `join last`) operate on the
   whole list. Drops the "blocks only accept the separator filter"
   restriction (`10-errors/005-non-separator-filter`, deleted — see
   milestone 1's "Done" list). Applied as part of milestone 1's execution;
   tracked as its own entry since it was raised as a distinct decision.
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
