# Boolean Block

## Boolean no else

Before.

««individual

Dear «full name»,

»»

Between.

««corporate

Dear «full name»,

»»

After.

## Else

««individual

Individual: «full name»

~

Corporate: «company name»

»»

««corporate

Individual: «full name»

~

Corporate: «company name»

»»

## Non-boolean string

««company name

Company: «company name»

~

No company.

»»

««other company

Company: «other company»

~

No company.

»»

## Empty string

««tagline

Tagline: «tagline»

~

No tagline.

»»

««other tagline

Tagline: «other tagline»

~

No tagline.

»»

## Zero number

««quantity

Quantity: «quantity»

~

No quantity.

»»

««other quantity

Quantity: «other quantity»

~

No quantity.

»»

## Null object else

««company info

Company name: «name»

~

No company information available

»»

««other company info

Company name: «name»

~

No company information available

»»

## Missing property

««missing

Has missing.

~

No missing.

»»

««present

Has present.

~

No present.

»»

## Unresolved property no else

««nothing here

Dear «full name»,

»»

## Tilde in text is literal

««individual

~~something~~
prefix ~~old~~
~~multi
line~~

»»

## Nested, both truthy

««outer

before-inner

«««inner

Dear «name»,

~

no name given

»»»

after-inner

~

outer falsy

»»

## Nested, inner falsy

««outer

before-inner

«««inner two

Dear «name»,

~

no name given

»»»

after-inner

~

outer falsy

»»

## Nested, outer falsy

««outer three

before-inner

«««inner

Dear «name»,

»»»

after-inner

~

outer falsy

»»
