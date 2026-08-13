# .NET Implementation

[`specs.md`](../specs.md) is the language-level spec — it defines the filter
*mechanism* (`name: value` syntax, chaining, defaults) and which filters are
guaranteed at the language level (see its Filters section for the current
list), but deliberately says nothing about which other filters exist or
exactly how each one formats its output, since a filter is a thin wrapper
around whatever formatting/parsing primitives the host runtime provides.
This document covers that part for Guillemets' .NET implementation. A
Guillemets implementation on a different runtime would document its own
such filters in its own equivalent file, not by editing
[`specs.md`](../specs.md) or this file.

## Date

`date` parses the value with `DateTime.Parse` (invariant culture) and formats
it back out using .NET custom date-and-time format strings, e.g. `dd/MM/yyyy`.

## Currency

`currency` parses the value with `decimal.Parse` (invariant culture) and
formats it with `"N2"` (two decimal places, invariant culture separators),
prefixing the filter's argument directly — no locale-aware symbol placement.

## Truncate

`truncate` counts UTF-16 characters (not words, not grapheme clusters) and
appends `…` once the value exceeds the length given as its argument.

## Join

Collapses a list via plain string concatenation (`string.Join`) — no
locale-aware list formatting beyond what the template itself writes. See
[`specs.md`](../specs.md)'s Filters section for the full behavior contract.

## Join Last

Same underlying `string.Join` as `join`, applied to just the last two items.
See [`specs.md`](../specs.md)'s Filters section for the full behavior contract.

## Upper

`string.ToUpper(CultureInfo.CurrentCulture)` — casing follows the host's
ambient culture, e.g. Turkish `tr-TR` maps `i` to `İ` (not `I`) under this
filter, same as it would for any other culture-aware casing in a .NET host.

## Lower

`string.ToLower(CultureInfo.CurrentCulture)`, the same ambient-culture
reasoning as `Upper`, above (Turkish `tr-TR` maps `I` to `ı`, not `i`).

Because both depend on `CurrentCulture`, their `/specs/08-filters` fixtures
(and `05-variable-definitions/006`) only pass under whatever ambient
culture the test process runs with — true today since CI defaults to
invariant/en-US and the fixture text has no culture-sensitive casing under
that, but would break under a different default (e.g. `tr-TR`, per the
Turkish mapping above). `FilterCultureTests.cs`'s
`Upper_filter_respects_ambient_culture`/`Lower_filter_respects_ambient_culture`
exercise that divergence directly under `[SetCulture("tr-TR")]`.

## Glossary & Localization

A glossary is supplied at parse time, via `Template.Create`'s `configure`
callback setting `ParseOptions.Glossary` (see `README.md`'s Custom filters
section for the same callback's other use), as an `IStringLocalizer`
(`Microsoft.Extensions.Localization.Abstractions`) — the same abstraction
ASP.NET Core apps already use for culture-aware resource lookup, so a
glossary can plug into whatever localization provider (`.resx`, a database,
a translation service) the host application already has, rather than the
engine inventing its own format.

The mapping direction is inverted from `IStringLocalizer`'s usual
key-to-display-string use: the resource *key* is the property name, and its
*value* is the localized term a template author types for it. Resolution
enumerates `IStringLocalizer.GetAllStrings()` and matches a template
segment against each entry's `Value`, case-insensitively, taking the
matching entry's `Name` as the property name. See
[`specs.md`](../specs.md)'s Glossary & Localization section for the full
matching/fallback behavior contract — a term with no entry falls back to
direct (PascalCase-of-space-words) resolution, so a glossary that's silent
on a given term and no glossary at all behave identically for that term.

Because that lookup happens during `Render`, not `Create`, the same parsed
`Template` re-resolves against whatever culture is ambient
(`CultureInfo.CurrentUICulture`) on the calling thread at each render
call — a `null`/absent glossary, or one with no entry for a given culture,
just falls back to direct resolution for that render. `Render` itself takes
no separate culture parameter; the ambient culture is the only input,
consistent with how `IStringLocalizer` already resolves elsewhere in a
.NET host.
