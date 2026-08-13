# Guillemets

A logicless, markdown-aware template engine for non-technical authors.

```markdown
Dear «full name»,

Your order **«order id»** is on its way to «company: name».
```

rendered against

```json
{
  "FullName": "Alice Smith",
  "OrderId": "A-1024",
  "Company": { "Name": "Acme Logistics" }
}
```

produces

```markdown
Dear Alice Smith,

Your order **A-1024** is on its way to Acme Logistics.
```

`«»` (guillemets) are the sole delimiter characters — they never collide with
standard markdown.

## Usage

```csharp
using Guillemets;
using System.Text.Json;

var template = Template.Create("Dear «full name»,");
var data = JsonDocument.Parse("""{ "FullName": "Alice Smith" }""").RootElement;
var output = template.Render(data);
// => "Dear Alice Smith,"
```

`Render`/`RenderObject` also accept plain C# objects
(`template.RenderObject(new { FullName = "Alice Smith" })`) and
`Newtonsoft.Json.Linq.JToken`.

### Custom filters

`Template.Create` takes an optional `configure` callback exposing
`ParseOptions`, whose `Filters` registry lets you add your own filters
alongside the built-ins (`join`, `date`, `upper`, ...):

```csharp
var template = Template.Create(text,
    options => options.Filters.Register<ReverseFilter>()
);
```

`IFilter` is one method — implement
`IEnumerable<string> Apply(IEnumerable<string> values, string? arg)` to add
one. Every filter maps over the current sequence of values and hands back a
sequence in turn — a single-value filter like `date` returns one string per
input, while a collapsing filter like `join` returns a shorter sequence. The
name a template uses to invoke a filter is derived from its class name — drop
the `Filter` suffix and lowercase it, so `ReverseFilter` is invoked as
`«text | reverse»`.

The same `configure` callback also sets `options.Glossary`, an
`IStringLocalizer` bridging a template's business vocabulary to model
property names that don't already match — see
[`docs/specs.md`](docs/specs.md)'s Glossary & Localization section.

## Development

```
dotnet test
```

## Documentation

- [`docs/specs.md`](docs/specs.md) — full syntax reference and behavior spec.
- [`docs/implementations/dotnet.md`](docs/implementations/dotnet.md) — this
  .NET implementation's own behavior, including its runtime-specific filters.
- [`docs/architecture.md`](docs/architecture.md) — how the engine is built.

## License

[MIT](LICENSE)
