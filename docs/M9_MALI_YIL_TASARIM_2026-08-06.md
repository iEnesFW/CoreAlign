# M9 · Mali Yıl (Çalışma Yılı) Ayrımı ve Yıl Değiştirici — Faz 1 Tasarım

**Tarih:** 2026-08-06 · **Statü:** Faz 1 = SADECE tasarım. Bu doküman hiçbir kod/şema değişikliği içermez.
**Ölçüm kaynağı:** canlı `corealign` veritabanı (PostgreSQL 18) + repo taraması.

---

## 0. Yönetici özeti

İhtiyaç: kullanıcı bir "çalışma yılı" seçsin, ekranlar ve raporlar o yılın verisini göstersin; geçmiş yıl salt-okunur kalsın; yıl değişince **hiçbir veri silinmesin**.

Bugünkü durum üç cümlede:

1. **Mali yıl kavramı kısmen var:** `tenants.fiscal_year_start_month` (bugün `1` = takvim yılı) ve tam bir **yıl sonu kapanış/açılış** özelliği (`YearEndHandlers`, `YearEndClosePage`) mevcut.
2. **Ama dönem defteri BOŞ:** `accounting_periods` tablosu var ve `GLPostingService:148` postingi kapalı dönemde durduruyor — fakat tabloda **0 satır** var. Yani bugün hiçbir dönem tanımlı değil ve kapalı-dönem kapısı fiilen hiç tetiklenmiyor.
3. **Global yıl bağlamı hiç yok:** hiçbir liste/rapor ekranı yıla göre süzmüyor, bir yıl değiştirici bileşeni yok, `user_preferences`'ta yıl alanı yok.

Bu yüzden M9 "yeni bir mali yıl motoru yazmak" değil, **var olan üç parçayı (tenant ayarı + dönem defteri + kapanış) bir bağlam ve bir süzgeç etrafında birleştirmek**tir.

---

## 1. Kapsam envanteri — hangi ekran hangi tarihe bakar

Aşağıdaki tablo canlı şemadan çıkarıldı. "Yıl süzgeci hangi kolona uygulanmalı" sorusunun cevabı belge tipine göre değişir; **muhasebe hangi tarihe göre yıla girdiğini** belirler.

| Tablo                       | Yıl süzgeci için doğru kolon                        | Neden                                                      | Index durumu                                                          |
| --------------------------- | --------------------------------------------------- | ---------------------------------------------------------- | --------------------------------------------------------------------- |
| `orders`                    | `order_date`                                        | belge tarihi                                               | ✅ `(tenant_id, order_date DESC, id DESC)`                            |
| `quotes`                    | `quote_date`                                        | belge tarihi                                               | ✅ `(tenant_id, quote_date DESC, id DESC)`                            |
| `invoices`                  | **`posting_date`** (rapor) / `issue_date` (liste)   | GL'e hangi döneme düştüğü `posting_date` ile belirlenir    | ✅ ikisi de indexli                                                   |
| `payments`                  | **`posting_date`** (rapor) / `payment_date` (liste) | aynı ayrım                                                 | ✅ `(tenant_id, payment_date DESC, id DESC)`; `posting_date` indexsiz |
| `journal_entries`           | `posting_date`                                      | tanım gereği                                               | ✅ 3 ayrı index                                                       |
| `vendor_bills`              | `bill_date`                                         | belge tarihi                                               | ✅ `(tenant_id, bill_date DESC, id DESC)`                             |
| `vendor_payments`           | `payment_date`                                      | belge tarihi                                               | ✅ `(tenant_id, payment_date DESC, id DESC)`                          |
| `purchase_orders`           | `order_date`                                        | belge tarihi                                               | ✅ `(tenant_id, order_date DESC, id DESC)`                            |
| `shipments`                 | `created_date`                                      | ⚠️ isim yanıltıcı ama iş tarihi bu                         | ✅ `(tenant_id, created_date DESC, id DESC)`                          |
| `return_requests`           | `requested_at_utc`                                  | belge tarihi                                               | ✅ `(tenant_id, requested_at_utc DESC)`                               |
| `goods_receipts`            | `receipt_date_utc`                                  | belge tarihi                                               | ❌ **index YOK**                                                      |
| `glass_projects`            | `created_at_utc`                                    | proje açılışı                                              | ❌ tarih-sıralı index YOK (`updated_at_utc` var)                      |
| `payroll_runs` / `payslips` | `period_year` (**zaten int**)                       | bordro kendi yılını taşıyor                                | —                                                                     |
| `stock_movements`           | zaman-partition'lı                                  | fiziksel hareket, yıl süzgeci muhasebe değil operasyon işi | partition pruning                                                     |

**Sonuç:** 13 belge tablosunun 11'inde yıl aralığı sorgusu **mevcut index'i kullanır** (`tenant_id` eşitlik + tarih aralığı = composite index'in doğal kullanımı). Yalnız `goods_receipts` ve `glass_projects` için index eklemek gerekir. Bu, M9'un performans maliyetinin **düşük** olduğunu söyleyen en önemli bulgu.

### 1.1 Yıl süzgecine GİRMEYECEK olanlar (bilinçli)

Bunlar "o yıla ait" değil, **cari durum**tur; yıl süzgeci uygulanırsa yanlış olur:

- Stok bakiyeleri (`stock_items.on_hand`), ürün/müşteri/tedarikçi kartları, fiyat listeleri, kullanıcılar, ayarlar.
- Cari bakiye (`customers.current_balance`) — kümülatiftir.
- Bilanço "as-of" raporları — zaten bir tarihe göre kümülatif çalışır (§2 Sprint-2 notu: `GetAccountBalancesAsOfAsync` tüm geçmişi toplar).

---

## 2. Bağlam tasarımı — yıl nerede yaşamalı?

Üç aday değerlendirildi:

| Seçenek                                                       | Artı                                                                                                                                  | Eksi                                                                                                                         | Karar           |
| ------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------- | ---------------------------------------------------------------------------------------------------------------------------- | --------------- |
| **A. Yalnız client state (Zustand)**                          | sıfır backend işi, anında                                                                                                             | sekme/yenileme arası kaybolur; backend raporları yılı bilmez; iki sekmede iki yıl → kullanıcı yanılır                        | ❌              |
| **B. Kullanıcı tercihi (kalıcı) + her isteğe açık parametre** | kullanıcı bazlı (muhasebeci 2025'te, satışçı 2026'da çalışabilir); backend her sorguda yılı **açıkça** alır → önbellek anahtarı temiz | her liste endpoint'ine parametre eklemek gerekir                                                                             | ✅ **ÖNERİLEN** |
| **C. JWT claim / tenant-genel ayar**                          | tek yer                                                                                                                               | tenant genelinde tek yıl = aynı anda iki yılda çalışılamaz; yıl değişimi token yenilemesi ister; önbellek zehirlenmesi riski | ❌              |

### 2.1 Kalıcılık yeri

`user_preferences` tablosu var (`mode_override`, `per_screen_overrides_json varchar(2000)`, `locale_override`, `theme_override`).

⚠️ **`per_screen_overrides_json` blob'una yazmayın.** O alan persona modülüne ait ve 2000 karakterle sınırlı; iki modülün aynı blob'u okuyup yazması INVARIANTS'taki "iki yazıcı" tuzağının aynısıdır (son yazan diğerini ezer). Doğru yol: **`fiscal_year integer NULL` adında ayrı bir kolon** (tek int, migration ucuz, çakışma imkânsız).

`NULL` = "cari yılı kullan" (kullanıcı hiç seçim yapmamış). Böylece mevcut 1 satırlık `user_preferences` verisi bozulmaz ve yeni kullanıcı otomatik cari yılda başlar.

### 2.2 İstemci ↔ sunucu sözleşmesi

```
GET /api/v1/orders?fiscalYear=2025&page=1&pageSize=25
```

- Parametre **opsiyonel**; verilmezse bugünkü davranış (süzgeçsiz) **birebir korunur** → geriye tam uyum, kademeli benimseme.
- Sunucu tarafında yıl → tarih aralığına çevirme **tek bir yerde** olmalı: `FiscalYearRange.For(tenant.FiscalYearStartMonth, year) → (startUtc, endUtcExclusive)`. Her handler kendi aritmetiğini yaparsa Ocak-dışı mali yılda kaçınılmaz olarak ayrışır (projede tekrarlayan "tek yazıcı" dersi).
- Aralık **yarı açık** olmalı (`>= start AND < end`), `BETWEEN` değil — `timestamptz` kolonlarda `BETWEEN` yılın son gününün 00:00'ından sonrasını düşürür (M8/accounting'de yaşanmış "as-of gün sonu" tuzağının aynısı).

### 2.3 Frontend

- `shared/lib/store/fiscalYearStore.ts` (cross-cutting → `shared`, FSD; `authStore`/`aiHelperStore` emsali).
- Navbar'da yıl değiştirici (yıl listesi: `accounting_periods`'tan mevcut yıllar + cari yıl).
- TanStack Query anahtarlarına yıl **girmeli** (`['orders','list',{...filters, fiscalYear}]`) — aksi halde yıl değişince eski yılın önbelleği gösterilir. Bu, M9'un en kolay gözden kaçan teknik detayı.

---

## 3. Takvim yılı vs mali yıl

- `tenants.fiscal_year_start_month` **zaten var** ve bugün `1`. Türkiye'de kurumlar vergisi mükelleflerinin ezici çoğunluğu takvim yılı kullanır; özel hesap dönemi (örn. Temmuz-Haziran) GİB iznine tabidir ama mümkündür.
- Tasarım baştan `fiscalYearStartMonth ≠ 1` durumunu desteklemeli, çünkü sonradan eklemek **tüm** aralık hesaplarını dolaşmayı gerektirir.
- Etiketleme kuralı (karar gerekiyor, §7-2): başlangıç ayı 7 olan bir tenant'ta 2026-07-01 … 2027-06-30 dönemi **"2026"** mı **"2026/27"** mı denir? Öneri: **kod içinde başlangıç yılı (2026)**, kullanıcıya gösterilen etiket `2026/27` (yalnız `startMonth ≠ 1` iken).

---

## 4. Geçmiş yıl politikası

| Konu                         | Öneri                                                                          | Gerekçe                                                                                                        |
| ---------------------------- | ------------------------------------------------------------------------------ | -------------------------------------------------------------------------------------------------------------- |
| Geçmiş yıl görünür mü?       | ✅ evet, tam okunur                                                            | veri silinmiyor; denetim ve karşılaştırma şart                                                                 |
| Geçmiş yıla **yazma**        | ❌ engellenmeli — ama **dönem kapanışı üzerinden**, yıl seçici üzerinden değil | yıl seçici bir görünüm tercihidir, yetki değil; asıl kapı `accounting_periods.status`                          |
| Zaten kapalı dönem           | `GLPostingService` postingi bugün de durduruyor (`SkippedClosedPeriod`)        | mekanizma hazır, **dönemler seed edilmediği için ölü**                                                         |
| Yıl sonu kapanışı            | mevcut `CloseFiscalYear`/`OpenFiscalYear` kullanılacak                         | yeniden yazma yok                                                                                              |
| Kapalı yılda belge düzenleme | reddedilmeli, kullanıcıya "dönem kapalı" mesajı                                | bugün sessizce Deferred outbox'a düşüyor → **kullanıcı hiçbir şey görmüyor** (M9'da düzeltilecek gerçek kusur) |

**Önemli tespit:** kapalı-dönem davranışı bugün _sessiz_. `GLPostingService` `SkippedClosedPeriod` döndürüyor, outbox mesajı `Deferred` kalıyor, kullanıcı faturayı kesmiş sanıyor ama GL'e hiçbir şey düşmüyor. Dönemler seed edilir edilmez bu sessizlik **görünür bir hataya** dönüşmeli.

---

## 5. "Sıfırlama" semantiği — VERİ SİLİNMEZ

Kullanıcının "yıl sıfırlama" beklentisi ERP'de üç farklı şey olabilir; hangisi olduğu netleşmeli (§7-3):

| Yorum                       | Ne yapar                                                         | Veri kaybı            | CoreAlign'daki karşılığı                                                                                                                                                                                                                                                                                                                                                                                                                                   |
| --------------------------- | ---------------------------------------------------------------- | --------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| **(a) Görünüm sıfırlama**   | ekranlar yeni yılı gösterir                                      | yok                   | M9'un ana işi (yıl seçici)                                                                                                                                                                                                                                                                                                                                                                                                                                 |
| **(b) Numaratör sıfırlama** | belge numarası yeni yılda 1'den başlar                           | yok                   | ✅ **zaten çalışıyor** — `DocumentSequence.ConsumeNext(nowUtc)` yeni yılın ilk belgesinde `CurrentYear`'ı günceller ve `NextNumber`'ı 1 yapar (kod okundu, satır 29-32). ⚠️ ama reset **takvim yılına** bağlı (`nowUtc.Year`), mali yıla değil → `fiscalYearStartMonth ≠ 1` olan tenant'ta numara 1 Ocak'ta sıfırlanır, 1 Temmuz'da değil. Bugün latent (başlangıç ayı 1). Ayrıca `ResetForYear(...)` metodunun **sıfır çağıranı** var (ölü admin API'si). |
| **(c) Bakiye devri**        | geçmiş yıl kâr/zararı 570/580'e, bilanço hesapları açılış fişine | yok (yeni fiş üretir) | mevcut `CloseFiscalYear` + `OpenFiscalYear`                                                                                                                                                                                                                                                                                                                                                                                                                |

**Hiçbirinde satır silinmez.** Bu doküman, "sıfırlama" kelimesinin `DELETE` anlamına gelmediğini açıkça kayda geçirir.

---

## 6. Performans

- Yıl süzgeci `WHERE tenant_id = @t AND date >= @start AND date < @end` biçimindedir; §1'deki 11 tabloda mevcut `(tenant_id, date DESC, id DESC)` index'i bunu **doğrudan** karşılar (eşitlik + aralık, index'in ilk iki kolonu).
- Keyset sayfalama bozulmaz: cursor koşulu aynı index üzerinde yıl aralığıyla birlikte çalışır.
- Eklenmesi gerekenler: `goods_receipts (tenant_id, receipt_date_utc DESC, id DESC)` ve `glass_projects (tenant_id, created_at_utc DESC, id DESC)`.
- Partition'lı tablolarda (ledger, audit, stock_movements) yıl aralığı **partition pruning'i güçlendirir** — performans artar, azalmaz.
- Ölçüm gerektiren tek nokta: raporlar. `>10k` satır beklenen yıl-süzgeçli her yeni rapor sorgusunda `EXPLAIN ANALYZE` (CLAUDE.md §4.11).

---

## 7. Karar bekleyen sorular

1. **Kapsam:** yıl süzgeci hangi ekranlara uygulanacak? Öneri: **belge listeleri + finansal raporlar**; kartlar (müşteri/ürün/stok) ve cari bakiyeler **hariç**.
2. **Etiketleme:** `startMonth ≠ 1` iken yıl adı `2026` mı `2026/27` mi? (Öneri: kodda 2026, etikette 2026/27.)
3. **"Sıfırlama"** hangi anlamda isteniyor — §5'teki (a), (b), (c) veya hepsi?
4. **Kullanıcı bazlı mı tenant bazlı mı?** Öneri kullanıcı bazlı (B). Tenant bazlı istenirse tasarım basitleşir ama iki yılda paralel çalışma imkânsız olur.
5. **Geçmiş yılda yazma:** dönem kapanışı zorunlu mu, yoksa "kapalı yıl uyarı verir ama yetkili geçebilir" mi?

---

## 8. Fazlı plan (Faz 2+)

### Faz 2 — Temel (kod, düşük risk)

1. `FiscalYearRange` saf yardımcısı (`startMonth`, `year` → yarı açık UTC aralığı) + birim testleri (Ocak-dışı başlangıç, artık yıl, sınır saatleri).
2. `user_preferences.fiscal_year integer NULL` kolonu (migration; NULL = cari yıl).
3. `GET/PUT` tercih uçları + `shared/lib/store/fiscalYearStore` + navbar yıl değiştirici.
4. **Hiçbir liste henüz süzülmez** — bağlam kurulur, davranış değişmez. (Geri alınabilir, kullanıcıya görünen tek şey yeni bir açılır liste.)

### Faz 3 — Süzgeci uygula (ekran ekran)

5. Sıra: Faturalar → Siparişler → Ödemeler → Yevmiye → Satınalma → Sevkiyat → İadeler. Her ekran ayrı teslim, her biri tarayıcıda doğrulanır.
6. Query anahtarlarına yıl eklenir (önbellek zehirlenmesi kapısı).
7. Eksik iki index eklenir.

### Faz 4 — Dönem defterini canlandır

8. Tenant açılışında (ve mevcut tenant için tek seferlik) `accounting_periods` seed'i: cari yıl + geçmiş yıllar için 12 dönem.
9. Kapalı dönemde belge düzenleme **sessiz Deferred yerine görünür 409** döndürür.

### Faz 5 — Yıl sonu akışını bağla

10. Mevcut `YearEndClosePage` yıl seçiciyle ilişkilendirilir; kapanış sonrası o yıl salt-okunur işaretlenir.
11. Numaratör reset'i mali yıla bağlanır (§5-b): `ConsumeNext` bugün takvim yılına bakıyor; `fiscalYearStartMonth ≠ 1` senaryosu için `FiscalYearRange` üzerinden yıl türetilmeli.

---

## 9. Bu dokümanın kanıtları

- `information_schema.columns` + `pg_indexes` (canlı DB, 2026-08-06): 13 belge tablosunun tarih kolonları ve index'leri tek tek listelendi.
- `accounting_periods`: **0 satır** (canlı sorgu).
- `tenants.fiscal_year_start_month = 1` (canlı sorgu).
- `user_preferences`: 14 kolon, yıl alanı yok, 1 satır.
- Kod referansları: `GLPostingService.cs:148` (kapalı dönem kapısı), `YearEndHandlers.cs`, `YearEndClosePage.tsx`, `fiscalYearCloseApi.ts`.
- Faz 1 kapsamı gereği **hiçbir dosya değiştirilmedi**.
