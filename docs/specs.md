# Templating

A logicless, markdown-aware template engine for non-technical authors.
Syntax is minimal, language-neutral, and keyboard-friendly.

This document uses MUST and SHOULD in the RFC 2119 sense. MUST marks a rule
the engine enforces — the parser throws `TemplateParseException` if it's
broken. SHOULD marks a convention this document recommends, which the parser
does not enforce.

---

## Delimiters

`«»` (guillemets) are the only delimiter characters. They never appear in
standard markdown, so they are unambiguous in any template context.

Multi-guillemet depth (`««`, `«««`, ...) exists for readability at nesting
levels. The engine accepts any consistent depth — the author chooses based on
surrounding context.

---

## Escaping

Only a character that starts an interpretation needs an escape — `\` has no
general "make whatever follows literal" meaning. It only does something
when immediately followed by one of a small, fixed set of symbols;
everywhere else, `\` is just a literal backslash and whatever follows it is
read completely normally.

`\«`, `\»`, and `\\` are recognized in ordinary template text. Every `«`
unconditionally tries to open a token or block, so a literal one always
needs escaping; a literal `»` only ever needs it inside a block's body,
where an unescaped `»»` would close the block early — outside any open
block, `»` was already just text. `\\` is a literal backslash.

```markdown
Use \« and \» to show guillemets literally, like this: \«full name\».
→ Use « and » to show guillemets literally, like this: «full name».
```

Inside a filter's value specifically (see Filters, below), three more
sequences are recognized: `\|` for a literal `|` (a bare ` | ` would
otherwise end the value and start the next pipeline stage), and `\n`/`\t`
for an actual newline/tab character — the only way to put one in a value,
since it's otherwise confined to a single line. None of the three mean
anything outside a filter's value — `\n` there is just the two characters
`\` and `n`.

There's no `\:` — a filter clause only ever looks for the *first* `: `, so
nothing after it is re-scanned for another one.

---

## Schema & Localization

Template authors write variable names as natural, space-separated words —
whatever terms make sense to them. Developers name the underlying model in
English, using standard code naming conventions. A schema bridges the two,
since the author's business vocabulary won't always match the developer's
code vocabulary.

### Template

```markdown
«quote no»
«full name»
«company: name»
```

### Model (C#)

```csharp
model.OfferNo
model.FullName
model.Company.Name
```

### Schema mapping

```markdown
Quote No  = quote no  = OfferNo
Full Name = full name = FullName
Company   = company   = Company
Name      = name      = Name
```

A template's space-separated words are matched, case-insensitively, against
the default language's localized terms in the schema. Whatever property name
a matched term maps to is what gets resolved.

## Variables

A single-line or multi-line token that resolves to a scalar value.

```markdown
«full name»
```

```markdown
«full
name»
```

resolves identically to `«full name»`.

### Nested Property Access

`:` is the property accessor. It drills into objects and, when it lands on a
list, applies a projection (equivalent to `.Select()`). Chaining across lists
uses `.SelectMany()` internally, so the result stays flat.

`: ` (colon immediately followed by exactly one space) MUST be written
together — `company: name`, not `company:name`. A colon with no following
space isn't recognized as the property accessor at all.

```
«company: name»
«quotes: prices: amount»
«quotes: prices: amount: dollar price»
```

## Blocks

A block opens with `««name` on its own line and closes with `»»` on its own
line. The double guillemet marks it as a block, not an inline variable — an
inline variable always uses a single `«»`, even across multiple lines (see
Variables, above).

The closing depth MUST match the opening depth exactly. Deeper depths
(`«««`/`»»»`, and so on) behave identically; they only exist to make nested
blocks easier to read.

Behavior is inferred from the resolved type of `name`:

| Resolved type | Behavior         |
| ---           | ---              |
| boolean       | conditional (if) |
| list          | loop             |
| object        | scope            |

No keyword is required. The same syntax covers all three cases.

```markdown
««individual
Dear «full name»,
»»

««quote items
**«description»**

«quantity» «unit» × «unit price» = «total»
»»

««company
Tax No: «tax no»
»»
```

When a variable doesn't exist in the current scope, the engine looks in the
enclosing scopes.

```markdown
Quote No: «quote no»

««company
«company name» has been given this quote number «quote no», valid for 1
month.
»»
```

### Resolving the Block Name

`name` is a property chain, resolved the same way as an inline variable (see
Nested Property Access, above) — including projection over lists.

If the chain doesn't resolve to anything at all (for example, it projects
through an empty list), the block is treated as falsy, the same as an
explicit `false`. This is not an error.

If the chain's last segment is a boolean property projected through a list,
the block filters the list down to the item(s) where that property is true
and scopes into the match, instead of collapsing the projected booleans into
a single truthy/falsy check:

```markdown
««items: active
Dear «full name»,
»»
```

Given `items` is a list of objects each with `active` and `full name`, this
finds the item where `active` is true and renders the body scoped to it —
`full name` resolves against that matched item, not the outer scope.

### Else

`~` on its own line inside a block separates the truthy and falsy branches.
It's used with boolean blocks and variable definitions.

```markdown
««individual
Dear «full name»,
~
Dear representatives of «company name»,
»»
```

Else also works when an object is null.

```markdown
««company info
Company name: «name»
~
No company information available
»»
```

### Magic Loop Variables

The following variables are injected automatically inside every loop block:

| Variable | Meaning              |
| ---      | ---                  |
| `«first»`| true on first item   |
| `«last»` | true on last item    |

### Negation

`!` prefix negates any boolean variable:

```markdown
«!last»    → true when not last item
«!first»   → true when not first item
```

In a longer property chain, only the final segment can be negated:

```markdown
«company: !active»
```

Negating an earlier segment (for example, `company: !active: something`) is
not supported.

## Variable Definitions

A block can capture its rendered output in a named variable instead of
rendering it inline. Add `= expression` after the variable name, where
`expression` is a property chain resolved the same way as a block header
(see Blocks, above) — boolean → if/else, list → loop, object → scope.

```markdown
««contact person = individual
«full name»
~
representatives of «company name»
»»
```

The defined variable is then available as a plain variable anywhere below
its definition:

```markdown
Dear «contact person»,

This quote has been prepared for «contact person».
```

If a defined variable's name matches an existing property in the current
scope, the variable wins — a reference to that name resolves to what was
defined, not the scope property it shadows.

> [!TIP]
>
> Inline ifs are not supported, use variable definitions instead.

## Tables

When a loop block's body is a markdown table, only the third row repeats —
the first two rows (heading and separator) render once, and any rows after
the third render once as a footer.

```markdown
««items
| Description   | Quantity          | Unit Price            | Total         |
| ------------- | ----------------- | --------------------- | ------------- |
| «description» | «quantity» «unit» | «unit price»          | «total»       |
|               |                   | **Subtotal**          | «subtotal»    |
|               |                   | **Tax (%«tax rate»)** | «tax»         |
|               |                   | **Grand Total**       | «grand total» |
»»
```

A body with fewer than three rows isn't treated as a table — it renders as a
normal repeating block instead.

## Inline Lists

A variable that resolves to a list of scalars is automatically joined with
`, ` (comma space) when used inline:

```markdown
Tags: «tags»
→ Tags: philosophy, wisdom, ancient-greek
```

### Inline List with Field Selection

When list items are objects, use `:` to project a field:

```markdown
«price quotes: amount»
«quotes: prices: amount: dollar price»
```

At each step, `:` either projects/flattens a list or accesses an object's
property, depending on what it encounters.

Override the default `, ` join with the `join`/`join last` filters — see
Filters, below.

## Filters

`name: value` attaches a filter to a property chain, chained with ` | `:

```markdown
«date | date: dd/MM/yyyy»
«amount | currency: $»
«description | length: 80»
«list: name | join: , »
```

`: ` (colon immediately followed by exactly one space) MUST be written
together, same as property access above — it marks where a filter's value
starts. Whatever follows, up to the next ` | ` or the end of the token, is
the value exactly as written — nothing is trimmed automatically. Use `\`
(see Escaping, above) for a value that needs to contain `|` or `»`
literally, or an actual newline/tab (`\n`/`\t`) — the last two are only
recognized inside a filter's value.

A filter's value is optional — write the bare name, with no `: value` at
all, to use its default. `join`'s default is `, ` when used inline, and a
newline when used as a block footer (see Block Footer, below) — a bare
`join` in a footer is a natural fit for joining loop output that already
looks like separate lines, e.g. a list of `- «name»` rows.

A fixed set of built-in filters is supported:

| Filter      | Value                                             |
| ---         | ---                                                |
| `date`      | a date/time format string, e.g. `dd/MM/yyyy`       |
| `currency`  | a currency symbol prefix, e.g. `$`                 |
| `length`    | a maximum character length to truncate to          |
| `join`      | the string used to join the whole list into one    |
| `join last` | the string used to join just the list's last pair  |

Filters chain into a pipeline, applied left to right. A filter that acts on
a single value (`date`, `currency`, `length`) maps over every item when its
input is still a list; `join`/`join last` act on the whole list at once and
produce a single string.

`join last` merges the last two items of the current list into one, joined
by its value; fewer than two items is a no-op. `join` collapses the entire
current list into a single string, joined by its value; zero or one items is
a no-op. Order matters — they're genuinely sequential stages, not a paired
configuration:

```markdown
«quote: tags | join last:  and  | join: , »
→ philosophy, wisdom and ancient-greek
```

The default auto-join (`, `, see Inline Lists, above) still applies if the
pipeline ends without fully collapsing the list to a string, so `join last`
alone is enough for the common "A, B and C" case.

### Block Footer

The same pipeline attaches to a block's last line, right before its closing
`»»`, applying to the block's own accumulated output instead of a property
chain:

```markdown
««tags = quote: tags
«name»
join: , »»
```

renders as a comma-separated list when used via `«tags»`. The pipeline MUST
be the only thing on that line — nothing else may share it, before or
after. When the block has an else branch, it goes on the last line of
whichever branch renders last: the truthy body if there is no `~`, the falsy
body if there is one. `~` itself always stays on its own line and is never
adjacent to it.

## Full Example — Customer Quote

Field names below mix casing (`Quote No`, `description`) to show that
resolution is case-insensitive — the same property resolves however the
author capitalizes it in the template.

```markdown
# Quote #«Quote No»

««Contact Person = individual
«Full Name»
~
representatives of «Company Name»
»»

**Customer:** «Contact Person»
**Date:** «Date»
**Valid Until:** «Valid Until»

---

Dear «Contact Person»,

We are pleased to present this quote for the requested services. Our team
will deliver high-quality work within the agreed timeline and aim to ensure
your satisfaction at every step.

## Items

««items
| Description   | Quantity          | Unit Price            | Total         |
| ------------- | ----------------- | --------------------- | ------------- |
| «Description» | «Quantity» «Unit» | «Unit Price»          | «Total»       |
|               |                   | **Subtotal**          | «Subtotal»    |
|               |                   | **Tax (%«Tax Rate»)** | «Tax»         |
|               |                   | **Grand Total**       | «Grand Total» |
»»

---

We look forward to working with you. This quote is valid until
«valid until». Please don't hesitate to contact us with any questions.

*«Company» — «Date | date: dd/MM/yyyy»*
```
