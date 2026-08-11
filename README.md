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

## Development

```
dotnet test
```

## Documentation

- [`docs/specs.md`](docs/specs.md) — full syntax reference and behavior spec.
- [`docs/architecture.md`](docs/architecture.md) — how the engine is built.

## License

[MIT](LICENSE)
