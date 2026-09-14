# Blockquote

## Loop

> ««items
>
> - «name»
>
> »»

## Multi paragraph item

> ««items
>
> Name: «name»
>
> Second paragraph.
>
> »»

## Nested block

> ««companies
>
> «company name»
>
> «««address
>
> - «city»
>
> »»»
>
> »»

## Unquoted block with quoted body

««items

> - «name»

»»

## Table

> ««table items
>
> | Description   | Total   |
> |---------------|---------|
> | «description» | «total» |
>
> »»

## Table with footer

> ««table items
>
> | Description   | Total      |
> |---------------|------------|
> | «description» | «total»    |
> | **Subtotal**  | «subtotal» |
>
> »»

## Table in a nested blockquote

> > ««table items
> >
> > | Description   | Total   |
> > |---------------|---------|
> > | «description» | «total» |
> >
> > »»

## Block footer

> ««tags
>
> - «name»
>
> join»»

## Multi paragraph item in a nested blockquote

> > ««items
> >
> > Name: «name»
> >
> > Second paragraph.
> >
> > »»
