# Templating

A markdown-aware template engine for non-technical authors. Syntax is minimal,
language-neutral, and keyboard-friendly.

This document uses MUST and SHOULD in the RFC 2119 sense. MUST marks a rule the
engine enforces — the parser throws `TemplateParseException` if it's broken.
SHOULD marks a convention this document recommends, which the parser does not
enforce.

---

## Delimiters

`«»` — guillemets, pronounced *ghee-uh-MAY* — are angle quotation marks
used for punctuation in French and several other languages. They're the
only delimiter characters this engine recognizes.

Multi-guillemet depth (`««`, `«««`, ...) exists for readability at nesting
levels. The engine accepts any consistent depth — the author chooses based on
surrounding context.

```markdown
««company
Tax No: «tax no»
«««quotes
Quote: «number»
»»»
»»
```

`company` opens at depth 2, and the nested `quotes` block opens one level deeper
at depth 3 purely so the two are easier to tell apart on the page — depth 2 all
the way down would behave identically.

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

> [!WARNING]
>
> A property chain MUST contain at least one segment. `«»` is a parse error,
> not a reference to the current scope — the same is true of a bare `.: ` or
> `..: ` navigator with no property chain after it (see Scope Navigation,
> below).

### Nested Property Access

`:` is the property accessor. It drills into objects and, when it lands on a
list, applies a projection (equivalent to `.Select()`). Chaining across lists
uses `.SelectMany()` internally, so the result stays flat.

```
«company: name»
«quotes: prices: amount»
«quotes: prices: amount: dollar price»
```

> [!IMPORTANT]
>
> `: ` (colon immediately followed by exactly one space) MUST be written
> together — `company: name`, not `company:name`. A colon with no following
> space isn't recognized as the property accessor at all; it renders as literal
> text instead of drilling into `company`.

Each segment matches the underlying property case-insensitively, regardless of
the model's own naming convention — `«full name»` resolves `FullName`,
`fullName`, and `full_name` identically. This holds for every built-in data
source (POCOs, `System.Text.Json`, Newtonsoft `JToken`); a third-party
`IDataSource` SHOULD do the same for `TryGetProperty` to behave consistently
with the rest of the engine.

## Blocks

A block opens with `««name` on its own line and closes with `»»` on its own
line. The double guillemet marks it as a block, not an inline variable — an
inline variable always uses a single `«»`, even across multiple lines (see
Variables, above).

> [!IMPORTANT]
>
> "On its own line" is enforced on both sides of the closing `»»` — nothing
> else may share that line, before or after it. Only a newline or the end of
> the template may follow; a template that ends right after `»»`, with no
> trailing newline at all, closes normally.

The closing depth MUST match the opening depth exactly. Deeper depths
(`«««`/`»»»`, and so on) behave identically; they only exist to make nested
blocks easier to read.

Behavior is inferred from the resolved type of `name`:

| Resolved type    | Behavior                                                |
| ---------------- | ------------------------------------------------------- |
| boolean          | conditional (if)                                        |
| list             | loop                                                    |
| object           | scope                                                   |
| string, number   | conditional (if) — truthy whenever the value is present |
| null, unresolved | conditional (if) — always falsy                         |

No keyword is required. The same syntax covers all cases.

> [!NOTE]
>
> For a string or number, truthiness is about *presence*, not content —
> `""` and `0` are truthy, the same as any other value. Only `null` and an
> unresolved chain are falsy. Use a filter or explicit comparison in the data
> layer if you need "is this blank/zero" instead of "is this present".

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

`««name` in a block is a property chain, resolved the same way as an inline
variable (see Nested Property Access, above) — including projection over lists.

```markdown
««quote: company
Tax No: «tax no»
»»
```

Above example passes `company` value of `quote` property to the block body. When
the chain projects through two list levels (e.g. `quotes: prices`, where each
quote has its own list of prices), a loop block flattens them into one combined
loop over every price, the same way chaining across lists already flattens for
an inline variable.

If the chain doesn't resolve to anything at all — whether because it projects
through an empty list, or because the named property doesn't exist anywhere in
the data at all — the block is treated as falsy, the same as an explicit
`false`. This is not an error.

### Else

`~` on its own line inside a block separates the truthy and falsy branches. It's
used with boolean blocks and variable definitions.

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

Else works the same way for a loop block whose list is empty — whether that's
because the list itself has zero items, or because "Filtering Out Items in
Lists" (below) filtered every item out:

```markdown
««items
- «description»
~
No items.
»»
```

### Magic Loop Variables

The following variables are injected automatically inside every loop block:

| Variable | Meaning              |
| ---      | ---                  |
| `«first»`| true on first item   |
| `«last»` | true on last item    |

```markdown
««items
«first»: «name»
»»
```

Given three items named `A`, `B`, `C`, this renders `true: A`, then `false: B`,
then `false: C` — only the first row's magic variable is `true`.

`first`/`last` always take precedence over an item property of the same name —
if a loop item's own data has a `first` or `last` field, that field becomes
unreachable via `«first»`/`«last»` inside that loop.

Inside a nested loop, `«first»`/`«last»` always refer to the *innermost* loop's
position — the same shadowing rule as any other name lookup falling back to an
enclosing scope (see Blocks, above), except `first`/`last` are always defined
the moment you're inside any loop, so they never fall back to an outer loop.
There's no *automatic* fallback to an outer loop's `first`/`last` — reaching one
deliberately requires explicit scope navigation (`..: `, see Scope Navigation,
below).

### Filtering Out Items in Lists

If the chain's last segment is a boolean property projected through a list,
resolving the chain filters the list down to the item(s) where that property is
true, instead of collapsing the projected booleans into a single truthy/falsy
check. This holds everywhere a property chain resolves, not just in a block
header:

```markdown
««items: active
Dear «full name»,
»»
```

Given `items` is a list of objects each with `active` and `full name`, the block
filters the list down to the item(s) where `active` is true and scopes into the
match — `full name` resolves against that matched item, not the outer scope.

Used inline (`«items: active»`), the same filtering happens, but there's no body
to scope into — each matched item's own display representation is used directly,
auto-joined like any other list (see Inline Lists, above). This is rarely useful
on its own, since a plain boolean field carries no display text of its own.

### Negation

`!` prefix negates the truthiness of any variable (see the type table under
Blocks, above, for what counts as truthy per resolved type):

```markdown
«!last»          → true when not last item
«!first»         → true when not first item
«!company name»  → true when company name is null or unresolved
```

> [!WARNING]
>
> A negated segment MUST be the last one in its property chain:
>
> ```markdown
> «company: !active»
> ```
>
> Negating an earlier segment (for example, `company: !active: something`) is
> invalid.

## Scope Navigation

Resolving a property chain (see Nested Property Access, above) normally searches
the current scope first, then falls back through each enclosing scope in turn
(see Blocks, above) — but only when the name isn't found locally. A property
that already exists in the current scope shadows same-named properties further
out. Inside a loop, the magic `«first»`/ `«last»` variables always win over an
item property of the same name too (see Magic Loop Variables, above).

`.: ` and `..: ` are two markers, written at the very start of a property chain,
that override this default and pin resolution to an exact scope instead.

`.: ` and `..: ` (dot(s), colon, exactly one space) follow the same fixed-token
rule as `: ` (see Nested Property Access, above) — written without the trailing
space, neither is recognized as a navigator at all.

### This Scope Only

`.: name` resolves `name` against the current scope's own data only — no falling
back to an enclosing scope, no magic-var shadowing, and no shadowing by a
defined variable (see Variable Definitions, below) of the same name either, so
`.: first`/`.: last` reach the current scope's own `first`/`last` property even
where the magic `«first»`/`«last»` would otherwise shadow it:

```markdown
««items
«first»    → the magic variable
«.: first» → the item's own "first" property, ignoring the magic variable
»»
```

If the current scope has no such property at all, the chain resolves to nothing
— the same as any other unresolved chain (see Resolving the Block Name, above).

### Climbing to a Parent Scope

`..: name` starts resolution one scope higher than usual — at the enclosing
scope rather than the current one — then applies the normal fallback/shadowing
rules again from there, including a further fallback beyond it if `name` isn't
found at that level either. Repeating the marker climbs one further level per
repetition, so `..: ..: name` climbs two levels before resolving `name`.

```markdown
««quotes
Quote: «name»
««items
Item: «name», quote: «..: name»
»»
»»
```

Given each item has its own `name` as well as the enclosing quote, `«name»`
inside the items loop resolves to the item's own name (it shadows the quote's),
while `«..: name»` climbs past that shadow to reach the quote's.

Climbing past the outermost scope isn't a parse error — there's simply nothing
there, so the chain resolves to nothing, the same as any other chain that can't
find its property (see Resolving the Block Name, above). Drilling into a `null`
object already works the same way (see Nested Property Access, above); climbing
past the outermost scope is that same rule applied to scopes instead of
properties, the same short-circuiting a null-conditional operator (`?.` in C#)
gives a chain of member accesses once one link is null. A chain can carry as
many `..: ` markers as the author writes, regardless of how many scopes actually
enclose it in the template — there's no engine-enforced cap.

### Combining Both

`..: ` and `.: ` compose: zero or more `..: ` climbs, followed by at most one
`.: `, then the property chain itself. The `.: ` applies at whichever scope the
climbs land on, pinning resolution to exactly that scope — including skipping
*that* scope's own magic-var shadowing:

```markdown
«..: .: first»
```

climbs one level, then reads that parent scope's own `first` property, ignoring
the parent's own magic `«first»` too.

> [!WARNING]
>
> A `.: ` marker MUST be the last one before the property chain:
>
> ```markdown
> «.: ..: name»
> «.: .: name»
> ```
>
> Both are invalid — a `..: ` climb or another `.: ` appearing after
> `.: ` has already pinned the scope isn't allowed.

Negation and filters both apply to the chain as a whole, after scope navigation
has resolved it, exactly as they do without any navigator:

```markdown
«..: !active»
«..: name / upper»
```

## Variable Definitions

A block can capture its rendered output in a named variable instead of rendering
it inline. Add `= expression` after the variable name, where `expression` is a
property chain resolved the same way as a block header (see Blocks, above) —
boolean → if/else, list → loop, object → scope.

```markdown
««contact person = individual
«full name»
~
representatives of «company name»
»»
```

The defined variable is then available as a plain variable anywhere below its
definition:

```markdown
Dear «contact person»,

This quote has been prepared for «contact person».
```

If a defined variable's name matches an existing property in the current scope,
the variable wins — a reference to that name resolves to what was defined, not
the scope property it shadows.

> [!TIP]
>
> Inline ifs are not supported, use variable definitions instead.

### Definition Scope

"Anywhere below its definition" is bounded by the nearest enclosing loop
iteration or object scope — the same boundary that governs regular property
fallback (see Blocks, above). A definition made inside a loop or object block
is visible for the rest of that same iteration/object's body, but doesn't
survive past the loop or object block closing:

```markdown
««items
««current = active
Yes
~
No
»»
«name»: «current»
»»
After loop: «current»
```

`current` is redefined fresh every iteration and is gone once the loop ends —
`After loop: «current»` resolves to nothing, regardless of what the last item's
value was.

A conditional (boolean) block is different: it doesn't open a new scope, so a
definition made inside one behaves exactly like a top-level definition — it
keeps leaking forward past the conditional's own closing `»»`, into whatever
scope was already active:

```markdown
««enabled
««greeting = enabled
Hi
~
Bye
»»
Message: «greeting»
»»
After flag: «greeting»
```

`After flag: «greeting»` still resolves to `Hi` — the conditional never
introduced a boundary for `greeting` to fall out of.

## Tables

When a loop block's body is a markdown table, only the third row repeats — the
first two rows (heading and separator) render once, and any rows after the third
render once as a footer.

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

> [!NOTE]
>
> A body with fewer than three rows isn't treated as a table — it
> renders as a normal repeating block instead. A one-row body (just
> `| «description» | «total» |`, no heading or divider) repeats that
> single row for every item, exactly like a non-table loop body would.

Column alignment across rows (matching `|` counts) is the author's
responsibility — the engine doesn't parse or validate table structure at
all, only which row repeats. A row with a different cell count than its
header still renders exactly as written, substituted and unmodified.

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

At each step, `:` either projects/flattens a list or accesses an object's
property, depending on what it encounters.

Override the default `, ` join with the `join`/`join last` filters — see
Filters, below.

## Filters

`name: value` attaches a filter to a property chain, chained with ` / `:

```markdown
«date / date: dd/MM/yyyy»
«amount / currency»
«description / truncate: 80»
«name / upper»
«list: name / join: , »
```

`: ` (colon immediately followed by exactly one space) MUST be written together,
same as property access above — it marks where a filter's value starts.

> [!NOTE]
>
> Whatever follows `: `, up to the next ` / ` or the end of the token, is the
> value exactly as written — nothing is trimmed automatically. `truncate: 80 `
> keeps its trailing space as part of the value. See Escaping, below, for how to
> fit a literal `/` or `»`, or an actual newline/tab, inside a value.

A filter's value is optional — write the bare name, with no `: value` at all, to
use its default; what that default resolves to, and whether a bare name is even
meaningful, is up to the filter itself.

Filters chain into a pipeline, applied left to right — each stage receives the
previous stage's output. A single-value filter maps over every item when its
input is still a list; a list-collapsing filter (like `join`) acts on the whole
list at once and produces a single string. Order matters for a pipeline mixing
both kinds — they're genuinely sequential stages, not a paired configuration.

A few filters are part of the language itself, not implementation-defined like
the formatting filters below — every implementation MUST provide them, with a
fixed contract that doesn't vary by runtime. Each gets its own subsection below
explaining why it belongs here rather than in a runtime's own filter catalog.

### Join

`join` collapses the entire current list into a single string, joined by its
value. Zero or one items is a no-op.

```markdown
«tags / join:  \/ »
→ philosophy / wisdom / ancient-greek
```

Its own default value (used when written bare, with no `: value`) is `, ` when
used inline, and a newline when used as a block footer (see Block Footer, below)
— a bare `join` in a footer is a natural fit for joining loop output that
already looks like separate lines, e.g. a list of `- «name»` rows. `join` is
guaranteed because Inline Lists (above) defines its default comma-join in terms
of it and `join last`.

### Join Last

`join last` merges the last two items of the current list into one, joined by
its value; fewer than two items is a no-op. Order matters when combined with
`join` — they're genuinely sequential stages, not a paired configuration:

```markdown
«quote: tags / join last:  and  / join: , »
→ philosophy, wisdom and ancient-greek
```

The default auto-join (`, `, see Inline Lists, above) still applies if the
pipeline ends without fully collapsing the list to a string, so `join last`
alone is enough for the common "A, B and C" case.

`join last`'s own bare-name default (used with no `: value` at all) is an empty
separator — the last two items merge with nothing between them. Unlike `join`,
there's no natural single default for `join last` across contexts, so write an
explicit value (e.g. `join last:  and `) rather than relying on the bare form.
Guaranteed alongside `join`, for the same reason — see Join, above.

### Upper

`upper` converts every value to uppercase, following whatever casing rules the
implementation's language/culture setting applies (see below). It takes no value
— write it bare, since anything after `: ` is ignored, the same as any filter
that has no use for its argument.

```markdown
«name / upper»
→ ADA LOVELACE
```

It's guaranteed because, unlike a date, currency, or truncate filter, it doesn't
parse the value through a host-specific primitive — it just transforms
characters. Exactly how casing behaves for a given language is still
implementation-defined (see below), but the filter itself is always available.

### Lower

`lower` converts every value to lowercase, the same shape as `upper` in every
other respect, guaranteed for the same reason:

```markdown
«name / lower»
→ ada lovelace
```

### Default

`default` substitutes its value for any value that would otherwise
render as empty — an unresolved chain (see Resolving the Block Name,
above) or a property whose own value is empty (an explicit null, or an
empty string). Applied per item when the input is still a list, the
same as `upper`/`lower`; a resolved, non-empty value passes through
unchanged.

```markdown
«nickname / default: N/A»
```

Given `nickname` is missing entirely, this renders `N/A`; given
`nickname` is `"Al"`, it renders `Al` unchanged. Guaranteed for the same
reason as `upper`/`lower` — it's a direct string substitution, not a
wrapper around a host-specific parsing/formatting primitive.

Other utility filters — formatting a date, a currency amount, truncating text,
and so on — are commonly provided but implementation-defined, not part of this
language-level spec. Each is a thin wrapper around whatever formatting/parsing
primitives the host runtime provides: a date filter around the runtime's own
date formatter, a currency filter around its number formatter, and so on. The
exact catalog and behavior necessarily vary by runtime, so every implementation
MUST document such filters separately rather than folding them in here. This
repository's .NET implementation documents its `date`, `currency`, `number`,
and `truncate` (plus any .NET-specific notes on `join`/`join last`/`upper`/
`lower`) in [`implementations/dotnet.md`](implementations/dotnet.md).

### Block Footer

The same pipeline attaches to a block's last line, right before its closing
`»»`, applying to the block's own accumulated output instead of a property
chain:

```markdown
««tags = quote: tags
«name»
join: , »»
```

renders as a comma-separated list when used via `«tags»`. The pipeline MUST be
the only thing on that line — nothing else may share it, before or after — and
MUST end right where the closing `»»` starts, with no line break between them. A
pipeline that isn't glued to the close this way isn't recognized as a footer at
all; it's ordinary literal body content instead.

When the block has an else branch, the footer goes on the last line of whichever
branch renders last: the truthy body if there is no `~`, the falsy body if there
is one. `~` itself always stays on its own line and is never adjacent to it.

An unescaped `»»` at the block's own depth always terminates the last filter's
value, even mid-value with no space before it — `join: , »»` isn't ambiguous,
the value is exactly `, `. This is the same closing-token rule that ends any
other block body (see Blocks, above), not something specific to filter values.

A table's own trailing footer rows (see Tables, below) are a different,
non-conflicting concept from this pipeline. The "glued to the close" rule above
keeps them from colliding in practice: a table row written on its own line, even
one that happens to look like a filter name, is just another literal row — the
pipeline only ever wins when it's written right up against `»»`.

In that glued form, a table always collapses to one rendered block of text, so
the pipeline applies to that whole rendered table as a single value, exactly
like it would for a conditional or scope block's single output. `join`/`join
last` are no-ops there (a single value has nothing to join), so they're harmless
if written out of habit. Any other filter (`truncate`, `date`, ...) would
reformat the entire rendered table text, which is never useful — don't attach a
filter pipeline to a table body.

## Full Example — Customer Quote

Field names below mix casing (`Quote No`, `description`) to show that resolution
is case-insensitive — the same property resolves however the author capitalizes
it in the template.

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

We are pleased to present this quote for the requested services. Our team will
deliver high-quality work within the agreed timeline and aim to ensure your
satisfaction at every step.

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

We look forward to working with you. This quote is valid until «valid until».
Please don't hesitate to contact us with any questions.

*«Company» — «Date»*
```

---

## Escaping

Only a character that starts an interpretation needs an escape — `\` has no
general "make whatever follows literal" meaning. It only does something when
immediately followed by one of a small, fixed set of symbols; everywhere else,
`\` is just a literal backslash and whatever follows it is read completely
normally.

`\«`, `\»`, `\~`, and `\\` are recognized in ordinary template text.

Every `«` unconditionally tries to open a token or block, so a literal one
always needs escaping. A literal `»` only ever needs it inside a block's body,
where an unescaped `»»` would close the block early — outside any open block,
`»` was already just text. A literal `~` only ever needs it on its own line
inside a block's body, where it would otherwise split the block into
truthy/falsy branches (see Else, above) — anywhere else, `~` was already just
text. `\\` is a literal backslash.

```markdown
Use \« and \» to show guillemets literally, like this: \«full name\».
→ Use « and » to show guillemets literally, like this: «full name».
```

Inside a filter's value specifically (see Filters, above), three more sequences
are recognized: `\/` for a literal `/` (a bare ` / ` would otherwise end the
value and start the next pipeline stage), and `\n`/`\t` for an actual
newline/tab character — the only way to put one in a value, since it's otherwise
confined to a single line. None of the three mean anything outside a filter's
value — `\n` there is just the two characters `\` and `n`.

```markdown
«names / join: \/»
→ Ada/Grace/Katherine
```

> [!NOTE]
>
> There's no `\:` — a filter clause only ever looks for the *first*
> `: `, so nothing after it is re-scanned for another one. Writing
> `truncate: 80: extra` doesn't need escaping to keep `: extra` as part
> of the value; it already is.

## Glossary & Localization

Template authors write variable names as natural, space-separated words —
whatever terms make sense to them. Developers name the underlying model in
English, using standard code naming conventions. Direct resolution (see Nested
Property Access, above) already bridges the two whenever the author's wording
and the developer's naming agree once matched case-insensitively. A glossary
exists for the terms where they don't.

### Template

```markdown
«quote no»
«full name»
«company: name»
```

### Model

```
OfferNo
FullName
Company.Name
```

### Glossary

```markdown
Quote No = OfferNo
```

A glossary is a table of rows, each mapping one localized term to the property
name it resolves to. Only `quote no` needs an entry above — `full name` and
`company: name` already reach `FullName`/`Company.Name` through direct
resolution, so listing them would be redundant.

A glossary's terms are scoped to a language. A template authored once may be
matched against different term tables depending on which language is active for
a given resolution. A business operating in Turkish and English might give the
same `OfferNo` property a Turkish term in one glossary and an English term in
another — either template author can write in their own vocabulary against the
same underlying model. What determines the active language, and how many
languages a glossary can hold at once, is host/runtime behavior, documented per
implementation rather than by this spec.

A template's space-separated words are matched, case-insensitively, against the
localized terms in the glossary.

> [!TIP]
>
> A glossary is additive, not exhaustive: it only needs to list the terms that
> actually diverge from their model's naming. A word with no matching entry
> falls back to direct resolution exactly as if no glossary were supplied at
> all, so a partial glossary and no glossary behave identically for every term
> it doesn't cover — there's no need to list `full name = FullName` just because
> `quote no = OfferNo` was needed elsewhere.

Each segment of a property chain (`company: name`) is resolved independently,
against direct resolution or the glossary in turn, so one glossary entry can
bridge a single segment of a chain without needing to cover the others.
