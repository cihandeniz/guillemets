# Escaping

## Guillemets and backslash

Use \« and \» for literal guillemets, and \\ for a literal backslash.

## Unrecognized sequence stays literal

The path uses a literal \a sequence, unchanged.

## Control escapes outside a filter value

Line one \n line two, and a \t tab, both literal outside filters.

## Double backslash before a guillemet

Path: \\«name»

## Close inside a block

««individual

«full name» uses \» as a closing guillemet.

»»

## Close run inside a deeper block

«««outer

Some »» text.

»»»

## Escaped tilde

««flag

Line one.
\~
Line two.

»»
