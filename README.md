# Guillemets

A logicless, markdown-aware template engine designed for non-technical users.
Syntax is minimal, language-neutral, and keyboard-friendly.

```markdown
Dear «full name»,

Your order **«order id»** is on its way to «company: name».
```

`«»` (guillemets) are the sole delimiter characters — they never collide with
standard markdown.

> [!NOTE]
>
> **Status: early development.** The engine currently resolves plain-text
> passthrough and scalar/nested/projected variable access (`«token»`, `«a: b»`,
> list projection & flattening). Blocks (conditionals, loops, scopes), variable
> definitions, tables, and parameters are specified in [SPECS.md](SPECS.md) but
> not yet implemented — see `/specs` for the full set of behavior fixtures
> driving the work.

## Syntax at a glance

- **Variables**: `«full name»` resolves against the data model, converting
  space-separated words to PascalCase (`FullName`).
- **Nested access**: `«company: name»` drills into objects; across a list it
  projects and flattens (`«quotes: prices: amount»`).
- **Blocks**: `««name` ... `»»` — behavior (if / loop / scope) is inferred
  from the resolved value's type, no keywords needed.
- **Inline lists**: a token resolving to a list of scalars auto-joins with
  `, `.

Full syntax reference: [SPECS.md](SPECS.md).

## Usage

```csharp
using System.Text.Json;
using Guillemets;

var data = JsonDocument.Parse("""{ "FullName": "Alice Smith" }""");
var output = TemplateEngine.Render("Dear «full name»,", data.RootElement);
// => "Dear Alice Smith,"
```

## Project layout

- `/src/Guillemets` — the class library.
- `/test/Guillemets.Tests` — NUnit test project.
- `/specs` — the fixture corpus (`.guil.md` template / `.json` data / `.md`
  expected output, or `.error` for expected parse failures) that serves as the
  acceptance contract for the engine.

## Development

```
dotnet test
```

Each fixture under `/specs` runs as a named test case. See
[CLAUDE.md](CLAUDE.md) for the coding conventions and implementation approach
used in this repo.

## License

[MIT](LICENSE)
