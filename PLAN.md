# Implementation plan

Living roadmap for building the guillemets engine against the `/specs`
fixture corpus. For *how* to work (TDD discipline, code style), see
`CLAUDE.md` — this file is *what's done and what's next*.

## Status

36 of 45 fixtures pass (`dotnet test` is authoritative). Done: `basics`,
`variables`, `conditional-blocks`, `loop-blocks`, `scope-blocks`.
`variable-definitions` is partially done — `definition-boolean` and
`definition-object` pass. Remaining fixtures are listed in
`FixtureTests.cs`'s `IGNORED_FIXTURES` set.

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
- **`Parsing/`**: recursive-descent, split by node kind rather than one
  monolithic `Parser` class. `Parser` (the composition root) builds a
  `ParserBuilder`, registers a factory per token type (`VariableParser`
  for `OpenToken`, `BlockParser` for `OpenBlockToken`, `TextParser` for
  `ITextToken`), calls `.Build()` to get back an `IParser`, then drives
  the top-level token loop itself (`while (!tokens.AtEnd) nodes.Add(
  parser.Parse(tokens.Current))`) — `TemplateEngine` only ever sees
  `Parser.Parse()` returning `List<INode>`.
  - **`IParser`**: one-method contract, `INode Parse(IToken token)` —
    shared by every node-level parser so dispatch is uniform regardless
    of which concrete parser ends up handling a token.
  - **`ParserBuilder`**: accumulates `Register<TToken>(Func<NodesParser,
    IParser> factory)` calls (fluent, returns `this`), deferring actual
    construction to `Build()`. Deferring via a factory — instead of a
    ready-made `IParser` instance — is what breaks the construction
    cycle between `NodesParser` (needs the concrete parsers to dispatch
    to) and `BlockParser` (needs `NodesParser` itself, to recurse into a
    block's body): `Build()` constructs `NodesParser` first, *then*
    invokes each factory with that already-built `NodesParser`.
  - **`NodesParser`**: implements `IParser` explicitly (mirrors
    `TemplateEngine : IRenderer`'s explicit-implementation style), so
    the only externally visible surface is `Parse(IToken)`; a private
    `Parse` does the real token→parser dispatch (linear scan over a
    `Dictionary<Type, IParser>`, matched via `Type.IsInstanceOfType` so
    an interface key like `ITextToken` still catches every concrete
    implementer). `ParseNodes(bool insideBlock, bool stopAtElse)` is the
    block-recursion entry point — `BlockParser` calls back into it for a
    block's truthy/falsy body — but those flags never surface outside
    `Parsing/`; `Parser`'s top-level loop goes through `Parse(IToken)`
    only, never `ParseNodes`.
  - **`VariableParser`** / **`TextParser`** / **`BlockParser`**: one
    class per node kind, each casting the `IToken` it receives to the
    concrete type it expects. `»»` is always on its own line (per
    SPECS.md) — no closing-newline special-casing. `BlockParser`
    requires the closing token's `Depth` to equal the opening token's
    `Depth` (`ValidateClosingDepth`), throwing `TemplateParseException`
    on a mismatch — depth beyond 2 is readability-only, but must still
    balance. Its header parsing (`ParseHeader`) also splits an `=`
    (tokenized as `EqualsToken`, `Tokenization/Symbols.cs`) into an
    optional captured variable name and the `PropertyChain` condition
    after it, via an `out string? variableName` parameter — no DTO type
    for "a block header." The name itself comes from
    `PropertyChainBuilder.PopVariableName()`, which pulls the single
    accumulated segment back out and clears the builder in place, rather
    than a throwaway `PropertyChain` via `Build()` just to unwrap it.
- **`Ast/PropertyChainBuilder.cs`**: builds a `PropertyChain` from
  tokens; tracks `!`-negation as `PropertyChain.LastSegmentNegated`
  (never encoded into the segment strings) and drops
  empty/whitespace-only fragments a `NegationToken` can split off a
  literal (e.g. the space in `"company: !active"`).
- **`Ast/Scope.cs`**: wraps the current `JsonElement` plus an optional
  `Parent` scope (how object-scope blocks and loop items chain back to
  their enclosing scope for upper-scope fallback) and optional
  `IsFirst`/`IsLast`, resolved ahead of real JSON lookup via
  `TryGetMagic` — how `«first»`/`«last»` exist without being real JSON
  properties; `TryGetMagic` itself recurses into `Parent` so a magic
  variable referenced from a nested object-scope block still resolves
  against the enclosing loop item.
- **`Ast/BlockNode.cs`**: resolves either a loop-items list
  (`PropertyResolver.ResolveLoopItems`) or a single value, dispatching to
  `LoopBehavior`/`ScopeBehavior`/`ConditionalBehavior` (`IBlockBehavior`)
  by resolved type (list/object/other) — selection between existing
  strategies, not per-type logic inline.
- **`Ast/ScopeBehavior.cs`**: object-scope blocks — renders the body
  against a new `Scope` wrapping the resolved object, parented to the
  block's enclosing scope.
- **`Ast/PropertyResolver.cs`**: `ResolveItemsMatchingLastSegment`
  implements filtered-item-scope (`items: active` → filter + loop over
  every match, per spec's plural "item(s)"). `ResolveScope` walks
  `Scope.Parent` to find which ancestor scope actually owns a given
  top-level property — how upper-scope fallback works.
- **`TemplateEngine.cs`**: the sole public type; resolves directly
  against `JsonElement` (no reflection/POCO adapter yet).
- **`Ast/VariableStore.cs`**: constructor-injected into `PropertyResolver`
  and exposed on `RenderContext.Variables`. `BlockNode.Render` checks
  `VariableName`: if set, it stores the block's rendered output (trailing
  newline trimmed, since `»»`/`~` are always on their own line and that
  structural newline isn't part of the captured value) under the name
  instead of emitting it inline, returning `string.Empty` at that
  position. `PropertyResolver.Resolve` checks the store for single-segment
  property chains the same way it checks `Scope.TryGetMagic` — a defined
  variable is looked up before falling through to JSON. Values are built
  directly as string `JsonElement`s (`Utf8JsonWriter.WriteStringValue`
  into an `ArrayBufferWriter<byte>`, then `JsonDocument.Parse` on those
  bytes — no `JsonSerializer` round-trip), the same
  sentinel-construction spirit as `JsonBooleans`, so downstream code
  (`VariableNode.Render`) doesn't need a separate non-JSON code path.

See `CLAUDE.md`'s C# code style section for the house rules that shaped
this design.

## Remaining milestones

In fixture-group order (see `/specs`, simplest → most complex; group
folders are numbered on disk purely for sort order — referred to here by
name only). **Reordered**: `parameters` now comes before
`variable-definitions` (was after `inline-lists`) —
`definition-list-separator` (the one remaining `variable-definitions`
fixture) needs `(separator = , )` parameter parsing, so parameters has
to land first regardless of fixture-number order on disk. The
pluggable-data-sources refactor below is not a numbered fixture-group
milestone, but it's sequenced immediately before `parameters` in the
actual work order (see its section for why):

1. `conditional-blocks` — done.
2. `loop-blocks` — done.
3. `scope-blocks` — done.
4. `parameters` — `format`/`currency`/`length`. Do this once the
   pluggable-data-sources refactor below lands, not before — it's the
   first milestone that actually needs typed (`DateTime`/`decimal`)
   access rather than just display strings/booleans.
5. `variable-definitions` — capturing a block's rendered output into a
   named, positionally-scoped variable. `definition-boolean` and
   `definition-object` done. `definition-list-separator` — the group's
   last remaining fixture — needs `(separator = , )` parameter parsing
   from the milestone above; do it last, once `parameters` is done.
6. `tables` — should mostly fall out of the above once blocks exist;
   confirm rather than build new.
7. `inline-lists` — field-selection projection and custom `(separator)`
   already work for `variables/nested-property-chained-list`-style cases;
   confirm the remaining fixtures, particularly the loop-with-separator
   form.
8. `integration` — the full worked example, combining everything above.
9. `errors` — currently 3 fixtures (`unclosed-guillemet`,
   `unclosed-block`, `mismatched-block-depth`). Add more error cases as
   new failure modes appear — extend `TemplateParseException` usage
   rather than introducing ad hoc exceptions.

## Planned: pluggable data sources (not started)

Today the engine resolves directly against `System.Text.Json.JsonElement`
end to end — `Scope`, `PropertyResolver`, `JsonBooleans`,
`ConditionalBehavior`/`LoopBehavior`/`ScopeBehavior`, `BlockNode`,
`VariableNode` all reference `JsonElement`/`JsonValueKind` by name, and
`TemplateEngine.Render(string, JsonElement)` is the only entry point.
SPECS.md's "Schema & Localization" section, though, already describes the
model in plain-C#-object terms (`model.OfferNo`, `model.Company.Name`),
not JSON-specific ones — so JSON was an implementation shortcut, not a
spec commitment. Requested: let the engine run against Newtonsoft.Json
(`JToken`/`JObject`) and against arbitrary business objects (POCOs), not
just `System.Text.Json`.

This is an architectural refactor, not a fixture group, so it doesn't
slot into the numbered milestones above — but it's next up, **before**
`parameters`: `parameters` is the first remaining milestone that needs
typed access (real `DateTime`/`decimal`, not just display strings), and
`IDataNode` as sketched below only exposes `AsDisplayString()`/
`AsBoolean()` — so this is also where the "Open questions" item about
growing the interface for `parameters` actually gets decided, not
deferred again. Sequencing: do this refactor now, prove it's
behavior-preserving (`dotnet test` stays fully green — the fixture suite
tests behavior, not `JsonElement` specifically), then `parameters`, then
close out `variable-definitions` with `definition-list-separator`.

### Shape

A small internal seam, `IDataNode`, replaces direct `JsonElement` use
everywhere in `/src/Guillemets/Ast`:

```csharp
internal interface IDataNode
{
    DataKind Kind { get; }   // Object, Array, String, Number, Boolean, Null, Undefined
    bool TryGetProperty(string name, out IDataNode value);
    IEnumerable<IDataNode> EnumerateArray();
    bool AsBoolean();
    string? AsDisplayString();   // backs VariableNode's current `value.ToString()`
}
```

`DataKind` uses one `Boolean` member (+ `AsBoolean()`), not
`JsonValueKind`'s split `True`/`False` members — that split is a
`System.Text.Json` quirk, not something to leak into the abstraction.
`JsonBooleans.TRUE`/`FALSE` (singleton sentinel values used by negation,
magic vars, and filtered-item-scope results) becomes a source-agnostic
equivalent — likely a plain `BooleanDataNode(bool)` value type in the
same file as the interface.

Every current internal type keyed on `JsonElement`/`JsonValueKind` swaps
to `IDataNode`/`DataKind`: `Scope.Data`, `PropertyResolver`'s resolve
methods, `ConditionalBehavior.Value`, `LoopBehavior.Items`,
`ScopeBehavior.Value`, `BlockNode`'s type checks, `VariableNode.Render`'s
`.ToString()` call. `PropertyChainBuilder`/`PropertyChain` are untouched
— they operate on template-side tokens, not the data side.

### Adapters

- **`JsonElementDataNode`** (`System.Text.Json`) — stays in the core
  `Guillemets` project (already the only JSON library referenced; no new
  dependency). Backs the existing `TemplateEngine.Render(string,
  JsonElement)` overload, now implemented as a thin wrap-and-delegate to
  a new `Render(string, IDataNode)` core path.
- **`PocoDataNode`** (reflection over arbitrary business objects) — also
  stays in core; needs only `System.Reflection`, no new package.
  Property lookup should follow the same convention already established
  for JSON: `PropertyChain` segments are already `.Dehumanize()`d to
  PascalCase before lookup, so this becomes an exact
  `Type.GetProperty(name)` call, consistent with how `JsonElement`
  lookup is exact-case today. Arrays/lists resolve via a plain
  `IEnumerable` check. Backs a new `TemplateEngine.Render(string,
  object)` overload.
- **`Guillemets.Newtonsoft`** — a new sibling project (own
  `Guillemets.Newtonsoft.csproj`, referencing core `Guillemets` +
  `Newtonsoft.Json`, version pinned in `Directory.Packages.props`), so
  the core package doesn't force a Newtonsoft dependency on consumers
  who don't need it — mirrors how e.g. ASP.NET Core ships
  `Microsoft.AspNetCore.Mvc.NewtonsoftJson` as a separate package rather
  than baking it into the core. Adds `JTokenDataNode` and a
  `Render(string, JToken)` overload (extension method on
  `TemplateEngine`, or a partial-class addition — TBD at implementation
  time).

`TemplateEngine` stays "the sole public type" per CLAUDE.md's existing
architecture note — these are overloads of `Render`, not new public
types, except for `IDataNode` itself if a consumer wants to write a
fourth adapter (XML, a database row, whatever) without waiting on us.

### Open questions to settle before coding (don't decide silently)

- Interface name: `IDataNode` vs `IDataSource` vs `IModelNode` — pick
  one and use it consistently; not settled yet.
- Does `PocoDataNode` belong in core, or should it also be a sibling
  project for symmetry with the Newtonsoft adapter? Leaning core (no
  extra package needed), but worth a second opinion.
- POCO property-name matching: exact PascalCase only (matching today's
  JSON behavior), or should it support case-insensitive / attribute-based
  remapping now that a real schema-mapping layer might eventually live
  here too (see "True schema/localization remapping" below)?
- `parameters` (`format`/`currency`/`length`, milestone 4 above, done
  right after this refactor) will need typed access (actual
  `DateTime`/`decimal`, not just string/bool) — `IDataNode` as sketched
  above only exposes `AsDisplayString()`/`AsBoolean()`. Decide now
  whether to grow the interface for that as part of this refactor
  (recommended, since `parameters` is next) rather than leaving it to be
  redesigned twice.
- Test strategy for multi-source parity: full `/specs` corpus per
  adapter (3x fixture count) is probably overkill; more likely a small
  targeted NUnit suite that runs a handful of representative fixtures
  through each adapter to prove behavioral parity, while `/specs`
  remains the `JsonElement` (default) contract.

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
