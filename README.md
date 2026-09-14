# Guillemets

A markdown-aware template engine for non-technical authors.

Templates use `«»`, guillemets, pronounced *ghee-uh-MAY*, the quotation marks
French and several other languages use. Markdown assigns them no meaning, so
`«first name»` drops into a heading, a table cell or a blockquote without
escaping, and every placeholder is still easy to spot while skimming.

> [!TIP]
>
> - [**Cheatsheet**](docs/cheatsheet.md): Every feature, one example each
> - [**Try it in .NET Fiddle**](https://dotnetfiddle.net/S6JocQ): Nothing to
>   install

<details>
<summary>How to type «»</summary>

| Platform                          | «                         | »                         |
| --------------------------------- | ------------------------- | ------------------------- |
| Windows (any keyboard)            | `Alt+0171` (numpad)       | `Alt+0187` (numpad)       |
| Windows, French AZERTY            | `AltGr+Z`                 | `AltGr+X`                 |
| macOS, US/English keyboard        | `Option+\`                | `Option+Shift+\`          |
| macOS, French AZERTY              | `Option+7`                | `Option+Shift+7`          |
| Linux (any keyboard, GNOME/GTK)   | `Ctrl+Shift+U 00ab Enter` | `Ctrl+Shift+U 00bb Enter` |
| Linux, French AZERTY or Turkish Q | `AltGr+Z`                 | `AltGr+X`                 |

On Windows/macOS, Turkish (Q) keyboards have no dedicated key for either
character — use the row above for your OS instead. If `Alt+0171`/ `Alt+0187`
doesn't work in a given Windows app, try `Alt+174`/`Alt+175` (same numpad
requirement, no leading zero) — a different Alt-code table that some apps read
instead.

On macOS, you can also set up **Text Replacement** once so `<<`/`>>` expand to
`«`/`»` automatically in any app: System Settings → Keyboard → Text Input → Text
Replacements… → `+` → add `<<` → `«`, then `>>` → `»`.

</details>

---

```markdown
Hi «first name»,

««is member

Welcome back — thanks for being a member!

~

Thanks for placing your first order with us!

»»

**Order #«order id»** — placed «order date / date: MMMM d, yyyy»

««items

| Item   | Qty        | Price              | Total                    |
| ------ | ---------- | ------------------ | ------------------------ |
| «name» | «quantity» | «price / currency» | «total / currency»       |
|        |            | **Order total**    | «order total / currency» |

»»

You ordered «items: name / join last:  and  / join: , ».

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

| Item   | Qty        | Price              | Total                    |
| ------ | ---------- | ------------------ | ------------------------ |
| Wireless Mouse | 1 | $24.99 | $24.99       |
| USB-C Hub | 1 | $39.99 | $39.99       |
| Mousepad | 2 | $11.26 | $22.52       |
|        |            | **Order total**    | $87.50 |

You ordered Wireless Mouse, USB-C Hub and Mousepad.

Shipping to Austin, TX.

Thanks for shopping with us!

---

<details>
<summary>Raw output</summary>

```markdown
Hi Priya,

Welcome back — thanks for being a member!

**Order #A-1042** — placed March 4, 2026

| Item   | Qty        | Price              | Total                    |
| ------ | ---------- | ------------------ | ------------------------ |
| Wireless Mouse | 1 | $24.99 | $24.99       |
| USB-C Hub | 1 | $39.99 | $39.99       |
| Mousepad | 2 | $11.26 | $22.52       |
|        |            | **Order total**    | $87.50 |

You ordered Wireless Mouse, USB-C Hub and Mousepad.

Shipping to Austin, TX.

Thanks for shopping with us!
```

</details>

---

One template covers both the member and first-time-buyer greeting (an `if` /
`else` block, inferred from `IsMember` being a boolean — no keyword needed),
repeats the order table's one row per item straight from a plain markdown table,
and turns the item list into a natural "A, B and C" sentence with the `join` /
`join last` filters. See [`docs/specs.md`](docs/specs.md) for the full language.

## Usage

```csharp
using Guillemets;

var template = Template.Create("Dear «full name»,");
var output = template.RenderObject(new { FullName = "Alice Smith" });
// => "Dear Alice Smith,"
```

`Render` also accepts `System.Text.Json.JsonElement`
(`template.Render(jsonElement)`) and `Newtonsoft.Json.Linq.JToken`;
`RenderObject` is the plain-C#-object overload, named apart because `object` is
too broad to overload `Render` on.

A `Template` is immutable and stateless once created — parse it once and
reuse the same instance for every `Render` call, including concurrently
across threads, rather than re-parsing per request.

### Custom filters

`Template.Create`'s optional `configure` callback exposes
`ParseOptions.Filters`: `Register(instance)` adds a filter alongside the
built-ins (`join`, `date`, `upper`, ...) or replaces one by name,
`Remove<TFilter>()` drops one. The template name is the class name minus its
`Filter` suffix, lowercased — `ReverseFilter` becomes `reverse`.

```csharp
using Guillemets.Filters;

public class ReverseFilter : IFilter
{
    public IEnumerable<string> Apply(IEnumerable<string> values, string? arg) =>
        values.Select(value => new string(value.Reverse().ToArray()));
}

var template = Template.Create(text,
    options => options.Filters.Register(new ReverseFilter())
);
// «text / reverse» reverses each value
```

The same `configure` callback also sets `options.Localizer`, an
`IStringLocalizer` that bridges a template's business vocabulary to model
property names when they don't already match — see
[`docs/specs.md`](docs/specs.md)'s Glossary & Localization section.

## Documentation

- [`docs/cheatsheet.md`](docs/cheatsheet.md) — every feature, one example each.
- [`docs/specs.md`](docs/specs.md) — full syntax reference and behavior spec.
- [`docs/implementations/dotnet.md`](docs/implementations/dotnet.md) — this
  .NET implementation's own behavior, including its runtime-specific filters.
- [`docs/architecture.md`](docs/architecture.md) — how the engine is built.
- [`docs/symbols.md`](docs/symbols.md) — the symbols the tokenizer recognizes.

## License

[MIT](LICENSE)
