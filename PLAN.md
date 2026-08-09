# Implementation plan

Living roadmap for building the guillemets engine against the `/specs`
fixture corpus. For *how* to work (TDD discipline, code style, the
`IGNORED_FIXTURES` convention), see `CLAUDE.md` — this file is *what's done
and what's next*.

## Status

Run `dotnet test` for the current, authoritative count — as of this
writing: 16 of 34 fixtures implemented (`00-basics`, `01-variables`, and all
of `02-conditional-blocks`, including the `--` else split and nested
if/else blocks — `005a`–`005c`, sharing one `005-nested-blocks.guil.md`,
confirming the recursive-descent parser balances nested `««…»»` pairs via
the call stack, with no depth-tracking needed on the tokens themselves),
the rest listed in `FixtureTests.cs`'s `IGNORED_FIXTURES` set.

## Architecture (as built so far)

Pipeline: **`Tokenizer` → `TokenCursor` → `Parser` → self-rendering `Ast`
nodes → `TemplateEngine`**, in `/src/Guillemets`.

- **`Tokenizer.cs`**: O(n) scan driven by instance fields
  (`_index`/`_position`/`_literalStart`/`_literalStartPosition`/`_tokens`)
  rather than parameters threaded through methods or tuples handed back
  between them — safe because a fresh `Tokenizer` is built per render (see
  `TemplateEngine.Render`), so there's no cross-call state to reset.
  `TryConsumeSymbol()` walks a **`SymbolNode`** trie (`SymbolNode.cs`) built
  once in `BuildSymbolTree()`: each node optionally carries a `Terminal`
  (`Func<Position, IToken>?`) plus a `char`-keyed dictionary of children.
  The walk is textbook **maximal munch with backtracking to the last
  accepting state** — descend as far as the template keeps matching a
  child, remember the deepest node that had a `Terminal`, and back off to
  that point (or report no match at all, letting the character fall back
  to literal text) once the walk dead-ends. This is what replaced *all* of
  the previous bespoke per-symbol logic: `«`/`»` resolve to `OpenToken`/
  `CloseToken` after one character but the trie also has a child under a
  second `«`/`»` resolving to `OpenBlockToken`/`CloseBlockToken` — no
  separate depth-counting or run-length code anywhere. `-` has **no**
  terminal after one or two characters (a lone `-` or a `--` not followed
  by `\n` is common in ordinary prose, so it must never become a token on
  its own) — only `--` immediately followed by `\n` resolves, to
  `ElseToken`, and that resolution **consumes the newline as part of the
  token** (unlike after `»»`, there's nothing left for `Parser` to trim
  off the following literal). Emits 6 terminal kinds total (`Tokens/
  OpenToken.cs`, `OpenBlockToken.cs`, `CloseToken.cs`, `CloseBlockToken.cs`,
  `ColonToken.cs`, `ElseToken.cs`) plus `LiteralToken.cs` for everything
  that never resolves through the trie.
- **`TokenCursor.cs`**: owns the token list + read position. Exposes
  `AtEnd`/`Current`/`Advance()` for reading, plus `TrimCurrentLiteral(length)`
  and `TrimLeadingNewlineIfPresent()` — the *only* two places that mutate a
  `LiteralToken` in place to consume part of its text (used by block-header
  parsing and by swallowing the newline after a closing `»»`). This exists
  specifically so `Parser` never touches a raw index. (`Skip(n)`/
  `CountConsecutive<TToken>()` existed at various points to count/skip runs
  of single-char Open/Close tokens for depth — both are gone now that the
  tokenizer's trie resolves depth itself via distinct token types, so
  every delimiter is exactly one token and `Advance()` is always enough.)
- **`Parser.cs`**: recursive-descent over the cursor. `Parse()` →
  `ParseNodes(insideBlock, stopAtElse)` loops calling `ParseNext()`, which
  dispatches purely on token type now — `OpenBlockToken` → `ParseBlock`,
  `OpenToken` → `ParseVariable` — no depth counting. `ParseBlockHeader`
  reads the header line into a `PropertyChain`, splitting the owning
  literal token at the first `\n` via `TokenCursor.TrimCurrentLiteral`.
  Throws `TemplateParseException` (with `Position`) on an unclosed `«`/
  `««`. `ParseBlock` parses the truthy body via `ParseNodes(insideBlock:
  true, stopAtElse: true)`, which stops the moment `TokenCursor.Current` is
  an `ElseToken` instead of consuming it as a node — if one was hit,
  `ParseBlock` advances past it (no trim needed, see `Tokenizer` above) and
  parses the falsy body the same way. No `ElseToken` found → `Falsy` stays
  `null`. Outside stop-at-else mode (a bare `--\n` at the top level, not
  inside any block), `ParseNext` turns the `ElseToken` back into a literal
  `--\n` (all three original characters — it's the whole consumed span),
  so the token isn't silently swallowed when it isn't acting as a
  separator.
- **`Ast/`**: `INode` has one method — `Render(RenderContext, JsonElement) :
  string` — and each node type (`LiteralNode`, `TokenNode`, `BlockNode`)
  implements it directly. No separate renderer classes, no visitor, no
  switch anywhere: dispatch is plain polymorphism, the node *is* its own
  renderer. `BlockNode` carries an optional `ElseBody` (`IReadOnlyList
  <INode>?`, defaults `null`); `Render` takes the truthy branch only when
  the resolved value's `ValueKind` is `True` — `False`, `Null`, and
  unresolved all fall through to `ElseBody` (or empty string if there
  isn't one). The falsy branch renders against the same `data`/scope as
  the block itself — there's no object to scope into when the condition
  is false or null.
  - **`PropertyChain.cs`**: `IList<string>` wrapped as a
    `ReadOnlyCollection<string>` — the property-access chain shared by both
    `TokenNode.Properties` and `BlockNode.Properties` (unified naming; used
    to be `Segments`/`Name` respectively).
  - **`PropertyResolver.cs`**: resolves a `PropertyChain` against a
    `JsonElement`, drilling into properties and flattening projection over
    arrays (`.Select()`/`.SelectMany()` per spec). Lives in `Ast` because
    nodes call it directly — no indirection layer.
  - **`Ast/Rendering/IRenderer.cs` + `RenderContext.cs`**: the *only* seam
    `BlockNode` needs to recurse into its own body.
    `RenderContext(PropertyResolver, IRenderer)` is threaded through every
    `Render` call; `TemplateEngine` implements `IRenderer` explicitly. This
    keeps the dependency one-directional — `Ast` depends on nothing outside
    itself + BCL; `TemplateEngine` depends on `Ast`, never the reverse.
- **`TemplateEngine.cs`**: the sole public type. `Render(template, data)`
  tokenizes, parses, builds one `RenderContext`, and calls `RenderAll`.
- Data model: resolves directly against `System.Text.Json.JsonElement`. No
  reflection/POCO adapter exists — the spec's `model.OfferNo`-style C#
  usage is aspirational, not built yet.

### House rules that shaped the above (see `CLAUDE.md` for the durable form)

- Constructors never call `new SomeService()` internally except for
  DTOs/records — real dependencies (`PropertyResolver`, `TokenCursor`,
  `RenderContext`, etc.) are constructor-injected and wired up at the
  composition root (`TemplateEngine.Render`, `Tokenizer.Tokenize()`).
- No `sealed` on any class or record — an explicit, consistently-applied
  house style, not an oversight.
- `Directory.Build.props` sets `EnforceCodeStyleInBuild` and
  `TreatWarningsAsErrors` — `dotnet build`/`dotnet test` fail on any
  `.editorconfig` violation or compiler warning, not just `dotnet format`.
  `.editorconfig` has a custom rule: any `static` field (regardless of
  accessibility) must be `SCREAMING_CASE`; other private fields stay
  `_camelCase`.

## Remaining milestones

In fixture-group order (see `/specs`, numbered simplest → most complex):

1. `02-conditional-blocks` — done: boolean if/no-else (`001a`/`001b`), the
   `--` else split (`002a`/`002b`), and null-object else (`003`).
2. `03-loop-blocks` — list loops, empty list, magic `first`/`last`, `!`
   negation, **plus `004-filtered-item-scope`** (new this session): a block
   name whose chain projects a boolean through a list should *filter* the
   list down to the matching item(s) and scope into the match, rather than
   collapsing multiple projected booleans into one truthy/falsy check (see
   SPECS.md's "Resolving the Block Name"). `PropertyResolver` only
   projects/flattens today — it has no filter/find step yet, so this needs
   real implementation work, not just a fixture unignore.
3. `04-scope-blocks` — object scope, upper-scope fallback (needs a real
   scope chain, not just a single `JsonElement`).
4. `05-variable-definitions` — capturing a block's rendered output into a
   named, positionally-scoped variable.
5. `06-tables` — should mostly fall out of the above once blocks exist
   (rendering is already pure literal substitution with no table special-
   casing); confirm rather than build new.
6. `07-inline-lists` — field-selection projection and custom `(separator)`
   already work for `01-variables/003`-style cases; confirm the remaining
   fixtures, particularly the loop-block-with-separator form.
7. `08-parameters` — `format`/`currency`/`length`.
8. `09-integration` — the full worked example, combining everything above.
9. `10-errors` — currently 1 fixture (`001-unclosed-guillemet`). Add more
   error cases as new failure modes are introduced by the above (e.g. a
   malformed block, a missing property) — extend `TemplateParseException`
   usage rather than introducing ad hoc exceptions.

## Known v1 scope decisions (not gaps to "fix" without discussion)

- **Multi-depth guillemets** (`«««…»»»` for nesting readability) are
  unimplemented — no fixture exercises them yet. Nesting itself *is* now
  exercised (`02-conditional-blocks/005a`–`005c`), just via same-depth `««`
  for both outer and inner block, not a deeper delimiter for the inner one.
- **True schema/localization remapping** — where a template's business term
  differs lexically from the model's property name (spec's `"quote no"` →
  `OfferNo` example) — is out of scope. Only direct
  PascalCase-of-space-words resolution (via Humanizer's `.Dehumanize()`) is
  implemented. There's no schema-file format designed yet.
- **Currency/date/truncation formatting** in the `08-parameters` and
  `09-integration` fixtures encode specific assumptions made when those
  fixtures were authored (no independent spec pins the exact format) — the
  fixtures are the acceptance contract now, so match them exactly rather
  than "correcting" the formatting.
- **Unresolved block name → falsy, not an error**: if a block's `name`
  chain doesn't resolve to anything at all (e.g. it projects through an
  empty list), it's treated as falsy, same as an explicit `false`. Fixed via
  `.SingleOrDefault()` in `BlockNode.Render`; see
  `02-conditional-blocks/004-unresolved-property-no-else`.
- **Boolean-through-list block names still crash on 2+ matches**: today
  `BlockNode.Render`'s `.SingleOrDefault()` throws if the chain's projection
  yields more than one value (e.g. `««items: active` where 2+ items have
  `active`). The intended fix is the filter-and-scope behavior in milestone
  2 above, not a defensive guard — see `03-loop-blocks/004-filtered-item-scope`
  and SPECS.md's "Resolving the Block Name".
- **`--` only requires a trailing newline, not a leading one.** The
  `SymbolNode` trie resolves `ElseToken` from `--` followed by `\n`, with
  no check on what precedes the first `-`. This is a real relaxation of
  the spec wording ("`--` on its own line" reads as both-sides) — no
  current fixture distinguishes the two, so this hasn't been confirmed
  against SPECS.md yet. Revisit if a fixture ever needs a `--` mid-line
  (e.g. an em dash in real content) to *not* be treated as a separator.
- **Multi-depth guillemets beyond exactly 2 are unhandled by the trie.**
  `SymbolTree`'s `«`/`»` paths are exactly 2 levels deep (1 char →
  `OpenToken`, 2 chars → `OpenBlockToken`, no child under the second `«`
  for a 3rd) — a run of 3+ `«` in a row now tokenizes as one
  `OpenBlockToken` (2 chars) followed by a separate `OpenToken` (1 char),
  rather than collapsing into a single deeper-nesting token. Still
  unexercised by any fixture (see "Multi-depth guillemets" above); the
  tree would need a 3rd level added deliberately, not just discovered as a
  side effect, if that's ever needed.
