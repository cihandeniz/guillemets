# Loop Block

## Items list

Items:

««items

- «description»: «quantity» «unit» × «unit price» = «total»

»»

## Items list, empty

Items:

««other items

- «description»

»»

## Empty list else

««other items

- «description»

~

No items.

»»

## Doubly flattened header

««quotes: prices

- «amount»

»»

## Block footer

««tags

«name»

join: ~ / upper »»

## Block footer with trim

Trimmed:

««tags

«name»

join: , ~»»

Done.

Escaped:

««tags

«name»

join: \~»»

Done.

Both:

««tags

«name»

join: \~~»»

Done.
