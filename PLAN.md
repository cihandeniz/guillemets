# Implementation plan

Living roadmap for building the guillemets engine against the `/specs`
fixture corpus. This file shrinks as milestones complete — it's *what's
left*, not a history of what's done. Agent/contributor working file, not
published documentation — see `README.md`/`docs/` for that. For *how*
it's built, see `docs/architecture.md`; for *how* to work (TDD
discipline, code style), see `CLAUDE.md`.

## Status

`dotnet test` is green: 124 passed, 4 skipped, 128 total, 0 failed.
Milestone 1 (`filter-syntax-redesign`) is behaviorally complete — the
`: `/` | ` grammar, the global `\` escapes, the scoped `\n`/`\t`/`\|`
filter-value escapes, every inline filter (`date`/`join`/`currency`/
`truncate`/`join last`), and the block-footer pipeline (including
`join`'s context-dependent bare default, `, ` inline vs. newline in a
footer) are all live. The two implementation cleanups formerly flagged
as `// TODO`s are done — `FilterParser` exposes a non-throwing
`TryParse`, and `IBlockBehavior.Render` now returns
`IEnumerable<string>` so `BlockNode` applies the footer pipeline
uniformly — and one narrow fixture case remains (see below). Pluggable
data sources (JSON, POCO, Newtonsoft `JToken`), `tables`, and
`inline-lists` are all done — see `docs/architecture.md`. As a side
effect, the `integration` milestone's `001-customer-offer` fixture now
passes across all three data sources too.

## Remaining milestones

In priority order, matching disk order under `/specs`
(`variable-definitions` and `tables` are fully done, so the list picks up
after them) — except milestone 1, promoted to the top: it changes
already-shipped parsing behavior and every other milestone below depends
on its grammar.

1. `filter-syntax-redesign` — behaviorally complete; both implementation
   cleanups formerly flagged as `// TODO` comments are done (`FilterParser`
   gained a non-throwing `TryParse` so `BodyParser.TryParseFooter` rewinds
   via a plain `if`/`else` instead of a `catch`, with `Parse` itself now
   implemented on top of `TryParse`; `IBlockBehavior.Render` returns
   `IEnumerable<string>` so `BlockNode` applies the footer pipeline once,
   uniformly, across `Conditional`/`Scope`/`Loop`, with the markdown-table
   path inside `LoopBehavior` still merging into one already-newline-joined
   item first). One narrow fixture case remains:
   `02-conditional-blocks/009-corrupted-filter-syntax-in-body` wants a
   filter name immediately followed by a bare `:` (no space — e.g.
   `join:oops`) to raise `Expected a space after ':'` instead of silently
   falling back to literal text. Today `:` only ever tokenizes as part of
   the fixed `: ` (colon+space) symbol, so `join:oops` is just one
   undifferentiated `LiteralToken` — nothing distinguishes "an attempted,
   malformed filter invocation" from "a colon that happens to appear in
   prose" (e.g. `Time: 10:30am`). The signal to use is narrower than "any
   bare colon": `FilterParser.TryParseStage`, when the name it just read is
   a *registered* filter name and the next raw character is `:` not
   followed by a space, is where this should raise — not a general
   tokenizer-level change, which would misfire on ordinary prose.
2. `integration` — the full worked example, combining everything above.
   `001-customer-offer` now passes on all three data sources (un-ignored
   in `JsonIntegrationTests`/`PocoIntegrationTests`/`JTokenIntegrationTests`).
   `002-almost-errors` is still `[Ignore]`d — it surfaced a real gap,
   not a filters/footer gap: `««missing thing»»` (a block header naming a
   property absent from the data entirely) throws
   `InvalidOperationException: Property 'MissingThing' was not found`
   instead of resolving falsy. The "Known v1 scope decisions" entry below
   ("unresolved block name → falsy") only actually holds today when the
   *container* resolves (e.g. an empty array whose items are never
   individually visited, as in
   `conditional-blocks/unresolved-property-no-else`) — a name with no
   container at all still throws, from `PropertyResolver.Project`'s
   `TryGetProperty` failure, reached via
   `BlockNode.ResolveBehavior`'s call to `TryResolveLoopItems` before the
   object/conditional fallback path ever gets a chance to run. Fixing
   this is a `PropertyResolver` change, not a filters one — worth its own
   pass rather than folding into filter work.
3. `errors` — currently 6 fixtures (`unclosed-guillemet`,
   `unclosed-block`, `mismatched-block-depth`, `literal-shares-close-line`,
   `unclosed-block-dangling-filter-pipe`, plus one retired alongside the
   old filter grammar). Add more error cases as new failure modes
   appear — extend `TemplateParseException` usage rather than introducing
   ad hoc exceptions.
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
- **Unresolved block name → falsy, not an error** — the *decision*, per
  `conditional-blocks/unresolved-property-no-else`. Not yet fully the
  *behavior*: still throws for a name with no resolvable container at
  all, rather than an existing-but-empty one — see milestone 2
  (`integration`) above.
- **Negating a non-last property-chain segment** (e.g. `people: !male:
  !parent`) is documented as unsupported (`docs/specs.md`, Negation), but
  isn't enforced yet — `PropertyChainNode.LastSegmentNegated` silently
  drops an earlier `!` instead of raising a `TemplateParseException`.
  Worth an `errors` fixture once someone decides it should actually fail
  loudly rather than stay silent.
