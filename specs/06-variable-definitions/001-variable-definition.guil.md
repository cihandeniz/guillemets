# Variable Definition

## From a boolean

««contact person = individual

«full name»

~

representatives of «company name»

»»

Dear «contact person»,

## From an object

««address = vendor

«street», «city»

»»

Delivery address: «address»

## From a list with a join footer

««tags = quote: tags

«name»

join: , »»

Tags: «tags»

## With an else branch

««other tags = empty quote: tags

«name»

~

No tags

join: , »»

Tags: «other tags»

## With a filter pipeline footer

««loud tags = quote: tags

«name»

upper / join: , »»

Tags: «loud tags»

## With a bare join footer

Tags:

««listed tags = quote: tags

- «name»

join»»

«listed tags»

## Shadows a property

««company = individual

«full name»

»»

Company: «company»
