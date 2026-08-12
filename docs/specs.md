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

Write a space after `:` (`company: name`, not `company:name`). The parser
ignores whitespace around `:` either way, so this is a style convention,
SHOULD, not a requirement.

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

### Custom Separator

Pass a separator using inner `()` as a named filter:

```markdown
«quote: tags (separator = , )»
```

### Last Separator

Repeat the `separator` filter to give a different value to the join right
before the list's last item — the classic "A, B, and C" style:

```markdown
«quote: tags (separator = , )(separator = , and )»
→ philosophy, wisdom, and ancient-greek
```

The first `(separator = ...)` joins every item except the last pair; the
second one, if present, replaces just that last join. With two items, only
the second value is used; with one item, no separator is used at all.

### Loop Block with Separator

Use the `(separator)` filter on the last line of the block:

```markdown
««tags = quote: tags
«name»
(separator = , )»»
```

renders as a comma-separated list when used via `«tags»`. The filter MUST be
the only thing on that line, immediately before the block's closing `»»` —
nothing else may share the line, before or after it.

When the block has an else branch, the filter goes on the last line of
whichever branch renders last: the truthy body if there is no `~`, the falsy
body if there is one. `~` itself always stays on its own line and is never
adjacent to the filter.

## Filters

Inner `(name = value)` syntax passes a named filter to the enclosing
expression. A fixed set of built-in filters is supported:

| Filter      | Value                                        |
| ---         | ---                                           |
| `date`      | a date/time format string, e.g. `dd/MM/yyyy`  |
| `currency`  | a currency symbol prefix, e.g. `$`            |
| `length`    | a maximum character length to truncate to     |
| `separator` | the string used to join a list's items        |

```markdown
«date (date = dd/MM/yyyy)»
«amount (currency = $)»
«description (length = 80)»
«list: name (separator = , )»
```

Filters are matched by name and resolved before the outer expression is
evaluated. Repeating `separator` sets a different join before the list's
last item — see Last Separator, under Inline Lists, above.

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
