# Architecture

This describes how the engine is built today. It's not a plan.

- Status and remaining work: `PLAN.md`
- Behavior and spec: `SPECS.md`
- Coding conventions: `CLAUDE.md`

## The pipeline, in one picture

A template string becomes a `Template`. A `Template` plus some data
becomes output.

```mermaid
flowchart LR
    subgraph Create["Template.Create(templateString)"]
        direction LR
        A[Template text] --> B[Tokenizer]
        B --> C[TokenCursor]
        C --> D[Parser]
        D --> E["Ast nodes\n(List&lt;INode&gt;)"]
    end

    subgraph Render["template.Render(data)"]
        direction LR
        E --> F[Renderer]
        G[IDataSource] --> F
        F --> H[Output string]
    end
```

Two calls, two jobs:

- **`Template.Create(text)`** tokenizes and parses once. It hands back a
  `Template` — just the parsed AST, nothing render-specific yet.
- **`template.Render(data)`** walks that AST against some data and
  produces a string. Call it as many times as you want, with different
  data each time. The `Template` itself never changes.

## Tokenization

Turns raw template text into a flat list of tokens (`OpenToken`,
`CloseBlockToken`, `LiteralToken`, ...).

- **`SymbolTree`** is a trie of the special characters (`«`, `»`, `~`,
  `:`, `!`, `=`). It matches the *longest* run it can, then backtracks
  if that run turns out invalid. This is how `«` vs `««` vs `«««` (block
  depth) falls out for free — the trie just loops on itself for repeated
  characters.
- **`Tokenizer`** does a single pass over the text. It doesn't know what
  the symbols mean — it just asks `SymbolTree` "how far does a token
  extend from here?" and advances.
- **`TokenCursor`** holds the token list and a read position for the
  parser. It's the only place a `LiteralToken` gets trimmed in place.

## Parsing

Recursive-descent, one small parser class per node kind — no giant
`switch` in a single `Parser` class.

```mermaid
flowchart TB
    Parser -->|builds| ParserBuilder
    ParserBuilder -->|registers| VariableParser
    ParserBuilder -->|registers| BlockParser
    ParserBuilder -->|registers| TextParser
    NodesParser -->|dispatches to| VariableParser
    NodesParser -->|dispatches to| BlockParser
    NodesParser -->|dispatches to| TextParser
    BlockParser -->|recurses via| NodesParser
```

- **`Parser`** is the composition root. It builds a `ParserBuilder`,
  registers one concrete parser per token type, then drives the
  top-level loop itself.
- **`ParserBuilder`** just accumulates `Register<TToken>(factory)`
  calls and builds everything at the end. Deferring construction this
  way is what breaks a circular dependency: `NodesParser` needs the
  concrete parsers to dispatch to, and `BlockParser` needs `NodesParser`
  back, to parse a block's body.
- **`NodesParser`** is the dispatcher. Given a token, it looks up which
  concrete parser handles that token's type and calls it.
- **`VariableParser`**, **`TextParser`**, **`BlockParser`** each handle
  one node kind. `BlockParser` is the interesting one: it checks that a
  block's closing `»»` run has the same depth as its opening `««`, and
  it splits `name = expr` headers into a captured variable name plus a
  condition.

## Ast & rendering

Once parsed, the template is a tree of `INode`s that know how to render
themselves against a `Scope`.

- **`Scope`** wraps the current `IDataSource` plus a link to its parent
  scope. This parent link is how a property lookup falls back to an
  enclosing scope, and how `«first»`/`«last»` reach back to the nearest
  enclosing loop.
- **`BlockNode`** looks at what a block's header resolves to and picks a
  behavior: a list → `LoopBehavior`, an object → `ScopeBehavior`,
  anything else → `ConditionalBehavior`. Same syntax, behavior chosen by
  the resolved `DataKind`.
- **`PropertyResolver`** does the actual property lookup, including the
  "filtered items" case (`items: active` loops over items where
  `active` is true) and walking up parent scopes to find who owns a
  property.
- **`VariableStore`** backs `««name = ...»»` variable definitions. A
  captured value is just a rendered string, wrapped as a
  `StringDataSource` — no round-trip through JSON needed.

## Pluggable data sources

The `Ast` layer never talks to `JsonElement` or reflection directly. It
only knows about one small interface:

```csharp
namespace Guillemets.Data;

public interface IDataSource
{
    DataKind Kind { get; }   // Object, Array, String, Number, Boolean, Null, Undefined
    bool TryGetProperty(string name, out IDataSource value);
    IEnumerable<IDataSource> EnumerateArray();
    bool AsBoolean();
    string? AsDisplayString();
}
```

Anyone can implement this to plug in a new data format. Today there are
two built-in adapters, plus a few internal sentinel values:

```mermaid
classDiagram
    class IDataSource {
        <<interface>>
        +Kind DataKind
        +TryGetProperty(name) bool
        +EnumerateArray() IEnumerable
        +AsBoolean() bool
        +AsDisplayString() string
    }
    class JsonElementDataSource
    class PocoDataSource
    class BooleanDataSource
    class StringDataSource
    class UndefinedDataSource

    IDataSource <|.. JsonElementDataSource : public
    IDataSource <|.. PocoDataSource : public
    IDataSource <|.. BooleanDataSource : internal
    IDataSource <|.. StringDataSource : internal
    IDataSource <|.. UndefinedDataSource : internal
```

- **`JsonElementDataSource`** (`Guillemets.Data.Json`) adapts
  `System.Text.Json.JsonElement`.
- **`PocoDataSource`** (`Guillemets.Data.Poco`) adapts any plain C#
  object via reflection. Property lookup is exact-case, same as the
  JSON side — no case-insensitive matching.
- **`BooleanDataSource`**, **`StringDataSource`**, **`UndefinedDataSource`**
  (`Guillemets.Data.Primitives`) are internal sentinel values: negation
  results, magic-variable booleans, captured variable strings, "this
  property doesn't exist." Not adapters, so they stay internal even
  though `IDataSource` is public.

### Where the `Render` methods live

`Template` has one core method: `Render(IDataSource)`. It's public, but
you'll rarely call it directly — each adapter brings its own friendlier
overload as an extension method:

- `template.RenderJson(jsonElement)`
- `template.RenderObject(poco)`

Both extension classes live in the plain `Guillemets` namespace, not
nested under `Guillemets.Data`. That's deliberate: C# already lets code
in a nested namespace see its parent namespace without an extra
`using`. So `Guillemets.Data.Json` can see `Template` for free — and
by putting the extension method in `Guillemets` too, any caller who
already wrote `using Guillemets;` gets `RenderJson`/`RenderObject` for
free as well. A future adapter (say, a `Guillemets.Newtonsoft` package
adding `JTokenDataSource`) should do the same: adapter type in its own
namespace, `Render` extension method in the project's root namespace.

`Renderer` (internal) holds the actual per-call state — it's built
fresh inside every `Render()` call, so one `Template` is safe to reuse
across many renders, even concurrently.

## Tests

- **`JsonElementDataSourceTests`** / **`PocoDataSourceTests`** unit-test
  `IDataSource` directly — no `Template` involved. `PocoDataSourceTests`
  covers arrays, `List<T>`, `HashSet<T>`, `Collection<T>`, and a lazy
  `IEnumerable<T>`, not just `List`.
- **`JsonIntegrationTests`** / **`PocoIntegrationTests`** are black-box:
  they go through `Template.Create(...).RenderX(...)`. Each has one test
  reading the full `specs/09-integration/001-customer-offer` fixture.
  Both are currently `[Ignore]`d — confirmed genuinely failing today,
  waiting on `tables` and `parameters` (see `PLAN.md`).
- **`SpecsRoot`** is a shared helper for finding `/specs` on disk, used
  by all of the above plus `SpecTests`.
- `/specs` itself stays the one JSON-based contract, run through
  `SpecTests`. The other adapters get a small targeted suite instead of
  running the whole corpus three times.

## Why it's shaped this way

- **`IDataSource`, not `IDataNode`.** `Node` would clash with `Ast`'s
  own `INode` tree.
- **`PocoDataSource` lives in core**, not a separate package. It only
  needs `System.Reflection` — no extra dependency to justify splitting
  it out.
- **Exact-case property matching**, matching how the JSON side already
  works. No case-insensitive or attribute-based remapping.
- **No typed accessors yet** (no `AsDateTime()`, `AsDecimal()`).
  `parameters` (`format`/`currency`/`length`) will need real typed
  values, not just display strings — but that shape isn't decided yet.
  It'll be designed test-first, once that milestone actually starts.
  Guessing at it now would risk designing it twice.
