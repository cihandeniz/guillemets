# Implementation plan

Living roadmap for building the guillemets engine against the `/specs`
fixture corpus. For *how* to work (TDD discipline, code style), see
`CLAUDE.md` — this file is *what's done and what's next*.

## Status

Run `dotnet test` for the current, authoritative count — as of this
writing: 19 of 37 fixtures implemented (`basics`, `variables`, and all of
`conditional-blocks`, including the `--` else split and nested if/else).
The rest are listed in `FixtureTests.cs`'s `IGNORED_FIXTURES` set.

## Architecture (as built so far)

Pipeline: **`Tokenizer` → `TokenCursor` → `Parser` → self-rendering `Ast`
nodes → `TemplateEngine`**, in `/src/Guillemets`.

- **`Tokenization/SymbolTree.cs`**: a trie of symbol characters (built
  once as `Symbols.TREE`), matched via maximal munch with backtracking to
  the last accepting state. Emits `OpenToken`/`OpenBlockToken`/
  `CloseToken`/`CloseBlockToken`/`ColonToken`/`ElseToken` plus
  `LiteralToken` for anything unmatched. Both `--` (→ `ElseToken`) and
  `»»` (→ `CloseBlockToken`) have two accepting states in the trie, one
  with a trailing `\n` and one without — the longer match wins when a
  newline follows, so the token itself consumes that newline; neither the
  tokenizer nor `Parser` needs any special-case trimming code for it.
- **`Tokenization/Tokenizer.cs`**: single-pass scan building tokens via
  `Tokenization/Tokens.cs`'s static factories, each taking a
  `TokenContext` (matched text + position).
- **`Tokenization/TokenCursor.cs`**: owns the token list + read position
  for `Parser`; the only place that mutates a `LiteralToken` in place
  (`TrimCurrentLiteral`), so `Parser` never touches a raw index.
- **`Parser.cs`**: recursive-descent (`Parse` → `ParseNodes` →
  `ParseNext` → `ParseBlock`/`ParseVariable`). Nesting is handled by the
  call stack, not depth-tracking. Token types that just fall back to
  literal output (`LiteralToken`/`ElseToken`/`ColonToken`) implement a
  shared `ITextToken.Text` marker so `ParseNext` collapses them into one
  branch instead of one `is` check per type. Throws
  `TemplateParseException` on an unclosed `«`/`««`.
- **`Ast/`**: `INode.Render(RenderContext, JsonElement)` — each node type
  (`LiteralNode`, `TokenNode`, `BlockNode`) renders itself, no
  visitor/switch. `BlockNode`'s falsy branch (`ElseBody`) is taken for
  `False`, `Null`, or an unresolved property.
- **`TemplateEngine.cs`**: the sole public type; resolves directly
  against `System.Text.Json.JsonElement` (no reflection/POCO adapter
  yet).

See `CLAUDE.md`'s C# code style section for the house rules (constructor
injection, no `sealed`, field-vs-local rules) that shaped this design.

## Remaining milestones

In fixture-group order (see `/specs`, simplest → most complex; group
folders are numbered on disk purely for sort order — referred to here by
name only):

1. `conditional-blocks` — done: boolean if/no-else, the `--` else split,
   and null-object else.
2. `loop-blocks` — list loops, empty list, magic `first`/`last`, `!`
   negation, plus `filtered-item-scope`: a block name whose chain
   projects a boolean through a list should *filter* to the matching
   item(s) and scope into it, not collapse to one truthy/falsy check (see
   SPECS.md's "Resolving the Block Name"). `PropertyResolver` has no
   filter/find step yet — real implementation work, not just a fixture
   unignore.
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
9. `errors` — currently 2 fixtures (`unclosed-guillemet`,
   `unclosed-block`). Add more error cases as new failure modes appear —
   extend `TemplateParseException` usage rather than introducing ad hoc
   exceptions.

## Known issues (decided, not yet designed/implemented)

- **`--` else syntax collides with Markdown Setext headings.** A line of
  `--` directly under a text line is valid CommonMark for an `<h2>`
  heading (`Heading\n--\n` → `<h2>Heading</h2>`), and a `---` horizontal
  rule is even more common in ordinary prose — SPECS.md's own "Full
  Example — Customer Quote" already has a bare `---` outside any block.
  `SymbolTree` matches `--` immediately followed by `\n` as `ElseToken`
  regardless of context; outside a block it currently falls back to
  literal text (see `Parser.ParseNext`'s `ITextToken` branch), so it
  doesn't crash, but the delimiter itself needs to change so else-syntax
  stops shadowing standard Markdown. Not yet designed (no replacement
  character/marker chosen) — needs a SPECS.md update alongside the code
  change once one is.

## Known v1 scope decisions (not gaps to "fix" without discussion)

- **Multi-depth guillemets** (`«««…»»»` for nesting readability) are
  unimplemented beyond the cosmetic nesting case
  (`conditional-blocks/nested-blocks`, same-depth `««` on both sides).
  `SymbolTree`'s `«`/`»` paths are only 2 levels deep — a run of 3+ in a
  row tokenizes as `OpenBlockToken` + `OpenToken`, not one deeper token.
  Needs a 3rd trie level added deliberately if this is ever needed.
- **True schema/localization remapping** (business term ≠ property name,
  e.g. spec's `"quote no"` → `OfferNo`) is out of scope — only direct
  PascalCase-of-space-words resolution via Humanizer is implemented. No
  schema-file format designed yet.
- **Currency/date/truncation formatting** in the `parameters`/
  `integration` fixtures encodes assumptions made when those fixtures
  were authored (no independent spec pins the exact format) — match the
  fixtures exactly rather than "correcting" the formatting.
- **Unresolved block name → falsy, not an error**: if a block's `name`
  chain doesn't resolve to anything (e.g. projects through an empty
  list), it's treated as falsy, same as an explicit `false` — via
  `.SingleOrDefault()` in `BlockNode.Render`. See
  `conditional-blocks/unresolved-property-no-else`.
- **Boolean-through-list block names still crash on 2+ matches**: today
  `.SingleOrDefault()` throws if a chain's projection yields more than
  one value (e.g. `««items: active` where 2+ items have `active`). The
  intended fix is the filter-and-scope behavior in milestone 2 above, not
  a defensive guard — see `loop-blocks/filtered-item-scope`.
- **`--` only requires a trailing newline, not a leading one** — a real
  relaxation of the spec wording ("`--` on its own line" reads as
  both-sides). Unconfirmed against SPECS.md since no fixture yet
  distinguishes the two. Moot once the delimiter itself changes — see
  "Known issues" above.
