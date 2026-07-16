# Cam Plaka Envanteri + Atölye Üretim Yürütme (MES) — Planlama

> Durum: **PLAN** (geliştirme başlamadı). Güncel: 2026-07-10. Tasarım iki grounding workflow'u ile kod tabanına oturtuldu (`wf_46de8631` + `wf_932805a9`); sentez ajanları + doğrudan kod doğrulaması.
> Kilitlenen kararlar §9, açık iş kararları §10.
> **Raporlama bu planın kapsamı DIŞI** (kullanıcı kararı — sonra tüm modüller için topluca yapılacak). Yalnız fire/plaka verisinin raporlanabilir tutulması için gerekli **kolon/boyut** notları bırakıldı.

## 0. Amaç

Cam fabrikasında **plaka (levha) envanterini** raf/konum + artan (offcut) takibiyle yönet ve **sipariş → MRP → tezgah rotası → plaka tüketimi → sipariş "üretim tamamlandı"** akışına bağla. Multi-tenant SaaS: modül **tenant feature-flag** (`GlassPlateTracking:Enabled`) ile opsiyonel.

## 1. Temel ilke — iki temsili karıştırma (consistency)

`SerialUnit` precedent'i (metadata vs cost-of-record):
- **`StockItem` + `StockMovement` = otoriter.** Miktar **m²** (`OnHand`=alan m², `AvgCost`=TL/m²) + maliyet; GL/COGS `StockMovement.TotalCost`'tan.
- **`GlassPlate` = per-plaka metadata** (boyut/raf/durum/artan-soyağacı); **maliyet üretmez.** Plaka **adedi** = GlassPlate COUNT.
- **Lockstep:** `Σ(Available plaka alanı m²)` = `StockItem.OnHand` (Product+Warehouse). `SyncProductStockAsync` `Product.StockQuantity`'yi tutar.
- N plaka mal-kabul = **tek** StockItem receipt (Σalan, `ApplyReceiptAsync`) + N `GlassPlate` (Fresh). Costing tek kaynak `IInventoryCostingService`.

## 2. Faz planı (her biri shippable)

| Faz | İçerik |
|---|---|
| **1 — Plaka & konum envanteri** (BAŞLANGIÇ) | Product.IsPlateTracked (+opsiyonel lot), GlassPlate+StorageLocation+GlassPlateConsumption, mal-kabul/tüketim/artan/**fire taksonomisi**, uygun-plaka arama, **depo-yetkisi (§4)**, azaldı/bitti bildirimi, **etiket/QR (§6)** |
| 2 — Tezgah & routing master data | WorkCenter+operatör+konum, ProductionRouting+RoutingStep (multi-op), ürün→routing |
| 3 — Sipariş ↔ MRP ↔ üretim | sipariş→make→PlannedProductionOrder→ProductionJob (traveler) |
| 4 — Atölye yürütme + **Operatör Modu (§5)** | tezgah kuyruğu, plaka seç, **optimizasyon (nester §3.4)**, başlat/fire/bitir, oto-geçiş, sipariş "üretim tamamlandı" |
| 5 — Analitik & cila | tezgah doluluk, plaka utilization %, fire oranı, yield %, WIP |

---

## 3. FAZ 1 — Detaylı blueprint

### 3.1 Plaka TANIMI = `Product` (+ opsiyonel lot)
Plaka türü ("Şeffaf 6mm 3210×2250") = **bir `Product`**. `Product.cs:59-60` zaten `Color`+`ThicknessMm` taşıyor. Yeni alanlar (`IsLotTracked` deseni, default false):
- `IsPlateTracked` bool · `MinRemnantAreaMm2`/`MinRemnantWidthMm`/`MinRemnantHeightMm` decimal? (**kullanıcı tanımlarsa var; tanımlamazsa minimum YOK → her kullanılabilir artan = artan**) · `MinPlateCount` int? · `StandardWidthMm`/`StandardHeightMm` decimal?
- **Lot opsiyonu:** plaka-ürünü istenirse **`IsLotTracked=true`** seçilebilir (mevcut `Product.IsLotTracked` + `Lot` + FEFO reuse) → `GlassPlate.LotId` dolar, mal-kabulde parti girilir, FEFO seçim uygulanır. Default kapalı.
- `Unit`="m2" zorunlu (`GlassLineMath.Area`). Migration idempotent `ADD COLUMN IF NOT EXISTS`.

### 3.2 `GlassPlate : TenantEntity, IHasConcurrencyToken`
`ProductId`(FK Restrict) · `WarehouseId`(FK) · `StorageLocationId?`(SetNull) · `LotId?` · `PlateNumber`(`(tenant,plate_number)` unique — **QR yükü, §6**) · `Kind`(Fresh|Remnant) · `Status`(Available|Reserved|InUse|Consumed|Scrapped FSM) · `WidthMm`/`HeightMm`/`ThicknessMm` · `OriginalAreaMm2`/`RemainingAreaMm2` · `ParentPlateId?`(soyağacı) · `SourceReceiptMovementId`(soft Guid) · `ReservedByJobId?` · `Condition` · `ReceivedAtUtc`/`ConsumedAtUtc?` · `ConcurrencyToken`. FSM self-guard → 409. Alan-korunumu: `Σ(çocuk artan)+kesilen+fire ≤ orijinal`.

### 3.3 `StorageLocation : TenantEntity` (raf/konum)
`WarehouseId`(FK) · `Code`(`(tenant,warehouse,code)` unique) · `Name` · `Kind`(Rack|Shelf|Pallet|Floor|Zone + CHECK) · `ParentLocationId?`(ağaç) · `IsActive`. Uygun-plaka önerisinde `Code+Name+yol` yüzeyleşir ("X rafında"). Mevcut `StockItem.BinLocation` string'e dokunulmaz.

### 3.4 Kesim + artan = **mevcut 2D nester reuse** (bbox varsayımı GÜNCELLENDİ)
**KEŞİF:** `ICuttingOptimizer2D` / `MaximalRectanglesOptimizer2D` (MaxRects) ZATEN VAR ve bağlı (`optimize-2d-nesting`/`cutting-plan` endpoint, `GlassProjectCuttingPlan` utilization/waste). Girdi=kesim istekleri (label,W,H,qty)+levha W×H+kerf; çıktı=`CuttingResult2D` (placements, `TotalWasteMm2`, `UtilizationPercent`, unplaced). İç `_freeRects` **artan dikdörtgenleri** zaten hesaplar (yalnız dışa vermiyor).

**Plaka tüketiminde reuse:** işin kesimlerini + seçili plakayı (sheet) optimizer'a ver → yerleşim + yield + **gerçek free-rect artanları** al. Küçük ekleme (rewrite değil): `CuttingResult2D`/`CuttingSheet2D`'ye `Offcuts` (final free-rects) ekle.
- **Artan** = nester'ın free-rect'i **≥ minimum** → `GlassPlate(Kind=Remnant, ParentPlateId)` (gerçek W×H); StockItem yalnız kesilen kadar düşer.
- **< minimum** (veya minimum tanımlıysa) → oto-fire (`reason=below-min`). Minimum tanımlı değilse → tüm kullanılabilir free-rect artan olur.
- **yield/utilization %** = `CuttingResult2D`'den bedava (Faz-5 KPI).
- **Operatör "optimizasyon" butonu (§5)** = bu engine (bu plakadan bu kesimler nasıl çıkar / hangi plaka en uygun). "bbox v1" artık depolama-şekli (dikdörtgen) anlamında; hesap **gerçek nester'la**.

### 3.5 Çoklu tüketim — `GlassPlateConsumption : TenantEntity`
`GlassPlateId`(FK) · `ProductId`/`WarehouseId`(denorm) · `OrderLineId?`/`JobId?` · `CutAreaMm2` · `Pieces` · `CutWidthMm/HeightMm` · `ResultingRemnantPlateId?` · `ScrappedAreaMm2` · `ScrapReasonCodeId?` · `WorkCenterId?`/`OperatorId?` (soft Guid — §3.6 raporlanabilirlik) · `StockMovementId?`(soft Guid) · `OccurredAtUtc` · `PostedByUserId`. 1 iş→N plaka (greedy best-fit/oldest); 1 plaka→N iş (artan zinciri). Tek `ConsumeGlassPlate` (`ITransactionalRequest`) atomik.

### 3.6 Fire (scrap) girişi — taksonomi + modlar (araştırıldı, karar verildi)
Mevcut `StockReasonCode`/`AllocationService.AdjustAsync`/`AdjustmentNegative(6)` omurgası üstüne **ince katman** — paralel model yok.

**Modlar:** **Area (m²)** kısmi kayıp (kötü/yanlış kesim, kenar fire, taşlama, offcut kesim kırığı) · **Count (adet/parça)** tam plaka (temper fırın kırığı, NiS patlama, taşıma/elleçleme kırığı, lamine ret, stok hasarı) · **Auto below-min** (§3.4). **Per-plaka** (GlassPlate id biliniyor: "bu plaka çatladı") vs **Bulk** (Product+Warehouse).

**Reason taksonomisi (seed, ~12):** kesim-kırığı / kötü-kesim / kenar-fire / taşlama-chip / temper-kırığı / NiS-patlama / lamine-defekt / elleçleme-kırığı / stok-hasar / raf-ömrü(coated/interlayer) / **below-min-offcut** → Category `DamageWriteOff` (veya taşıma=`Loss`, ömür=`Expired`) `AffectsCost=true` → **DR 689 / CR 153** (mevcut write-off gate). Yıkıcı-numune/QC + **rework-yeniden-beslenen** → `AffectsCost=false` (miktar düşer, GL yok).
- **`StockReasonCategory`'de `Scrap`/`Fire` üyesi YOK** → **ekle** (int-enum append = **migration'sız**) ve `WriteOffCategories`'e dahil et → fire, generic hasardan ayrı raporlanabilir. (Açık karar §10.)

**Zorunlu alanlar:** `ReasonCodeId` (zorunlu, maliyet-eşlemini sürer) · ScrapMode+miktar · Product/Warehouse veya GlassPlateId · `PostedByUserId`(JWT). **Atölyeden:** `WorkCenterId`/`OperatorId` (soft Guid). Ops: Not, **Foto** (mevcut `IFileUploadService` `glass-photo` profili). `unitCost`=AvgCost (kullanıcı girmez). Seed = **always-run system seeder** (DemoData arkasında DEĞİL — PayrollSystemDataSeeder dersi).

**`ScrapGlassPlateCommand`** (`ITransactionalRequest`) + validator (miktar>0, reason non-empty, Count→plaka kimliği, Area→kalan-alan içinde). GlassPlate→`Scrapped`. **Raporlanabilirlik:** her fire zaten `StockMovement(AdjustmentNegative)` = reason/category/product/warehouse/operator/tarih/maliyet taşır → sonraki raporlama fazında reason×product×warehouse×tezgah×operatör×tarih×maliyet sorgulanabilir (costed 689 vs non-cost ayrık).

### 3.7 Bildirim (azaldı / bitti) + **tek-tık ikmal**
`GlassPlate` `Consumed`/`Scrapped` → aynı transaction'da Outbox `GlassPlateDepletedCheckEvent`. Subscriber Product+Warehouse için Available adet+m²: ==0→"bitti", ≤`MinPlateCount`(veya ≤`MinStock`)→"azaldı". InApp(+Email). Template `NotificationTemplateSeeder` **GLOBAL** `GlassPlateLow`/`GlassPlateDepleted` tr+en (eksikse sessiz no-op). Payload **stabil, tarih YOK**.

**Kolay ikmal (bildirimden kısayol — kullanıcı isteği):** payload `productId`+`warehouseId` taşır; in-app **`NotificationBell`** (`src/features/collaboration/ui`) `GlassPlateLow`/`GlassPlateDepleted` kategorisini tanıyıp **CTA butonu** render eder → iki hızlı aksiyona yönlendirir: **(a) "Plaka Ekle / Mal Kabul"** — `ReceiveGlassPlates` modalı o ürün+depo için ön-dolu (kullanıcıda fiziksel yeni plaka var); **(b) "Sipariş Oluştur (Reorder)"** — mevcut Buy→`PurchaseRequisition` sink ile satın-alma. **Şema değişikliği YOK** (`NotificationMessage.PayloadJson` + `NotificationBell` kategori-farkındalığı yeter). Aynı kısayollar Plaka ekranı (Fiziksel Plakalar) header'ında da: azalan/biten plaka-ürünleri için tek-tık **Mal Kabul** + **Reorder**. Amaç: kullanıcı "sayfa nerede" aramasın — bildirimden tek tıkla ekleyip yönetsin.

### 3.8 Application / 3.9 Plaka ekranı / 3.10 Infra / 3.11 Test
- **Commands:** `UpdateProduct`(IsPlateTracked+lot+alanlar), `ReceiveGlassPlates`, `MoveGlassPlate`, `ConsumeGlassPlate`, `ScrapGlassPlate` — hepsi `ITransactionalRequest`. **Queries:** `ListGlassPlates`(keyset, **depo-yetkisi §4 uygulanır**), **`UsablePlatesForCut`**(Available+bbox sığan+en-iyi-sığma→en-eski→en-yakın-raf, warehouse+konum projekte), `GlassPlateWhereUsed`, `ListStorageLocations`.
- **FE `features/glass-plates/`** (FSD, feature-flag): 4 sekme — Plaka Tanımları (IsPlateTracked Products), Fiziksel Plakalar (ListGlassPlates + Move/Fire/Where-used + Mal-Kabul/Fire modal), Konumlar (StorageLocation ağaç), Uygun Plaka Arama (UsablePlatesForCut, "X rafında"). §17 platform-UX (kolon/kayıtlı-görünüm/inline-edit) reuse. i18n tr+en tam parite. §102 route dörtlüsü.
- **DB:** Migration `PhaseNN` idempotent (enum int+CHECK, enum-string `DEFAULT '<Member>'`); DbSet-siz repo `ToTable("<çoğul>")`; index `(tenant,is_plate_tracked) WHERE`, `(tenant,product_id,status)`, `(tenant,warehouse,storage_location)`, partial `RemainingAreaMm2 WHERE Available`, unique `(tenant,plate_number)`.
- **Test:** plaka FSM; alan-korunumu; nester-reuse offcut; below-min oto-fire→AdjustmentNegative+reason; artan tekrar önerilir; UsablePlatesForCut sığma+sıralama; ScrapEntry Area/Count aynı m²; **depo-yetkisi cross-user izolasyon**; StockItem lockstep; bildirim bir-kez dedup; cross-tenant; N+1 bütçe; has-pending "No changes".

---

## 4. Depo yetkisi (per-user warehouse access) — YENİ, çapraz-kesen

Yönetici kullanıcılara **belirli depolar** atar; kullanıcı yalnız yetkili depolarındaki plaka/stoğu görür; öneride depo gösterilir. **Yok — sıfırdan, ama `IPortalScopeService` deseniyle.**
- **`UserWarehouseAccess : TenantEntity`** (user_id soft-Guid, warehouse_id **gerçek FK Restrict**, granted_by_user_id; unique `(tenant,user,warehouse)`; DbSet-siz `ToTable`).
- **`IWarehouseAccessScope`** (Application/Common) + impl `PortalScopeService` gibi: JWT user + `RequireTenantId`; **PlatformAdmin/TenantAdmin → Unrestricted** (boş liste DEĞİL, açık sinyal); değilse izinli id kümesi.
- **Uygulama:** yalnız yeni glass-plate read'lerinde (`ListGlassPlates`/`UsablePlatesForCut`) `.Where(allowed.Contains(WarehouseId))` **kısıtlıysa**. **GLOBAL EF query filter EKLEME** (order-confirm/MRP/DRP/COGS/cycle-count'u kırar). DTO'ya `WarehouseId`+`WarehouseName`.
- **Yönetim UI:** ayrı ekran değil → **mevcut kullanıcı-yönetimi ekranına** "depo yetkileri" (yönetici zaten kullanıcıları görüyor). `AssignUserWarehousesCommand`(`ITransactionalRequest`, atomik tam-set replace, `[Authorize(Roles=TenantAdmin)]`, granting-user JWT'den).
- **Güvenlik:** tenant izolasyonu BİRİNCİL (bu ikinci eksen AND'lenir); **deny-by-default**; server-side id; cross-tenant+cross-user izolasyon testi.
- **Rollout (kritik, açık karar §10):** mevcut tenant'larda sıfır grant → gün-1 her non-admin tüm plakadan mahrum → ya (a) mevcut tüm depoları mevcut non-admin'lere veren backfill migration, ya (b) tenant başına feature-flag (managers yapılandırana dek kısıtlama kapalı). **Öneri: (b) flag.** Write-path (yalnız kendi deposundan issue/transfer) = sonraki artımlı benimseme (ayrı karar).

## 5. Operatör Modu (Faz 4 UX) — çok önemli

**Tek normal sayfa** (`/dashboard/production/workstations`, standart layout içinde): kullanıcının **yetkili tüm tezgahları** listelenir, tüm operasyonlar burada. **Default = normal sayfa.**
- **"Operator Mode" = sayfa-içi ekran-modu** (ayrı rota/kiosk DEĞİL): mevcut **persona/UX-mode altyapısı** reuse (`@/shared/lib/persona`: `useScreenUxMode`/`ScreenPersonaMenu`). Mod açıkken app chrome (sidebar/navbar + gereksiz) gizlenir, iri-dokunmatik + minimum yazı.
- **Operatör akışı (basit):** kuyruktan iş seç → **Plaka seç** (`UsablePlatesForCut`, yalnız yetkili depolar, "X rafında") → **Optimizasyon** (§3.4 nester: bu plakadan bu kesimler / en uygun plaka) → **Başlat** → **Fire gir** (§3.6, iri butonlar) → **Bitir** → routing'de sonraki tezgaha **oto-geçiş** → son adım → sipariş "üretim tamamlandı".
- **Otomatik yenilenme:** operatör "yenile" bilmez → `refetchInterval` canlı polling (kuyruğa iş düşünce/başka tezgah bitirince ekran kendi güncellenir).
- **Cihaz uyarlaması:** tablet/telefon responsive ince-ayar (sonra).
- **Kiosk auth (açık karar §10):** paylaşılan tablette tam login yerine hafif yöntem (PIN / kart-badge / isim-seç) önerilir.

## 6. Sektörel ek kapsam — "tam çözüm" için eklenecekler (öncelikli)

| # | Öğe | Öncelik | Faz | Reuse / Yeni |
|---|---|---|---|---|
| A | **Plaka barkod/QR etiket + tezgahta okut** | must | 1 | **YENİ** (QR/barcode lib yok — QRCoder/ZXing); etiket PDF = mevcut QuestPDF; QR yükü=tenant+PlateNumber; `ScanPlateLookup` query |
| B | **Müşteri-malı / konsinye cam** (free-issue, GL-baskılı valuation) | must | 1-2 | **YENİ** (grep=0); cam fabrikasında sık — GlassPlate'e ownership boyutu, StockItem valuation + inventory/COGS GL baskılanır |
| C | 2D nesting/optimizasyon | — | 1/4 | **REUSE** (§3.4 — zaten var, sadece free-rect dışa aç) |
| D | Kalite/izlenebilirlik **sertifikası** (per plaka/sipariş) | should | 2 | reuse QuestPDF + SerialUnit/GlassPlate soyağacı (temper/heat-soak/CE) — yeni renderer |
| E | Teslimat **paketleme birimi** (A-frame/sehpa/kasa) | should | 3-4 | reuse Shipment/packing-slip/freight/e-Despatch — yalnız rack-gruplama yeni |
| F | **Rework/yeniden-işlem** döngüsü (kusurlu panel önceki adıma) | should | 4+ | routing kurulunca; scrap dalı §3.6 reuse |
| G | `MinPlateCount`(adet)→MRP reorder/auto-requisition | nice | 2-3 | küçük wiring, mevcut Buy→PurchaseRequisition |
| H | Yield/utilization % KPI | nice | 5 | nester `WasteMm2`/`UtilizationPercent`'ten |

**REUSE — YENİDEN YAZMA (zaten var):** QC-hold (GoodsReceipt `QcStatus` — gelen plaka muayenesi!), procure-to-pay (PO/PR/GoodsReceipt/3-way-match — plaka tedariki), MRP make-vs-buy→PurchaseRequisition, FIFO `StockCostLayer`+`CostingMethod`, `GlassLineMath` m²/dm²/cm² UoM, `IProductionExecutionService`, CRP kapasite, freight/e-Despatch, packing-slip PDF, GlassType `PricePerM2`.

## 7. Faz 2–5 özet
- **Faz 2:** `WorkCenterOperator`(WorkCenter↔Employee)+konum; `ProductionRouting`+`RoutingStep` (multi-op — CRP'nin ertelenen kısmı) + ürün→routing. **NOT: routing/ProductionJob/traveler/oto-geçiş TAMAMEN GREENFIELD ve en büyük/riskli faz** (yalnız WorkCenter+PlannedProductionOrder+IProductionExecutionService+CRP var).
- **Faz 3:** sipariş→MRP make→PlannedProductionOrder→`ProductionJob`(traveler)+`ProductionJobStep`.
- **Faz 4:** §5 Operatör Modu + atölye yürütme çekirdeği.
- **Faz 5:** doluluk/utilization/fire-oranı/yield.

## 8. (Raporlama — kapsam dışı)
Kullanıcı kararı: raporlar sonra tüm modüller için topluca. Yalnız hazırlık notu: fire by-reason raporu için `StockReasonCategory.Scrap` üyesi (§3.6) + consumption'daki `WorkCenterId`/`OperatorId` boyutları yeterli veri sağlar.

## 9. Kilitlenen kararlar
1. Artan = **nester free-rect (gerçek dikdörtgen)** reuse; minimum **opsiyonel** (yoksa minimum yok).
2. Plaka tanımı = `Product`(IsPlateTracked) + **opsiyonel lot** (IsLotTracked reuse).
3. Miktar m² StockItem otoriter; adet=GlassPlate COUNT; GlassPlate maliyet üretmez.
4. Tüketim=`GlassPlateConsumption`; 1 iş→N plaka, 1 plaka→N iş.
5. Fire = `AdjustmentNegative`+reason taksonomisi (§3.6); Area/Count/Auto+per-plaka/bulk; below-min→689; eşik-altı artan `Scrapped` satırı saklanır.
6. Depo-yetkisi = `UserWarehouseAccess`+`IWarehouseAccessScope` (opt-in accessor, global-filter DEĞİL); UI mevcut kullanıcı-yönetiminde; deny-by-default.
7. Operatör = tek normal sayfa + **Operator Mode ekran-modu** (persona reuse), oto-yenileme; kiosk değil.
8. Modül tenant feature-flag; başlangıç Faz 1.
9. Bildirim = Outbox + NotificationDispatcher (stabil payload); **azaldı/bitti bildirimi CTA taşır → tek-tık Mal Kabul veya Reorder** (§3.7); şema değişikliği yok.

## 10. Karara bağlananlar (kullanıcı "sen karar ver" dedi — gerekçeli)
1. **`StockReasonCategory.Scrap` EKLENİR** (migration'sız int-enum; fire generic hasardan ayrı; `WriteOffCategories`'e dahil).
2. **Maliyet politikası: fiziksel kaybolan her şey = maliyetli (GL 689).** Yıkıcı numune/QC testi dahil (malzeme gerçekten tüketildi → Loss/DamageWriteOff). **Rework fire DEĞİLDİR** — kusurlu panel önceki routing adımına döner (Faz 4 back-edge), cam kaybolmaz, miktar-nötr. Böylece "maliyetsiz fire" belirsizliği yok — temiz kural: **fire = kayıp = 689; rework = routing.**
3. **Depo-yetkisi = tenant feature-flag** (`WarehouseAccessEnforced`, default **KAPALI** = kısıtlama yok, tam geriye-uyum). Açılınca **deny-by-default** (yönetici atayana dek). **Varsayılan depo otomatik verilmez.** Write-path kısıtı (yalnız kendi deposundan issue/transfer) = sonraki artımlı benimseme.
4. **Kiosk/tablet auth = istasyon bir kez giriş yapar** (cihaz/kiosk oturum token'ı) + **operatör her aksiyonda isim-seç + 4-hane PIN** (fire/üretim doğru operatöre yazılsın; ek donanım gerektirmez; kart/RFID opsiyonel-gelecek).
5. **Konsinye/müşteri-malı cam = Faz 2** (ayrı dilim; Faz-1'i şişirmez; valuation/GL ownership'e dokunur).
6. **Lot = opsiyonel, ürün başına** (kullanıcı plaka-ürününde seçer; default kapalı).
