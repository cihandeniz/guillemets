# Item Filtering

## Filtered item scope

Before.

««items: active

Dear «full name»,

»»

After.

## Filtered item scope negated

««items: !active

Dear «full name»,

»»

## No match

Before.

««no match items: active

Dear «full name»,

»»

After.

## Sparse flag

««sparse items: active

Dear «full name»,

»»

## Nested chain

««quotes: prices: active

Amount: «amount»

»»
