# Implementation plan

Living roadmap for building the guillemets engine against the `/specs`
fixture corpus. For *how* to work (TDD discipline, code style, the
`IgnoredFixtures` convention), see `CLAUDE.md` — this file is *what's done
and what's next*.

## Status

Run `dotnet test` for the current, authoritative count — as of this
writing: 7 of 29 fixtures implemented (`00-basics`, `01-variables`), the
rest listed in `FixtureTests.cs`'s `IgnoredFixtures` set.

## Architecture (as built so far)

Pipeline: **`Tokenizer` → `Parser` → `Renderer`**, in `/src/Guillemets`.

- **`Tokenizer.cs`**: a flat lexer, O(n), single pass. Emits only 4 terminal
  kinds (`Tokens/OpenToken.cs`, `CloseToken.cs`, `ColonToken.cs`,
  `LiteralToken.cs`), each carrying a `Position(Line, Column)`. It has no
  concept of a "guillemet span" or "path segment" — that's entirely the
  parser's job. Line/column are tracked incrementally in the same scan
  (`Position.NextLine()`/`NextColumn()`), not recomputed per token.
- **`Parser.cs`**: a stateful, sequential walk over the token list (index
  cursor + `is`-pattern branching — this is normal parser-writing, not the
  switch-vs-polymorphism concern that applies to the renderer stage). Builds
  `Ast/LiteralNode.cs` and `Ast/TokenNode.cs` (`INode`, a pure marker
  interface — AST types carry no behavior). A `ColonToken` outside an
  `Open…Close` span becomes literal `:` text; inside one, it separates
  `TokenNode.Segments`. Throws `TemplateParseException` (with `Position`) on
  an unclosed `«`.
- **`Renderers/`**: one renderer class per node type
  (`LiteralNodeRenderer`, `TokenNodeRenderer`), each extending
  `NodeRendererBase<TNode>` to get a strongly-typed `Render(TNode, ...)`
  override with no manual casting. `TemplateEngine.cs` holds one instance of
  each and a `switch` that *selects* which instance to use — the per-type
  behavior itself lives in the renderer classes, not in the switch.
  `TokenNodeRenderer` resolves `:`-separated segments recursively:
  encountering an array mid-path projects the remaining segments over every
  item and flattens (matching the spec's `.Select()`/`.SelectMany()`
  description); the final result (one value or many) is joined with `, `,
  which is also exactly the inline-list-join behavior.
- Data model: resolves directly against `System.Text.Json.JsonElement`. No
  reflection/POCO adapter exists — the spec's `model.OfferNo`-style C#
  usage is aspirational, not built yet.

## Remaining milestones

In fixture-group order (see `/specs`, numbered simplest → most complex):

1. `02-conditional-blocks` — boolean blocks, `--` else, null-object else.
   **First group needing real block parsing** (`««name` / `--` / `»»`) —
   the parser currently only handles inline `«token»` spans, nothing
   multi-line/structural yet. This is the next significant design step, not
   a small renderer tweak.
2. `03-loop-blocks` — list loops, empty list, magic `first`/`last`, `!`
   negation.
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
