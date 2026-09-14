# Table

## Table block

««full items

| Description   | Quantity          | Unit Price   | Total      |
| ------------- | ----------------- | ------------ | ---------- |
| «description» | «quantity» «unit» | «unit price» | «total»    |
|               |                   | **Subtotal** | «subtotal» |

»»

## Multi footer rows

««totals items

| Description     | Total              |
|-----------------|--------------------|
| «description»   | «total»            |
| **Subtotal**    | «running subtotal» |
| **Tax**         | «tax»              |
| **Grand Total** | «grand total»      |

»»

## No footer rows

««items

| Description   | Total   |
|---------------|---------|
| «description» | «total» |

»»

## Below minimum rows

««minimal items

| Description   |
| «description» |

»»

## Footer scope fallback

««company

Company: «name»

«««items

| Description   | Total               |
|---------------|---------------------|
| «description» | «total»             |
| **Subtotal**  | «fallback subtotal» |

»»»

»»

## Mismatched columns

««single item

| Description   | Quantity | Total |
|---------------|----------|-------|
| «description» | «total»

»»

## Line before close becomes footer row

««join items

| Description   | Total   |
|---------------|---------|
| «description» | «total» |
join

»»

## Filter footer glued to close

««join items

| Description   | Total   |
|---------------|---------|
| «description» | «total» |

join»»

## Scalar list table

««tags

| Tag    |
|--------|
| «this» |

»»
