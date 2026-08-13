# .NET Implementation

[`specs.md`](../specs.md) is the language-level spec — it defines the filter
*mechanism* (`name: value` syntax, chaining, defaults) and guarantees
`join`/`join last`, but deliberately says nothing about which other filters
exist or exactly how each one formats its output, since a filter is a thin
wrapper around whatever formatting/parsing primitives the host runtime
provides. This document covers that part for Guillemets' .NET implementation.
A Guillemets implementation on a different runtime would document its own
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
