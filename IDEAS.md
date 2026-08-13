# Ideas

Speculative, non-committed notes — things worth remembering, not things
anyone's decided to build. Unlike `PLAN.md` (actionable, shrinks to
empty), this file only grows and only loses an entry once it's either
promoted into `PLAN.md` as a real milestone or deliberately rejected.

## Compile-time source generation instead of runtime optimization

A Roslyn *incremental source generator* that reads a `.guil.md` template
(via `AdditionalFiles`, paired with its target type through MSBuild item
metadata — e.g. `<AdditionalFiles Include="email.guil.md"
GuillemetsModel="MyApp.OrderEmail" />`, read back via
`AnalyzerConfigOptions`) and emits a plain C# method that renders it
directly against that type — no `IDataSource`, no `Scope` walk, no
runtime interpretation at all.

Why this fits guillemets specifically: block behavior (if/loop/scope) is
inferred from a property's *resolved type*, which today only exists at
runtime because `IDataSource` doesn't reveal its shape until asked. A
known C# type (or a JSON Schema, which is a class model in a different
serialization) supplies that shape at generation time instead, so the
generator can emit a plain `if`/`foreach`/null-check rather than
resolving `DataKind` at render time. Everything else in the `Ast` is
already fully resolved at parse time (table layout, `ClimbLevels`/
`ThisScopeOnly`, negation, filter pipeline shape) and needs nothing from
the schema — a codegen backend would just be a second way of walking the
same `IRenderable` tree (emit C# syntax instead of interpreting against
`Scope`).

Two real costs, not details to wave away:

- **Glossary/culture.** `Template.Create`'s parsed tree deliberately
  doesn't commit to a culture today — the same `Template` re-resolves
  the glossary against whatever `CultureInfo.CurrentUICulture` is
  ambient at each `Render` call (see `docs/implementations/dotnet.md`).
  Baking property names into generated C# means picking a glossary and
  culture at generation time — either generate once per supported
  culture, or keep a small runtime glossary-lookup step in the
  generated code for that one piece.
- **JSON Schema needs its own type-mapping layer.** A C# type already
  carries property names, CLR types, nullability, and collection
  element types for free (reflection, or a Roslyn `INamedTypeSymbol`).
  A JSON Schema doesn't — mapping it to something the generator can
  walk (types, `$ref`, nullable-via-union-type, ...) is a whole
  subsystem on its own, not something the `Ast` buys for free.

Shape, if pursued: its own package (`Guillemets.SourceGeneration` or
similar), architecturally separate from the interpreter rather than
folded into the base API. Roughly as much implementation work as the
current engine, not a small addition.
