# Magic Loop Variables

## First

««items

«««first

First up: «description»

~

«description»

»»»

»»

## Not first

««items

«««!first

---

»»»

«description»

»»

## Property collision

««collision items

«first»: «name»

»»

## Nested loop first and last

««quotes

Quote:

«««items

«first»«last»: «name»

»»»

»»

## First and last after filter

««filtered items: active

«first»«last»: «name»

»»

## Through a nested scope

««companies

«««address

«company name»: «city»

««««first

> (head office)

»»»»

»»»

»»
