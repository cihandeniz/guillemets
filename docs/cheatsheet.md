# Cheatsheet

Every feature, one example each. `→` separates a template from its output.
Full behaviour lives in [specs.md](specs.md); .NET-only filters in
[implementations/dotnet.md](implementations/dotnet.md).

## Markdown passes through

### Headings

```markdown
# Quote 1042

## Items
```

### Emphasis

```markdown
*italic* **bold** ~~struck~~ `code`
```

### Lists

```markdown
- one
- two
  - nested
```

### Ordered list

```markdown
1. first
2. second
```

### Task list

```markdown
- [x] shipped
- [ ] invoiced
```

### Table

```markdown
| Item | Qty |
| ---- | --- |
| Hub  | 2   |
```

### Blockquote

```markdown
> Terms apply.
```

### Code block

````markdown
```csharp
var x = 1;
```
````

### Link and image

```markdown
[docs](docs/specs.md) ![logo](logo.png)
```

### Horizontal rule

```markdown
Above

---

Below
```

### Comment

```markdown
<!-- internal note -->
Hello.
```

## Variables

### Variable

```markdown
Hi «first name»,
→
Hi Ada,
```

### Nested property

```markdown
Ships to «company: city».
→
Ships to Austin.
```

### List, auto-joined

```markdown
Tags: «tags»
→
Tags: philosophy, wisdom
```

### Field from each item

```markdown
Items: «items: name»
→
Items: Wireless Mouse, USB-C Hub
```

## Blocks

### If / else

```markdown
««is member

Welcome back!

~

Welcome!

»»
→
Welcome back!
```

### Loop

```markdown
««items

- «name»

»»
→
- Wireless Mouse
- USB-C Hub
```

### Scope

```markdown
««company

«street», «city»

»»
→
1 Main St, Austin
```

### Empty list fallback

```markdown
««items

- «name»

~

Nothing ordered.

»»
→
Nothing ordered.
```

### Current value

```markdown
««tags

- «this»

»»
→
- philosophy
- wisdom
```

### First and last item

```markdown
««items

«first»/«last»: «name»

»»
→
true/false: Wireless Mouse
false/true: USB-C Hub
```

### Negation

```markdown
««!is member

Become a member!

»»
→
Become a member!
```

### Keep only flagged items

```markdown
««items: active

- «name»

»»
→
- Hub
```

### Table from one row

```markdown
««items

| Item   | Qty   |
| ------ | ----- |
| «name» | «qty» |

»»
→
| Item   | Qty   |
| ------ | ----- |
| Wireless Mouse | 1 |
| USB-C Hub | 2 |
```

### Capture output as a name

```markdown
««delivery = company

«city»

»»

Ships to «delivery».
→
Ships to Austin.
```

## Filters

### Chain with ` / `

```markdown
«first name / upper»
→
ADA
```

### Custom separator

```markdown
Tags: «tags / join: ; »
→
Tags: a; b; c
```

### Natural sentence

```markdown
Tags: «tags / join last:  and  / join: , »
→
Tags: a, b and c
```

### Fallback value

```markdown
Hi «nickname / default: friend»,
→
Hi friend,
```

### Block footer

```markdown
««items

«name»

join last:  and  / join: , »»
→
Wireless Mouse and USB-C Hub
```

## Scope navigation

### Pin to current scope

```markdown
««items

«.: name»

»»
→
inner
```

### Reach the parent

```markdown
««items

«name» of «..: name»

»»
→
inner of outer
```

## Whitespace

### Blank lines are required

```markdown
««is member

Welcome.

»»
→
Welcome.
```

### Trim around a block

```markdown
Tags:

««~tags

- «this»

~»»

Done.
→
Tags:
- a
Done.
```

### Wrap a long reference

```markdown
Hi «first
name»,
→
Hi Ada,
```

## Blockquotes

### Block inside a blockquote

```markdown
> ««items
>
> - «name»
>
> »»
→
> - Wireless Mouse
> - USB-C Hub
```

### Nested depth

```markdown
> > ««tags
> >
> > - «this»
> >
> > »»
→
> > - a
> > - b
```

### Table inside a blockquote

```markdown
> ««items
>
> | Item   | Qty   |
> | ------ | ----- |
> | «name» | «qty» |
>
> »»
→
> | Item   | Qty   |
> | ------ | ----- |
> | Wireless Mouse | 1 |
> | USB-C Hub | 2 |
```

## Escaping

### Literal guillemets

```markdown
Use \«name\» for a variable.
→
Use «name» for a variable.
```

### Literal backslash and tilde

```markdown
50\~60 and a\\b
→
50~60 and a\b
```

## .NET filters

See [implementations/dotnet.md](implementations/dotnet.md) for arguments and
culture behaviour.

```markdown
«due date / date: dd/MM/yyyy»
«amount / currency»
«amount / number: N2»
«description / truncate: 10»
```
