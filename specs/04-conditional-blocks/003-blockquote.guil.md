# Blockquote

## Conditional

Note:

> ««shown
>
> It is shown.
>
> »»

## Else

> ««items
>
> - «name»
>
> ~
>
> No items.
>
> »»

> ««other items
>
> - «name»
>
> ~
>
> No items.
>
> »»

## Nested conditional

> ««shown
>
> Outer shown.
>
> «««inner shown
>
> Inner shown.
>
> ~
>
> Inner hidden.
>
> »»»
>
> «««inner hidden
>
> Inner shown.
>
> ~
>
> Inner hidden.
>
> »»»
>
> »»

## Conditional in a nested blockquote

> Outer.
>
> > ««shown
> >
> > It is shown.
> >
> > ~
> >
> > It is hidden.
> >
> > »»
> >
> > ««hidden
> >
> > It is shown.
> >
> > ~
> >
> > It is hidden.
> >
> > »»

## Renders nothing

> Done.
>
> ««show note
>
> hidden
>
> »»
>
> Bye.
