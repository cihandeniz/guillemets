# This Scope

## Reaches own property over a magic variable

««items

«first» / «.: first»

»»

## Skips the fallback

Quote No: «quote no»

««other items

«description» — quote no: «quote no», own quote no: «.: quote no»

»»

## Skips a defined variable

««quote no = quote

«number»

»»

Quote no (defined): «quote no»
Quote no (own): «.: quote no»

## Pinned this then chain

Inner: «.: this: name»

## Pinned before item filtering

««.: active items: active

Dear «full name»,

»»

## This shadows an item property

««shadow items

- magic: `«this»`, property: «.: this»

»»
