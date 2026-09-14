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

## Dynamic columns

««quarterly items

| Item   | «quarters» |
| ------ | ---------- |
| «name» | «amounts»  |

»»

## Literal, single value and list columns

««report rows

| Name    | «quarters» | Note   | «period» | «regions» |
| ------- | :--------: | ------ | -------- | --------: |
| «label» | «amounts»  | «note» | «span»   | «shares»  |

»»

## Cells not matching columns

««uneven rows

| Item   | «quarters» |
| ------ | ---------- |
| «name» | «amounts»  |

»»

## Joining a cell list explicitly

««tag rows

| Item   | Tags                |
| ------ | ------------------- |
| «name» | «labels / join: , » |

»»

## Empty column list

««empty column rows

| Item   | «empty quarters» |
| ------ | ---------------- |
| «name» | «amounts»        |

»»

## Row without a closing pipe

««open rows

| Item   | «quarters»
| ------ | ----------
| «name» | «amounts»

»»

## Bare join in a cell

««join column rows

| Item   | «quarters»       |
| ------ | ---------------- |
| «name» | «amounts / join» |

»»
