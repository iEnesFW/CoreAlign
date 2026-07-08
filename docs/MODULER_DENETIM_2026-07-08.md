# CoreAlign — Modüler Kod Denetimi (Taze Tarama)

**Tarih:** 2026-07-08
**Kapsam:** Tüm backend modülleri + 4 frontend yüzeyi + DB/migration/domain. Son ~10 günde değişen dosyalara ağırlık verildi.
**Yöntem:** 6 paralel derin-denetim ajanı (para, sipariş, stok/ürün/MRP, cam modülü, entegrasyon/auth/KVKK, DB/migration). Kod AKTİF geliştirildiği için önce **kalibrasyon** yapıldı (neler düzeldi), sonra güncel koda karşı taze tarama. En kritik bulgular gerçek koda karşı elle doğrulandı. .NET SDK bu ortamda yok → backend statik analiz.

---

## 0. Kalibrasyon — önceki denetimden bu yana DÜZELENLER

Önceki iki rapordaki (genel + cam) birçok bulgu koda inmiş. Doğrulananlar (tekrar raporlanmadı):

- **E-posta artık gerçekten gönderiliyor** — `EmailService` no-op değil, `IEmailSender`'a delege ediyor (şifre-sıfırlama/doğrulama çalışıyor).
- **Rıza (consent) gating eklendi** — `NotificationDispatcher.HasMarketingConsentAsync` fail-closed; rızasız pazarlama bildirimi bloklanıyor.
- **E-fatura satıcı bilgisi** artık `BuildSellerParty(tenant)` ile tenant'tan (VKN/vergi dairesi/adres) doldruluyor — **ama yalnız faturada; e-İrsaliye'de hâlâ hardcoded (§7'de KRİTİK).**
- **Cam modülü**: pen drawing-intent (opening|glassPanel|divide), freehand pen, panel hardware→BOM, canlı maliyet `bom/preview` endpoint'i, arc-end hizalama, hardware offset clamp, i18n anahtarları, mojibake — hepsi landed. **Ama bir kısmı yarım/yanlış implementasyon (§6).**
- **Sipariş/sevkiyat**: dispatched-cancel stok kaybı, restockable iade guard'ı, reorder alan kaybı, GetById 200-null — düzelmiş.
- **Cam ticaret**: VAT konfigüre edilebilir, convert-time stok over-commit (soft) engellendi, nesting net-alan utilization, shaped panel siluet — landed.

**Sonuç:** Ekip hızlı ve doğru yönde ilerliyor. Ancak taze tarama, hem **yeni kodda regresyonlar** hem de **yarım kalan özellikler** hem de **daha önce dokunulmamış alanlarda derin sorunlar** ortaya çıkardı.

---

## 1. Yönetici Özeti — bu turun kesişen teması: "yarım kalan özellikler"

Bu turun en tehlikeli sınıfı **kısmen implemente edilmiş, "bitti" görünen ama fiilen çalışmayan** özellikler. Kullanıcıya/geliştiriciye tamamlanmış izlenimi verdikleri için sessiz veri/para hatası riski taşıyorlar:

1. **Çoklu-döviz cari bakiyesi yanlış.** `LedgerPostingHelpers.PostAsync` kur=`1m` sabitliyor; yabancı-para fatura/tahsilatta müşteri/tedarikçi cari bakiyesi karışık-birim toplamı oluyor. Gelen e-fatura da `ExchangeRate=1` ile kaydediliyor. Tenant FX override ise repo bug'ı yüzünden **ölü** (hiç uygulanmıyor). → §2.
2. **Cam↔host kalıcı bağı yarım.** `hostWallId` alanı tanımlı ve okunuyor ama **hiçbir yerde atanmıyor** (doğrulandı) → "persistent bond" fiilen yok, yalnız aynı-işlem co-move çalışıyor; 3D stretch-gizmo bağı tamamen bypass ediyor. → §6.
3. **Fire (scrap) özelliği FE-only.** Tam frontend akışı var ama backend endpoint/handler yok → buton 404. → §3.
4. **DSAR export gerçekten saklanmıyor.** `MarkCompleted(Guid.NewGuid())` sahte dosya id'si veriyor; ayrıca sahiplik (owner) kontrolü yok → tenant-içi IDOR. `keepFinancialTrail` bayrağı hiçbir davranışı etkilemiyor. → §7.
5. **Merkezi fiyatlandırma iç satışa bağlı değil.** `IPricingService` yalnız portal akışlarında; Orders/Quotes fiyat/vergi/indirimi hep kullanıcı girdisinden alıyor. `ResolveTaxAsync`/`ResolveDiscountAsync` hiç çağrılmıyor. → §5.

Bunların yanında **yeni-kod regresyonları** (partial-payment cari çift-credit, order→invoice kredi/FX guard eksik, FIFO transfer değer sızıntısı, seri sevkiyat stok defterine dokunmuyor, divide guard arc/köşe panelleri kaçırıyor) ve **DB borçları** (yeni entity'lerde FK/CHECK/concurrency eksik) var.

**Önem etiketleri:** KRİTİK = para/stok/hukuki bütünlük veya çalışmayan çekirdek akış · YÜKSEK = ciddi işlevsel hata/eksik · ORTA = doğruluk/UX/ölçek · DÜŞÜK = hijyen/borç. `(YENİ KOD)` = son ~10 günde değişen dosyada.

---

## 2. Para · Muhasebe · Fatura · Ödeme · FX

### [KRİTİK] Çoklu-döviz cari bakiyesi bozuk — ledger helper kur=1 sabitliyor, running balance belge para biriminde (YENİ KOD)

- **Konum:** `server/src/CoreAlign.Application/Invoices/EventHandlers/InvoiceLedgerHandlers.cs:28,36` (**doğrulandı**: ctor'a `1m` geçiliyor, `signed = Math.Abs(amount)` foreign) + `server/src/CoreAlign.Infrastructure/Repositories/PaymentRepository.cs:175-176,190`
- **Teşhis:** `CustomerLedgerEntry` ctor `AmountInBase = Amount * ExchangeRate`'i doğru hesaplıyor ama helper her çağrıda `exchangeRate: 1m` veriyor → USD/EUR faturada `AmountInBase == foreign Amount`. `RunningBalanceAfter` ve `Customer.CurrentBalance` belge-para-birimi tutarlarının karışık toplamı; 120 kontrol hesabıyla (TRY) mutabakat tutmaz. Vendor tarafında (`VendorLedgerPoster`) aynı hata.
- **Öneri:** Helper'a gerçek `exchangeRate`'i (invoice/payment `ExchangeRate` veya FX snapshot) geçir; `signed`'i `entry.AmountInBase` üzerinden hesapla; balance sorguları `AmountInBase` toplasın. Cari tek base para biriminde tutulmalı.

### [KRİTİK] Kısmî tahsilatta cari'ye ÇİFT alacak — PaymentConfirmed + InvoicePartiallyPaid ikisi de credit yazıyor

- **Konum:** `server/src/CoreAlign.Application/Invoices/EventHandlers/InvoiceLedgerHandlers.cs:91-105` vs `:209-245`
- **Teşhis:** `CreatePaymentHandler` tek akışta hem `payment.Confirm()` (→tam tutar credit) hem `invoice.RecordPayment()` (→uygulanan tutar credit) fırlatıyor. Kısmî ödemede cari iki kez alacaklanıp bakiye olması gerekenin altına düşüyor. Tam ödeme yolu (`InvoicePaidLedgerHandler`) cari'ye yazmıyor — asimetri, partial'ın ledger post'u fazlalık.
- **Öneri:** Cari alacak tek kaynaktan gelsin — `InvoicePartiallyPaidLedgerHandler`'ın ledger post'unu kaldır (yalnız fatura-durum sinyali kalsın). "1 tahsilat = 1 cari credit" entegrasyon testi ekle.

### [KRİTİK] Sipariş→Fatura yolunda kredi limiti + FX snapshot YOK (standalone ile parite değil)

- **Konum:** `server/src/CoreAlign.Application/Invoices/Handlers/GenerateInvoiceFromOrderCommandHandler.cs:46-150`
- **Teşhis:** `CreateStandaloneInvoiceCommandHandler` `ICreditLimitGuard` + FX resolver uyguluyor; order→invoice handler ikisini de yapmıyor, yalnız `order.ExchangeRate`'i (sipariş anındaki kur) kopyalıyor. Kredi limiti aşan müşteriye order üzerinden fatura kesilebilir; FX snapshot'sız fatura yanlış TRY karşılığı verir.
- **Öneri:** Order→invoice handler'a `ICreditLimitGuard` + FX resolver enjekte et; `Issue` öncesi limit + (currency≠TRY ise) fatura tarihinde FX snapshot — standalone ile paritede.

### [KRİTİK] Tedarikçi tevkifatı (stopaj) muhasebeleşmiyor — AP brüt kalıyor, "360 Ödenecek Vergiler" yok

- **Konum:** `server/src/CoreAlign.Application/Purchasing/VendorBillingHandlers.cs` (`VendorGLLines.Bill`), rol eşlemesi `GLPostingService.cs:48`
- **Teşhis:** Tedarikçi faturası GL'i yalnız `DR gider/GRIR+191 / CR 320 AP`; tevkifat legi yok. Tek withholding rolü `WithholdingReceivable→193` (aktif, sales için doğru), ama vendor tevkifatının gerektirdiği **pasif 360** rolü yok. Tevkifatlı alışta AP brüt kalır, ödenecek stopaj kayda geçmez → tedarikçiye brüt ödeme riski.
- **Öneri:** `GLPostingKey.WithholdingPayable(→"360")` rolü + VendorBill'e withholding alanları + posting'e `CR 360` legi; AP kredisi net (`Total − withholding`).

### [YÜKSEK] Tenant FX override yapısal olarak ölü — resolver sessizce global kura düşüyor

- **Konum:** `server/src/CoreAlign.Infrastructure/Fx/TenantOverrideFxProvider.cs:24-30` + `PlatformFxAuditRepositories.cs:91-103`
- **Teşhis:** `TryGetTenantOverrideAsync` repo metodunu çağırıp bellekte `.Where(TenantId==tenantId)` filtreliyor; ama o metod SQL'de sabit `.Where(TenantId==Guid.Empty)` uyguluyor → yalnız global döner, tenant override satırı hiç gelmez, `FirstOrDefault()` daima null. Tenant'ın pazarlıklı kuru **hiç uygulanmıyor**.
- **Öneri:** Override'a özel repo metodu: `IgnoreQueryFilters` + açık `TenantId==tenantId && Source==Override && ValidOnDate<=asOf`.

### [YÜKSEK] Yabancı para gelen e-Fatura → VendorBill `ExchangeRate=1` ile dönüşüyor (YENİ KOD)

- **Konum:** `server/src/CoreAlign.Application/IncomingInvoices/IncomingInvoices.cs:148-164`
- **Teşhis:** `ProcessIncomingInvoiceCommand` `Currency`'yi taşıyor ama ExchangeRate hiç çözülmüyor; `new VendorBill(...)` `exchangeRate` vermiyor → default `1m`. USD/EUR fatura `Currency="USD"` ama kur=1 → TRY karşılığı ve AP/GL değerlemesi yanlış.
- **Öneri:** `IFxRateProvider` enjekte et; currency≠TRY ise `IssueDate`'te kuru çöz, bulunamazsa `DomainException` (sessizce 1 kullanma).

### [YÜKSEK] Avans mahsubu inline GL postluyor ve GLPostingResult'ı yok sayıyor — kapalı dönem/eşlenmemiş hesapta sessiz GL kaybı (YENİ KOD)

- **Konum:** `server/src/CoreAlign.Application/Payments/Handlers/PaymentCommandHandlers.cs:253-265` (`OffsetCustomerAdvanceHandler`)
- **Teşhis:** Diğer tüm GL postlamaları outbox'tan (`IGLPostingOutbox`) giderken avans mahsubu `_gl.PostAsync`'i inline çağırıp sonucu atıyor. `PostAsync` kapalı dönemde `SkippedClosedPeriod`, eşlenmemiş hesapta `SkippedUnmapped` döner ve GL oluşturmaz — ama `payment.Apply` + AR azaltımı zaten yapıldı → avans "tüketilmiş" ama muhasebe yansımamış.
- **Öneri:** Avans mahsubunu `IGLPostingOutbox`'a taşı (retry) veya inline sonucu kontrol et: `Skipped*` dönerse telafi/ret ile AR azaltımını GL'siz bırakma.

### [YÜKSEK] Fatura listesi KPI'ları ve durum filtreleri yalnız MEVCUT SAYFA üzerinden hesaplanıyor (FE)

- **Konum:** `src/pages/invoices/InvoicesPage.tsx:122-164,227-269,287-288`
- **Teşhis:** Liste server-side sayfalı ama `outstanding/overdue/paidTotal` ve durum sayaçları tek sayfadan `forEach` ile toplanıyor; bucket/dueSoon filtresi client-side. "Overdue" seçilince yalnız o sayfadakiler görünür, CSV export sadece görünen sayfayı indirir → yanıltıcı özet + veri kaybı.
- **Öneri:** Bucket/dueSoon'u backend query'sine, KPI toplamlarını server-side aggregate endpoint'e taşı; export için tüm-sonuç endpoint'i.

### [YÜKSEK] Recurring fatura üretiminde per-dönem idempotency yok — dış transaction başarısızsa çift fatura (YENİ KOD)

- **Konum:** `server/src/CoreAlign.Application/Invoices/Recurring/Handlers/RunRecurringInvoiceNowCommandHandler.cs:46-49` + `RecurringInvoiceRunner.RunOnceAsync:101-103`
- **Teşhis:** İç `CreateStandaloneInvoiceCommand` faturayı commit ediyor; `RecordOccurrence(periodKey, dto.Id)` sonra dış save'de yazılıyor. Arada hata olursa fatura kalıcı ama occurrence ilerlememiş → job aynı dönem için ikinci faturayı üretir.
- **Öneri:** Üretilen faturaya `(template_id, period_key)` unique kısıtı (retry 23505→zaten-var) veya occurrence'ı üretimle aynı transaction'a al; job'da period-key advisory lock.

### Orta/Düşük (para) — özet

- **[ORTA] PendingApproval (3-way hold) tedarikçi faturasına ödeme uygulanabiliyor** (`VendorBillingHandlers.cs:580,919`, `VendorBill.RecordPayment:166`) — GL'ye postlanmadan ödeme → negatif AP. → ödeme guard'larına `PendingApproval` ekle.
- **[ORTA] FE'de para `number` (float) ile aritmetik** (`PaymentCreateModal.tsx:119-124`, `ProcessIncomingInvoiceModal.tsx:110-123`) — kuruş sapması; epsilon 0.001 zaten farkında. → minor-unit int veya round; over-allocation'ı backend doğrulasın.
- **[ORTA] TCMB feed yalnız ForexSelling saklıyor, okuma BuyingRate döndürüyor** (`TcmbFxIngestJob.cs:56,64`, `FxRateProvider.cs:137`) — alış/satış ayrımı çökmüş. → tek "resmi kur" kabul et ya da buying/selling ayrı kolon.
- **[ORTA] `Money.ToBaseCurrency`/VendorBill/Invoice/Payment kur≤0'da sessizce 1:1'e düşüyor** (`Money.cs:56`, `VendorBill.cs:60`) — eksik kuru maskeliyor. → yabancı-para yolunda kur≤0 iken throw.
- **[ORTA] `VendorPayment.IsDraft` postlanma durumunu yok sayıyor** (`VendorPayment.cs:33`) — postlanmış avans UpdateDraft'a açık. → `!IsPosted` ekle.
- **[ORTA] FE component içinde doğrudan try/catch** (`PaymentCreateModal.tsx:154`, `AdvanceOffsetModal.tsx:130`, `CustomerLedgerTab.tsx:70`) — safeRequest atlanıyor. → `safeRequestWithNotify`.
- **[DÜŞÜK]** Gelen fatura fetch job catch-all yutuyor + concurrent 23505 riski (`IncomingInvoiceFetchJob.cs:78`); Dunning alt-sınırsız + tekrarlı bildirim (`DunningReminderDataSource.cs:52`); `document_sequences` `NextNumber++` (advisory-lock ile korunuyor ama §4.6 lafzına aykırı); `exchange_rates` PK v4 GUID (§4.3 v7 ister); fmtCurrency 6+ dosyada kopya.

---

## 3. Sipariş · Teklif · Satınalma · İade · Sevkiyat

### [KRİTİK] Fire (scrap) uçtan uca kopuk — tam FE özelliği, BE endpoint/handler YOK (YENİ KOD)

- **Konum:** `src/features/orders/api/ordersApi.ts:194` (`POST /orders/{id}/scrap`) + `OrderScrapModal.tsx` + `useOrderQueries.ts:217`; BE: `OrdersController.cs` (scrap endpoint yok), `Application/Orders` altında `ScrapOrderLineCommand`/handler yok. Domain `Order.RecordLineScrap` (`Order.cs:648`) mevcut ama wire edilmemiş.
- **Teşhis:** "Fire Kaydet" butonu 404/405 döner; özellik tamamen çalışmaz. Ayrıca fire hiçbir `StockMovement` + fire-zararı GL üretmiyor.
- **Öneri:** `ScrapOrderLineCommand(...) : ITransactionalRequest` + handler (domain `RecordLineScrap` + stok düşüşü + fire GL) + slim controller `[HttpPost("{id}/scrap")]`; DTO'yu BE ile hizala.

### [KRİTİK] `ShipmentStatus.Returned` ölü durum — iade edilen sevkiyat asla kapanmıyor

- **Konum:** `server/src/CoreAlign.Domain/Entities/Shipment.cs:171` (FSM `Dispatched→Returned` izinli) + `Enums/ShipmentStatus.cs`
- **Teşhis:** FSM `Returned`'a izin veriyor ama kod tabanında bu geçişi çağıran hiçbir yer yok (`MarkReturned` metodu bile yok). İade akışı stoğu geri koyuyor ama Shipment `Dispatched`'te kalıyor → iade edilen mal "yolda/teslim" görünür, lojistik/e-irsaliye raporu tutarsız.
- **Öneri:** `Shipment.MarkReturned(reason)` + `ReceiveReturnedItemsCommandHandler`'da iade edilen order line'lara bağlı sevkiyatları `MarkReturned` yap; ya da durumu kullanmıyorsan FSM+enum'dan kaldır.

### [YÜKSEK] QC-hold mal kabulünde PO "Received" ama stok yok → 3-way match & GR/IR dengesizliği (YENİ KOD)

- **Konum:** `server/src/CoreAlign.Application/Purchasing/PurchaseOrderHandlers.cs:372-381` (QC gerekiyorsa `ApplyStockAndGlAsync` atlanır) + `VendorBillingHandlers.cs:192-193`
- **Teşhis:** `requiresQc` iken `QuantityReceived` ilerliyor (PO Received olur) ama stok+GR/IR clearing QC-approve'a erteleniyor. Bu arada fatura kesilirse 3-way match henüz stoğa girmemiş miktara karşı geçer, GR/IR clearing tek taraflı borçlanır; QC reddedilirse 322 borcu asılı kalır.
- **Öneri:** QC-pending'te `QuantityReceived`'ı ilerletme (ayrı `QuantityAwaitingQc` tut); 3-way match yalnız QC-onaylı/stoğa girmiş miktara karşı çalışsın.

### [YÜKSEK] `CreateShipment` idempotency/kilit yok → eşzamanlı çift sevkiyat ile aşırı-sevk

- **Konum:** `server/src/CoreAlign.Application/Shipments/Handlers/ShipmentHandlers.cs:32-81`
- **Teşhis:** `QuantityRemainingToShip` kontrolü var ama idempotency key/advisory lock yok; iki eşzamanlı istek aynı snapshot'ı okuyup ikisi de geçebilir → aşırı-sevk. `OrderLine.RecordShipment` üst-sınır guard'ı taşımıyor.
- **Öneri:** `CreateShipmentCommand`'e `OperationId` + replay guard; veya order başına advisory lock; `RecordShipment`'e `QuantityShipped+qty <= Quantity` guard'ı.

### [YÜKSEK] `ApproveOrder` gövdeyi yok sayıyor; toplu onayda `ApprovedByUserId = Guid.Empty` (audit boşluğu) (YENİ KOD)

- **Konum:** `OrdersController.cs:106-107` + `BulkOrderActionCommand.cs:63` + `OrderFsmHandlers.cs:36`
- **Teşhis:** Tekil onay body'yi atıp `CurrentUserId` kuruyor (kafa karıştırıcı kontrat). Toplu onay `new ApproveOrderCommand(orderId)` → `ApprovedByUserId=null` → `order.Approve(Guid.Empty)`: kim onayladı bilgisi kayıp.
- **Öneri:** `BulkOrderActionCommandHandler`'a `ICurrentUserAccessor` enjekte edip `currentUserId` geçir; controller body kabul etmeyecekse imzadan kaldır.

### [ORTA] E-İrsaliye SellerParty hardcoded — bkz. §7 (KRİTİK, iki ajan bağımsız doğruladı, `EDespatch.cs:169`).

### Orta/Düşük (sipariş) — özet

- **[ORTA] `Order.ApplyRevision` yalnız ProductId eşleşen satırı günceller** (`Order.cs:594-619`) — yeni/silinen satır sessizce yutulur, aynı ProductId'den ikincisi güncellenmez. → LineNumber tabanlı reconcile veya validator'da "yalnız fiyat/miktar" kısıtı.
- **[ORTA] Receive validator sıfır-miktarlı satırları geçiriyor** (`PurchaseOrderValidators.cs:28`) — boş GRN + sequence israfı. → `Must(l => l.Any(x => x.Quantity>0))`.
- **[ORTA] Quote→Order convert `OrderType/Source` sabit `Standard/Manual`** (`ConvertQuoteToOrderCommandHandler.cs:64`) — teklif niyeti (Sample/Blanket) taşınmıyor. Tax/FX/fiyat doğru taşınıyor (regresyon yok). → Quote'a Type/Source ekle veya Source=Quote.
- **[DÜŞÜK] Confirm-decrement ile Reserve→Dispatch-consume iki ayrı stok yolu** (`OrderStockEffectHandlers.cs:126` vs `ShipmentHandlers.cs:142`) — bugün çift-sayım yok (consume 0 bulur) ama koruyucu invariant yok; ön-kontrol (global StockQuantity) ile asıl düşüş (warehouse ATP) sapıyor. → Confirmed sipariş için sevkiyatı engelle veya tek stok-etki servisi.
- **[DÜŞÜK] `OrderScrapModal` hardcoded TR `defaultValue`** (§1.4) — scrap özelliğiyle birlikte ele al.

---

## 4. Stok · Envanter · Ürün · Fiyatlandırma · MRP

### [KRİTİK] FIFO ürünlerde depo-arası transfer değer-nötrlüğünü bozuyor

- **Konum:** `server/src/CoreAlign.Infrastructure/Services/AllocationService.cs:371-409` (`ApplyTransferAsync`)
- **Teşhis:** `unitCost` çıkış bacağı çalışmadan önce `source.AvgCost`'tan okunuyor; FIFO'da gerçek çıkış maliyeti katman-bazında hesaplanıp `transferOut.UnitCost`'a yazılıyor ama varış bacağı (`ApplyReceiptAsync`) bu doğru değeri değil bayat `AvgCost`'u kullanıyor → transfer değer-nötr değil (envanter değeri yaratılıyor/yok oluyor), hedefteki yeni FIFO katmanı da yanlış maliyetle açılıp hatayı yayıyor.
- **Öneri:** Varış bacağında `transferOut.UnitCost`'u kullan (WeightedAverage'da aynı sonuç, FIFO'da doğru katman maliyeti).

### [KRİTİK] Merkezi fiyatlandırma iç satışa (Orders/Quotes) hiç bağlı değil; `ResolveTax/Discount` hiç çağrılmıyor

- **Konum:** `server/src/CoreAlign.Infrastructure/Services/PricingService.cs:140-200` + `IPricingService`'in tek çağrıldığı yerler CustomerPortal/DealerPortal handler'ları
- **Teşhis:** `IPricingService.ResolveTaxAsync`/`ResolveDiscountAsync` hiçbir handler'dan çağrılmıyor (grep doğruladı); `PriceResolutionResult.TaxRatePercent` üç yolda da sabit `0m`. İç Order/Quote fiyat/indirim/vergiyi tamamen kullanıcı girdisinden alıyor — PriceList/DiscountRule/TaxRule/CustomerProductPrice hiç çözümlenmiyor. Kurulan altyapı iki portal akışıyla sınırlı.
- **Öneri:** İç Order/Quote handler'larına `ResolveAsync` çağrısı ekle (öneri/varsayılan), `IsManualPriceOverride` ile elle değiştirme; `ResolveAsync` içinde tax/discount'ı birleştir.

### [KRİTİK] `PricingService.ResolveBatchAsync` sıralı foreach N+1 — katalog sayfası başına 300-400 round-trip

- **Konum:** `server/src/CoreAlign.Infrastructure/Services/PricingService.cs:115-123, 42-113`
- **Teşhis:** `ResolveBatchAsync` her istek için sırayla `ResolveAsync` çağırıyor; her biri ayrı ürün+müşteri `GetByIdAsync`, CPP, price-list sorgusu. `ListCustomer/DealerCatalogProducts` her sayfa yüklemesinde (100 ürün) çağırıyor.
- **Öneri:** Gerçek batch: müşteri bir kez, ürünler `GetByIdsAsync`, CPP müşteri başına tek sorgu, price-list bir kez → bellek-içi dictionary.

### [KRİTİK] Product/ProductVariant concurrency token client'a ulaşmıyor → 409 guard'ı pratikte çalışmıyor (YENİ KOD)

- **Konum:** `Products/DTOs/ProductDtos.cs` (token yok), `Commands/ProductCommands.cs:56-102`, `Handlers/UpdateProductCommandHandler.cs:23`
- **Teşhis:** `Product.ConcurrencyToken` EF'te yapılandırılmış ama `ProductDto` dışarı vermiyor, `UpdateProductCommand` içeri almıyor; handler taze `GetByIdAsync` ile yükleyip mutasyona uğratıyor → EF original-value her zaman güncel DB ile eşleşiyor, stale-submit senaryosu HİÇ 409 üretmiyor. `ProductVariant`, FE tipleri de aynı.
- **Öneri:** DTO'lara `ConcurrencyToken` ekle; command'lara `ExpectedConcurrencyToken`; handler'da `Entry(p).Property(x=>x.ConcurrencyToken).OriginalValue = request.Expected...`; FE submit'te geri gönder.

### [YÜKSEK] Seri numarası sevkiyatı miktar defterine hiç dokunmuyor (YENİ KOD)

- **Konum:** `server/src/CoreAlign.Application/Inventory/Serials/SerialHandlers.cs:56-82` (`ShipSerialUnitsCommandHandler`)
- **Teşhis:** Yalnız `SerialUnit.Status=Shipped` yapıyor; `ApplyIssueAsync`/`StockItem.OnHand`/`StockMovement`/`Product.StockQuantity`/FIFO katmana dokunmuyor → seri-soyağacı ile miktar defteri desync.
- **Öneri:** Ship/Register'ı `ApplyIssueAsync`/`ApplyReceiptAsync` ile aynı transaction'da tetikle veya reconciliation kontrolü.

### [YÜKSEK] `CostingMethod.Standard` seçilebiliyor ama sessizce ağırlıklı-ortalamaya düşüyor; FIFO katmanlı üründe yöntem sessizce değişebiliyor (YENİ KOD)

- **Konum:** `InventoryCostingService.cs:25-29` (`!= Fifo` → WA dalı), `Product.SetCostingMethod` (`Product.cs:276`), validator'da CostingMethod kuralı yok (`ProductValidators.cs`)
- **Teşhis:** `Standard` seçen tenant farkında olmadan `AvgCost` ile maliyetleniyor, variance kaydı yok, hata da yok. Ayrıca FIFO katman biriktirmiş ürünün yöntemi tek PUT ile değişebiliyor (geçmiş katman kontrolü yok). FE "Standard"ı gizliyor ama validator API-direct girişi engellemiyor.
- **Öneri:** `Standard` için gerçek mantık+variance GL veya validator'da reddet; `SetCostingMethod`'a açık FIFO-katman guard'ı (`ConflictException`).

### [YÜKSEK] MRP "tüm katalogu belleğe yükleme" anti-pattern'i asıl sık çağrılan yolda (RunPreviewAsync) hâlâ duruyor

- **Konum:** `server/src/CoreAlign.Infrastructure/Mrp/Planning/MrpPlanningDataLoader.cs:41-43,51,64,110-113`
- **Teşhis:** Önceki fix `MrpService` dashboard/suggestion'ı keyset'e çevirdi ama aynı `_db.Products.Where(...).ToListAsync()` `MrpPlanningDataLoader.LoadAsync`'te (preview/commit/change-impact/capacity'nin HEPSİNİN kaynağı, çok daha sık) aynen duruyor; tüm katalog sonrası stok/vendor/BOM/demand sorgularına sınırsız liste geçiyor.
- **Öneri:** `LoadAsync`'i de keyset-batch'e çevir (aynı desen).

### [YÜKSEK] PriceList/PriceListItem/DiscountRule/TaxRule concurrency token yok + tier ekleme TOCTOU

- **Konum:** `PriceList.cs`, `PriceListItem.cs`, `Pricing/{DiscountRule,TaxRule}.cs` (IHasConcurrencyToken yok); `PriceListItemHandlers.cs:53-88` (bellek-içi overlap kontrolü, DB unique yok)
- **Teşhis:** Dört fiyat/kural entity'si token'sız → eşzamanlı düzenleme sessiz üzerine-yazma. Çakışan miktar-aralığı check-then-act; iki eşzamanlı istek çakışan tier ekleyebilir, `ResolveAsync` kazananı `UpdatedAtUtc`'ye göre seçiyor (iş kuralı değil).
- **Öneri:** Dört entity'ye `IHasConcurrencyToken`; tier overlap için `EXCLUDE USING gist` veya `pg_advisory_xact_lock`.

### Orta/Düşük (stok/MRP) — özet

- **[ORTA] Serial where-used N+1** (`SerialHandlers.cs:90-113`), **cycle-count Post satır-başı N+1** (`StockCountHandlers.cs:209` → `AllocationService.AdjustAsync`), **sipariş/iade stok akışında composite-key N+1** (`OrderStockEffectHandlers.cs:164,331`, `ReturnRequestStockHandler.cs:66`) — → batch overload'lar.
- **[ORTA] FIFO açılış-bakiye köprüsü yalnız sipariş akışına bağlı** (`StockOpeningBalanceBridge` çağrı noktaları) — direkt issue/adjust/production katman seedlemiyor → `ResolveIssueCostAsync` hard-error. → köprüyü tüm giriş noktalarına.
- **[ORTA] Change-impact committed run yerine "şimdi" preview'a bakıyor** (`MrpProductionHandlers.cs:66`) → persiste satırlardan besle veya staleness bayrağı.
- **[ORTA] `Product.Update()` 39 pozisyonel parametre** (`Product.cs:119`) + Create'te çift-set — parametre yer değişimi derleyiciyle yakalanmaz. → mantıksal gruplara böl.
- **[ORTA] TaxRule/DiscountRule DB CHECK yok** (`PricingConfigurations.cs`); katalog önizlemede para-birimi tutarsızlığı sessiz (`CustomerPortalDirectOrderHandlers.cs:235`).
- **[DÜŞÜK] Standard costing FE guard'ı validator'da yok** (`ProductValidators.cs`); depo-arası transfer lot-takipli ürünü desteklemiyor (`AllocationService.cs:377`, `LotId:null`); SerialUnit FSM'in yarısı (Return/Scrap/Assembly) hiçbir komuttan çağrılmıyor; MRP 523-ürün batch testi InMemory provider'da (SQLite/Npgsql translate kanıtlanmıyor).

---

## 5. Cam Mekan (3D) — yeni landed işlerin denetimi

> Önceki cam raporundaki bulguların ÇOĞU landed. Bu bölüm **yeni kodun doğru çalışıp çalışmadığını** denetler. Sonuç: çekirdek düzeltmeler doğru yönde ama **birkaçı yarım/yanlış**.

### [KRİTİK] "Persistent cam↔host bond" yarım — `hostWallId` hiçbir yerde ATANMIYOR (YENİ KOD)

- **Konum:** `src/features/glass-enclosure/model/project.types.ts:490` (tanım) + `model/wallAttachment.ts:85,93` (yalnız okuma). **Doğrulandı:** `grep hostWallId` yalnız tanım satırını döndürüyor, test dışı hiçbir atama yok.
- **Teşhis:** Autofill/hole-fill (run'ların oluştuğu yer) `hostWallId`'yi doldurmuyor; resolver saf-geometri fallback'ine düşüyor. "Persistent bond"un asıl vaadi (drift sonrası kurtarma) fiilen çalışmıyor; yalnız aynı-işlem co-move çalıştığı için sorun gizli.
- **Öneri:** Autofill/hole-fill run oluşturma yolunda `hostWallId`'yi gerçek host wall id'siyle doldur — yoksa "persistent" iddiası doğru değil.

### [KRİTİK] 3D stretch-gizmo ile duvar uzatma bond resolver'ı tamamen bypass ediyor — cam kopuyor (YENİ KOD)

- **Konum:** `scene/builders/WallObject.tsx:1467-1495` (`commitLength`), `:1405-1454` (`commitArcLength`), `:1531-1555` (`commitSide`) — üçü de ham `updateWall(...)` çağırıyor, `commitWallPatch` değil
- **Teşhis:** `commitLength.fromStart` dalı `originX/originY`'yi kaydırıyor (pose değişikliği) ama ham Zustand setter'ı çağırıyor; `resolveAttachedRunIds`/`moveWallWithAttachments` tetiklenmiyor. Duvarın başlangıç-ucundan çekince üstündeki cam eski konumda kalıyor (aynı oturumda, reload beklemeden). Inspector "length co-move yapmaz" kuralının kapsamadığı, daha kötü bir 3D yol.
- **Öneri:** Origin değiştiren dalları `commitWallPatch` (veya `resolveAttachedRunIds`+`moveWallWithAttachments`+`persistRun`) üzerinden geçir.

### [YÜKSEK] Divide guard arc run + rounded/notch köşe panellerini kaçırıyor (YENİ KOD)

- **Konum:** `scene/DesignerCanvas.tsx:728-736` (guard yalnız `shapeKind/topShape/archRiseMm`), `:747-748` (fraction `divideWall.lengthMm` düz uzunluk), kıyasla `model/panelOutline.ts:178-190` (`panelIsShaped` `cornerRadiiMm/cornerNotchMm`'i de sayıyor)
- **Teşhis:** Guard run/wall eğriliğini (`geomArcRadiusMm`, `bendAngleDeg`) kontrol etmiyor → düz-panelli kavisli run bölünebiliyor, `splitPanelsAtLength` lineer aritmetikle yanlış fiziksel konuma böler. Ayrıca `cornerRadiiMm/cornerNotchMm` panelleri şekilli sayılmadığından bölünüp iki düz dikdörtgene indirgeniyor (fix'in önlemeye çalıştığı hata sınıfı).
- **Öneri:** Guard'a arc/bend kontrolü + fraction'ı `developedLengthMm` ile hizala; ad-hoc koşulu canonical `panelIsShaped(p)` ile değiştir.

### [YÜKSEK] Panel bölme donanımı konuma bakmadan koşulsuz SOL yarıya atıyor (YENİ KOD)

- **Konum:** `model/panelSplit.ts:21-22` (`left = {...panel}` tüm hardware'i alır, `right.hardware = []`)
- **Teşhis:** Donanımın gerçek `offsetXmm`'i sağ tarafta olsa bile mekanik olarak sol panelde kalıyor (geçersiz offset ile). "Sağ yarı donanımı kaybeder"den daha geniş: bölme sonrası donanım her zaman yanlış olabilir.
- **Öneri:** Her donanımın `offsetXmm`'ini kesim noktasıyla karşılaştırıp doğru panele ata, offset'i yeni genişliğe göre yeniden ölçekle.

### [YÜKSEK] LiveCostPreview hâlâ paralel yerel maliyet kaynağı çalıştırıyor — backend hatasında sessizce yerel gösteriyor (YENİ KOD)

- **Konum:** `ui/LiveCostPreview.tsx:4,47-68,70` (`calculateCost` her render koşulsuz; `preview.isError` hiç okunmuyor)
- **Teşhis:** `bom/preview` "single source of truth" iddiasına rağmen `costCalculator` hâlâ aktif: ilk mount'ta, `projectId` null iken ve **backend hatasında sessizce** yerel tahmini gösteriyor; "Live Preview" rozeti kaynağı ayırt etmiyor. FX-fiyatlı/arc projede iki hesaplayıcı farklı sayı verir.
- **Öneri:** `preview.isLoading/isError/data` durumlarını UI'da ayırt et; yerel tahminde açık "tahmini" rozeti, hatada görünür uyarı.

### [YÜKSEK] Greenhouse (sera) beşik çatı/mahya-uç camı BOM/nesting'e hiç girmiyor — sessiz eksik fiyatlama

- **Konum:** `scene/geometries/PitchedGreenhouseGeometry.tsx` (ham mesh, `GlassProjectPanel` bağı yok) + `server/.../Services/IBOMComposer.cs:102-274` (yalnız `run.Panels`)
- **Teşhis:** Greenhouse preset 3D'de eksiksiz görünüyor ama üçgen mahya-uç + eğimli çatı camları hiç `GlassProjectPanel` üretmiyor → teklif/BOM/kesim yalnız ayrı yerleştirilmiş run'ları fiyatlıyor; seranın cam alanının çoğu **sıfır fiyatlanıyor** (fark edilmeden eksik teklif).
- **Öneri:** Greenhouse/Pitched modunda mahya + çatı düzlemleri için `GlassProjectPanel`/BOM satırları sentezle (`shapeKind:'polygon'`), veya yalnız-görselse BOM ekranında açık uyarı.

### [YÜKSEK] Freehand/divide self-intersection ZAYIF algoritmayı çağırıyor + snap-to-close yok (YENİ KOD)

- **Konum:** `scene/DesignerCanvas.tsx:660,984` + `PolygonSurfaceObject.tsx:215` (hepsi `polygonSelfIntersects`); sertleştirilmiş `outlineSelfIntersects`/`sanitizeFreeOutline` (`wallFeatureGeometry.ts:223,241`) hiçbir shipping yolundan çağrılmıyor. `PenController.tsx:207-217` freehand bitişinde `CLOSE_SNAP_MM` kullanmıyor.
- **Teşhis:** Sertleştirilen fix (collinear/vertex-touch kesişimi) ihtiyaç duyan yola bağlanmamış; freehand kapanış kenarı koşulsuz çiziliyor → algılanamayan kendini-kesen kontur.
- **Öneri:** Çağrıları `outlineSelfIntersects`'e yönlendir (veya mantığı birleştir); freehand bitişinde kapanış mesafesini toleransla kontrol et/snap.

### Orta/Düşük (cam) — özet

- **[YÜKSEK] Undo/redo bool-flag donanım senkron atlıyor** (`useSceneSync.ts:52-67,93-107`) — kulp geri alınınca BOM eski donanımı faturalamaya devam; **[YÜKSEK] sahne hydration kaydedilmemiş donanımı görünmez yapıp sonraki düzenlemede BOM'dan siliyor** (`designerStore.projectToScene`) — gerçek veri kaybı yolu. → bool→hardware sentezini backend'e taşı; hardware'i DTO'dan hydrate et.
- **[ORTA] `SetRunPanelsDto` FluentValidation yok** (WidthMm>0 yerine `Math.Max(1,...)`, duplicate-id yok); bayat panel id'si sessizce yeni PK; roof/slab hareketi altındaki eğimli cam run'u co-move etmiyor (`hostSlabId` yok); duvar silme bağlı camı ne siliyor ne rebağlıyor (`designerStore.removeWall:760`).
- **[ORTA] Brush seal + gasket strip aynı BOM kategori bucket'ı** (`panelHardware.ts:12,54`) — fırça conta sessizce düşüyor; kataloğa bağlanamayan donanım sessizce fiyatsız (`HardwareManager.tsx:34`); ground-pen tolerans sabiti 7.5× agresif (`PenController.tsx:20`); autosave↔bom/preview debounce yarışı; `BomManualPricingServices.cs:13,18` ölü %20 VAT.
- **[DÜŞÜK]** `DesignerTabBar.tsx:35` / `CuttingReportView.tsx:156` / `RevisionsPanel.tsx:156` hardcoded i18n; tr/en JSON'da trailing NUL-byte dolgu; cross-project stok TOCTOU kilitsiz (bilinçli soft, dokümante et); divide `.find()` çoklu-run disambiguation yok; `polygonSelfIntersects`/`PenController` testsiz.

---

## 6. E-Fatura · Bildirim · Auth · KVKK · MasterData

### [KRİTİK] E-İrsaliye (e-Despatch) satıcı kimliği hâlâ hardcoded "Tenant Seller" — GİB reddi (YENİ KOD)

- **Konum:** `server/src/CoreAlign.Application/Shipments/EDespatch/EDespatch.cs:169` (**iki ajan bağımsız doğruladı**)
- **Teşhis:** Fatura tarafı `BuildSellerParty(tenant)`'a taşındı ama e-İrsaliye handler'ı hâlâ `new SellerParty("Tenant Seller", null, null, ...)` — VKN/TCKN/adres null. UBL-TR DespatchAdvice GİB'e VKN'siz gidiyor → üretimde kesin red.
- **Öneri:** Ortak `BuildSellerParty(tenant)` helper'ını `EInvoice` altına taşıyıp e-İrsaliye handler'ı da kullansın; VKN yoksa submission'ı reddet.

### [KRİTİK] DSAR erişim/indirme sahiplik kontrolü yok — tenant-içi IDOR (YENİ KOD)

- **Konum:** `server/src/CoreAlign.API/Controllers/Privacy/DataSubjectRequestsController.cs:37-39` (`GetById`), `:64-72` (`DownloadExport`) + `DataSubjectRequestService.cs:202-208`
- **Teşhis:** IDOR guard yalnız tenant seviyesinde; `GetById` (`[Authorize]`, rol yok) `id`'yi doğrudan servise geçiyor, `currentUser.Id == RequesterUserId` kontrolü hiçbir yerde yok. Aynı tenant'taki herhangi bir kullanıcı başkasının DSAR kaydını/exportunu görebilir. Kodun kendi örneği (`AcknowledgeNotificationMessageHandler:32` owner kontrolü yapıyor) bu deseni takip etmiyor.
- **Öneri:** `GetById`/`BuildExportAsync`'e `currentUserId` ekle; `RequesterUserId != currentUserId` (admin hariç) → `NotFoundException`.

### [KRİTİK] DSAR access/portability tamamlanıyor ama gerçek export dosyası üretilip saklanmıyor (YENİ KOD)

- **Konum:** `DataSubjectRequestService.cs:78,126` (`MarkCompleted(DateTime.UtcNow, Guid.NewGuid())`)
- **Teşhis:** `Guid.NewGuid()` rastgele, hiçbir dosyaya bağlı olmayan id; `BuildExportAsync` yalnız in-memory DTO kuruyor, hiçbir storage yazımı yok. `DataExportFileId` alanı fonksiyonel olarak anlamsız/yanıltıcı. Talep "tamamlandı" görünür ama kalıcı export yok.
- **Öneri:** `MarkCompleted`'ten önce export'u üret, storage'a yaz, gerçek key'i ver; ya da `DataExportFileId`'yi kaldırıp export'u her zaman on-demand üret (yanıltıcı alanı temizle).

### [YÜKSEK] `keepFinancialTrail` bayrağı DSAR erasure'da kabul ediliyor ama hiçbir davranışı etkilemiyor (YENİ KOD)

- **Konum:** `DataSubjectRequestService.cs:89-112` → `PiiAnonymizer.cs:28-70` → `UserAnonymizer.cs:31-65`
- **Teşhis:** Bayrak her katmandan geçiyor ama yalnız dönüş DTO'suna kopyalanıyor; `UserAnonymizer`/`PrivacyEraseService`/`AnonymizeCustomerChildren` bunu parametre almıyor/dallanmıyor. Admin "finansal izi koru" seçse de seçmese de erasure aynı çalışıyor — hayali seçenek.
- **Öneri:** İmzalara `keepFinancialTrail`'i gerçekten ekle; `false`'ta finansal referansları da anonymize et — veya bayrağı kaldırıp "finansal iz her zaman korunur" olarak netleştir.

### [YÜKSEK] `verify-email` endpoint'inde rate limiting eksik — token brute-force (YENİ KOD)

- **Konum:** `server/src/CoreAlign.API/Controllers/AuthController.cs:95`
- **Teşhis:** login/register/refresh/forgot/reset/2fa hepsinde `[EnableRateLimiting("auth")]` var; `verify-email`'de yok → doğrulama token'ı sınırsız denenebilir (§ auth 10/dk tutarsız).
- **Öneri:** `[EnableRateLimiting("auth")]` ekle.

### Orta/Düşük (entegrasyon) — özet

- **[ORTA] Marketing consent gate hata durumunda fail-open riski** (`NotificationDispatcher.cs:308`) — `GetLatestAsync` throw ederse davranış çağırana bağlı; dar try/catch + explicit fail-closed ile netleştir.
- **[ORTA] IBAN validator checksum doğrulamıyor** (`MasterDataValidators.cs:196,212`) — boşluklu/checksum-yanlış IBAN kabul; mod-97 doğrula.
- **[ORTA] `Payslip.Anonymize` yalnız 2 alan** (`Payslip.cs:135`) — `EmployeeNumber`/FK re-identification; politika INVARIANTS'a yaz.
- **[DÜŞÜK]** verify-email token-geçersiz vs kullanıcı-yok farklı exception (enumeration, doğrulanmalı); bulk import satır/dosya sınırı (doğrulanmalı); `LoginCommand` `ITransactionalRequest` değil (bilinçli olabilir, doğrula).
- **Yanlış-pozitif elendi (ajan doğruladı):** CashPosition/DuplicateDetection tenant filtresi OK (TenantEntity global filter); MockEFaturaProvider prod'da kayıtlı değil.

---

## 7. DB · Migration · Domain

### [KRİTİK] `StockCostLayer` (FIFO katmanları) FK'siz + yanıltıcı yorumla "meşrulaştırılmış" (YENİ KOD)

- **Konum:** `Persistence/Configurations/StockCostLayerConfiguration.cs:24-27`; migration `20260807000000_Phase124StockCostLayers.cs:15-32`
- **Teşhis:** `ProductId/WarehouseId/StockItemId/SourceMovementId` yalnız index, gerçek FK yok. Yorum "StockMovement convention'ı yansıtıyor" diyor ama aynı repo'daki `StockItem`/`StockMovement` gerçek `HasForeignKey().OnDelete(Restrict)` kullanıyor (iddia yanlış). Ürün/depo silinince orphan FIFO katmanı → maliyet bozulur (§4.4 soft-Guid yasağı).
- **Öneri:** Takip migration'ı ile 4 FK (Restrict); yanıltıcı yorumu sil (§1 ihlali de).

### [YÜKSEK] Phase124/125 miktar/maliyet CHECK constraint yok; Phase129 FK adları snapshot ile uyuşmuyor (YENİ KOD)

- **Konum:** `Phase124StockCostLayers.cs` / `Phase125SerialUnits.cs` (qty/cost CHECK yok); `Phase129GlassPanelHardware.cs:24-26` (raw-SQL FK adları) vs `CoreAlignDbContextModelSnapshot.cs:19542-19561` (EF convention adları)
- **Teşhis:** Yeni stok/serial tablolarının miktar/maliyet kolonları yalnız domain'de korunuyor, DB CHECK yok (§4.4). Phase129'un raw-SQL FK adları EF `ApplySnakeCaseNaming()` convention'ıyla farklı → sonraki `migrations add` gereksiz DROP+CREATE (rename) üretir (snapshot drift).
- **Öneri:** Phase127 deseniyle idempotent CHECK migration'ı; Phase129 FK'lerini `RENAME CONSTRAINT` ile EF adına çevir veya `.HasConstraintName()` ile sabitle.

### [YÜKSEK] `GlassProjectPanelHardware` concurrency token yok + `PanelId` FK'si Configuration'da eksik (migration'da var); `IncomingInvoice.LinkedVendorBillId` FK tanımsız (YENİ KOD)

- **Konum:** `GlassProjectPanelHardware.cs` (token yok), `GlassEnclosureProjectConfigurations.cs:155` (PanelId yalnız index); `IncomingInvoice.cs:17` + Configuration (FK yok)
- **Teşhis:** Migration DB'de panel_id CASCADE FK kuruyor ama EF Configuration modellemiyor (snapshot drift); BOM'a giren donanım eşzamanlı yazmaya açık, token yok. `LinkedVendorBillId` soft-Guid.
- **Öneri:** Configuration'a `HasOne<GlassProjectPanel>().HasForeignKey(PanelId).OnDelete(Cascade)`; gerekirse `IHasConcurrencyToken`; `LinkedVendorBillId` için `HasOne<VendorBill>().OnDelete(SetNull)`.

### Orta/Düşük (DB) — özet + pozitifler

- **[ORTA] `GLPostingService.cs` working-tree'de §1 yorum yasağı ihlali** (çok satırlı `//` açıklamalar, `// WHY:` değil) — commit'ten önce temizle (finansal-kritik dosya).
- **[ORTA] `EInvoiceStatuses` serbest string status CHECK'siz** — bilinçli mi doğrula, öyleyse INVARIANTS'a not.
- **[DÜŞÜK]** `TempPendingProbe` scratch-adlı migration (zararsız ama §4.2 örneği, INVARIANTS'a taşınmamış); ~34 finansal entity'de `ConcurrencyToken.HasDefaultValue(0L)` tutarsız (gelecekte gereksiz DDL); Phase78/127'nin 18 CHECK constraint'i snapshot/registry'de kayıtsız.
- **Temiz bulundu (ajan doğruladı):** migration duplicate/ileri-tarihli-sıra/scratch **temiz**; MediatR pipeline sırası + `ITransactionalRequest` kapsamı + `IGlobalReadable` tenant-FK istisnası **doğru**; grouped-COALESCE/nested-Distinct EF tuzağı **yok**; commit edilmemiş dosyalarda sözdizim/build kırıcı **yok**.

---

## 8. Öncelikli Yol Haritası

**Faz 1 — para/hukuki bütünlük (durdurucu):**

1. Çoklu-döviz cari bakiyesi: ledger helper gerçek kur + `AmountInBase` (§2-1); kısmî tahsilat çift-credit (§2-2); gelen e-fatura kur=1 (§2-6); tenant FX override ölü repo (§2-5).
2. E-İrsaliye seller hardcoded → GİB reddi (§6-1); DSAR IDOR + export saklanmıyor + keepFinancialTrail no-op (§6).
3. Order→invoice kredi limiti + FX snapshot (§2-3); vendor tevkifat 360 (§2-4); avans mahsubu inline GL (§2-7).

**Faz 2 — çalışmayan/yarım özellikler (kullanıcı "bitti" sanıyor):** 4. Fire (scrap) backend'i (§3-1); shipment `Returned` bağı (§3-2). 5. Cam: `hostWallId` atama + stretch-gizmo bond (§5); divide guard arc/köşe (§5); greenhouse çatı camı BOM (§5); donanım undo/hydration veri kaybı (§5). 6. Merkezi fiyatlandırmayı iç satışa bağla + ResolveTax/Discount (§4-2); Product concurrency token client'a (§4-4).

**Faz 3 — stok/ölçek doğruluğu:** 7. FIFO transfer değer sızıntısı (§4-1); seri sevkiyat stok defteri (§4-5); Standard costing (§4-6); MRP RunPreview unbounded (§4); pricing batch N+1 (§4-3). 8. CreateShipment idempotency (§3-4); QC-hold 3-way match (§3-3); recurring invoice idempotency (§2-9).

**Faz 4 — DB borcu & cila:** 9. StockCostLayer FK + yeni tablo CHECK'leri + Phase129 FK adı/snapshot drift (§7); fiyat/kural entity concurrency (§4); IBAN checksum, verify-email rate-limit (§6). 10. `GLPostingService` yorum temizliği, N+1 batch'ler, i18n/NUL, fmtCurrency tekilleştirme.

---

_Bu rapor 6 paralel derin-denetim ajanının statik analiziyle üretildi; en kritik yeni bulgular (çoklu-döviz ledger kur=1, `hostWallId` atanmıyor, e-İrsaliye seller, DSAR IDOR/export) gerçek koda karşı elle doğrulandı. Kod aktif geliştirildiği için satır numaraları son commit'lerle kayabilir — düzeltmeden önce ilgili dosyayı teyit et. Runtime doğrulaması (özellikle FX/ledger/DSAR/e-İrsaliye) düzeltme sonrası entegrasyon testiyle yapılmalı._
