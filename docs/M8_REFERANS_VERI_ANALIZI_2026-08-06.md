# M8 · Referans Veri Disiplini — Faz 1 Analiz Raporu

**Tarih:** 2026-08-06 · **Kapsam:** para birimi, stok birimi (UoM), müşteri girişi
**Statü:** Faz 1 = SADECE analiz. Bu rapor hiçbir kod/şema değişikliği içermez.
**Ölçüm kaynağı:** canlı `corealign` veritabanı (PostgreSQL 18, 1 tenant, 60 ürün / 7 müşteri / 32 sipariş / 18 fatura) + repo taraması.

---

## 0. Yönetici özeti

Üç referans alanın da **iki paralel gerçeği** var: bir küratörlü tablo ve onun yanında serbest metin bir kolon. Küratörlü taraf UI'da mevcut, ama yazma yolları serbest metni besliyor; sonuç, referans tablosunun fiilen boşta durması.

| Alan           | Küratörlü kaynak                                          | Gerçekte yazılan                                  | Ölçülen uyum                                                                              |
| -------------- | --------------------------------------------------------- | ------------------------------------------------- | ----------------------------------------------------------------------------------------- |
| Stok birimi    | `units_of_measure` (37 satır: ADET, METREKARE, KILOGRAM…) | `products.unit` serbest metin (`Kg`, `pcs`, `M2`) | **60/60 ürün eşleşmiyor (%0 uyum)**                                                       |
| Para birimi    | `currencies` (4 satır: TRY, USD, EUR, GBP)                | 100+ tabloda `varchar(3)` serbest metin, FK yok   | Veri temiz (%100 TRY) ama **kur beslemesi 21 para birimi getiriyor**, seçilebilir liste 4 |
| Müşteri girişi | —                                                         | `customers` serbest metin alanları                | 7 müşterinin **1'inde geçersiz VKN**, 5'inde telefon yok, 5'inde vergi no yok             |

**En kritik bulgu (gizli, henüz patlamamış):** ürün birimi e-faturaya **ham** gidiyor. `UblTrInvoiceXmlBuilder` satır 335/363 `unitCode="{line.UomCode ?? "C62"}"` yazıyor. Bugün `invoice_lines.uom_code` 18/18 satırda BOŞ olduğu için `C62`'ye düşüyor ve faturalar geçerli. Biri ürün birimini doldurduğu anda XML'e `unitCode="Kg"` / `unitCode="ADET"` gidecek — bunlar UN/ECE Rec-20 kodu değil, GİB reddeder. Yani mevcut "temizlik" bir tasarım değil, **kullanılmıyor olmanın yan etkisi**.

---

## 1. Giriş noktaları envanteri

### 1.1 Para birimi

| Yüzey             | Yer                                                                                                                                                | Giriş biçimi                            | Değerlendirme                                                                               |
| ----------------- | -------------------------------------------------------------------------------------------------------------------------------------------------- | --------------------------------------- | ------------------------------------------------------------------------------------------- |
| Admin SPA         | `shared/ui/form/CurrencySelect.tsx`                                                                                                                | `currencies` lookup'ından açılır liste  | ✅ küratörlü. Bilinmeyen bir değer saklıysa onu da kendi seçeneği olarak korur (kilitlemez) |
| Admin SPA         | `CustomerFormModal`, `OrderFormModal`, `ProductFormModal`, `VendorFormModal`, `VendorChildModals`, `CompanyProfileSection`, `MasterDataQuickModal` | `CurrencySelect`                        | ✅ 7 form da aynı bileşeni kullanıyor                                                       |
| B2B portal        | `apps/b2b/.../NewOrderForm.tsx:29`                                                                                                                 | `selectedCustomer?.currency \|\| 'TRY'` | ⚠️ giriş yok, müşteriden miras; fallback sabit `'TRY'`                                      |
| Customer portal   | —                                                                                                                                                  | para birimi girişi yok                  | ✅                                                                                          |
| Mobil             | —                                                                                                                                                  | para birimi girişi yok                  | ✅                                                                                          |
| Backend API       | `UpsertTenantSmtpSettings` dışındaki tüm `*Command`'lar                                                                                            | `string Currency`                       | ⚠️ hiçbir validator `currencies` tablosuna karşı doğrulamıyor                               |
| Toplu içe aktarma | `Imports/Products/ProductBulkImporter.cs:80`                                                                                                       | `row.Currency.ToUpper()`, boşsa `"TRY"` | ❌ **doğrulama yok** — `XYZ` yazan bir CSV satırı sessizce kabul edilir                     |
| Kur beslemesi     | `TcmbFxIngestJob`                                                                                                                                  | TCMB'den 21 para birimi                 | ⚠️ `currencies` tablosuna satır EKLEMEZ                                                     |

### 1.2 Stok birimi (UoM)

| Yüzey                | Yer                                                         | Giriş biçimi                                                                       | Değerlendirme                                                                         |
| -------------------- | ----------------------------------------------------------- | ---------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------- |
| Admin SPA            | `ProductFormModal.tsx:505`                                  | **serbest metin** `register('unit')`                                               | ❌ asıl kirlilik kaynağı                                                              |
| Admin SPA            | `ProductFormModal.tsx:516+`                                 | `baseUomId` / `salesUomId` / `purchaseUomId` → `units_of_measure` açılır listeleri | ✅ küratörlü — ama **aynı formda serbest metinle yan yana**                           |
| Admin SPA            | `OrderFormModal.tsx:403`                                    | `uom?.code ?? product.unit ?? ''`                                                  | ⚠️ küratörlü kod varsa onu, yoksa serbest metni satıra yazar → tek kolonda iki alfabe |
| Admin SPA            | `JobFormModal.tsx:29,98`                                    | **serbest metin**, varsayılan `'PCS'`                                              | ❌ üretim işi birimi hiçbir listeye bağlı değil                                       |
| B2B / portal / mobil | —                                                           | birim girişi yok (okur, yazmaz)                                                    | ✅                                                                                    |
| Toplu içe aktarma    | `ProductBulkImporter.cs:39,76`                              | serbest metin, boşsa `"pcs"`                                                       | ❌ doğrulama yok                                                                      |
| Cam modülü           | `glass_hardware_items.unit`, `glass_project_bom_lines.unit` | `varchar(20)` serbest metin                                                        | ❌ ayrı bir üçüncü alfabe                                                             |

### 1.3 Müşteri girişi

| Alan         | Doğrulama (bugün)                                       | Boşluk                                                                             |
| ------------ | ------------------------------------------------------- | ---------------------------------------------------------------------------------- |
| `name`       | zorunlu, ≤200                                           | trim yapılıyor                                                                     |
| `email`      | format, ≤256                                            | ⚠️ tenant içi tekillik **yok** (duplicate-detection raporu var, engel yok)         |
| `phone`      | ≤30, `CountryAddressRules` ile ülkeye göre hane aralığı | ✅ ülke bilinen ise                                                                |
| `taxNumber`  | ≤50, **checksum yok**                                   | ❌ M7'de FE'ye eklenen VKN checksum'ı müşteri formunda **yok**; backend'de hiç yok |
| `nationalId` | ≤512 (şifreli kolon), **checksum yok**                  | ❌ aynı                                                                            |
| adres        | `CountryAddressRules` posta kodu + eyalet zorunluluğu   | ✅ bilinen ülkelerde                                                               |

---

## 2. Şema envanteri

### 2.1 Para birimi kolonları — 100+ tablo, FK sıfır

- **`currencies` tablosuna işaret eden FK sayısı: 0.** (`pg_constraint` sorgusu boş döndü.)
- Tip tutarsızlığı — aynı kavram dört farklı biçimde saklanıyor:
  | Biçim | Örnek tablo | Not |
  | --- | --- | --- |
  | `varchar(3)` | `orders`, `invoices`, `payments`, `products`, … (çoğunluk) | kanonik |
  | `varchar(8)` | `exchange_rates.currency`, `dealer_commission_ledger_entries.currency` | gereksiz geniş |
  | **`character(3)`** | `employees.salary_currency`, `payroll_runs.currency` | ⚠️ **boşlukla doldurulur** — `'TRY'` yazıp `'TRY '` okunabilir; string karşılaştırması ve `Dictionary` anahtarı sessizce ıskalar |
  | farklı ad | `tax_declarations.currency_code` | isimlendirme sapması |
- CHECK constraint: yok (para birimi için).

### 2.2 Birim kolonları — iki model üst üste

| Model            | Kolonlar                                                                                                                                 | Durum                                                                                                                                                                    |
| ---------------- | ---------------------------------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| **FK modeli**    | `products.base_uom_id / sales_uom_id / purchase_uom_id`, `*_lines.uom_id` (order/invoice/quote/PO/return/vendor-bill/recurring)          | `units_of_measure`'a gerçek FK — **ama yalnız `products.base_uom_id` için** (`pg_constraint`: 2 FK, biri self-referans). Satır tablolarındaki `uom_id` kolonları FK'siz. |
| **Metin modeli** | `products.unit varchar(20)`, `*_lines.uom_code varchar(20)` (bir yerde `varchar(16)`), `glass_*.unit`, `production_jobs.unit_of_measure` | serbest metin, doğrulanmıyor                                                                                                                                             |

`recurring_invoice_template_lines.uom_code` **varchar(16)**, kardeşleri varchar(20) → 17-20 karakterlik bir kod tekrarlayan faturada sessizce kesilir.

### 2.3 Referans tablolar

- `currencies`: 4 satır (TRY/USD/EUR/GBP), global.
- `units_of_measure`: 37 satır, **tenant-scoped** (`tenant_id` = tek tenant). Yeni tenant açıldığında bu 37 satır otomatik gelmezse UoM listesi boş açılır — seed yolu doğrulanmalı (Faz 2 kontrol maddesi).
- `countries`: 5 satır. `customer_addresses`'te 1 satır var ve o satırın ülkesi **2 harfli ISO değil**.

---

## 3. Canlı veritabanı kirlilik raporu (ölçüm)

### 3.1 Stok birimi — %0 uyum

```
products.unit dağılımı            units_of_measure.code (37 satır)
  Kg    → 54 satır                  KILOGRAM, ADET, METREKARE, METRE, LITRE, ...
  pcs   →  5 satır
  M2    →  1 satır
```

Üç değerin **hiçbiri** referans tablosunda yok. Uyum: **0/60**.

Ayrıca büyük/küçük harf ve dil karışık: `Kg` (TR/EN karışımı), `pcs` (EN), `M2` (birim simgesi ASCII'leştirilmiş). Referans tablo ise TR-büyük-harf (`KILOGRAM`).

**Cam alan hesabı tuzağı (kod okunarak doğrulandı — `GlassLineMath.cs:11-22`):** alan birimi string eşleşmesiyle tanınır; kabul edilen tam liste `m2 / sqm / metrekare / dm2 / cm2 / mm2` (trim + küçük harf + `²`→`2` sonrası). Sonuçlar:

| Değer                            | Tanınır mı?                        |
| -------------------------------- | ---------------------------------- |
| `M2` (bugünkü veri)              | ✅                                 |
| `METREKARE` (küratörlü kod)      | ✅ tesadüfen — `metrekare` listede |
| `SANTIMETREKARE` (küratörlü kod) | ❌ — yalnız `cm2` tanınıyor        |
| `MTK` (GİB kodu)                 | ❌                                 |

Yani birim alfabesi değiştirilirken bu fonksiyon **aynı commit'te** güncellenmezse cam satırlarında m² türetmesi sessizce durur ve girilen miktar adet gibi işlenir (fiyat ve kesim listesi yanlış çıkar).

### 3.2 Para birimi — veri temiz, katalog eksik

- `orders/invoices/quotes/payments/vendor_bills`: **%100 `TRY`**, tek bir kirli değer yok.
- `exchange_rates`: **21 farklı para birimi** (USD, JPY, GBP, CHF, EUR, SEK, AUD, QAR, NOK, KWD, AZN, SAR, RUB, CAD, DKK, KRW, AED, KZT, RON, PKR, CNY) — hepsi TCMB beslemesinden.
- `currencies`: 4 satır.
- **Sonuç:** kuru olan 17 para birimi kullanıcıya seçtirilemiyor. Kirlilik değil, **kapasite kaybı**. Karşı yön de mümkün: `currencies`'e elle satır eklenirse ve TCMB o kodu getirmiyorsa, o para biriminde kesilen belge kur bulamaz.

### 3.3 Müşteri verisi

```
toplam 7 · e-posta boş 0 · telefon boş 5 · vergi no boş 5
geçersiz VKN (10 hane değil) 1 · geçersiz TCKN 0 · trim'lenmemiş ad 0
tenant içi yinelenen e-posta grubu 0 · yinelenen vergi no grubu 0
```

Küçük veri kümesi; yine de 5 kayıtta vergi no yok ve 1 kayıtta biçimsel olarak geçersiz VKN var. Duplicate **tespiti** var (`/dashboard/reports/duplicates`), duplicate **engeli** yok.

---

## 4. GİB `unitCode` eşleme önerisi

### 4.1 Bugünkü davranış (üç ayrı yol, üçü de yanlış olabilir)

| Yol                                                                                      | Kod                             | Davranış          | Risk                                                 |
| ---------------------------------------------------------------------------------------- | ------------------------------- | ----------------- | ---------------------------------------------------- |
| `Application/EInvoice/UblTrInvoiceXmlBuilder.cs:335,363`                                 | `unitCode="{UomCode ?? "C62"}"` | ham metni geçirir | ❌ `Kg`, `ADET` gibi geçersiz kod → entegratör reddi |
| `Application/EInvoice/UblTrInvoiceXmlBuilder.cs:122` (irsaliye)                          | `unitCode="C62"` sabit          | her şey "adet"    | ❌ kg/m² satan için yanlış beyan                     |
| `Infrastructure/Providers/EFatura/Common/UblTrInvoiceBuilder.cs:186,200` + `Foriba…:323` | `unitCode="C62"` sabit          | her şey "adet"    | ❌ aynı                                              |

İki UBL kurucusu arasında da sözleşme farkı var: biri geçiriyor, diğeri sabitliyor. Aynı fatura hangi sağlayıcıdan gittiğine göre farklı `unitCode` taşıyabilir.

### 4.2 Önerilen eşleme tablosu (UN/ECE Rec-20, UBL-TR kod listeleri)

| Kavram                                  | `units_of_measure.code`       | GİB `unitCode`                      | Not                                                                                                      |
| --------------------------------------- | ----------------------------- | ----------------------------------- | -------------------------------------------------------------------------------------------------------- |
| Adet                                    | ADET                          | `C62`                               | varsayılan                                                                                               |
| Kilogram                                | KILOGRAM                      | `KGM`                               |                                                                                                          |
| Gram                                    | GRAM                          | `GRM`                               |                                                                                                          |
| Ton (metrik)                            | TON                           | `TNE`                               |                                                                                                          |
| Miligram                                | MILIGRAM                      | `MGM`                               |                                                                                                          |
| Metre                                   | METRE                         | `MTR`                               |                                                                                                          |
| Santimetre                              | SANTIMETRE                    | `CMT`                               |                                                                                                          |
| Milimetre                               | MILIMETRE                     | `MMT`                               |                                                                                                          |
| Kilometre                               | KILOMETRE                     | `KMT`                               |                                                                                                          |
| Metrekare                               | METREKARE                     | `MTK`                               | cam için kritik                                                                                          |
| Santimetrekare                          | SANTIMETREKARE                | `CMK`                               |                                                                                                          |
| Metreküp                                | METREKUP                      | `MTQ`                               |                                                                                                          |
| Litre                                   | LITRE                         | `LTR`                               |                                                                                                          |
| Mililitre                               | MILILITRE                     | `MLT`                               |                                                                                                          |
| Paket                                   | PAKET                         | `PK`                                |                                                                                                          |
| Kutu                                    | KUTU                          | `BX`                                |                                                                                                          |
| Koli                                    | KOLI                          | `CT`                                |                                                                                                          |
| Palet                                   | PALET                         | `PF`                                |                                                                                                          |
| Rulo                                    | ROLE                          | `RO`                                |                                                                                                          |
| Takım                                   | TAKIM                         | `SET`                               |                                                                                                          |
| Çift                                    | CIFT                          | `PR`                                |                                                                                                          |
| Düzine                                  | DUZINE                        | `DZN`                               |                                                                                                          |
| Saat                                    | SAAT                          | `HUR`                               |                                                                                                          |
| Gün                                     | GUN                           | `DAY`                               |                                                                                                          |
| Dakika                                  | DAKIKA                        | `MIN`                               |                                                                                                          |
| Hektar                                  | HEKTAR                        | `HAR`                               |                                                                                                          |
| Dekar                                   | DEKAR                         | `DAA`                               | Rec-20'de yok → **karar gerekiyor** (`MTK`'ye çevirip 1000× mi, yoksa reddedip kullanıcıya mı bırakalım) |
| İnç / Fit / Yarda / Libre / Ons / Galon | INC/FIT/YARDA/LIBRE/ONS/GALON | `INH`/`FOT`/`YRD`/`LBR`/`ONZ`/`GLL` | TR e-faturada nadir; tabloda dursun                                                                      |

> Eşleme **kod tarafında sabit bir sözlük** olmalı (`units_of_measure`'a bir `gib_unit_code` kolonu eklemek ikinci bir kirlilik yüzeyi açar; kullanıcı elle "KGM" yazabilir). Sözlükte olmayan bir birim için davranış: **belgeyi reddet ve kullanıcıya söyle**, sessizce `C62`'ye düşme — bugünkü sessiz düşüş yanlış beyandır.

### 4.3 Bunun tek kapıya alınması

Üç yazma noktası tek bir `GibUnitCodeMap.Resolve(uomCode) → string` üzerinden geçmeli; UBL kurucularının hiçbiri kendi kararını vermemeli. (Bu, projede tekrar tekrar uygulanan "tek yazıcı" kuralının aynısı — bkz. INVARIANTS'taki alan/açınım/kesim maddeleri.)

---

## 5. Müşteri hatalı-giriş önleme önerisi

M7'de `src/shared/lib/inputMask.ts` içine **gerçek** TCKN / VKN (GİB) / IBAN (mod-97) algoritmaları eklendi ve firma profili formunda kullanılıyor. Müşteri/tedarikçi formları bunu **henüz kullanmıyor**.

| Öneri                                                                                                                              | Katman                                 | Etki                                                                           |
| ---------------------------------------------------------------------------------------------------------------------------------- | -------------------------------------- | ------------------------------------------------------------------------------ |
| VKN/TCKN maskesi + checksum uyarısı                                                                                                | `CustomerFormModal`, `VendorFormModal` | anında geri bildirim, mevcut kayıtları bozmaz (uyarı, blok değil)              |
| Aynı checksum'ın backend validator'ında **blok** olarak zorlanması                                                                 | `CreateCustomerCommandValidator` vb.   | ⚠️ mevcut 1 geçersiz kayıt düzenlenemez hale gelir → önce temizlik, sonra blok |
| Tenant içi `(tenant_id, lower(email))` ve `(tenant_id, tax_number)` **uyarı** (kayıt sırasında "bu vergi no zaten X müşterisinde") | handler + FE                           | duplicate'i doğuşta engeller; unique index'ten daha yumuşak                    |
| `country` alanını ISO-2'ye normalize                                                                                               | `customer_addresses`                   | bugün 1/1 satır ISO değil                                                      |
| e-posta `lower(trim())` normalize + `normalized_email` kolonu                                                                      | şema                                   | duplicate tespiti bugün her sorguda `lower()` hesaplıyor (index kullanamaz)    |

---

## 6. Kademeli geçiş planı (Faz 2-5)

Sıralama **blast radius'a göre en küçükten** kuruldu; her faz tek başına teslim edilebilir ve geri alınabilir.

### Faz 2 — Kapıları kapat (yeni kirlilik girmesin) · risk: DÜŞÜK · şema değişikliği: YOK

1. `ProductBulkImporter`: `Currency` `currencies`'e karşı, `Unit` `units_of_measure`'a karşı doğrulansın; eşleşmeyen satır **hata satırı** olarak raporlansın (import zaten satır-bazlı hata raporluyor).
2. `ProductFormModal`: serbest metin `unit` alanı **açılır listeye** çevrilsin (`units_of_measure` + mevcut değeri kendi seçeneği olarak koru — `CurrencySelect`'in kanıtlanmış deseni).
3. `JobFormModal`: aynı dönüşüm.
4. Backend: `Currency` taşıyan komut validator'larına `currencies` kontrolü (tek paylaşılan `CurrencyMustExist` kuralı).
5. `GibUnitCodeMap` + üç UBL yazma noktasının tek kapıya alınması; sözlükte olmayan birimde **açık hata**.
   → _Bu adım tek başına §0'daki gizli e-fatura kusurunu kapatır._

### Faz 3 — Katalogları hizala · risk: DÜŞÜK · şema: küçük

6. `currencies` seed'i TCMB'nin getirdiği 21 para birimini kapsasın (veya tersi: `CurrencySelect` `exchange_rates`'ten beslensin — **karar gerekiyor**).
7. `units_of_measure` seed'inin **her yeni tenant'ta** koştuğu doğrulansın (bugün tek tenant var; ikinci tenant açıldığında liste boş gelirse Faz 2'deki açılır listeler kilitlenir — bu Faz 2'nin ön koşulu, sırası öne alınabilir).
8. `character(3)` → `varchar(3)` düzeltmesi (`employees.salary_currency`, `payroll_runs.currency`) + `recurring_invoice_template_lines.uom_code` 16 → 20.

### Faz 4 — Mevcut veriyi temizle · risk: ORTA · geri alınabilir

9. Eşleme tablosu ile `products.unit` backfill'i: `Kg→KILOGRAM (54)`, `pcs→ADET (5)`, `M2→METREKARE (1)`. **60 satır** — elle gözden geçirilebilir büyüklükte.
10. Aynı commit'te `GlassLineMath.AreaUnitDivisor`'a yeni kodların eklenmesi (yoksa cam m² türetmesi durur — §3.1).
11. Geçersiz VKN taşıyan 1 müşterinin düzeltilmesi; `country` ISO-2 normalizasyonu.

### Faz 5 — Sertleştir · risk: YÜKSEK · en son

12. `products.unit` / `*_lines.uom_code` üzerine CHECK veya FK; para birimi kolonlarına `currencies`'e FK (`ON DELETE RESTRICT`).
    ⚠️ 100+ kolon; partition'lı tablolarda FK kısıtları var (bkz. INVARIANTS "partition'lı tablonun id'sine FK verilemez"). Önce hangi kolonların gerçekten kısıtlanabileceği çıkarılmalı.
13. Müşteri checksum'larının backend'de blok'a çevrilmesi (Faz 4 temizliğinden sonra).
14. `(tenant_id, normalized_email)` / `(tenant_id, tax_number)` partial unique — **yalnız** duplicate sayısı 0 olduğu doğrulandıktan sonra (bugün 0).

---

## 7. Karar bekleyen sorular

1. **Para birimi kataloğu:** `currencies` elle mi genişletilsin, yoksa seçilebilir liste `exchange_rates`'ten mi türesin? (İkincisi katalogla beslemeyi otomatik senkron tutar ama "TRY dışı satmıyorum" diyen tenant'a 21 seçenek gösterir.)
2. **Birim alfabesi:** küratörlü kod TR-büyük-harf (`KILOGRAM`) kalsın mı, yoksa doğrudan GİB koduna (`KGM`) mı geçilsin? İkincisi eşleme katmanını gereksizleştirir ama kullanıcıya teknik kod gösterir ve mevcut 37 satırın hepsi yeniden adlandırılır.
3. **`DEKAR`** gibi Rec-20 karşılığı olmayan birimler: çevir mi, reddet mi?
4. **Faz 5 sertleştirme kapsamı:** 100+ para birimi kolonunun tamamı mı, yoksa yalnız belge/defter tabloları mı?

---

## 8. Bu raporun kanıtları

- Şema sorguları: `information_schema.columns`, `pg_constraint` (canlı DB, 2026-08-06).
- Kirlilik sorguları: `products.unit`, `*_lines.uom_code`, `*.currency` üzerinde `GROUP BY` sayımları.
- Kod referansları dosya + satır numarasıyla verildi; hiçbiri varsayım değil, okunarak doğrulandı.
- Faz 1 kapsamı gereği **hiçbir dosya değiştirilmedi**.
