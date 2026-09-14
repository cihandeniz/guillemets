# Templating

A markdown-aware template engine for non-technical authors. Syntax is
minimal and language-neutral, favoring readability over ease of typing —
see [`README.md`](../README.md) for why.

MUST and SHOULD are used in the RFC 2119 sense. MUST marks a rule the engine
enforces — the parser throws `TemplateParseException` if it's broken. SHOULD
marks a convention the parser does not enforce.

---

## Delimiters

`«»` — guillemets, pronounced *ghee-uh-MAY* — are the angle quotation marks used
for punctuation in French and several other languages. They are this engine's
only delimiters.

Everything else is markdown the engine never interprets — a table, a blockquote,
a list, a fence — reaching the output exactly as written, with only the `«...»`
inside it replaced:

```markdown
> Terms apply to «name».
> No refunds.
```

renders as

```markdown
> Terms apply to Alice.
> No refunds.
```

A `>` or a `|` takes on meaning only where a block's own lines are built around
it — see In a Blockquote under Blocks and As a Table under Loop Blocks, below.

Multi-guillemet depth (`««`, `«««`, ...) exists for readability at nesting
levels. The engine accepts any consistent depth.

```markdown
««company

Tax No: «tax no»

«««quotes

Quote: «number»

»»»

»»
```

`quotes` opens one level deeper than `company` purely so the two are easier to
tell apart on the page — depth 2 all the way down behaves identically.

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
> A property chain MUST contain at least one segment. `«»` is a parse error, not
> a reference to the current scope — the same is true of a bare `.: ` or `..: `
> navigator with no property chain after it (see Scope Navigation, below).

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

Each segment matches its property case-insensitively, whatever the model's own
naming convention — `«full name»` resolves `FullName`, `fullName` and
`full_name` identically. Every built-in data source does this, and a third-party
`IDataSource` SHOULD do the same in `TryGetProperty`.

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
> keeps its trailing space as part of the value. Symbols that carry meaning
> elsewhere in the language — `~`, `!`, `=`, `: `, `.: `, `..: ` — are plain
> text inside a value and need no escape. See Escaping, below, for how to fit a
> literal `/` or `»`, or an actual newline/tab, inside a value.

A filter's value is optional — a bare name uses the filter's own default, if it
has a meaningful one.

Filters chain into a pipeline, applied left to right — each stage receives the
previous stage's output. A single-value filter maps over every item when its
input is still a list; a list-collapsing filter (like `join`) acts on the whole
list at once and produces a single string. Order matters for a pipeline mixing
both kinds — they're genuinely sequential stages, not a paired configuration.

The filters below are part of the language itself: none of them wraps a
host-specific parsing or formatting primitive, so every implementation MUST
provide them with the same contract.

### Join

`join` collapses the entire current list into a single string, joined by its
value. Zero or one items is a no-op.

```markdown
«tags / join:  \/ »
→ philosophy / wisdom / ancient-greek
```

Its bare default is `, ` inline, and a newline as a block footer (see Block
Footer, below) — a fit for loop output that already looks like separate lines, a
list of `- «name»` rows say.

### Join Last

`join last` merges the last two items of the current list into one, joined by
its value; fewer than two items is a no-op. Order matters when combined with
`join`:

```markdown
«quote: tags / join last:  and  / join: , »
→ philosophy, wisdom and ancient-greek
```

The default auto-join (`, `, see Inline Lists, below) still applies if the
pipeline ends without fully collapsing the list to a string, so `join last`
alone is enough for the common "A, B and C" case.

Its bare default is an empty separator — the last two items merge with nothing
between them. There's no natural default here as there is for `join`, so write
the value explicitly (`join last:  and `).

### Upper

`upper` converts every value to uppercase, following whatever casing rules the
implementation's language/culture setting applies. It takes no value — write it
bare, since anything after `: ` is ignored.

```markdown
«name / upper»
→ ADA LOVELACE
```

How casing behaves for a given language stays implementation-defined, but the
filter itself is always available.

### Lower

`lower` converts every value to lowercase, identical to `upper` otherwise:

```markdown
«name / lower»
→ ada lovelace
```

### Default

`default` substitutes its value for any value that would otherwise render as
empty — an unresolved chain (see Resolving the Block Name, below) or a property
whose own value is an explicit null or an empty string. Applied per item when
the input is still a list, the same as `upper`/`lower`.

```markdown
«nickname / default: N/A»
```

Given `nickname` is missing entirely, this renders `N/A`; given `"Al"`, it
renders `Al` unchanged.

### Truncate

`truncate` shortens a value to the length given as its argument, appending `…`
when it had to cut. A value already within the limit passes through untouched.
Applied per item when the input is still a list, the same as `upper`/`lower`.

```markdown
«description / truncate: 10»
```

Given `description` is `Consulting services`, this renders `Consulting…`; given
`Alice`, it renders `Alice` unchanged.

> [!IMPORTANT]
>
> The argument MUST be a whole number, zero or greater. A missing, non-numeric,
> or negative argument is a parse error.

Length is counted in the runtime's own string units. An implementation SHOULD
back the cut off by one rather than split a character those units encode as a
pair; how much further it goes (combining marks, ZWJ sequences) belongs in its
own doc.

Other utility filters — formatting a date or a currency amount, say — wrap a
host primitive, so their catalog and behavior vary by runtime. Every
implementation MUST document its own separately rather than folding them in
here. This repository's .NET implementation covers `date`, `currency` and
`number`, plus any .NET-specific notes on the guaranteed filters, in
[`implementations/dotnet.md`](implementations/dotnet.md).

## Blocks

A block opens with `««name` on its own line and closes with `»»` on its own
line. The double guillemet is what marks it as a block — an inline variable
always uses a single `«»`, even across multiple lines (see Variables, above).

A blank line MUST surround `««name` and `»»` on every side; a markdown formatter
treats them as plain text and would otherwise merge a marker into an adjacent
paragraph and corrupt the template. Only the two *inside* the block are
swallowed — they're its body padding. The two *outside* are ordinary
surrounding text the block merely requires be there.

```markdown
Before.

««individual

Dear «full name»,

»»

After.
```

renders, given `{ "Individual": true, "FullName": "Alice Smith" }`, as

```markdown
Before.

Dear Alice Smith,

After.
```

> [!IMPORTANT]
>
> Missing any of the four is a hard parse error. Escape with `\«` for a
> literal `««...` that isn't meant to be a block.

> [!NOTE]
>
> `»»` must not share a line with preceding text — only a newline or end
> of template may follow it to count as a close. Anything else on that
> line makes it ordinary text, and the search for a real close continues.

The closing depth MUST match the opening depth exactly. Deeper depths behave
identically; they only make nested blocks easier to read.

> [!TIP]
>
> A run of the same guillemet may not exceed 7 deep. Depth is only ever
> compared within one block's own open/close pair, never to a sibling's or an
> ancestor's — so past a readable depth, reuse any depth rather than grow it.

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
> For a string or number, truthiness is *presence*, not content — `""` and `0`
> are truthy. Only `null` and an unresolved chain are falsy. For "is this
> blank/zero", compare in the data layer instead.

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

The body is scoped to `quote`'s `company`. When the chain projects through two
list levels (`quotes: prices`, where each quote has its own list of prices), a
loop block flattens them into one loop over every price, the same way chaining
across lists already flattens for an inline variable.

A chain that resolves to nothing — an empty projection, or a property that
exists nowhere in the data — makes the block falsy, the same as an explicit
`false`. This is not an error.

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
the only thing on that line and MUST end right where the closing `»»` starts,
with no line break between them. A pipeline not glued to the close this way is
ordinary literal body content, not a footer.

> [!NOTE]
>
> A new custom filter can retroactively change how an already-written template
> parses, if its name matches a glued last line:
>
> ```markdown
> ««notes
>
> Summary text.
>
> highlight»»
> ```
>
> A hard parse error with no `highlight` filter registered, different output
> the moment a host app registers one — even for an unrelated feature.
> Expected, not a bug; keep custom filter names distinctive.

When the block has an else branch, the footer goes on the last line of whichever
branch renders last: the truthy body if there is no `~`, the falsy body if there
is one. `~` itself always stays on its own line and is never adjacent to it.

An unescaped `»»` at the block's own depth always terminates the last filter's
value, even mid-value with no space before it — `join: , »»` gives exactly `, `.
This is the same closing-token rule that ends any other block body.

A table's own trailing footer rows (see As a Table, below) are a different,
non-conflicting concept. The "glued to the close" rule keeps them apart: a row
written on its own line is just another literal row, even one that looks like a
filter name.

Glued to a table, the pipeline sees the whole rendered table as one value.
`join`/`join last` are harmless no-ops there; anything else reformats the entire
table text, which is never useful — don't attach a pipeline to a table body.

### In a Blockquote

A block works inside a markdown blockquote — every line prefixed with `>`. The
rule is an equivalence: a quoted block renders exactly as the same block
unquoted would, with the quote marker kept on every line.

```markdown
> ««items
>
> - «name»
>
> »»
```

renders, given two items, as

```markdown
> - A
> - B
```

The block's opening, closing, `~` else and footer lines, and the blank lines
the syntax requires *inside* the block, all carry the marker. How that marker
is spelled and counted is covered under Quote Markers, below.

The blank line *before* the opening and *after* the closing sit outside the
block and may be at any depth, or be an ordinary empty line — which is what
makes a block the first thing in a blockquote work:

```markdown
Note:

> ««shown
>
> It is shown.
>
> »»
```

That line reaches the output as the template wrote it, so it is what separates
one quoted block from the next. An ordinary empty line between two of them
leaves two blockquotes; a `>` line there joins them into one.

```markdown
> ««items
>
> - «name»
>
> »»

> ««others
>
> - «name»
>
> »»
```

renders as two blockquotes, one per block.

A block whose own lines are unquoted may still have quoted *content* in its
body — the markers are then just literal text, and nothing above applies:

```markdown
««items

> - «name»

»»
```

A table loop body (see As a Table, below) works inside a blockquote too: every
row line carries the marker, the repeating row included, so the heading renders
once and each item becomes one more quoted row.

> [!NOTE]
>
> A block's closing must sit at the same depth as its opening; a mismatch is an
> error.

### Quote Markers

Inside a blockquote (see In a Blockquote, above) what counts is the **depth** —
how many `>` markers deep the line sits — not the exact spelling, so `>` and
`> ` are the same depth and mix freely within one block. That matters because
editors and formatters strip the trailing space, leaving blank quoted lines as a
bare `>`.

Everything after the marker run is content, preserved as written, so `>- «name»`
renders as `>- A`. A `>` that is not part of a line's leading run is ordinary
content too.

A *blank* line keeps the spelling the template gave it, minus any trailing
space: a `> >` blank line inside a depth-2 quote renders as `> >`, and a `>>`
one as `>>` — a blank line has no content for a trailing space to separate.

Blank lines the engine produces *within* a block's own output — between the
paragraphs of a multi-paragraph loop item (see Blank Lines in the Output,
below), or where a block that rendered nothing stood — carry the block's marker
too, copying the spelling of its opening line, so they read no differently from
one the author wrote. An empty line there would end the blockquote and split it
in two, which is why the depth has to match rather than merely being tolerated.
The blank line after a block's close is not one of these: it belongs to the
template and keeps whatever the template gave it (see In a Blockquote, above).

## Conditional Blocks

A block whose name resolves to a boolean is a conditional: its body renders once
when the value is true, and not at all when it's false. A string or number
behaves the same way on presence, and `null` or an unresolved chain is always
falsy (see the type table under Blocks, above).

### Else

`~` on its own line inside a block separates the truthy and falsy branches. Like
`««name` and `»»` (see Whitespace, below), a blank line MUST surround it on both
sides, swallowed the same way and for the same reason.

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

It works the same for an empty loop, whether the list has zero items or
Filtering Out Items in Lists (below) removed them all:

```markdown
««items

- «description»

~

No items.

»»
```

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

## Loop Blocks

A block whose name resolves to a list is a loop: its body renders once per item,
each item becoming the current scope for the names inside it.

### The Current Value

`«this»` renders the value the current scope sits on, whatever that value is.
It's injected automatically in every scope, not only inside a loop.

Its main use is a loop over a list of scalars, where an item has no property to
name:

```markdown
Tags:

««tags

- «this»

»»
```

Given `tags` is `["philosophy", "wisdom"]`, this renders a `- philosophy` line
and a `- wisdom` line. Without `«this»` such a list can only be rendered
inline, auto-joined onto one line (see Inline Lists, below).

`this` sits in a chain position like any other name, so it composes with
everything else: `«this / upper»` filters it, `«!this»` negates it, a table row
cell (`| «this» |`) renders it per item, `««this` opens a block on it, and
`«..: this»` reaches the enclosing scope's value (see Scope Navigation, below).

On an object, `«this»` renders that object's own display representation —
adapter-dependent and rarely useful, the same caveat as inline filtering (see
Filtering Out Items in Lists, below). Open a block to reach its fields instead.

`this` always takes precedence over a property of the same name, exactly as
`first`/`last` do — a `this` field in the data is unreachable via `«this»` and
needs `«.: this»` (see This Scope Only, below).

`this` also stands alone: it can't be followed by `: ` to drill further, since
`«this: name»` could only ever mean `«.: name»` — it's an error naming that
replacement. Pinning first keeps `this` an ordinary property name, so
`«.: this: name»` reads the data's own `this` field and drills into that.

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
position: they're defined the moment you're inside any loop, so they never fall
back to an outer one. Reaching an outer loop's takes explicit scope navigation
(`..: `, see Scope Navigation, below).

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

Used inline (`«items: active»`), the filtering is the same but there's no body
to scope into — each match's own display representation is auto-joined like any
other list (see Inline Lists, below). Rarely useful on its own, since a boolean
field carries no display text.

> [!NOTE]
>
> ```markdown
> «quotes: prices: active»
> ```
>
> The filtered list is whichever one the last segment is a direct boolean
> property of, not necessarily the chain's first segment — here, each quote's
> `prices`, flattened and scoped into the matched `price`, not `quote`. A price
> missing `active` (sparse JSON) is just falsy, not an error.

### As a Table

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
> A body with fewer than three rows isn't treated as a table — it renders as a
> normal repeating block instead. A one-row body (just `| «description» |
> «total» |`, no heading or divider) repeats that single row for every item,
> exactly like a non-table loop body would.

Column alignment across rows is the author's responsibility — the engine only
decides which row repeats, never parsing or validating table structure. A row
with a different cell count than its header still renders exactly as written.

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

A defined variable shadows a same-named property in the current scope.

> [!TIP]
>
> Inline ifs are not supported, use variable definitions instead.

### Definition Scope

"Anywhere below its definition" is bounded by the nearest enclosing loop
iteration or object scope — the same boundary that governs regular property
fallback (see Blocks, above). A definition inside one is visible for the rest of
that body, but doesn't survive its closing `»»`:

```markdown
««items

«««current = active

Yes

~

No

»»»

«name»: «current»

»»

After loop: «current»
```

`current` is redefined fresh every iteration and is gone once the loop ends —
`After loop: «current»` resolves to nothing, regardless of what the last item's
value was.

A conditional block is different: it opens no new scope, so a definition inside
one leaks forward past its closing `»»` like a top-level one, into whatever
scope was already active:

```markdown
««enabled

«««greeting = enabled

Hi

~

Bye

»»»

Message: «greeting»

»»

After flag: «greeting»
```

`After flag: «greeting»` still resolves to `Hi` — the conditional never
introduced a boundary for `greeting` to fall out of.

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
Filters, above.

## Scope Navigation

Resolving a property chain (see Nested Property Access, above) normally searches
the current scope first, then falls back through each enclosing scope in turn
(see Blocks, above) — so a property that already exists locally shadows
same-named ones further out. The magic `«this»`, and `«first»`/`«last»` inside a
loop, shadow a property of the same name too (see The Current Value and Magic
Loop Variables, above).

`.: ` and `..: ` are two markers, written at the very start of a property chain,
that override this default and pin resolution to an exact scope instead.

`.: ` and `..: ` (dot(s), colon, exactly one space) follow the same fixed-token
rule as `: ` (see Nested Property Access, above) — written without the trailing
space, neither is recognized as a navigator at all.

### This Scope Only

`.: name` resolves `name` against the current scope's own data only — no falling
back to an enclosing scope, no magic-var shadowing, and no shadowing by a
defined variable (see Variable Definitions, above) of the same name either, so
`.: first`/`.: last`/`.: this` reach the current scope's own `first`/`last`/
`this` property even where the magic `«first»`/`«last»`/`«this»` would otherwise
shadow it:

```markdown
««items

«first»    → the magic variable
«.: first» → the item's own "first" property, ignoring the magic variable
«.: this»  → the item's own "this" property, ignoring the magic variable

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
«««quotes

Quote: «name»

««««items

Item: «name», quote: «..: name»

»»»»

»»»
```

Given each item has its own `name` as well as the enclosing quote, `«name»`
inside the items loop resolves to the item's own name (it shadows the quote's),
while `«..: name»` climbs past that shadow to reach the quote's.

Climbing past the outermost scope isn't a parse error — there's simply nothing
there, so the chain resolves to nothing, the same as any other chain that can't
find its property (see Resolving the Block Name, above). It's the rule for
drilling into a `null` object (see Nested Property Access, above) applied to
scopes instead of properties, the same short-circuiting a null-conditional
operator (`?.` in C#) gives once one link is null. A chain may carry as many
`..: ` markers as the author writes, however many scopes actually enclose it —
there's no cap.

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
> Both are invalid — a `..: ` climb or another `.: ` appearing after `.: ` has
> already pinned the scope isn't allowed.

Negation and filters both apply to the chain as a whole, after scope navigation
has resolved it, exactly as they do without any navigator:

```markdown
«..: !active»
«..: name / upper»
```

The same holds for filtering out items in a list (see Filtering Out Items in
Lists, above) — a navigator only changes which scope the chain starts
resolving from, not whether list-filtering applies to it:

```markdown
«.: items: active»
«..: quotes: active»
```

## Whitespace

How a template's blank lines, hard wraps and line endings reach the output.
The block rules above are written against these.

### Sharing a Blank Line

Adjacent or nested blocks share one blank line at their boundary, not two; the
start or end of a template (or of an enclosing block) needs none.

```markdown
««individual

Dear «full name»,

»»

««company

«company name»

»»
```

One blank line separates the two blocks — it satisfies both the first block's
after-close and the second's before-open at once, not two in a row.

A footer line needs a blank line before it too, same as `»»` — only its own
gluing to `»»` is exempt. An empty body needs one blank line, not two.

### Blank Lines in the Output

Blank lines beyond the ones the syntax requires are the author's, and render as
written — however many there are, before a block, after it, or anywhere in its
body. A block that renders nothing leaves a single blank line where it stood,
so the text around it reads as two paragraphs.

```markdown
Done.

««show note

»»

Bye.
```

renders, given `{ "ShowNote": false }`, as

```markdown
Done.

Bye.
```

The one exception is the very end of the output, where a blank line has nothing
left to separate. A trailing run of them is trimmed back to a single newline.

A loop renders its items one after another. When every item is a single
paragraph they follow each other directly, so a body of `- «name»` gives a tight
markdown list. When any item spans two or more paragraphs, a blank line goes
between all of them instead. Trimming a nested block's blank lines (see below)
can change which of the two cases an item falls into.

```markdown
««items

- «name»

»»
```

renders, given two items, as

```markdown
- alpha
- beta
```

### Trimming Blank Lines Around a Block

A `~` written inside a block marker removes the blank line on that marker's
outer side. `««~` drops the blank line before the block's opening, `~»»` drops
the one after its closing. Each marker acts on its own side, so write both to
close the gap above and below, or one to close a single side.

```markdown
Tags:

««~tags

- «name»

~»»

Done.
```

renders, given two tags, as

```markdown
Tags:
- alpha
- beta
Done.
```

The blank lines stay REQUIRED in the template. `~` changes what is rendered,
never what may be written — a block carrying one is laid out exactly like any
other block.

Because the marker sits inside the guillemets, it never competes with the
negation, scope-navigation or footer slots: `««~!active`, `««~.: items` and
`join: , ~»»` all parse. A footer value that itself ends in `~` needs the `\~`
escape (see Escaping, below) so it isn't read as the marker — `join: \~~»»`
joins with a literal `~` and still trims. This is the same escaping a value
ending in `»` or containing `/` already needs.

> [!NOTE]
>
> `~` is also the else marker (see Else, above), but the two never collide. An
> else `~` stands alone on its line; a trim `~` is written flush against a
> guillemet run. Longest match decides, so `~»»` is always a trimmed close and
> a lone `~` is always an else.

`~` binds to the guillemet run, so depth costs nothing extra — `«««~` opens a
depth-3 block and `~»»»` closes one. The marker plays no part in depth
matching: a block opened with `«««~` closes with either `»»»` or `~»»»`.

Trimming composes with the rules above rather than overriding them. A trimmed
nested block no longer leaves a blank line inside the item that encloses it, so
that item may stop spanning paragraphs — which in turn decides whether the loop
separates its items:

```markdown
««tags

«name»

«««~featured

(featured)

~»»»

»»
```

renders, given a featured `alpha` and a plain `beta`, as

```markdown
alpha
(featured)
beta
```

Without the inner `~` markers the `alpha` item would span two paragraphs, and a
blank line would then separate every item.

Three rules settle the edges:

- A blank line that two trimmed markers share is removed once, not twice.
  Trimming never joins two lines into one.
- `~` removes only the single blank line the syntax requires. Blank lines
  beyond it are the author's and still render, as everywhere else.
- A marker with no blank line to remove — at the start or end of the template,
  or flush against an enclosing block's boundary — does nothing, and is not an
  error.

### Line Wrapping

A `«...»` may be hard-wrapped across lines — by an editor's fill command, a
formatter enforcing a column limit, or by hand. A single newline inside one
stands in for the space that `: `, `.: `, `..: ` and ` / ` require, and
separates words inside a property name, so a token means the same thing
wrapped as it does on one line.

```markdown
Shipping to «shipping address:
city». Shout «name /
upper». Total «order total
/ currency».
```

renders exactly as the same text unwrapped would.

> [!NOTE]
>
> Only a *single* newline does this. A blank line is a paragraph break, so a
> paragraph that happens to begin with `/ ` is ordinary text and never a
> filter stage. This is what keeps wrapping from reaching across the blank
> lines the block rules depend on.

```markdown
one

/ two
```

renders as written.

A block's opening is the one exception: it MUST stay on one line. `««quotes:`
with its `items` on the next line is an error, because that marker's own line
is what the blank-line rules above are written against.

Inside a blockquote a wrapped `«...»` carries the quote marker on each of its
continuation lines, and there it is still a marker — it is stripped before the
wrapped text is read, so the equivalence holds for a wrapped reference exactly
as it does for a block:

```markdown
> Company name: «company:
> name»
```

renders as

```markdown
> Company name: Acme
```

### Line Endings

A template's own line-ending style (LF or CRLF, detected once from the source)
governs the whole rendered output — line breaks embedded in resolved data, a
multi-line string value say, are normalized to match. Data never mixes styles
into the output.

## Escaping

Only a character that starts an interpretation needs an escape. `\` has no
general "make whatever follows literal" meaning — followed by anything outside a
small, fixed set of symbols, it is just a literal backslash and what follows it
is read normally.

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
> There's no `\:` — a filter clause only ever looks for the *first* `: `, so
> nothing after it is re-scanned for another one. Writing `truncate: 80: extra`
> doesn't need escaping to keep `: extra` as part of the value; it already is.

## Glossary & Localization

Template authors write variable names as natural, space-separated words;
developers name the underlying model in standard code conventions. Direct
resolution (see Nested Property Access, above) bridges the two wherever they
agree case-insensitively. A glossary covers the terms where they don't.

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

A glossary's terms are scoped to a language, so one template may be matched
against different term tables depending on which language is active. A business
operating in Turkish and English can give `OfferNo` a term in each, letting
either author write in their own vocabulary against the same model. What
determines the active language is host behavior, documented per implementation
rather than by this spec.

A template's space-separated words are matched, case-insensitively, against the
localized terms in the glossary.

> [!TIP]
>
> A glossary is additive, not exhaustive: it only needs the terms that actually
> diverge from their model's naming. A word with no matching entry falls back to
> direct resolution exactly as if no glossary were supplied — there's no need to
> list `full name = FullName` just because `quote no = OfferNo` was needed.

Each segment of a property chain (`company: name`) is resolved independently,
against direct resolution or the glossary in turn, so one glossary entry can
bridge a single segment of a chain without needing to cover the others.

## Comments

No dedicated comment syntax — a template is markdown, and markdown already has
one. An HTML comment isn't `«»` syntax, so the engine passes it through as
ordinary literal text; it renders exactly as written and disappears only once
that markdown is itself turned into HTML.

```markdown
<!-- reminder: confirm pricing before this goes out -->
Hello, «name»!
```

renders as

```markdown
<!-- reminder: confirm pricing before this goes out -->
Hello, Ada!
```

> [!TIP]
>
> The comment is still present in the rendered markdown — Guillemets never
> strips it. Only a downstream markdown-to-HTML renderer makes it invisible.

---

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
