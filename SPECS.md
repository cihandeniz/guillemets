# Templating

A logicless, markdown-aware template engine designed for non-technical users.
Syntax is minimal, language-neutral, and keyboard-friendly.

---

## Delimiters

`«»` (guillemets) are the sole delimiter characters. They never appear in
standard markdown, making them unambiguous in any template context.

On Turkish keyboard: `AltGr+Z` = `«`, `AltGr+X` = `»`.

Multi-quote depth (`««`, `«««`, ...) is used for readability at nesting levels.
The engine accepts any consistent depth — the author chooses based on
surrounding context.

Tokens may span multiple lines. Whitespace including newlines inside `«»` is
normalized to a single space before resolution.

```markdown
«geçerlilik
tarihi»
```

resolves identically to `«geçerlilik tarihi»`.

---

## Schema & Localization

Template authors write variable names in any language using natural,
space-separated words. Developers define the model in English using standard
naming conventions. Localization and naming convention conversions bridges the
two.

### Template

```markdown
«teklif no»
«ad soyad»
«şirket: adı»
```

### Model (C#)

```csharp
model.OfferNo
model.FullName
model.Company.Name
```

### Schema mapping

```markdown
Teklif NO = teklif no  = OfferNo
Ad Soyad  = ad soyad   = FullName
Şirket    = şirket     = Company
Adı       = adı        = Name
```

Space-separated words in templates map to PascalCase/camelCase in the model.
They use localization values (case insensitively) of the default language to
resolve back to property names.

## Variables

Single-line or multi-line token resolving to a scalar value.

```markdown
«ad soyad»
```

### Nested Property Access

`:` is the property accessor. It drills into objects and, when encountered on a
list, applies a projection (equivalent to `.Select()`). Chaining across lists
uses `.SelectMany()` internally to keep the result flat.

```
«şirket: adı»
«teklifler: fiyatlar: tutar»
«teklifler: fiyatlar: tutar: dolar fiyatı»
```

## Blocks

A block opens with `««name` on its own line and closes with `»»` on its own
line. Behavior is inferred from the resolved type of `name`:

| Resolved type | Behavior         |
| ---           | ---              |
| boolean       | conditional (if) |
| list          | loop             |
| object        | scope            |

No keyword is required. The same syntax covers all three cases.

```markdown
««bireysel
Sayın «ad soyad»,
»»

««teklif kalemleri
**«açıklama»**

«adet» «birim» × «birim fiyat» = «toplam»
»»

««şirket
Vergi No: «vergi no»
»»
```

When a variable does not exist in existing scope, it looks for upper scopes.

```markdown
Teklif No: «teklif no»

««şirket
«şirket adı» firmasına özel sunduğumuz «teklif no» numaralı bu teklifin
geçerlilik süresi 1 aydır.
»»
```

### Else

`--` on its own line inside a block separates the truthy and falsy branches.
Used with boolean blocks and variable definitions.

```markdown
««bireysel
Sayın «ad soyad»,
--
Sayın «şirket adı» yetkilileri,
»»
```

Else works also when a given object is null.

```markdown
««şirket bilgisi
Şirket adı: «şirket adı»
--
Şirket bilgisi bulunmamaktadır
»»
```

### Magic Loop Variables

The following variables are injected automatically inside every loop block:

| Variable | Meaning              |
| ---      | ---                  |
| `«ilk»`  | true on first item   |
| `«son»`  | true on last item    |

### Negation

`!` prefix negates any boolean variable:

```markdown
«!son»    → true when not last item
«!ilk»    → true when not first item
```

## Variable Definitions

A block can capture its rendered output into a named variable instead of
rendering inline. Use `= condition` after the variable name.

```markdown
««görüşülecek kişi = bireysel
«tam adı»
--
«şirket adı» yetkilileri
»»
```

The defined variable is then available as a plain variable anywhere below its
definition:

```markdown
Sayın «görüşülecek kişi»,

Bu teklif «görüşülecek kişi» adına hazırlanmıştır.
```

The right-hand side of `=` follows the same context-aware block rules: boolean →
if/else, list → loop, object → scope.

> [!TIP]
>
> Inline ifs are not supported, use variable definitions instead.

### Tables

A block can be supports beginning and ending `|` to fit into a table without
breaking markdown table syntax.

```markdown
| Açıklama   | Adet           | Birim Fiyat            | Toplam         |
| ---        | ---            | ---                    | ---            |
| ««kalemler                                                            |
| «açıklama» | «adet» «birim» | «birim fiyat»          | «toplam»       |
| »»                                                                    |
|            |                | **Ara Toplam**         | «ara toplam»   |
|            |                | **KDV (%«kdv oranı»)** | «kdv»          |
|            |                | **Genel Toplam**       | «genel toplam» |
```

## Inline Lists

A variable that resolves to a list of scalars is automatically joined with `, `
(comma space) when used inline:

```markdown
Etiketler: «etiketler»
→ Etiketler: felsefe, bilgelik, antik-yunan
```

### Inline List with Field Selection

When list items are objects, use `:` to project a field:

```markdown
«fiyat teklifleri: tutar»
«teklifler: fiyatlar: tutar: dolar fiyatı»
```

The `:` chain resolves lists via projection and flattening, objects via property
access — whichever the engine encounters at each step.

### Custom Separator

Pass a separator using inner `()` as a named parameter:

```markdown
«teklif: etiketler (ayraç = , )»
```

### Loop Block with Separator

Use the `(ayraç)` parameter on the last line of the block:

```markdown
««etiketler = teklif: etiketler
«ad»
(ayraç = , )»»
```

renders as a comma-separated list when used via `«etiketler»`.

## Parameters

Inner `(name = value)` syntax passes named parameters to the enclosing
expression. A fixed set of built-in parameters is supported:

```markdown
«tarih (format = GG/AA/YYYY)»
«tutar (para birimi = ₺)»
«açıklama (uzunluk = 80)»
«liste: ad (ayraç = , )»
```

Parameters are positional by name and resolved before the outer expression is
evaluated.

## Full Example — Customer Offer

```markdown
# Teklif #«Teklif NO»

««Görüşülecek Kişi = bireysel
«Tam Adı»
--
«Şirket Adı» yetkilileri
»»

**Müşteri:** «Görüşülecek Kişi»
**Tarih:** «Tarih»
**Geçerlilik:** «Geçerlilik Tarihi»

---

Sayın «Görüşülecek Kişi»,

Talep edilen hizmetler için bu teklifi sunmaktan memnuniyet duyarız. Ekibimiz,
üzerinde anlaşılan zaman çizelgesi içinde yüksek kaliteli iş teslim edecek ve
her adımda memnuniyetinizi sağlamayı hedefleyecektir.

## Kalemler

| Açıklama   | Adet           | Birim Fiyat            | Toplam         |
| ---        | ---            | ---                    | ---            |
| ««kalemler                                                            |
| «Açıklama» | «Adet» «Birim» | «Birim Fiyat»          | «Toplam»       |
| »»                                                                    |
|            |                | **Ara Toplam**         | «Ara Toplam»   |
|            |                | **KDV (%«KDV Oranı»)** | «KDV»          |
|            |                | **Genel Toplam**       | «Genel Toplam» |

---

Sizinle çalışmayı dört gözle bekliyoruz. Bu teklif «geçerlilik tarihi» tarihine
kadar geçerlidir. Herhangi bir sorunuz olması durumunda bizimle iletişime
geçmekten çekinmeyiniz.

*«Şirket» — «Tarih (format = GG/AA/YYYY)»*
```
