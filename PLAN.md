# Implementation plan

Living roadmap for building the guillemets engine against the `/specs`
fixture corpus. For *how* to work (TDD discipline, code style, the
`IGNORED_FIXTURES` convention), see `CLAUDE.md` — this file is *what's done
and what's next*.

## Status

Run `dotnet test` for the current, authoritative count — as of this
writing: 10 of 31 fixtures implemented (`00-basics`, `01-variables`, and the
boolean/no-else half of `02-conditional-blocks`), the rest listed in
`FixtureTests.cs`'s `IGNORED_FIXTURES` set.

## Architecture (as built so far)

Pipeline: **`Tokenizer` → `TokenCursor` → `Parser` → self-rendering `Ast`
nodes → `TemplateEngine`**, in `/src/Guillemets`.

- **`Tokenizer.cs`**: a flat lexer, O(n), single pass. Emits only 4 terminal
  kinds (`Tokens/OpenToken.cs`, `CloseToken.cs`, `ColonToken.cs`,
  `LiteralToken.cs`), each carrying a `Position(Line, Column)`. `Tokenize()`
  returns a `TokenCursor` directly (not a raw list) — the cursor is the
  tokenizer's real output.
- **`TokenCursor.cs`**: owns the token list + read position. Exposes
  `AtEnd`/`Current`/`Advance()`/`Skip(n)`/`CountConsecutive<TToken>()` for
  reading, plus `TrimCurrentLiteral(length)` and
  `TrimLeadingNewlineIfPresent()` — the *only* two places that mutate a
  `LiteralToken` in place to consume part of its text (used by block-header
  parsing and by swallowing the newline after a closing `»»`). This exists
  specifically so `Parser` never touches a raw index — see the "Parser
  rewrite" note below.
- **`Parser.cs`**: recursive-descent over the cursor. `Parse()` →
  `ParseNodes(closeDepth)` loops calling `ParseNext()`, which dispatches to
  `ParseVariable` (inline `«token»` → `TokenNode`) or `ParseBlock` (`««name`
  … `»»` → `BlockNode`, added this session — this was the "first group
  needing real block parsing" milestone, now built for the boolean/if case).
  `ParseBlockHeader` reads the header line into a `PropertyChain`, splitting
  the owning literal token at the first `\n` via
  `TokenCursor.TrimCurrentLiteral`. Throws `TemplateParseException` (with
  `Position`) on an unclosed `«`/`««`.
- **`Ast/`**: `INode` has one method — `Render(RenderContext, JsonElement) :
  string` — and each node type (`LiteralNode`, `TokenNode`, `BlockNode`)
  implements it directly. No separate renderer classes, no visitor, no
  switch anywhere: dispatch is plain polymorphism, the node *is* its own
  renderer.
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

1. `02-conditional-blocks` — boolean if/no-else is done (`001`, `002`).
   Still open: `003`/`004` (`--` else split) and `005` (null-object else) —
   needs the parser to recognize a `--`-only line inside a block body and
   split it into truthy/falsy node lists.
2. `03-loop-blocks` — list loops, empty list, magic `first`/`last`, `!`
   negation, **plus `005-filtered-item-scope`** (new this session): a block
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
  unimplemented — no fixture exercises them yet.
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
  `02-conditional-blocks/006-unresolved-property-no-else`.
- **Boolean-through-list block names still crash on 2+ matches**: today
  `BlockNode.Render`'s `.SingleOrDefault()` throws if the chain's projection
  yields more than one value (e.g. `««items: active` where 2+ items have
  `active`). The intended fix is the filter-and-scope behavior in milestone
  2 above, not a defensive guard — see `03-loop-blocks/005-filtered-item-scope`
  and SPECS.md's "Resolving the Block Name".
