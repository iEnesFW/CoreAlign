# CoreAlign — Kapsamlı Kod Denetimi

**Tarih:** 2026-07-06
**Kapsam:** Backend (63 Application modülü, ~2.550 `.cs`), 4 frontend yüzeyi (~1.060 `.ts/.tsx`), 285 migration.
**Yöntem:** 6 paralel derin-denetim ajanı; kritik modüller (para/sipariş/stok/e-fatura/MRP/cam) satır-satır, diğerleri geniş tarama. Tüm KRİTİK bulgular gerçek koda karşı doğrulandı.

---

## 0. Yöntem, kapsam sınırları ve okuma notu

- **.NET SDK bu ortamda yok** → backend derlenip çalıştırılamadı; backend bulguları **statik analiz + kod okuma** ile. Yani "derleme hatası" değil, **mantık/eksik/yanlış-implementasyon** hatalarıdır. Runtime doğrulaması için gate'leri kendi makinende koşman gerekir.
- **Frontend gate'leri** temiz bir `git HEAD` worktree'sinde koşuldu: **admin + customer-portal + b2b `tsc --noEmit` → 0 hata (RC=0)**. Vitest bu ortamda koşulamadı (aşağıda "Ortam" notu).
- **⚠️ Ortam uyarısı (önemli):** Bu denetimin koştuğu Linux mount'unda dosyalar CRLF farkı ve **uncommitted dosyalarda satır-ortası kesilme (truncation)** gösteriyordu; `node_modules` Windows'ta kurulmuş. Sonuç: mount üzerinde doğrudan `npm run typecheck/test` koşan **herkes sahte hata görür**. Gate'ler `D:\CoreAlign` (Windows host) veya temiz worktree'de koşulmalı. **Repoya `.gitattributes` (`* text=auto eol=lf`) eklenmesi bu sınıf sorunu kökten çözer.** (Dosya-okuma taramaları gerçek diski okuduğu için bulgular güvenilir.)
- **Dokümantasyon drift'i:** CLAUDE.md/INVARIANTS birkaç yerde güncel değil (three.js "r128 sınırı" gerçekte 0.183; sprint13 "D-3 outbox tenant-aware değil" artık düzeltilmiş). Detay §8'de — çünkü yanlış "kural" da hataya yol açar.

**Önem etiketleri:** KRİTİK = para/stok/hukuki bütünlük veya kritik akış bozulması · YÜKSEK = işlevsel hata/ciddi eksik · ORTA = doğruluk/ölçek riski · DÜŞÜK = hijyen/borç.

---

## Özet — en kritik 15 bulgu

| #   | Modül              | Bulgu                                                                                    | Önem   |
| --- | ------------------ | ---------------------------------------------------------------------------------------- | ------ |
| 1   | Notifications/Auth | Şifre-sıfırlama & e-posta-doğrulama **hiç gönderilmiyor** (`IEmailService` no-op)        | KRİTİK |
| 2   | EInvoice           | E-fatura satıcı bilgisi hardcoded `"Tenant Seller"` → GİB'e geçersiz belge               | KRİTİK |
| 3   | Privacy/KVKK       | DSAR "veri taşınabilirliği/erişim" talepleri **hiçbir iş yapmadan** "tamamlandı" oluyor  | KRİTİK |
| 4   | Consents           | Rıza kaydediliyor ama gönderim öncesi **hiç kontrol edilmiyor** (KVKK ihlali)            | KRİTİK |
| 5   | Shipments          | Sevk edilmiş (Dispatched) sevkiyat iptali **stok/COGS geri almıyor** → kalıcı stok kaybı | KRİTİK |
| 6   | Returns            | Hasarlı (`Restockable=false`) iade **satılabilir stoğa** geri giriyor                    | KRİTİK |
| 7   | Payments           | `VoidPayment` retry'de faturaları **çift ters çeviriyor** (idempotency yok)              | KRİTİK |
| 8   | Fx                 | Kur cache'i **tenant-scope'suz** → Tenant A'nın anlaşmalı kuru Tenant B'ye sızıyor       | KRİTİK |
| 9   | Products           | `Product.StockQuantity` **concurrency token'sız** → eşzamanlı satışta oversell           | KRİTİK |
| 10  | MRP                | Release DTO uyumsuz: "Make" (üretim) planlı siparişleri release'de **kayboluyor**        | KRİTİK |
| 11  | Glass              | Panel/Run/WorkOrder concurrency token **EF'de bağlı değil** → sessiz lost-update         | KRİTİK |
| 12  | Privacy            | Silme (erasure) kapsamı eksik: çalışan/bordro/tedarikçi **IBAN/TCKN silinmiyor**         | KRİTİK |
| 13  | Payroll            | SGK muafiyet bayrağı (`SgkExempt`) hesapta **yok sayılıyor** → yanlış SGK matrahı        | YÜKSEK |
| 14  | Glass              | BOM/fiyat toplamı çok-para-birimli kalemleri **FX'siz** topluyor → yanlış tutar          | KRİTİK |
| 15  | Storage/Notif      | S3/Azure/Redis/WebPush sağlayıcıları **stub** (çok-node prod'da dosya/cache/push yok)    | YÜKSEK |

---

## 1. Para · Muhasebe · Fatura · Ödeme · FX

### [KRİTİK] `VoidPayment` retry'de faturaları çift ters çeviriyor

- **Konum:** `server/src/CoreAlign.Application/Payments/Handlers/PaymentCommandHandlers.cs:377-400`
- **Teşhis:** `ReversePayment` faturalara `payment.Void()`'dan önce uygulanıyor; `Void()` guard'ı yalnız event tekrarını engelliyor. Ağ retry'sinde `Status==Void` olsa bile `Applications` yeniden yükleniyor → `ReversePayment` ikinci kez çalışıp faturayı Paid→Issued'a düşürüyor, AR bozuluyor. `VoidPaymentCommand`'da idempotency key yok.
- **Çözüm:** Handler başına erken terminal guard: `if (payment.Status == PaymentStatus.Void) return PaymentMapper.ToDto(payment);` — ters çevirme yalnız ilk geçişte çalışsın.

### [KRİTİK] FX kur cache'i tenant-scope'suz — kur sızıntısı

- **Konum:** `server/src/CoreAlign.Infrastructure/Fx/FxRateProvider.cs:148-149` (key), `:72` (yaz), `:52-56` (oku)
- **Teşhis:** Cache key `fx-rates:{code}:{date}` tenant içermez ama cache'lenen değer tenant-özel (tenant override + tenant kaynak tercihi). Paylaşımlı `IMemoryCache` singleton olduğu için Tenant A'nın anlaşmalı kuru 4 saat TTL boyunca Tenant B'ye servis edilir → tüm dönüşümler bozulur. `fx-rates:latest` de aynı kusurda.
- **Çözüm:** Key'e tenant ekle: `fx-rates:{tenantId ?? "global"}:{code}:{date}` (TRY-pivot kısayolu scope'suz kalabilir).

### [YÜKSEK] `journal_entries` denge CHECK constraint'i yok

- **Konum:** `server/src/.../Migrations/20260622000000_Phase78FinancialCheckConstraints.cs` (tüm migration ağacı tarandı)
- **Teşhis:** Phase78 `debit/credit>=0` ve tek-yön CHECK'lerini ekliyor ama **`status='Posted' → total_debit=total_credit`** invariant'ı hiçbir migration'da yok. Denge yalnız domain `JournalEntry.Post`'ta tutuluyor; herhangi bir Dapper/raw SQL/kötü migration dengesiz Posted journal yazabilir. CLAUDE.md §4.4(4) bunu zorunlu kılar.
- **Çözüm:** İdempotent: `ALTER TABLE journal_entries ADD CONSTRAINT ck_journal_entries_balanced CHECK (status <> 'Posted' OR total_debit = total_credit)`; modele bildir veya RAW_SQL_INDEX_REGISTRY'ye kaydet.

### [YÜKSEK] `GLPostingService.NextJournalNumberAsync` advisory-lock'suz read-modify-write

- **Konum:** `server/src/CoreAlign.Application/Accounting/Services/GLPostingService.cs:275-289`
- **Teşhis:** GL auto-posting yolu `ConsumeAsync` yerine `GetAsync`+`ConsumeNext` kullanıyor ama `pg_advisory_xact_lock` **almıyor**. Aynı `JournalNumber` dizisini tüketen iki eşzamanlı auto-post çakışır → 23505 → 409/500 + geri alınan posting. Explicit journal çağrıları kilidi alıyor (tutarsız).
- **Çözüm:** `GetAsync`'i `pg_advisory_xact_lock(hashtextextended("docseq:{tenantId}:JournalNumber"))` ile sar.

### [ORTA] `UnapplyPaymentCommand` idempotent değil + muhtemelen `ITransactionalRequest` değil

- **Konum:** `server/src/CoreAlign.Application/Payments/Handlers/PaymentCommandHandlers.cs:344-361`
- **Teşhis:** Manuel `_uow.SaveChangesAsync` çağrılıyor (pipeline envelope'u yerine); `Payment.Unapply` reversal event'i yaymadığı için fatura ve müşteri ledger'ı tutarsız kalabilir. Retry'de `PaymentApplicationException` (400) — idempotent replay yok.
- **Çözüm:** Komutu `ITransactionalRequest` yap; idempotent guard ekle (uygulama yoksa mevcut durumu dön); ledger reversal'ın outbox ile tetiklendiğini doğrula.

### [ORTA] Çoğu `tax_rate`/`discount_percent` kolonunda `[0,100]` CHECK yok

- **Konum:** `server/src/.../Migrations/20260622000000_Phase78FinancialCheckConstraints.cs:28-30` (yalnız `customer_product_prices`)
- **Teşhis:** ~36 percent kolonundan yalnız 3'ünde CHECK var. Kapsam dışı: `invoice_lines.*`, `order_lines.*`, `quote_lines.*`, `purchase_order_lines.*`, `vendor_bill_lines.*`, `tax_rules.rate_percent`, `tax_rates.rate_percent`. Domain guard var ama DB savunma-derinliği yok (§4.4(2)).
- **Çözüm:** İdempotent `BETWEEN 0 AND 100` CHECK migration'ı ile finansal satır + tax tablolarını kapla.

### [ORTA] Kapalı döneme defer'lenen GL posting'i kalıcı sıkışabilir (drain yolu)

- **Konum:** `server/src/CoreAlign.Application/Accounting/Services/GLPostingService.cs:146-147`
- **Teşhis:** Auto sub-ledger posting kapalı döneme denk gelirse `SkippedClosedPeriod` ile defer ediliyor (replayable). Outbox drain artık tenant-aware (§0), ama defer edilen GL kayıtlarının yeniden-deneme tetikleyicisinin gerçekten çalıştığı doğrulanmalı; aksi halde tamlık açığı.
- **Çözüm:** Deferred GL posting'lerin periyodik yeniden-deneme yolunu (Hangfire job) doğrula/ekle.

### [ORTA] `ProcessPaymentWebhookHandler` `ITransactionalRequest` değil (atomiklik zayıf)

- **Konum:** `server/src/CoreAlign.Application/Billing/Handlers/ProcessPaymentWebhookHandler.cs:47-155`
- **Teşhis:** Her dalda manuel `_uow.SaveChangesAsync`; `PaymentAttempt` + `order.MarkPaid` + activation outbox arasında hata olursa yarım durum kalabilir. İdempotency `order.Status==Paid` guard'ıyla korunuyor (iyi) ama tek-transaction garantisi yok.
- **Çözüm:** Komutu `ITransactionalRequest` yap; manuel save'leri kaldır (behavior halleder), Paid guard'ı koru.

### [DÜŞÜK] `FxRatesController.ManualSync` legacy hata gövdesi dönüyor

- **Konum:** `server/src/CoreAlign.API/Controllers/FxRatesController.cs:57-64`
- **Teşhis:** `StatusCode(410, new { error, message })` — CLAUDE.md §3.4'ün yasakladığı şekil; frontend `ApiResponse` interceptor'ından geçmez (contract kırılması, güvenlik sorunu değil).
- **Çözüm:** `ApiResponse<object>.Failure(...)` dön.

### [DÜŞÜK] Ad-hoc credit note (ReturnRequestId'siz) durable idempotency key'siz

- **Konum:** `server/src/CoreAlign.Application/Invoices/Handlers/IssueCreditNoteCommandHandler.cs:102-127`
- **Teşhis:** Durable replay yalnız `ReturnRequestId != null` iken var; ad-hoc credit note retry'si yeni numara tüketip mükerrer belge keser. (Not: doküman'daki "cache-set-before-commit" sorunu bu handler'da YOK — kod tamamen durable.)
- **Çözüm:** No-return yolu için client-supplied `OperationId` unique key ekle (`VendorPayment.OperationId` deseni).

### [DÜŞÜK] KDV1 dönem toplaması tüm ay faturalarını belleğe çekiyor

- **Konum:** `server/src/CoreAlign.Infrastructure/Repositories/TaxAggregationRepository.cs:33-51`
- **Teşhis:** Per-invoice `TaxBreakdownJson` C#'ta parse edildiği için ayın her faturası materialize ediliyor. Tenant+ay sınırlı (bugün kabul edilebilir), yüksek hacimde foresight riski.
- **Çözüm:** Hot olursa tax breakdown'ı sorgulanabilir şemaya taşı/server-side aggregate et.

---

## 2. Sipariş · Teklif · Satınalma · İade · Sevkiyat

### [KRİTİK] Sevk edilmiş (Dispatched) sevkiyatın iptali stok/COGS/FSM geri almıyor

- **Konum:** `server/src/CoreAlign.Domain/Entities/Shipment.cs:110-120` + `server/src/CoreAlign.Application/Shipments/Handlers/ShipmentHandlers.cs:221-228`
- **Teşhis:** `Shipment.Cancel()` yalnız `Delivered`'ı bloklar; `Dispatched` iptal edilebilir. Dispatch anında stok tüketiliyor + COGS GL + `RecordShipment` yapılıyor; `CancelShipmentHandler` ise **sadece statü flip** — stok geri konmaz, COGS reverse edilmez, sipariş `Shipped` kalır. Kalıcı stok kaybı + asılı COGS.
- **Çözüm:** `Cancel()`'i `Dispatched` için reddet **veya** handler'a tam telafi ekle (allocation geri-koy + COGS reversal outbox + `RecordShipment` geri alma), tek transaction'da.

### [KRİTİK] Shipment FSM iki farklı guard kullanıyor — `Cancel()` geçiş tablosunu baypaslıyor

- **Konum:** `server/src/CoreAlign.Domain/Entities/Shipment.cs:159-173` (tablo) vs `:110-120` (`Cancel`)
- **Teşhis:** `EnsureTransitionAllowed` tablosu `Dispatched → yalnız Delivered/Returned` derken `Cancel()` bu tabloyu kullanmaz, elle sadece `Delivered`'ı engeller → yukarıdaki stok kaybının kök nedeni. Reddedilen geçişin guard'ı yok.
- **Çözüm:** `Cancel()`'i de `EnsureTransitionAllowed` üzerinden geçir; tabloya iptal edilebilir kaynak durumları ekle (tek doğruluk kaynağı).

### [KRİTİK] Hasarlı iade (`Restockable=false`) satılabilir stoğa geri giriyor

- **Konum:** `server/src/CoreAlign.Domain/Entities/ReturnRequest.cs:195-199` → `server/src/CoreAlign.Application/Returns/EventHandlers/ReturnRequestStockHandler.cs:36-92`
- **Teşhis:** `MarkReceived` snapshot'ı **tüm** satırlar için üretiyor; `Restockable` filtresi yok. Handler hepsini `ApplyReceipt` ile geri stokluyor → hasarlı/karantina malı satılabilir envantere dönüyor + COGS ters kaydı yapılıyor. `ReturnRequestLine.Restockable` hiç kullanılmıyor.
- **Çözüm:** Snapshot'ı `Lines.Where(l => l.Restockable)` ile filtrele; non-restockable için scrap/karantina akışı ayır.

### [YÜKSEK] Reorder (önceki siparişten oluştur) mutlak iskonto/maliyet alanlarını düşürüyor

- **Konum:** `server/src/CoreAlign.Application/Orders/Handlers/CreateOrderFromPreviousCommandHandler.cs:30-42, 50-68`
- **Teşhis:** `LineDiscountAmount`, `IsManualPriceOverride`, `UnitCostSnapshot`, `WithholdingTaxCodeId` ve başlık `HeaderDiscountAmount` aktarılmıyor (yalnız yüzde kopyalanıyor). Mutlak iskontolu siparişten reorder → iskonto sıfırlanır, yanlış tutar. (Tax/FX doğru kopyalanıyor.)
- **Çözüm:** `OrderLineInput`/`CreateOrderCommand` kurulumuna bu alanları ekle.

### [YÜKSEK] `GetById` endpoint'leri null handler sonucunu 200-null döndürüyor (404 yerine)

- **Konum:** `server/src/CoreAlign.API/Controllers/PurchaseOrdersController.cs:36, 93` · `ShipmentsController.cs:41`
- **Teşhis:** Handler `null` dönebiliyor, controller `.ToOk()` ile **HTTP 200 + null body** üretiyor (bilinen ERP-PAYMENT-404 anti-pattern'i). Frontend "bulundu" sanıp null'a erişince patlar. (`VendorBillsController`/`OrdersController` bu buga sahip değil.)
- **Çözüm:** Handler'lar not-found'da spesifik exception fırlatsın (`PurchaseOrderNotFoundException` vb.).

### [YÜKSEK] `IssueEDespatch` — eksik alıcı bilgisiyle GİB'e e-irsaliye gidebiliyor

- **Konum:** `server/src/CoreAlign.Application/Shipments/EDespatch/EDespatch.cs:124-126`
- **Teşhis:** UBL e-irsaliye outbox handler'da customer null ise "Tenant Seller"/"Alıcı" fallback ile VKN/TCKN'siz belge gönderilebilir → resmi belgede yanlış/eksik taraf.
- **Çözüm:** Outbox handler'da customer/TaxNumber/adres zorunlu kontrolü; eksikse `Failed` bırak, gönderme.

### [YÜKSEK] Shipped sipariş iptali giriş noktasına göre farklı davranıyor (tutarsız)

- **Konum:** `server/src/CoreAlign.Domain/Entities/Order.cs:129-134` (`IsCancellable`) vs `:337-357` (`Cancel()`) vs `:438` (`IsTransitionAllowed`)
- **Teşhis:** Üç kaynak çelişiyor: `CancelOrderCommand` Shipped'te fırlatıyor ama `UpdateOrderCommand`'in `ChangeStatus(Cancelled)` yolu Shipped iptaline izin verip **tüketilmiş stoğu geri koymuyor**. Davranış API girişine göre değişiyor.
- **Çözüm:** Tek politika belirle (ya Shipped iptalini yasakla ya her yolda tam stok/COGS telafisi); dört yer aynı kuralı uygulasın.

### [ORTA] `UpdateOrderCommand` ile onayda iskonto/FX değişikliği sessizce kaybolabilir

- **Konum:** `server/src/CoreAlign.Application/Orders/Handlers/UpdateOrderCommandHandler.cs:58-81`
- **Teşhis:** `HasSameLines` yalnız `(ProductId,Quantity,UnitPrice)` karşılaştırıyor; iskonto/vergi/FX değişse bile `headerOrLinesChanged=false` → draft update atlanıyor ama `ChangeStatus(Confirmed)` yine çalışıyor. "Kaydet+onayla"da yeni finansal değerler yok sayılır.
- **Çözüm:** `HasSameLines`'a iskonto/vergi/FX alanlarını ekle veya Draft'ta durum geçişinden önce daima draft alanlarını uygula.

### [ORTA] FSM handler'ları `ITransactionalRequest` olmasına rağmen manuel `SaveChangesAsync` çağırıyor

- **Konum:** `server/src/CoreAlign.Application/Orders/Handlers/OrderFsmHandlers.cs:86` (ve Submit/Approve/Cancel/Deliver/Close)
- **Teşhis:** Pipeline zaten atomik save sağlıyor; handler'lar ekstra `_uow.SaveChangesAsync` ile bunu ikiye bölüyor (§3.9 "handler'da manuel SaveChanges yönetme" ihlali). Transaction içinde olduğu için rollback güvenli ama sözleşme kırık, çift-save.
- **Çözüm:** Manuel save'leri kaldır; ara-save gereken bootstrap için ayrı non-transactional yol.

### [ORTA] Revizyon snapshot'ı vergi-kod/UOM/withholding kimliklerini taşımıyor

- **Konum:** `server/src/CoreAlign.Application/Orders/Revisions/RevisionHandlers.cs:139-157`
- **Teşhis:** `TaxRateId`, `UomId/UomCode/UomConversionFactor`, `WithholdingTaxCodeId` taşınmıyor; efektif yüzde taşındığı için tutar çoğu kez korunur ama e-fatura GİB kodu ve birim-dönüşümlü satırlarda yanlış kod/miktar riski.
- **Çözüm:** `BuildSnapshot`'a bu alanları ekle.

### [ORTA] `ConvertQuoteToOrder` satır maliyetini `0` yapıyor (kâr raporu bozulabilir)

- **Konum:** `server/src/CoreAlign.Application/Quotes/Handlers/ConvertQuoteToOrderCommandHandler.cs:122`
- **Teşhis:** `unitCostSnapshot: 0m`; onayda COGS AvgCost'tan hesaplanınca telafi olsa da satır maliyet snapshot'ı 0 kalıp `ShipmentLine.UnitCostSnapshot`'a taşınıyor → kâr/maliyet raporu 0 görebilir. (Tax/FX/fiyat/iskonto doğru taşınıyor.)
- **Çözüm:** Dönüşümde ürünün güncel `AverageCost`'unu snapshot yap.

### [DÜŞÜK] Doğrudan `DeliverOrder` kısmi sevkli siparişi Delivered yapabiliyor

- **Konum:** `server/src/CoreAlign.Application/Orders/Handlers/OrderFsmHandlers.cs:156-179`
- **Teşhis:** "Tüm satırlar sevk edildi" ön-koşulu yalnız `DeliverShipmentHandler`'da; doğrudan `DeliverOrderCommand` bu kontrolü yapmıyor.
- **Çözüm:** Ön-koşulu ekle veya komutu yalnız shipment akışından tetiklenir yap.

---

## 3. Stok · Envanter · Ürün · MRP

### [KRİTİK] `Product.StockQuantity` concurrency token'sız → oversell

- **Konum:** `server/src/CoreAlign.Domain/Entities/Product.cs` (token YOK — doğrulandı) + `AllocationService.cs:131,260`, `OrderStockEffectHandlers.cs:172,187,294`
- **Teşhis:** `StockItem` (depo defteri) token'la 409 korumalı ama `Product.StockQuantity` (global satılabilir rollup) hiç token taşımıyor; sipariş-onay availability guard'ı doğrudan bunu okuyor. Eşzamanlı sipariş/allocation/consume/iade son-yazan-kazanır → oversell (§16.1/§4.6 ihlali).
- **Çözüm:** `Product`'a `IHasConcurrencyToken`+`ConcurrencyToken` ekle (StockItem deseni) **veya** `StockQuantity`'yi `StockItem` toplamından türetilmiş tek-kaynak yap.

### [KRİTİK] MRP release DTO uyumsuz — "Make" (üretim) planlı siparişleri release'de kayboluyor

- **Konum:** `src/features/mrp/model/mrp-planning.types.ts:283-290` ↔ `server/src/CoreAlign.Application/Mrp/MrpPlanningService.cs:212-278` + `IMrpPlanningService.cs:6-9`
- **Teşhis:** Frontend `ReleaseResult.productionOrderIds` bekliyor; backend `ReleaseAsync` yalnız `RequisitionIds` (Buy) üretiyor, Make kalemleri için hiç `PlannedProductionOrder` yaratmıyor/döndürmüyor. Make planlı siparişleri release edilince sessizce kayboluyor, UI `productionOrderIds`=undefined okuyor (ERP-MRP-005 açık).
- **Çözüm:** `ReleaseAsync`'i Make için production order üretecek şekilde tamamla; `ReleaseResult`/DTO'ya `ProductionOrderIds` ekle.

### [YÜKSEK] MRP dashboard/reorder/projeksiyon yanlış tarih kolonu (`UpdatedAtUtc`) kullanıyor

- **Konum:** `server/src/CoreAlign.Infrastructure/Mrp/MrpService.cs:62-63, 349-354`
- **Teşhis:** Planlama motoru talep geçmişini doğru (`Order.OrderDate`) kullanıyor ama canlı dashboard/reorder/projeksiyon endpoint'lerini besleyen legacy `MrpService` hâlâ `UpdatedAtUtc` ile grupluyor. `UpdatedAtUtc` her satır dokunuşunda değişir → pencere/bucket kayar → yanlış günlük talep → yanlış reorder point / stockout-günü / önerilen miktar.
- **Çözüm:** `l.Order.OrderDate` kullan (data loader'daki join'i yansıt).

### [YÜKSEK] `ProductionExecutionService` bileşen bazında N+1 stok okuması

- **Konum:** `server/src/CoreAlign.Application/Inventory/Services/ProductionExecutionService.cs:39-55` → `AllocationService.cs:221`
- **Teşhis:** Her BOM bileşeni için tek-tek `GetAsync`/`GetByIdAsync`; çok bileşenli mamulde üretim başına N+1 round-trip (§4.11 ihlali).
- **Çözüm:** Bileşen `StockItem`'larını tek batch (`GetOnHandByProductLotAsync`) ile önden yükle, bellek-içi uygula.

### [YÜKSEK] MRP dashboard/suggestion tüm kataloğu sınırsız `ToListAsync()` çekiyor

- **Konum:** `server/src/CoreAlign.Infrastructure/Mrp/MrpService.cs:159-161, 242-244`
- **Teşhis:** Aktif+stok-takipli her ürün belleğe alınıp C#'ta işleniyor; 100k+ ürünlü tenant'ta OOM/GC + lineer transfer (§11.1 "sınırsız sorgu yok").
- **Çözüm:** Aday filtresini (available<reorderPoint) SQL'e taşı veya keyset/batch stream; dashboard'u server-side aggregate et.

### [YÜKSEK] MRP preview/carry-forward `int.MaxValue` sayfa boyutu

- **Konum:** `server/src/CoreAlign.Application/Mrp/MrpPlanningHandlers.cs:51` + `MrpPlanningService.cs:205`
- **Teşhis:** Preview her çağrıda run'ın TÜM production order'larını `int.MaxValue` ile çekiyor; büyük planda ağır.
- **Çözüm:** Keyset'li batch veya doğrudan projeksiyon; `int.MaxValue` kaldır.

### [ORTA] PurchaseOrderLine per-line teslim tarihi yok — MRP zaman-fazlama başlığa düşüyor

- **Konum:** `server/src/CoreAlign.Infrastructure/Mrp/Planning/MrpPlanningDataLoader.cs:216-237`
- **Teşhis:** Planlanan makbuzlar PO **başlık** `ExpectedDate`'ine bucket'lanıyor (satır tarihi yok) → çok-satırlı PO'da yanlış bucket → yanlış Reschedule-In/Out mesajları (ERP-MRP-002 açık).
- **Çözüm:** `purchase_order_lines.expected_date` kolonu ekle; data loader satır tarihini önceliklendir, başlık fallback.

### [ORTA] MRP net-change modu sessizce Regenerative gibi davranıyor

- **Konum:** `server/src/CoreAlign.Infrastructure/Mrp/MrpPlanningService.cs:88-95`
- **Teşhis:** `Mode=NetChange` API'de kabul ediliyor ama motor tam regeneration yapıyor; kullanıcı beklediği inkremental performansı almıyor, davranış vaatle uyuşmuyor.
- **Çözüm:** NetChange'i "desteklenmiyor" olarak gizle **veya** kalıcı plan tablosu+diff ile gerçek inkremental uygula; en azından "regenerative çalıştırıldı" bilgisi ver.

### [ORTA] MRP yazma endpoint'leri yalnız `[Authorize]` — rol kısıtı yok (tutarsız yetki)

- **Konum:** `server/src/CoreAlign.API/Controllers/MrpController.cs:89-145` (commit/release/firm/dismiss) vs `:178-215` (`TenantAdmin` isteyen production-order işlemleri)
- **Teşhis:** Requisition zinciri başlatan release herhangi bir authenticated kullanıcıya açık; aynı ağırlıktaki production-order işlemleri `TenantAdmin` istiyor (§3.3 ihlali).
- **Çözüm:** Yazma MRP endpoint'lerine tutarlı `[Authorize(Roles=TenantAdminRole)]`; salt-okuma preview `[Authorize]` kalabilir.

### [ORTA] `GenerateRequisitionSuggestions` durable idempotency key'siz

- **Konum:** `server/src/CoreAlign.Infrastructure/Mrp/MrpService.cs:157-237`
- **Teşhis:** Endpoint her tetiklenişte tüm adaylar için yeni requisition+outbox üretiyor; çift-tık/retry aynı `asOfDate` için mükerrer PR (§16.2 ihlali).
- **Çözüm:** `(tenant, asOfDate)` bazlı guard/idempotency ekle.

### [DÜŞÜK] `operationId` alınıyor ama idempotency için kullanılmıyor (ölü kontrat)

- **Konum:** `server/src/CoreAlign.Application/Mrp/MrpPlanningService.cs:212, 280-302`
- **Teşhis:** `ReleaseAsync/FirmProductionOrderAsync/ReleaseProductionOrderAsync` `operationId`'yi hiç kullanmıyor; idempotency doğal FSM flag'lerine dayanıyor (genelde yeterli ama parametre yanıltıcı).
- **Çözüm:** `operationId`'yi gerçek idempotency kaydına bağla ya da imzadan kaldır.

### [DÜŞÜK] `stock_items` sıcak-satır FILLFACTOR ayarı yok

- **Konum:** `server/src/CoreAlign.Infrastructure/Persistence/Configurations/InventoryConfigurations.cs:46-74`
- **Teşhis:** `OnHand/Reserved` her harekette UPDATE (sıcak satır) ama `fillfactor=85` yok → yüksek hacimde page-split/WAL amplification (§4.6).
- **Çözüm:** İdempotent `ALTER TABLE stock_items SET (fillfactor=85)` (ve `document_sequences`).

---

## 4. E-Fatura · Bildirim · Provider · Outbox · Import

### [KRİTİK] Tüm auth/işlem e-postaları sessizce yutuluyor (`IEmailService` no-op)

- **Konum:** `server/src/CoreAlign.Infrastructure/Services/EmailService.cs:17-82` (kayıt: `InfrastructureServiceRegistration.cs:328`)
- **Teşhis:** **Doğrulandı** — kayıtlı tek `IEmailService` implementasyonunun 7 metodu da yalnız `LogInformation` yazıp `Task.CompletedTask` dönüyor. `ForgotPasswordCommandHandler` (şifre sıfırlama), `RegisterCommandHandler` (e-posta doğrulama), duplicate-registration, security-alert, invoice-issued, order-comment, dealer-approval hepsi bu ölü yolu kullanıyor. **Kullanıcı şifresini sıfırlayamaz / hesabını doğrulayamaz.** İşin acısı: gerçek `TenantAwareSmtpEmailProvider`, `SendGridEmailProvider`, `SmtpEmailSender` sınıfları VAR ama bu handler'lara bağlı değil.
- **Çözüm:** `EmailService` metotlarını gerçek gönderime bağla (mevcut `IEmailProvider`/notification-outbox yolunu delege et) veya `IEmailService`'i o yola köprüleyen bir implementasyonla değiştir; auth e-postalarını entegrasyon testiyle doğrula.

### [KRİTİK] E-Fatura satıcı kimliği hardcoded `"Tenant Seller"` placeholder

- **Konum:** `server/src/CoreAlign.Application/EInvoice/InvoiceIssuedEInvoiceOutboxHandler.cs:101-110` (**doğrulandı**)
- **Teşhis:** `BuildSellerParty` her fatura için `Name:"Tenant Seller"`, `TaxNumber:null`, `TaxOffice:null`, adres `null` dönüyor; bu UBL-TR XML'e girip gerçek sağlayıcıya (Nilvera/Foriba) gönderiliyor. Satıcı VKN/unvan/vergi dairesi olmayan e-Fatura/e-Arşiv GİB tarafından reddedilir/geçersizdir → **tenant hiç yasal e-belge kesemez.** Alıcı tarafı doğru dolduruluyor, yalnız satıcı sabit.
- **Çözüm:** Satıcıyı tenant/şirket ayarlarından doldur (VKN, unvan, vergi dairesi, adres); eksikse submission'ı `Failed` yap.

### [YÜKSEK] Cloud dosya depolama (S3/Azure Blob) yalnız `NotSupportedException` stub

- **Konum:** `server/src/CoreAlign.Infrastructure/Storage/S3FileStorage.cs:32-44` · `AzureBlobFileStorage.cs:32-44`
- **Teşhis:** Tüm metotlar SDK yok diye fırlatıyor; tek çalışan `LocalFileSystemStorage`. Yatay ölçekli prod'da paylaşımlı obje deposu olmayınca belge indirme/iletme, ürün görselleri, glass ekleri, imza/foto node'lar arası tutarsızlaşır. (`Storage:Provider=S3` seçilirse startup fail-fast — iyi, ama özellik yok.)
- **Çözüm:** AWSSDK.S3 / Azure.Storage.Blobs ekleyip adapter'ları tamamla, ya da paylaşımlı NFS hedefini netleştir+belgele.

### [YÜKSEK] Zamanlı denetim (KVKK audit) export'u üretilen dosyayı hiç teslim etmiyor

- **Konum:** `server/src/CoreAlign.Application/Compliance/Audit/ScheduledAuditExportJob.cs:117` (+ `EmailMessage.cs:3` attachment yok)
- **Teşhis:** `ExportAsync` dosyayı (`byte[]`) üretiyor ama yalnız `fileName`/`rowCount` metadata e-postaya konuyor; `Content` atılıyor. Job "başarılı" loglar, alıcı "X satır export edildi" metni alır, dosya asla gitmez.
- **Çözüm:** E-posta kanalına attachment desteği ekle veya dosyayı depoya yazıp güvenli indirme linki gönder.

### [YÜKSEK] Zamanlı rapor teslimi raporu ne render ediyor ne ekliyor

- **Konum:** `server/src/CoreAlign.Application/Jobs/ReportScheduleJob.cs:103`
- **Teşhis:** Yalnız rapor metadata'sı (`reportKey`/`format`/`filtersJson`) e-postaya konuyor; `IReportRenderer` enjekte edilmediği için rapor render bile edilmiyor, ek yok. Kullanıcı zamanlı rapor kurar, boş e-posta gelir.
- **Çözüm:** Job'a rapor render pipeline'ını enjekte edip çıktıyı attachment/link olarak teslim et.

### [ORTA] `EmailQueuedOutboxHandler` DI'da register değil → planlı e-postalar dead-letter'a düşüyor

- **Konum:** `server/src/CoreAlign.Application/Common/Email/EmailQueuedOutboxHandler.cs` (kayıt listesi `ApplicationServiceRegistration.cs:103-142`'te yok)
- **Teşhis:** `"EmailQueued"` outbox satırı yazılıyor ama tüketen handler `IOutboxMessageHandler` olarak kayıtlı değil → `OutboxProcessor` bilinmeyen tipi dead-letter'a atıyor. Rapor/audit export teslimi bu yüzden de işlenmez.
- **Çözüm:** `EmailQueuedOutboxHandler` + gerekli `IEmailSender`'ı register et; e-posta outbox happy-path testi ekle.

### [ORTA] Redis dağıtık cache tümüyle stub → çok-node'da stale cache

- **Konum:** `server/src/CoreAlign.Infrastructure/Caching/RedisDistributedCacheService.cs:13-42`
- **Teşhis:** Tüm metotlar fırlatıyor; varsayılan in-memory (tenant-scoped, cross-tenant sızıntı yok — iyi) ama dağıtık değil: çok-node'da her node ayrı cache, tenant-geneli invalidation yayılmaz → dashboard/lookup/rapor stale.
- **Çözüm:** StackExchange.Redis adapter'ı tamamla (tenant-prefix key + pub/sub invalidation) veya tek-node'u zorunlu kıl+belgele.

### [ORTA] AI Helper retriever tüm scope-içi embedding'leri sınırsız belleğe çekiyor

- **Konum:** `server/src/CoreAlign.Infrastructure/AiHelper/PostgresKnowledgeRetriever.cs:36`
- **Teşhis:** `ScopedChunks(query).Select(c => c.Embedding).ToListAsync()` LIMIT'siz; tenant+public tüm chunk embedding'leri RAM'e, cosine C#'ta. pgvector yokluğu için bilinçli tasarım ama her `/ask`'ta lineer bellek+CPU.
- **Çözüm:** Aday kümesini ön-filtrele (trigram/btree + `Take(N)`) veya pgvector HNSW retriever'a geç.

### [ORTA] Web/FCM push yapılandırıldığında bile göndermeden sahte başarı dönüyor

- **Konum:** `server/src/CoreAlign.Infrastructure/Notifications/Push/WebPushProvider.cs:35-36` · `FcmPushProvider.cs:45,54`
- **Teşhis:** `WebPushProvider` VAPID varken bile hiç HTTP POST/şifreleme yapmadan `Ok(guid)` dönüyor (sahte başarı). `FcmPushProvider` Google'ın 2024'te kapattığı legacy endpoint'i kullanıyor → 401/404. Push kanalı seçili tenant'larda bildirimler çalışmaz.
- **Çözüm:** `WebPushProvider`'ı gerçek VAPID payload ile gönder ya da `NotSupported` yap; FCM'i HTTP v1 + OAuth2'ye geçir.

### [ORTA] Toplu import satır tavanı yok — büyük dosyada bellek baskısı

- **Konum:** `server/src/CoreAlign.Infrastructure/Services/Imports/BulkImportRowReader.cs:21`
- **Teşhis:** Reader gerçek (CsvHelper+ClosedXML) ama tüm satırları `List`'e materialize edip preview'ı session store'a yazıyor; tek koruma 10MB. Satır adedi sınırı/streaming yok.
- **Çözüm:** Streaming/`IEnumerable` + satır tavanı (ör. 50k); preview'ı özet/sayfalı tut.

### [DÜŞÜK] Import commit satır-satır tek tek mediator gönderiyor (N+1 yazım)

- **Konum:** `server/src/CoreAlign.Application/Imports/Common/BulkImporterBase.cs:96`
- **Teşhis:** Her satır ayrı `_mediator.Send(...)` → 10k satır = 10k transaction; ayrıca `skipInvalidRows=false` yolunda bütünsel atomiklik yok (kısmi commit).
- **Çözüm:** Chunk bazında tek transaction; hata izolasyonu chunk seviyesinde.

### [DÜŞÜK] Varsayılan virüs tarayıcı `NoOpVirusScanner` — her yüklemeyi "temiz" işaretliyor

- **Konum:** `server/src/CoreAlign.Infrastructure/Storage/NoOpVirusScanner.cs:9-12` (kayıt: `StorageRegistration.cs:16-24`)
- **Teşhis:** `VirusScan:Provider` `ClamAv` değilse tarama yapmadan `Clean` dönüyor; kullanıcı yüklemeleri (ek, foto, belge) taranmıyor. Gerçek `ClamAvVirusScanner` var ama opt-in.
- **Çözüm:** Prod'da `ClamAv` zorunlu kıl; NoOp yalnız dev.

### [DÜŞÜK] `StubElectronicInvoiceGateway` sahte "Accepted" + `EInvoiceOptions.Provider` varsayılanı "Stub"

- **Konum:** `server/src/CoreAlign.Infrastructure/EInvoice/StubElectronicInvoiceGateway.cs:37-51` (+ `EInvoiceOptions.cs:10`)
- **Teşhis:** Stub gateway XML iyi-biçimliyse sağlayıcıya hiç gitmeden "Submitted"/"Accepted" dönüyor. Şu an DI gerçek adapter'ı bağlıyor (ölü kod) ama options varsayılanı hâlâ `"Stub"`; biri yeniden register ederse tüm e-faturalar sahte-gönderilmiş görünür.
- **Çözüm:** Stub'ı test-only/`#if DEBUG` yap; `Provider` varsayılanını gerçek değere çek.

---

## 5. Auth · Gizlilik (KVKK) · Consent

### [KRİTİK] DSAR "veri taşınabilirliği" & "erişim" talepleri hiç iş yapmadan "Completed" oluyor

- **Konum:** `server/src/CoreAlign.Application/Privacy/DataSubjectRequestService.cs:103-114` (Portability) ve `:60-77` (Access)
- **Teşhis:** `ProcessPortabilityRequestAsync` yalnız `MarkInProgress→MarkCompleted`; hiçbir veri paketi üretmiyor, `DataExportFileId` hiç dolmuyor. `ProcessAccessRequestAsync` siparişleri çekip `_ = await` ile atıyor. Talep "tamamlandı" görünür ama veri özneye verilmez → KVKK/GDPR ihlali + denetimde yanlış kanıt.
- **Çözüm:** `ExportMyData` toplama mantığını bu handler'lara taşı, JSON/dosya üretip depola ve `MarkCompleted(now, exportFileId)` ile bağla.

### [KRİTİK] Rıza (consent) kaydediliyor ama gönderim öncesi hiç kontrol edilmiyor

- **Konum:** `server/src/CoreAlign.Application/Consents/ConsentHandlers.cs` (yalnız capture/list/withdraw); `Application/Notifications` altında `consent`/`Purpose` referansı **sıfır**
- **Teşhis:** `UserConsent` yalnız depolanıyor; bildirim/pazarlama gönderim hattında rıza sorgusu yok. Rızası olmayan/geri çekmiş kullanıcıya ticari ileti engellenmiyor → KVKK/GDPR açık ihlali.
- **Çözüm:** Pazarlama bildirimi gönderiminden önce ilgili `Purpose` için geçerli/geri-çekilmemiş consent kontrolü; yoksa blokla.

### [KRİTİK] Silme (erasure) kapsamı eksik — çalışan/bordro/tedarikçi IBAN/TCKN silinmiyor

- **Konum:** `server/src/CoreAlign.Infrastructure/Repositories/PrivacyEraseService.cs:17, 92`
- **Teşhis:** Erasure yalnız CustomerContacts/Addresses, LoginAuditLogs, ActivityLogs, token, UserSessions'a dokunuyor. `employees.iban/sgk_registration_no/national_id`, `payslips.national_id`, `vendor_bank_accounts.iban` hiç temizlenmiyor (`Employee`/`Payslip`'te `Anonymize` metodu bile yok). Silme sonrası hassas veri DB'de kalır.
- **Çözüm:** `Employee`, `Payslip`, `VendorBankAccount` için `Anonymize()` + erase cascade adımları; finansal saklama gerekiyorsa maskele + `KeepFinancialTrail`.

### [YÜKSEK] DSAR gönderiminde IDOR + cross-tenant silme riski

- **Konum:** `server/src/CoreAlign.API/Controllers/Privacy/DataSubjectRequestsController.cs:28-29` → `DataSubjectRequestService.cs:46-57` + `PiiAnonymizer`
- **Teşhis:** `Submit`, gövdeden gelen `RequesterUserId`/`RequesterCustomerId`'yi doğrulamadan kaydediyor; işleme yolu yüklenen user'ın `TenantId`'sini işlem yapanla karşılaştırmıyor ve `User` global filtreden muaf → sahte özne-id ile **başka tenant'ın kullanıcısı** silinebilir/dışa aktarılabilir. (Karşılaştır: `EraseCustomerByAdminHandler` tenant kontrolünü yapıyor.)
- **Çözüm:** `Submit`'te subject-id'yi oturum kullanıcısına/tenant'ına bağla; `PiiAnonymizer`'da `TenantId` uyuşmazlığında `NotFoundException`.

### [ORTA] Self-servis hesap silme (`/me/erase`) MFA step-up içermiyor

- **Konum:** `server/src/CoreAlign.API/Controllers/PrivacyController.cs:24`
- **Teşhis:** Admin silme ve admin DSAR `[RequireRecentMfa]` taşırken, geri-alınamaz anonimleştiren `/me/erase` yalnız username onayıyla korunuyor; çalınmış oturumda tek adımda kalıcı silme tetiklenebilir.
- **Çözüm:** `/me/erase`'e `[RequireRecentMfa]` ekle.

### [ORTA] AI Helper reindex endpoint'i Development'ta anonim + rate-limit'siz

- **Konum:** `server/src/CoreAlign.API/Controllers/AiHelperController.cs:142-158`
- **Teşhis:** `admin/reindex` `[AllowAnonymous]`; koruma yalnız `!IsDevelopment() && !IsInRole("TenantAdmin")`. Dev'de tamamen açık ve tüm KB'yi yeniden indeksleyen pahalı işi tetikliyor (DoS/kaynak tüketimi).
- **Çözüm:** `[AllowAnonymous]`'ı kaldır, `[Authorize(Roles="TenantAdmin")]` + rate-limit.

### [ORTA] Rıza kaydı nullable kullanıcı kimliğiyle oluşturuluyor

- **Konum:** `server/src/CoreAlign.Application/Consents/ConsentHandlers.cs:25`
- **Teşhis:** `UserIdOrThrow()` yerine nullable `_currentUser.UserId`; kimliksiz oturumda boş `Guid` ile consent yazılabilir → sahipsiz kayıt, `Withdraw` eşleşmesi bozulur.
- **Çözüm:** `UserIdOrThrow()` kullan; gerçek anonim rıza gerekiyorsa fingerprint tabanlı ayrı akış.

> **Auth çekirdeği sağlam (doğrulandı):** refresh token rotation + reuse-detection tam (reuse'da zincir+session revoke+audit+alert), BCrypt, reset/verify token tek-kullanımlık TTL'li, reset sonrası tüm refresh+session revoke, webhook imzası timing-safe + replay penceresi, doküman-forward IDOR-safe. Tek kırık nokta yukarıdaki e-posta gönderimi (§4).

---

## 6. Cam Mekan (Glass) · Bordro · B2B · Portal

### [KRİTİK] Glass Panel/Run/WorkOrder concurrency token EF'de bağlı değil

- **Konum:** `server/src/CoreAlign.Infrastructure/Persistence/Configurations/GlassEnclosureProjectConfigurations.cs:77-101, 118-134, 300-325`
- **Teşhis:** Üç entity de `ConcurrencyToken` kolonu taşıyor ve DbContext Modified'da otomatik bump'lıyor, ama bu üç config `.IsConcurrencyToken()` **çağırmıyor** (karşılaştır: `GlassProject`/`FieldSurvey`/`GlassWorkOrderRevision` çağırıyor). Token WHERE'e girmeyince EF optimistic concurrency uygulamıyor; `GlassWorkOrder.BomSnapshotTotal` (para) ve status eşzamanlı yazımda sessizce ezilir (§4.6 ihlali).
- **Çözüm:** Üç Configure'a `builder.Property(x => x.ConcurrencyToken).IsConcurrencyToken();` ekle; SQLite `:memory:` çift-context testiyle 409'u kanıtla.

### [KRİTİK] Glass BOM/fiyat toplamı çok-para-birimli kalemleri FX'siz topluyor

- **Konum:** `server/src/CoreAlign.Application/GlassEnclosure/Services/IBOMComposer.cs:127, 188-190, 225`
- **Teşhis:** `profileCost`/`glassCost`/`hardwareCost` kendi katalog para birimlerinde (`GlassType.Currency`, `HardwareItem.Currency`, `ProfileItem.Currency`) toplanıp tek proje para birimi altında `wasteCost` ve grand total'a ekleniyor; hiçbir FX dönüşümü yok. Katalog kalemleri farklı para birimindeyse toplam aritmetik olarak anlamsız.
- **Çözüm:** Her satırı compose anında proje/base para birimine `exchange_rates` (decimal 18,6) ile çevirip topla; ya da katalog para birimini proje ile eşleşmeye zorla (validator).

### [YÜKSEK] Bordro — SGK muafiyet bayrağı (`SgkExempt`) hesapta yok sayılıyor

- **Konum:** `server/src/CoreAlign.Application/Payroll/Runs/PayrollPayslipFactory.cs:24-34` + `Calculation/PayrollCalculationService.cs:12-13`
- **Teşhis:** `SalaryComponent` bağımsız `TaxExempt` ve `SgkExempt` taşıyor ama `ResolveEarnings` yalnız `TaxExempt`'i gross'tan dışlıyor; tek `gross` hem gelir vergisi hem SGK matrahına besleniyor. Sonuç: `SgkExempt=true` kalem yine SGK matrahına giriyor (fazla kesinti); `TaxExempt=true` kalem SGK'ya da girmiyor. Matrahlar ayrılmalı.
- **Çözüm:** Ayrı `sgkBase` ve `incomeTaxBase` biriktir (her bileşenin kendi bayrağına göre), ikisini de calc input'a geçir; muaf-kalem birim testi ekle. (Not: bordro GL dengesi Σdebit=Σcredit doğrulandı — sorun matrah ayrımında.)

### [YÜKSEK] Glass BOMComposer'da N+1 (panel/donanım başına tek tek `GetByIdAsync`)

- **Konum:** `server/src/CoreAlign.Application/GlassEnclosure/Services/IBOMComposer.cs:181, 215`
- **Teşhis:** `foreach panel` içinde `GetByIdAsync(panel.GlassTypeId)` ve kit-item loop'unda `GetByIdAsync(kitItem.HardwareItemId)`; 100 panel = 100+ sorgu (§4.11 ihlali).
- **Çözüm:** Benzersiz id setini `GetByIdsAsync` ile tek sorguda yükle, `Dictionary`'den oku.

### [YÜKSEK] B2B Portal — Bildirim tercihleri ekranı işlevsiz stub

- **Konum:** `apps/b2b/src/pages/ProfilePage.tsx:264-297` (i18n `b2b.profile.notificationsComingSoon`)
- **Teşhis:** "Bildirimler" bölümü 3 tür listeliyor ama her biri statik `<span>` "Varsayılan açık"; `checked/onChange/useMutation` **yok**. Bayi tercihlerini gerçekte değiştiremiyor; "yakında" metni bunu itiraf ediyor.
- **Çözüm:** Gerçek toggle + backend persist ekle ya da yanıltıcı olmayan bir duruma indir.

### [ORTA] Glass BOM satır açıklamaları backend'de hardcoded (lokalize edilemez)

- **Konum:** `server/src/CoreAlign.Application/GlassEnclosure/Services/IBOMComposer.cs:153, 169` (TR) — İngilizce etiketlerle karışık
- **Teşhis:** Kullanıcıya görünen BOM satır adları backend'de sabit TR/EN karışık; çok-dilli tenant'ta yanlış dil, `t()` disiplini dışı.
- **Çözüm:** Satır etiketlerini stabil enum/anahtar olarak sakla, çeviriyi UI'da `t()` ile yap.

### [ORTA] Glass wizard Step4 — "Lazermetre entegrasyonu yakında" placeholder

- **Konum:** `src/features/glass-enclosure/wizard/ui/Step4QuickDimensions.tsx:275`
- **Teşhis:** Boyut adımında kullanıcıya görünen, kurulmamış lazer-ölçüm özelliğine dair "yakında" notu (söz verilmiş-özellik izlenimi).
- **Çözüm:** Backlog net değilse placeholder'ı kaldır.

### [ORTA] Glass FSM — üretim-sonrası hata yolu eksik (`Ready → Defective` yok)

- **Konum:** `server/src/CoreAlign.Domain/Entities/GlassEnclosure/GlassProject.cs:255-273`
- **Teşhis:** `Defective`'e yalnız `InProduction`/`InTransit`'ten geçilebiliyor; `Ready` (sevke hazır) parçada kusur tespit edilirse yol yok, kullanıcı iptal+yeniden-açmak zorunda.
- **Çözüm:** `Ready => ... or Defective` geçişini ekle; her yeni+reddedilen geçiş için test.

### [YÜKSEK] Glass saha keşif formu native `window.prompt/alert` ile veri topluyor (ondalık ölçüm kaybı)

- **Konum:** `src/features/glass-enclosure/ui/FieldSurveyForm.tsx:204, 217, 405-407` (+ `QuoteSummaryView.tsx:173,189`, `ExportMenu.tsx:35,42`, `installation/AcceptanceFormPage.tsx:183` — toplam 11 kullanım/6 dosya)
- **Teşhis:** Ölçüm girişi ve red gerekçesi zincirleme `window.prompt()`, sonuç `window.alert()` ile toplanıyor — dark-mode'suz, validasyonsuz, stillenemez (§2.2/§2.4 ihlali). `parseInt(valueStr,10)` (`:407`) ondalık ölçümü sessizce atıyor (12.5 mm girilemiyor).
- **Çözüm:** `window.*` çağrılarını modal/confirm-dialog + form component'leriyle değiştir; ölçüm için `parseFloat` + inline validasyon.

### [DÜŞÜK] Glass BOM satır miktar/maliyet negatif-guard yok

- **Konum:** `server/src/CoreAlign.Domain/Entities/GlassEnclosure/GlassProjectBOMLine.cs:49, 84`
- **Teşhis:** `Quantity`/`UnitCost` negatif/sıfır kontrolü yok; hatalı override negatif satır maliyeti üretip proje toplamını sessizce bozar.
- **Çözüm:** Domain guard + idempotent `CHECK (quantity >= 0 AND unit_cost >= 0)`.

### [DÜŞÜK] Glass nakliye maliyetinde mesafe çarpanı yok

- **Konum:** `server/src/CoreAlign.Application/GlassEnclosure/Services/IBOMComposer.cs` (transportCost)
- **Teşhis:** `TransportRatePerKm` bir mesafeyle çarpılmadan doğrudan ekleniyor → mesafeden bağımsız sabit nakliye.
- **Çözüm:** `distanceKm` girdisi al ve `+ distanceKm * TransportRatePerKm`; mesafe yoksa alanı `TransportFlatFee` olarak yeniden adlandır.

> **Portal IDOR temiz (doğrulandı):** B2B/Customer portal derin denetiminde scope sızıntısı bulunamadı — `PortalScopeService` partiyi JWT claim'inden çözüyor, tüm handler'lar entity sahipliğini doğruluyor, cross-party 404 dönüyor. DashboardStatsRepository agregasyonu server-side; Warranty/Installation tam implementasyon.

---

## 7. Frontend — 4 yüzey + build gate'leri

- **Tip kontrolü (temiz worktree):** admin + customer-portal + b2b `tsc --noEmit` → **0 hata**. Mount üzerinde görülen TS hataları truncation artefaktıydı (§0), gerçek kodda yok.
- **i18n paritesi (pozitif):** tr↔en anahtar farkı **sıfır** — admin 6332/6332, customer-portal 392/392, b2b 299/299, mobil 104/104. Admin JSX'inde t() dışı hardcoded TR metin bulunamadı.
- **Hijyen (pozitif):** kaynakta `console.*` = 0 (yalnız `logger.ts`), `@ts-ignore` yalnız NSwag-üretimi client'ta, boş `catch{}` = 0, `TODO/FIXME` = 0.
- **Gerçek fonksiyonel frontend eksikleri:** B2B bildirim tercih stub'ı (§6), glass native `window.prompt/alert` + ondalık kaybı (§6), glass "lazer yakında" placeholder (§6).

### [ORTA] Ölü i18n anahtarları (tanımlı, hiç render edilmiyor)

- **Konum:** admin `src/app/i18n/locales/{tr,en}.json` — `...mailNotice` (~1328), `...warehousesComingSoon` (~1145)
- **Teşhis:** İkisinin de 0 kod referansı var. `warehousesComingSoon` artık gereksiz (gerçek `StockByWarehouseTab` render ediliyor); `mailNotice` kaldırılmış mail-UI'sinden kalmış.
- **Çözüm:** İkisini tr+en'den sil; kalan "yakında" anahtarlarını referans kontrolünden geçir.

### [DÜŞÜK] Vitest bu ortamda koşulamadı (ortam sınırı)

- **Konum:** komut `npm run test` — tüm yüzeyler
- **Teşhis:** `node_modules` Windows kurulumu (linux rollup/esbuild binary yok) → Vitest patlıyor; testlerin yeşil/kırmızı durumu bu ortamda doğrulanamadı (kod kusuru değil).
- **Çözüm:** Testleri Windows host'ta veya temiz `npm install`'lı Linux'ta koş.

### [DÜŞÜK] NSwag client'ları 9 Haziran spec'inden — backend değiştiyse yeniden üret

- **Konum:** `src/shared/api/EMCM.Client.ts`, `openapi/v1*.json`
- **Teşhis:** Client'lar yüzey-scope'lu ve tutarlı; ama specler 9 Haz tarihli. O tarihten sonra backend endpoint değiştiyse tip drift oluşur.
- **Çözüm:** Backend değişikliği olduysa `nswag.json` ile yeniden üret.

---

## 8. Dokümantasyon drift'i (CLAUDE.md/INVARIANTS güncel değil)

Yanlış "kural" da gelecekte hataya yol açar — bunlar düzeltilmeli:

- **three.js "r128 sınırı / CapsuleGeometry yasak / OrbitControls CDN'de yok":** Repo gerçekte `three@0.183.0` + `@react-three/fiber@9` + `@react-three/drei@10` kullanıyor (`package.json:45-74`); `OrbitControls` (`SceneViewport.tsx:201`) ve `CapsuleGeometry` bu sürümde MEVCUT. Kural bayat.
- **sprint13 "D-3 Background outbox drain is not tenant-aware":** Artık **düzeltilmiş** — `OutboxRepository.GetDueAcrossTenantsAsync` `IgnoreQueryFilters()` + `OutboxProcessor.DrainAsync` her mesajda `_tenantContext.PushScope(message.TenantId)` yapıyor. Blocker doc güncellenmeli.
- **`IXminConcurrency` PG18'de NO-OP** notu doğru; ama bazı blocker maddeleri (StockItem concurrency, MRP snapshot reconcile) RESOLVED olarak işaretli — açık sanılıp tekrar ele alınmamalı.

---

## 9. Doğrulanan sağlam alanlar (bunları BOZMA)

Denetimde yanlış-pozitif olarak elenen veya sağlam bulunan yerler — refaktör dürtüsüne karşı koru:

- **Finansal çekirdek:** withholding GL dengesi, credit-note reversal, `Payment.Apply` self-dedup, ApplyVendorPayment natural-key idempotency, ledger insert'lerinde `pg_advisory_xact_lock` (müşteri+satıcı), 23505/23503/concurrency→409 mapping, tüm yarışan finansal entity'lerde `IHasConcurrencyToken`, FX scale tekliği (`numeric(18,6)`), para tipi (`numeric(18,4)`).
- **Sipariş çekirdeği:** sipariş-onay→stok-düşüş atomik (stok yetersizse tüm confirm rollback), VendorBilling 3-way match + PPV/GRIR reversal, ReceivePurchaseOrder idempotency, BulkOrderAction izole hata. **MRP-BUG-5 (convert-to-PO tax/FX kaybı) RESOLVED.**
- **Stok/MRP:** negatif stok reddi hem domain hem handler, `StockItem` token+unique index, StockCount canlı-reconcile + `(product,lot)` batch, LotSizing/DemandForecaster (SES/Holt/Holt-Winters + z·σ·√LT) matematiksel doğru.
- **Auth/güvenlik:** refresh rotation + reuse-detect, BCrypt, tek-kullanımlık token, webhook timing-safe imza, doküman-forward IDOR-safe, commit edilmiş secret yok.
- **Frontend:** 3 SPA tip-temiz, i18n tam senkron, console/ts-ignore/hardcoded-metin yok.

---

## 10. Önceliklendirilmiş aksiyon planı

**Hafta 1 — hukuki/para/kritik akış (durdurucu):**

1. `IEmailService` no-op'unu gerçek SMTP'ye bağla (§4-1) — şifre sıfırlama olmadan ürün canlıya çıkamaz.
2. E-fatura satıcı bilgisini tenant ayarlarından doldur (§4-2) — yasal belge kesilemiyor.
3. DSAR access/portability gerçek implementasyon + cross-tenant/IDOR guard (§5-1, §5-4) + erasure PII kapsamı (§5-3) — KVKK.
4. Consent enforcement (§5-2) — KVKK.
5. Dispatched shipment iptali stok/COGS telafisi + FSM tek-guard (§2-1, §2-2); hasarlı iade restock (§2-3).

**Hafta 2 — para/stok doğruluğu:** 6. FX cache tenant-scope (§1-2); VoidPayment idempotency (§1-1); Product concurrency/oversell (§3-1); Glass concurrency wiring (§6-1) + multi-currency BOM FX (§6-2); bordro SGK matrah ayrımı (§6-3). 7. journal denge CHECK + percent CHECK'ler (§1-3, §1-6); GL journal-number advisory lock (§1-4).

**Hafta 3 — işlevsel eksik/ölçek:** 8. MRP release Make akışı (§3-2) + MrpService tarih kolonu (§3-3) + unbounded/ N+1 (§3-4, §3-5, §4-N+1). 9. Zamanlı rapor/audit export teslimi + EmailQueued handler kaydı (§4). 10. GetById 404 düzeltmeleri, reorder alan kayıpları, revizyon snapshot (§2).

**Sürekli:** `.gitattributes` (eol=lf) ekle (§0); dokümantasyon drift'ini düzelt (§8); her düzeltmeye cross-tenant + idempotency + FSM testi (§8.2).

---

_Bu rapor statik analiz + hedefli doğrulama ile üretildi. Runtime doğrulaması (özellikle e-posta/e-fatura/DSAR akışları) düzeltme sonrası entegrasyon testiyle yapılmalı._
