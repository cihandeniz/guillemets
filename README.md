# Guillemets

A markdown-aware template engine for non-technical authors. `«»` (guillemets)
are the sole delimiter characters — they never collide with standard markdown.

```markdown
Hi «first name»,

««is member
Welcome back — thanks for being a member!
~
Thanks for placing your first order with us!
»»

**Order #«order id»** — placed «order date | date: MMMM d, yyyy»

««items
| Item   | Qty        | Price                 | Total                       |
| ------ | ---------- | --------------------- | --------------------------- |
| «name» | «quantity» | «price | currency: $» | «total | currency: $»       |
|        |            | **Order total**       | «order total | currency: $» |
»»

You ordered «items: name | join last:  and  | join: , ».

Shipping to «shipping address: city», «shipping address: state».

Thanks for shopping with us!
```

rendered against

```json
{
  "FirstName": "Priya",
  "IsMember": true,
  "OrderId": "A-1042",
  "OrderDate": "2026-03-04",
  "OrderTotal": 87.50,
  "Items": [
    { "Name": "Wireless Mouse", "Quantity": 1, "Price": 24.99, "Total": 24.99 },
    { "Name": "USB-C Hub", "Quantity": 1, "Price": 39.99, "Total": 39.99 },
    { "Name": "Mousepad", "Quantity": 2, "Price": 11.26, "Total": 22.52 }
  ],
  "ShippingAddress": { "City": "Austin", "State": "TX" }
}
```

produces

---

Hi Priya,

Welcome back — thanks for being a member!

**Order #A-1042** — placed March 4, 2026

| Item   | Qty        | Price                 | Total                       |
| ------ | ---------- | --------------------- | --------------------------- |
| Wireless Mouse | 1 | $24.99 | $24.99       |
| USB-C Hub | 1 | $39.99 | $39.99       |
| Mousepad | 2 | $11.26 | $22.52       |
|        |            | **Order total**       | $87.50 |

You ordered Wireless Mouse, USB-C Hub and Mousepad.

Shipping to Austin, TX.

Thanks for shopping with us!

<details>
<summary>Raw output</summary>

```markdown
Hi Priya,

Welcome back — thanks for being a member!

**Order #A-1042** — placed March 4, 2026

| Item   | Qty        | Price                 | Total                       |
| ------ | ---------- | --------------------- | --------------------------- |
| Wireless Mouse | 1 | $24.99 | $24.99       |
| USB-C Hub | 1 | $39.99 | $39.99       |
| Mousepad | 2 | $11.26 | $22.52       |
|        |            | **Order total**       | $87.50 |

You ordered Wireless Mouse, USB-C Hub and Mousepad.

Shipping to Austin, TX.

Thanks for shopping with us!
```

</details>

---

One template covers both the member and first-time-buyer greeting (an
`if`/`else` block, inferred from `IsMember` being a boolean — no keyword
needed), repeats the order table's one row per item straight from a plain
markdown table, and turns the item list into a natural "A, B and C" sentence
with the `join`/`join last` filters. See [`docs/specs.md`](docs/specs.md) for
the full language.

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

`IFilter` is one method — implement `IEnumerable<string>
Apply(IEnumerable<string> values, string? arg)` to add one. Every filter maps
over the current sequence of values and hands back a sequence in turn — a
single-value filter like `date` returns one string per input, while a collapsing
filter like `join` returns a shorter sequence. The name a template uses to
invoke a filter is derived from its class name — drop the `Filter` suffix and
lowercase it, so `ReverseFilter` is invoked as `«text | reverse»`.

The same `configure` callback also sets `options.Glossary`, an
`IStringLocalizer` bridging a template's business vocabulary to model property
names that don't already match — see [`docs/specs.md`](docs/specs.md)'s Glossary
& Localization section.

## Documentation

- [`docs/specs.md`](docs/specs.md) — full syntax reference and behavior spec.
- [`docs/implementations/dotnet.md`](docs/implementations/dotnet.md) — this
  .NET implementation's own behavior, including its runtime-specific filters.
- [`docs/architecture.md`](docs/architecture.md) — how the engine is built.

## License

[MIT](LICENSE)
