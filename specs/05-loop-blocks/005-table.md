# Table

## Table block

| Description   | Quantity          | Unit Price   | Total      |
| ------------- | ----------------- | ------------ | ---------- |
| Consulting | 2 day | 1500 | 3000    |
| Setup | 1 unit | 500 | 500    |
|               |                   | **Subtotal** | 3500 |

## Multi footer rows

| Description     | Total              |
|-----------------|--------------------|
| A   | 10            |
| B   | 20            |
| **Subtotal**    | 30 |
| **Tax**         | 5              |
| **Grand Total** | 35      |

## No footer rows

| Description   | Total   |
|---------------|---------|
| A | 10 |
| B | 20 |

## Below minimum rows

| Description   |
| A |
| Description   |
| B |

## Footer scope fallback

Company: Acme

| Description   | Total               |
|---------------|---------------------|
| A | 10             |
| B | 20             |
| **Subtotal**  | 30 |

## Mismatched columns

| Description   | Quantity | Total |
|---------------|----------|-------|
| Consulting | 3000

## Line before close becomes footer row

| Description   | Total   |
|---------------|---------|
| Consulting | 3000 |
| Setup | 500 |
join

## Filter footer glued to close

| Description   | Total   |
|---------------|---------|
| Consulting | 3000 |
| Setup | 500 |

## Scalar list table

| Tag    |
|--------|
| philosophy |
| wisdom |

## Dynamic columns

| Item   | Q1 | Q2 | Q3 |
| ------ | ---------- | ---------- | ---------- |
| A | 1 | 2 | 3  |
| B | 4 | 5 | 6  |

## Literal, single value and list columns

| Name    | Q1 | Q2 | Q3 | Note   | 2026 | EU | US | APAC |
| ------- | :--------: | :--------: | :--------: | ------ | -------- | --------: | --------: | --------: |
| A | 1 | 2 | 3  | ok | H1   | 10 | 20 | 30  |

## Cells not matching columns

| Item   | Q1 | Q2 | Q3 |
| ------ | ---------- | ---------- | ---------- |
| A | 1 | 2  |
| B | 3 | 4 | 5 | 6  |

## Joining a cell list explicitly

| Item   | Tags                |
| ------ | ------------------- |
| A | x, y |
| B | z |

## Empty column list

| Item   |  |
| ------ | ---------------- |
| A |         |

## Row without a closing pipe

| Item   | Q1 | Q2 | Q3
| ------ | ----------
| A | 1 | 2 | 3

## Bare join in a cell

| Item   | Q1 | Q2 | Q3       |
| ------ | ---------------- | ---------------- | ---------------- |
| A | 1 | 2 | 3 |
