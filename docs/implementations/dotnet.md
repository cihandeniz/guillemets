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

`date` parses the value with `DateTime.Parse` (invariant culture — that's how
every built-in data source represents a stored value, see `PocoDataSource`/
`JsonElementDataSource`/`JTokenDataSource`) and formats it back out using .NET
custom date-and-time format strings, e.g. `dd/MM/yyyy`, against the ambient
culture (`CultureInfo.CurrentCulture`) at render time. A rendered document
reflects whoever's generating it, not a hardcoded locale.

```markdown
«date / date: dd/MM/yyyy»
→ 04/03/2026
```

> [!NOTE]
>
> A custom format string's literal-looking separators aren't literal — `/`
> means "the culture's date separator," not a slash. Under `tr-TR`, whose date
> separator is `.`, `dd/MM/yyyy` renders `11.07.2026`, not `11/07/2026`. This
> is .NET's own custom-format-string behavior, not something this filter adds
> — escape a separator (`\/`) if a literal slash is genuinely intended
> regardless of culture.
>
> That `\/` is a .NET format-string escape, evaluated by `DateTime.ToString()`
> itself, and it collides with Guillemets' own `\/` (see Escaping in
> [`specs.md`](../specs.md)): Guillemets unescapes every `\/` in a filter's
> value before the value ever reaches the filter, so `dd\/MM\/yyyy` arrives at
> `DateTime.ToString()` as `dd/MM/yyyy` — .NET never sees the backslashes, and
> the date separator substitutes as normal. There's currently no way to ask
> for a literal `/` inside a `date` format string specifically; only Guillemets'
> own delimiter-escaping meaning of `\/` is honored.

## Currency

`currency` parses the value with `decimal.Parse` (invariant culture, same
reasoning as `date` above) and formats it against the ambient culture's own
currency convention — symbol, symbol placement, and default decimal count all
come from the culture, not a hardcoded prefix. With no argument, it uses
.NET's standard `"C"` format:

```markdown
«amount / currency»
→ $1,234.50         (en-US)
→ 1.234,50 €         (de-DE)
→ ₺1.234,50          (tr-TR)
```

An argument overrides the decimal count while keeping the culture's own
symbol and placement — `C0`/`C3` for zero/three decimal places, say:

```markdown
«amount / currency: C0»
→ $1,235             (en-US)
→ 1.235 €             (de-DE)
```

> [!IMPORTANT]
>
> The argument is a .NET format string, the same as `date`/`number` — it is
> **not** a currency symbol prefix. `currency: $` is invalid; `$` isn't a
> recognized custom numeric format specifier, so .NET treats it as a literal,
> dropping the number entirely (`ToString("$", ...)` → `"$"`). Let the
> culture supply the symbol; use the argument only to override decimal count
> (`C0`, `C3`, ...).

> [!NOTE]
>
> Standard `"C"` also picks the culture's own symbol *placement*, not just
> its symbol — trailing (`1.234,50 €`) for `de-DE`, leading (`$1,234.50`) for
> `en-US`. That's .NET's own `"C"` behavior, not something this filter adds
> or can turn off.

A host that always bills in one currency regardless of who's reading the
document — everything is Turkish Lira, say, even when rendered for an
English-speaking reader — can fix the *symbol* while still letting the
ambient culture drive digit grouping and decimal separators, by registering
its own `CurrencyFilter` instance:

```csharp
options => options.Filters.Register(new CurrencyFilter("TL"))
```

```markdown
«amount / currency»
→ TL1,234.50         (en-US ambient — grouping/decimals still en-US)
→ 1.234,50 TL         (de-DE ambient — grouping/decimals still de-DE)
```

`FilterRegistry.Register` takes an instance; re-registering an existing name
replaces it (there's no separate "edit" — the default `currency` above is
what's being replaced).
`FilterRegistry.Remove<TFilter>()` drops a filter entirely, built-in or
custom, making it unavailable (`«x / truncate»` then fails to parse with
"Unknown filter 'truncate'"). Every built-in filter class is public for
exactly this reason — any of them can be targeted by `Register`/`Remove`,
not just `CurrencyFilter`/`TruncateFilter`.

> [!NOTE]
>
> A brand-new custom `IFilter`'s template name is derived from its class
> name: a trailing `Filter` suffix is stripped if present, and the rest is
> lower-cased/word-split the same way property names are (`Bold` → `bold`,
> `SmartQuotes` → `smart quotes`). A class name that doesn't end in `Filter`
> just uses its full name as-is — it isn't an error, and nothing gets
> mis-sliced.

## Number

`number` parses the value with `decimal.Parse` (invariant culture, same
reasoning as `date` above) and formats it back out against the ambient
culture using a .NET standard or custom numeric format string given as its
argument, e.g. `N2` — the same primitive `currency` wraps, minus the fixed
`"N2"` and the prefix.

```markdown
«amount / number: N2»
→ 1,234.50
```

With no argument, it uses .NET's default general format — under `en-US`, no
thousands separator and no fixed decimal places; under a culture with a
different decimal separator (`tr-TR`'s `,`, say), that separator instead.

```markdown
«amount / number»
→ 1234.5
```

## Truncate

`truncate` counts UTF-16 characters (not words, not grapheme clusters) and
appends `…` once the value exceeds the length given as its argument.

```markdown
«description / truncate: 10»
→ A wireless…
```

> [!NOTE]
>
> Counting UTF-16 characters, not grapheme clusters, can still land the cut
> point in the middle of a multi-char emoji or other symbol outside the Basic
> Multilingual Plane. `truncate` backs the cut point off by one in that
> specific case so it never splits a surrogate pair in half — it does not
> attempt full grapheme-cluster awareness (combining marks, ZWJ sequences,
> and the like) beyond that.

A missing or non-numeric argument (`truncate` with no value, or `truncate:
abc`) is a render-time `TemplateParseException` pointing at the filter's own
position, not an unwrapped `.NET` exception — every filter gets this for
free from the same choke point (`FilterNode.Apply`), not just `truncate`.

## Join

Collapses a list via plain string concatenation (`string.Join`) — no
locale-aware list formatting beyond what the template itself writes.

```markdown
«tags / join: , »
→ red, green, blue
```

See [`specs.md`](../specs.md)'s Filters section for the full behavior contract.

## Join Last

Same underlying `string.Join` as `join`, applied to just the last two items.

```markdown
«tags / join last:  and  / join: , »
→ red, green and blue
```

See [`specs.md`](../specs.md)'s Filters section for the full behavior contract.

## Upper

`string.ToUpper(CultureInfo.CurrentCulture)` — casing follows the host's ambient
culture, e.g. Turkish `tr-TR` maps `i` to `İ` (not `I`) under this filter, same
as it would for any other culture-aware casing in a .NET host.

```markdown
«name / upper»
→ ADA LOVELACE
```

## Lower

`string.ToLower(CultureInfo.CurrentCulture)`, the same ambient-culture reasoning
as `Upper`, above (Turkish `tr-TR` maps `I` to `ı`, not `i`).

```markdown
«name / lower»
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
