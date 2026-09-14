# Composing Navigators

## Combine climb and pin

««quotes

«first» / «.: first»

«««items

«..: first» / «..: .: first»

»»»

»»

## Negation with a navigator

««company

«««quotes

Quote «number»: «..: !active»

»»»

»»

## Filter with a navigator

««company

«««quotes

Quote «number»: «..: name / upper»

»»»

»»

## Navigator as a block header

««region

«««company

Own items:

««««items

- «name»

»»»»

Region items:

««««..: items

- «name»

»»»»

»»»

»»

## This as a block header and parent

««groups

«««this

- «this» of «..: this»

»»»

»»

## In a filter value

«missing / default: .: x ..: y»

## Line wrap

««wrap quotes

Quote: «name»

«««items

«first» / «.:
first», quote «..:
name»

»»»

»»

## Line wrap in a blockquote

> ««quoted items
>
> - «name» of «..:
> owner»
>
> »»
