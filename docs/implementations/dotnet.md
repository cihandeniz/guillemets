# .NET Implementation

[`specs.md`](../specs.md) is the language-level spec — it defines the filter
*mechanism* (`name: value` syntax, chaining, defaults) and which filters are
guaranteed at the language level (see its Filters section for the current list).
It deliberately says nothing about which other filters exist or exactly how each
one formats its output, since a filter is a thin wrapper around whatever
formatting/parsing primitives the host runtime provides.

This document covers that part for Guillemets' .NET implementation. A Guillemets
implementation on a different runtime would document its own such filters in its
own equivalent file, not by editing [`specs.md`](../specs.md) or this file.

## Date

`date` parses the value with `DateTime.Parse` (invariant culture) and formats it
back out using .NET custom date-and-time format strings, e.g. `dd/MM/yyyy`.

```markdown
«date | date: dd/MM/yyyy»
→ 04/03/2026
```

## Currency

`currency` parses the value with `decimal.Parse` (invariant culture) and formats
it with `"N2"` (two decimal places, invariant culture separators), prefixing the
filter's argument directly — no locale-aware symbol placement.

```markdown
«amount | currency: $»
→ $1,234.50
```

> [!NOTE]
>
> `"N2"` includes a thousands separator by default (`1,234.50`, not `1234.50`) —
> that's .NET's own `"N2"` behavior, not something this filter adds or can turn
> off.

## Truncate

`truncate` counts UTF-16 characters (not words, not grapheme clusters) and
appends `…` once the value exceeds the length given as its argument.

```markdown
«description | truncate: 10»
→ A wireless…
```

## Join

Collapses a list via plain string concatenation (`string.Join`) — no
locale-aware list formatting beyond what the template itself writes.

```markdown
«tags | join: , »
→ red, green, blue
```

See [`specs.md`](../specs.md)'s Filters section for the full behavior contract.

## Join Last

Same underlying `string.Join` as `join`, applied to just the last two items.

```markdown
«tags | join last:  and  | join: , »
→ red, green and blue
```

See [`specs.md`](../specs.md)'s Filters section for the full behavior contract.

## Upper

`string.ToUpper(CultureInfo.CurrentCulture)` — casing follows the host's ambient
culture, e.g. Turkish `tr-TR` maps `i` to `İ` (not `I`) under this filter, same
as it would for any other culture-aware casing in a .NET host.

```markdown
«name | upper»
→ ADA LOVELACE
```

## Lower

`string.ToLower(CultureInfo.CurrentCulture)`, the same ambient-culture reasoning
as `Upper`, above (Turkish `tr-TR` maps `I` to `ı`, not `i`).

```markdown
«name | lower»
→ ada lovelace
```

> [!NOTE]
>
> Because both depend on `CurrentCulture`, their `/specs/08-filters` fixtures
> (and `05-variable-definitions/006`) only pass under whatever ambient culture
> the test process runs with. That's true today since CI defaults to
> invariant/en-US and the fixture text has no culture-sensitive casing under
> that, but it would break under a different default (e.g. `tr-TR`, per the
> Turkish mapping above). `FilterCultureTests.cs`'s
> `Upper_filter_respects_ambient_culture` test (and its `Lower` counterpart)
> exercises that divergence directly under `[SetCulture("tr-TR")]`.

## Glossary & Localization

A glossary lets a template author write in their own words while the model
stays in ordinary code-friendly names. Supply one via `ParseOptions.Localizer`
— an `IStringLocalizer` (`Microsoft.Extensions.Localization.Abstractions`),
the same abstraction ASP.NET Core already uses for resource lookup, so a
glossary can be backed by a `.resx` file, a database, or a translation
service:

```csharp
Template.Create(text, options => options.Localizer = myLocalizer);
```

A developer wires up each resource entry once; a translator only ever edits
its localized text afterward. Guillemets uses that pairing in reverse from
`IStringLocalizer`'s usual purpose: the entry's `Name` identifies the model
property, and its `Value` is what a template author actually types.

```
Name: FullName          Value: Tam Ad
```

```markdown
«tam ad»
→ resolves the model's FullName property
```

See [`specs.md`](../specs.md)'s Glossary & Localization section for the full
matching/fallback contract — in short, a term with no matching entry falls
back to direct resolution, so a glossary that's silent on a given term and no
glossary at all behave identically for that term.

Not every model is PascalCase, though. `ParseOptions.PropertyNameConversion`
(`Func<string, string>`) turns a glossary entry's `Name` — or, when nothing
matches, the template segment itself — into the actual property name. It
defaults to `Dehumanize()` (`full name` → `FullName`), so ordinary
PascalCase/camelCase models need no configuration. A `snake_case` model can
supply its own conversion instead, and have it apply everywhere a name gets
resolved, not just wherever the glossary happens to cover:

```csharp
options.PropertyNameConversion = key => string.Join("_", key.Split(' ').Select(w => w.ToLowerInvariant()));
// «full name» and a glossary entry named "Full Name" both resolve to full_name
```

> [!NOTE]
>
> Setting `PropertyNameConversion` replaces the default outright — it doesn't
> run on top of it. Pass the identity function (`key => key`) to use
> segments/resource keys exactly as written, with no normalization at all.

> [!NOTE]
>
> A glossary re-resolves on every `Render` call, against whatever culture is
> ambient on the calling thread — the same parsed `Template` can serve
> multiple cultures without being re-created. A missing glossary, or one with
> no entry for the current culture, just falls back to direct resolution for
> that render.
