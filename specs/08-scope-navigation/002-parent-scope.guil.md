# Parent Scope

## Climb to parent

««quotes

Quote: «name»

«««items

Item: «name», quote: «..: name»

»»»

»»

## Climb two levels

««company

Company: «name»

«««quotes

Quote: «name»

««««items

Item: «name», quote: «..: name», company: «..: ..: name»

»»»»

»»»

»»

## Climb past root resolves to nothing

Value: «..: name»

## Climb past available nesting resolves to nothing

««empty items

Item: «..: ..: name»

»»
