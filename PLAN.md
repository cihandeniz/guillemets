# Implementation plan

Living roadmap for building the guillemets engine against the `/specs`
fixture corpus. For *how* to work (TDD discipline, code style), see
`CLAUDE.md` — this file is *what's done and what's next*.

## Status

31 of 44 fixtures pass (`dotnet test` is authoritative). Done: `basics`,
`variables`, `conditional-blocks`, `loop-blocks`. Remaining fixtures are
listed in `FixtureTests.cs`'s `IGNORED_FIXTURES` set.

## Architecture (as built so far)

Pipeline: **`Tokenizer` → `TokenCursor` → `Parser` → self-rendering `Ast`
nodes → `TemplateEngine`**, in `/src/Guillemets`.

- **`Tokenization/SymbolTree.cs`**: a trie of symbol characters
  (`Symbols.TREE`), maximal munch with backtracking to the last
  accepting state. `ExtendMatch` reports how deep it got even on total
  failure, so `Tokenizer` can trust that depth directly. `Add(path,
  createToken, repeat, newline)` builds it: `repeat` loops the terminal
  node onto itself so any run length `>= 2` matches as one token (how
  `«`/`»` support arbitrary block depth, exposed as `Depth` on
  `OpenBlockToken`/`CloseBlockToken`); `newline` chains one more hop
  through `Position.NEWLINE` before assigning the token (how the else
  marker requires `~` to be followed by a newline).
- **`Tokenization/Tokenizer.cs`**: single-pass scan via `Tokens.cs`'s
  static factories; carries no knowledge of symbol shapes itself, just
  advances by whatever `SymbolTree` reports (floored at 1).
- **`Tokenization/TokenCursor.cs`**: token list + read position for
  `Parser`; the only place that mutates a `LiteralToken` in place
  (`TrimCurrentLiteral`).
- **`Parser.cs`**: recursive-descent (`Parse` → `ParseNodes` →
  `ParseNext` → `ParseBlock`/`ParseVariable`), nesting via the call
  stack. `»»` is always on its own line (per SPECS.md) — no
  closing-newline special-casing. `ParseBlock` requires the closing
  token's `Depth` to equal the opening token's `Depth`
  (`ValidateClosingDepth`), throwing `TemplateParseException` on a
  mismatch — depth beyond 2 is readability-only, but must still balance.
- **`Ast/PropertyChainBuilder.cs`**: builds a `PropertyChain` from
  tokens; tracks `!`-negation as `PropertyChain.LastSegmentNegated`
  (never encoded into the segment strings) and drops
  empty/whitespace-only fragments a `NegationToken` can split off a
  literal (e.g. the space in `"company: !active"`).
- **`Ast/Scope.cs`**: wraps the current `JsonElement` plus optional
  `IsFirst`/`IsLast`, resolved ahead of real JSON lookup via
  `TryGetMagic` — how `«first»`/`«last»` exist without being real JSON
  properties.
- **`Ast/BlockNode.cs`**: resolves either a loop-items list
  (`PropertyResolver.ResolveLoopItems`) or a single conditional value,
  dispatching to `LoopBehavior`/`ConditionalBehavior` (`IBlockBehavior`)
  — a two-way selection, not per-type logic inline.
- **`Ast/PropertyResolver.cs`**: `ResolveItemsMatchingLastSegment`
  implements filtered-item-scope (`items: active` → filter + loop over
  every match, per spec's plural "item(s)").
- **`TemplateEngine.cs`**: the sole public type; resolves directly
  against `JsonElement` (no reflection/POCO adapter yet).

See `CLAUDE.md`'s C# code style section for the house rules that shaped
this design.

## Remaining milestones

In fixture-group order (see `/specs`, simplest → most complex; group
folders are numbered on disk purely for sort order — referred to here by
name only):

1. `conditional-blocks` — done.
2. `loop-blocks` — done.
3. `scope-blocks` — object scope, upper-scope fallback (needs a real
   scope chain, not just a single `JsonElement`).
4. `variable-definitions` — capturing a block's rendered output into a
   named, positionally-scoped variable.
5. `tables` — should mostly fall out of the above once blocks exist;
   confirm rather than build new.
6. `inline-lists` — field-selection projection and custom `(separator)`
   already work for `variables/nested-property-chained-list`-style cases;
   confirm the remaining fixtures, particularly the loop-with-separator
   form.
7. `parameters` — `format`/`currency`/`length`.
8. `integration` — the full worked example, combining everything above.
9. `errors` — currently 3 fixtures (`unclosed-guillemet`,
   `unclosed-block`, `mismatched-block-depth`). Add more error cases as
   new failure modes appear — extend `TemplateParseException` usage
   rather than introducing ad hoc exceptions.

## Known v1 scope decisions (not gaps to "fix" without discussion)

- **True schema/localization remapping** (business term ≠ property name)
  is out of scope — only direct PascalCase-of-space-words resolution via
  Humanizer is implemented.
- **Currency/date/truncation formatting** in the `parameters`/
  `integration` fixtures matches the fixtures as authored, not an
  independently pinned spec — don't "correct" it without discussion.
- **Unresolved block name → falsy, not an error** — see
  `conditional-blocks/unresolved-property-no-else`.
- **Negating a non-last property-chain segment** (e.g.
  `people: !male: !parent`) is documented as unsupported (SPECS.md,
  Negation), but isn't enforced yet — `PropertyChain.LastSegmentNegated`
  silently drops an earlier `!` instead of raising a
  `TemplateParseException`. Worth an `errors` fixture once someone
  decides it should actually fail loudly rather than stay silent.
