# Architecture

This describes how the engine is built today.

For the full syntax and behavior spec, see [specs.md](specs.md).

## The pipeline, in one picture

A template string becomes a `Template`. A `Template` plus some data becomes
output.

```mermaid
flowchart LR
    subgraph Create["Template.Create(text)"]
        direction LR
        A[Template text] --> B[Tokenizer]
        B --> C[TokenCursor]
        C --> D[Parser]
        D --> E["Ast nodes"]
    end

    subgraph Render["template.Render(data)"]
        direction LR
        E --> F[Renderer]
        G[IDataSource] --> F
        F --> H[Output string]
    end
```

`Template.Create(text)` tokenizes and parses once, giving back a `Template` —
just the parsed tree, nothing render-specific yet. `template.Render(data)`
walks that tree against some data and produces a string. You can call
`Render` as many times as you like, with different data each time; the
`Template` itself never changes.

## Tokenization

The tokenizer turns raw template text into a flat list of tokens: `OpenToken`,
`CloseBlockToken`, `LiteralToken`, and so on.

`SymbolTree` is a trie of the special characters (`«`, `»`, `~`, `:`, `!`,
`=`, `(`, `)`). It always matches the longest run it can, backtracking if
that run turns out invalid — this is how `«` vs. `««` vs. `«««` (block
depth) falls out for free, without any special-casing.

`Tokenizer` makes one pass over the text. It doesn't know what the symbols
mean; it just asks `SymbolTree` how far the next token extends and moves on.

`TokenCursor` holds the resulting token list plus a read position for the
parser to walk. It's also the one place a `LiteralToken` gets trimmed, when
a block header needs to peel a line ending off the text that follows it.

## Parsing

Parsing is recursive-descent, with one small parser class per kind of node —
no single giant `switch` doing all the work.

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
    BlockParser -->|calls| FilterParser
```

`Parser` is the composition root: it registers one concrete parser per token
type, then drives the top-level loop. `NodesParser` is the dispatcher —
given a token, it finds the parser registered for that token's type and
calls it. `VariableParser`, `TextParser`, and `BlockParser` each handle one
node kind; `BlockParser` is the interesting one, since it also checks that a
block's closing `»»` matches its opening depth, and splits a `name = expr`
header into a captured variable name plus a condition.

`FilterParser` parses `(name = value)` groups, such as `(separator = , )`,
into a `FilterNode`. It isn't wired into the normal dispatch table, because
`(` is ordinary text almost everywhere in a template — instead, `BlockParser`
asks it to try parsing a filter only in the one place one can legally
appear: a line of its own, right before a block's closing `»»`. If that
doesn't pan out, the `(` is treated as plain text instead.

## Ast & rendering

Once parsed, the template is a tree of `INode`s that render themselves
against a `Scope`.

`Scope` wraps the current `IDataSource` plus a link to its parent scope.
That parent link is how a property lookup falls back to an enclosing scope,
and how `«first»`/`«last»` reach back to the nearest enclosing loop.

`BlockNode` looks at what a block's header resolves to and picks a
behavior: a list becomes a `LoopBehavior`, an object a `ScopeBehavior`,
anything else a `ConditionalBehavior`. Same syntax every time — only the
resolved type decides. An optional `(separator = ...)` footer rides along
and is handed to `LoopBehavior`, which then joins each iteration with that
separator instead of concatenating them.

`PropertyResolver` does the actual property lookup: walking up parent scopes
to find who owns a property, and handling the "filtered items" case where
`items: active` loops over only the items whose `active` is true.

`VariableStore` backs `««name = ...»»` definitions. A captured value is just
the rendered string, wrapped as a `StringDataSource` — no round-trip through
JSON needed.

## Pluggable data sources

The Ast layer never talks to `JsonElement` or reflection directly. It only
knows about one small interface:

```csharp
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
three built-in adapters, plus a few internal sentinel values, all shipped in
the one `Guillemets` package:

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
    class JTokenDataSource
    class BooleanDataSource
    class StringDataSource
    class UndefinedDataSource

    IDataSource <|.. JsonElementDataSource : public
    IDataSource <|.. PocoDataSource : public
    IDataSource <|.. JTokenDataSource : public
    IDataSource <|.. BooleanDataSource : internal
    IDataSource <|.. StringDataSource : internal
    IDataSource <|.. UndefinedDataSource : internal
```

`JsonElementDataSource` adapts `System.Text.Json.JsonElement`.
`PocoDataSource` adapts any plain C# object via reflection, exact-case, the
same as the JSON side. `JTokenDataSource` adapts
`Newtonsoft.Json.Linq.JToken` — the handful of `JTokenType` kinds that
`DataKind` has no equivalent for collapse into `String` (still displayable)
or `Undefined` (structural/rare tokens like comments).

`BooleanDataSource`, `StringDataSource`, and `UndefinedDataSource` are
internal sentinel values used inside the engine itself — negation results,
magic loop variables, captured variable strings, "this property doesn't
exist." They aren't adapters, so unlike the three above they stay internal.

Each adapter also brings a friendlier way to call `Render`, as an extension
method on `Template`: `template.Render(jsonElement)`,
`template.Render(jToken)`, `template.RenderObject(poco)`. These live
alongside their adapter's data source but sit in the plain `Guillemets`
namespace, so anyone who already has `using Guillemets;` gets them for free.

The actual rendering work happens in `Renderer`, which is built fresh for
every call to `Render` — so a single `Template` is safe to reuse across many
renders, even concurrently.

## Tests

`JsonElementDataSourceTests`, `PocoDataSourceTests`, and
`JTokenDataSourceTests` unit-test each `IDataSource` adapter directly,
without going through `Template` at all. `JsonIntegrationTests`,
`PocoIntegrationTests`, and `JTokenIntegrationTests` are black-box instead,
going through `Template.Create(...).Render(...)`.

The `/specs` fixture corpus itself is the main acceptance suite, run once
through `SpecTests` against JSON data — the other two adapters get a small
targeted suite each rather than re-running the whole corpus three times.
