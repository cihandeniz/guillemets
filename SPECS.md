# Templating

A logicless, markdown-aware template engine designed for non-technical users.
Syntax is minimal, language-neutral, and keyboard-friendly.

---

## Delimiters

`«»` (guillemets) are the sole delimiter characters. They never appear in
standard markdown, making them unambiguous in any template context.

On Turkish keyboard: `AltGr+Z` = `«`, `AltGr+X` = `»`.

Multi-quote depth (`««`, `«««`, ...) is used for readability at nesting levels.
The engine accepts any consistent depth — the author chooses based on
surrounding context.

```markdown
«valid
until»
```

resolves identically to `«valid until»`.

---

## Schema & Localization

Template authors write variable names using natural, space-separated words —
whatever terms make sense to them. Developers define the model in English
using standard naming conventions. A schema bridges the two, since the
author's business vocabulary won't always match the developer's code
vocabulary.

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

Space-separated words in templates map to PascalCase/camelCase in the model.
They use localization values (case insensitively) of the default language to
resolve back to property names.

## Variables

Single-line or multi-line token resolving to a scalar value.

```markdown
«full name»
```

### Nested Property Access

`:` is the property accessor. It drills into objects and, when encountered on a
list, applies a projection (equivalent to `.Select()`). Chaining across lists
uses `.SelectMany()` internally to keep the result flat.

Always write a space after `:` (`company: name`, not `company:name`) —
whitespace normalizes away during parsing either way, so this is a house style
for readability, not a parser requirement. Follow it in every fixture and
example.

```
«company: name»
«quotes: prices: amount»
«quotes: prices: amount: dollar price»
```

## Blocks

A block opens with `««name` on its own line and closes with `»»` on its own
line. The double guillemet is what marks this as a block rather than an
inline variable (see Variables, above, which always use a single `«»`, even
when its content spans multiple lines) — the closing depth must match the
opening depth. Deeper consistent depths (`«««`/`»»»`, ...) are available
purely for nesting readability when a block contains another block, and
behave identically to `««`/`»»`. Behavior is inferred from the resolved type
of `name`:

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

When a variable does not exist in existing scope, it looks for upper scopes.

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

If the chain does not resolve to anything at all (e.g. it projects through an
empty list), the block is treated as falsy, same as an explicit `false` —
this is not an error.

If the chain's last segment is a boolean property projected through a list,
the block filters the list down to the item(s) where that property is true
and scopes into the match, rather than collapsing the projected booleans into
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

`--` on its own line inside a block separates the truthy and falsy branches.
Used with boolean blocks and variable definitions.

```markdown
««individual
Dear «full name»,
--
Dear representatives of «company name»,
»»
```

Else works also when a given object is null.

```markdown
««company info
Company name: «name»
--
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

## Variable Definitions

A block can capture its rendered output into a named variable instead of
rendering inline. Use `= condition` after the variable name.

```markdown
««contact person = individual
«full name»
--
representatives of «company name»
»»
```

The defined variable is then available as a plain variable anywhere below its
definition:

```markdown
Dear «contact person»,

This quote has been prepared for «contact person».
```

The right-hand side of `=` follows the same context-aware block rules: boolean →
if/else, list → loop, object → scope.

> [!TIP]
>
> Inline ifs are not supported, use variable definitions instead.

### Tables

A block can be supports beginning and ending `|` to fit into a table without
breaking markdown table syntax.

```markdown
| Description   | Quantity          | Unit Price            | Total         |
| ------------- | ----------------- | --------------------- | ------------- |
| ««items       |                   |                       |               |
| «description» | «quantity» «unit» | «unit price»          | «total»       |
| »»            |                   |                       |               |
|               |                   | **Subtotal**          | «subtotal»    |
|               |                   | **Tax (%«tax rate»)** | «tax»         |
|               |                   | **Grand Total**       | «grand total» |
```

## Inline Lists

A variable that resolves to a list of scalars is automatically joined with `, `
(comma space) when used inline:

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

The `:` chain resolves lists via projection and flattening, objects via property
access — whichever the engine encounters at each step.

### Custom Separator

Pass a separator using inner `()` as a named parameter:

```markdown
«quote: tags (separator = , )»
```

### Loop Block with Separator

Use the `(separator)` parameter on the last line of the block:

```markdown
««tags = quote: tags
«name»
(separator = , )»»
```

renders as a comma-separated list when used via `«tags»`.

## Parameters

Inner `(name = value)` syntax passes named parameters to the enclosing
expression. A fixed set of built-in parameters is supported:

```markdown
«date (format = DD/MM/YYYY)»
«amount (currency = $)»
«description (length = 80)»
«list: name (separator = , )»
```

Parameters are positional by name and resolved before the outer expression is
evaluated.

## Full Example — Customer Quote

```markdown
# Quote #«Quote No»

««Contact Person = individual
«Full Name»
--
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

| Description   | Quantity          | Unit Price            | Total         |
| ------------- | ----------------- | --------------------- | ------------- |
| ««items       |                   |                       |               |
| «Description» | «Quantity» «Unit» | «Unit Price»          | «Total»       |
| »»            |                   |                       |               |
|               |                   | **Subtotal**          | «Subtotal»    |
|               |                   | **Tax (%«Tax Rate»)** | «Tax»         |
|               |                   | **Grand Total**       | «Grand Total» |

---

We look forward to working with you. This quote is valid until
«valid until». Please don't hesitate to contact us with any questions.

*«Company» — «Date (format = DD/MM/YYYY)»*
```
