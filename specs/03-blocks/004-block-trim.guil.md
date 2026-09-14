# Block Trim

## None

Tags:

««company

- «name»

»»

Done.

## Before

Tags:

««~company

- «name»

»»

Done.

## After

Tags:

««company

- «name»

~»»

Done.

## Both

Tags:

««~company

- «name»

~»»

Done.

## Nested, both trimmed

Both:

««~company

«name»

«««~address

(«city»)

~»»»

~»»

Done.

## Nested, inner trimmed

Inner:

««company

«name»

«««~address

(«city»)

~»»»

»»

Done.

## Nested, outer trimmed

Outer:

««~company

«name»

«««address

(«city»)

»»»

~»»

Done.

## Shared blank line

Start:

««one

one

~»»

««~two

two

»»

End.

## Keeps extra blank lines

Tags:


««~company

- «name»

~»»


Done.

## Flush inside block

Flush:

««outer

«««~inner

inner text

»»»

»»

End.

## At max guillemet depth

«««««««~company

- «name»

~»»»»»»»
