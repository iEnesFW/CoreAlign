# Cam Mekan Modülü — Piyasa Analizi, Mimari & Yol Haritası

> CoreAlign ERP içine eklenecek **3D destekli Cam Mekan tasarım & üretim** modülü için kapsamlı plan. Proje kurallarına (`CLAUDE.md`) ve mevcut FSD/Clean Architecture düzenine tam uyumludur.

---

## 0. Yönetici Özeti

Modül, kullanıcıya **canlı 3D ortamda parametrik cam mekan tasarımı** yaptıracak; çıktısı eksiksiz **kesim raporu + maliyet analizi + teklif + sipariş aktarımı + üretim takvimi + müşteri portalı + saha ölçü PWA**. Mevcut CoreAlign altyapısı (multi-tenant, Customer/Order/Inventory/Vendor/Purchasing, Three.js + R3F + drei + lil-gui paketleri zaten kurulu) bu modüle birebir uygundur — sıfırdan altyapı kurulmuyor, üzerine inşa ediliyor.

**Hedef kullanıcı:** cam balkon / cam mekan üreticileri & bayileri.

**Uçtan uca akış (sales-to-cash):**

```
Salesperson lead → Surveyor saha ölçüsü (PWA) → Designer 3D tasarım →
ClimateAdvisor + WindLoad + Thermal/Acoustic hesap → Maliyet + İskonto →
Teklif PDF + Share Link → Müşteri portalda imza → Approver onay →
Order + WorkOrder + Stock alloc + (yetersizse PO draft) → Production schedule →
Producer çıktı + DXF/CSV CNC → Installer montaj → Closed + Notifications + Audit
```

**Diferansiyatör:** rakipler ya tek başına çizim/teklif aracı (Aluvector, Cam Balkon Analiz Pro) ya da klasik masaüstü ERP (Technosoft, Makrosoft). CoreAlign tek üründe **3D parametrik tasarımcı + tam ERP (cari/stok/sipariş/fatura/muhasebe/satınalma) + multi-tenant SaaS + modern web + saha PWA + müşteri portalı**.

**Hukuk/standart uyumu:** TS 498 wind load + TS EN 12150 temperli cam + TS EN 14449 lamine + ISO 12944 korozyon + KVKK + EU Class 2A sızdırmazlık.

---

## 1. Piyasa & Rakip Analizi

### 1.1 Sistem Tipleri (Müşterinin Karar Vereceği Ana Eksen)

| Tip                                     | Karakter                                      | Tipik m² fiyat (2025-26) | Kullanım                                     |
| --------------------------------------- | --------------------------------------------- | ------------------------ | -------------------------------------------- |
| **Katlanır (KCS / Bi-fold)**            | Panel paketlenip kenara toplanır, %100 açılım | 5.600–6.750 TL           | Klasik balkon, geniş açıklık isteyen         |
| **Sürgülü (Slide)**                     | Paneller paralel kayar, eşikli/eşiksiz        | 5.900–7.850 TL           | Engel istemeyen, modern                      |
| **Isıcamlı (Heat-insulated, çift cam)** | Sürme veya katlanır + çift cam yalıtım        | 8.500–10.500 TL          | Yıl boyu kullanılan, ısı/ses yalıtımı kritik |
| **Giyotin**                             | Cam yukarı/aşağı sürer, esnek havalandırma    | 6.500–9.500 TL           | Cafe/restoran, kış bahçesi                   |
| **Menteşeli (Hinged / Fransız)**        | Kapı gibi açılır, sabit + hareketli panel     | 4.500–6.000 TL           | Küçük balkon, koruma amaçlı                  |
| **Sabit (Kış bahçesi cephe)**           | Hareket etmeyen panel + tavan                 | 7.500–14.000 TL          | Kapalı yaşam alanı genişletme                |

### 1.2 Türkiye'de Öne Çıkan Profil/Sistem Markaları

- **Albert Genau** — `SlideMaster` (ısıcamlı sürme, Speed-HD makara, 150 kg panel kapasitesi), `Tiara Twinmax` (ısıcamlı katlanır, 5.1× yalıtım, EU Class 2A su geçirmezlik). En üst segment.
- **Vizyon** — `Gold`, `Gold Plus`, `Vizyon`, `Makrowin`, `Aluway`, `Alusel` serileri; 8-10 mm temperli cam; eşikli/eşiksiz sürme.
- **Winsa** — Cam balkon + alüminyum doğrama tek çatı; geniş bayi ağı.
- **BKS, ASF, Metroer, A1, Ertan** — Yerli üretim, OEM ağırlıklı; orta segment.
- **Asaş, Alkon** — Profil ekstrüzyon + sistem birlikte (Slidekon vb. kenetli sürme).
- **Rehau** — Kış bahçesi + ısıcam + giyotin uzmanlığı.
- **Camoda (Şişecam grubu)** — Cam tarafında otorite + aksesuar.

### 1.3 Global Referans Sistemler (Kalite/Detay Çıtası)

- **Solarlux SL 25 / SL 25XXL** (Almanya) — All-glass slide-and-turn, 65 kg/panel, 15 mm TSG'ye kadar.
- **Sunflex SF20/22/42** — 19.2 m sistem genişliği, otomatik tahrik.
- **Lumon** (Finlandiya) — Çerçevesiz cam, 6/8/10/12 mm, 20 dB trafik gürültüsü azaltma.

Bu üçü, **render kalitesi, parametrik kısıt yönetimi ve kataloglarının dijital sunumu** açısından örnek alınmalıdır.

### 1.4 Aksesuar & Donanım Markaları

- **Rulman / Makara**: Giesse, Albert Genau patentli Speed-HD, Maksimum, Asaş muadilleri (her panelde 2 rulman standart).
- **Menteşe**: pimli menteşe, çift kanat menteşe, ısıcam ağır kanat menteşesi.
- **Kilit**: lük boynuz, İspanyol kilit, parmak izi/anahtarlı.
- **Diğer**: stoplama, fren, mafsallı gönye, kıl/EPDM fitil, cam balkon zinciri, kapak çeşitleri.
- Yaygın Türkiye perakendecileri: Ayrıntı Shop, Kobibest, Tema Alüminyum (B2B).

### 1.5 Mevcut Yazılım Rakipleri

| Yazılım                   | Güçlü Yön                                                                        | Boşluk (CoreAlign Fırsatı)                              |
| ------------------------- | -------------------------------------------------------------------------------- | ------------------------------------------------------- |
| **Cam Balkon Analiz Pro** | Mobil, kesim ölçüsü + maliyet + WhatsApp teklif; 3D görüntüleme; CNC veri export | ERP entegrasyonu yok, multi-tenant değil, modüler değil |
| **Aluvector**             | KCB + ALM editör, 1D/2D nesting, %25 fire tasarrufu, PDF + QR teklif             | Tek başına ürün; tedarik/stok/cari ERP yok              |
| **Real Cam Balkon**       | Çizim + teklif + maliyet + raporlama                                             | UI eski, SaaS değil                                     |
| **Master Cam Balkon**     | Online çizim + hesap                                                             | 3D zayıf, üretim akışı kısıtlı                          |
| **Technosoft / PenCAD**   | PVC kapı/pencere CNC otomasyon, kesim opt.                                       | Cam balkon odaklı değil                                 |
| **Makrosoft**             | Cam balkon programı                                                              | Web değil, modern stack değil                           |

**CoreAlign'ın kazanma tezi:** Tek üründe **3D parametrik tasarımcı + ERP (cari, stok, sipariş, fatura, muhasebe) + multi-tenant SaaS + modern web**. Rakipler ya tek başına çizim/teklif ya da klasik masaüstü ERP — ikisini de profesyonelce birleştiren yok.

---

## 2. Mevcut CoreAlign Altyapısı — Modüle Hazırlık Durumu

| Bileşen                                         | Durum                       | Modül Etkisi                                                        |
| ----------------------------------------------- | --------------------------- | ------------------------------------------------------------------- |
| Multi-tenant (`TenantEntity`, otomatik filter)  | ✅ Hazır                    | Tüm yeni entity'ler `TenantEntity` türetir                          |
| Customer / Order / OrderLine / Invoice          | ✅ Hazır                    | Teklif/sipariş `Order` üzerine bağlanır                             |
| Product / StockItem / StockMovement / Warehouse | ✅ Hazır                    | Profil/cam/aksesuar stok kalemi olarak temsil edilir (junction)     |
| Vendor / PurchaseOrder / PurchaseOrderLine      | ✅ Hazır                    | Stok altı → otomatik PO draft (`IPurchaseOrderSuggester`)           |
| Brand / UnitOfMeasure / TaxRate / PriceList     | ✅ Hazır                    | Marka & fiyat yönetimi mevcut; `BrandVendor` junction eklenecek     |
| DocumentSequence                                | ✅ Hazır                    | `GE-{YYYY}-{####}` proje kod prefix seed                            |
| Outbox / TransactionBehavior / Pipeline         | ✅ Hazır                    | Notification dispatch idempotent + transaction güvenliği            |
| SignalR (real-time push)                        | ✅ Hazır                    | InApp bildirim + paylaşılan teklif "görüntülendi" feedback          |
| Module katalog + TenantModule abonelik          | ✅ Hazır                    | `glass-enclosure` modül kodu seed edilecek                          |
| Auth + RBAC policy                              | ✅ Hazır                    | 7 rol × 34 aksiyon (bkz. §10.1 Permission Matrix)                   |
| Three.js + @react-three/fiber + drei + lil-gui  | ✅ `package.json`'da kurulu | 3D tasarımcı direkt başlayabilir                                    |
| TanStack Query + Zustand + Zod + RHF            | ✅ Hazır                    | Form & state akışı tipli; designer için command-pattern (undo/redo) |
| FSD + Clean + CQRS + MediatR + FluentValidation | ✅ Hazır                    | Modül aynı pattern'i takip edecek                                   |
| safeRequest + logger + i18n (tr+en) + dark mode | ✅ Hazır                    | Sıfır tolerans kurallarına otomatik uyum                            |
| Vite workspace yapısı                           | ✅ Hazır (`apps/*`)         | `apps/glass-enclosure-field` PWA ayrı build için doğal yer          |
| QuestPDF (Application katmanında ekleyebilir)   | ⚠ Henüz yok                 | F5'te NuGet ekleme (Apache 2.0 community)                           |
| EPPlus / CsvHelper (import için)                | ⚠ Henüz yok                 | F1'de NuGet ekleme                                                  |
| DynamicExpresso (formula engine)                | ⚠ Henüz yok                 | F1'de NuGet ekleme                                                  |
| Workbox (PWA service worker)                    | ⚠ Henüz yok                 | F2a'da npm ekleme                                                   |
| Web Bluetooth API                               | ✅ Tarayıcı native          | F2a'da Chrome/Edge desteği gerek (Safari sınırlı)                   |

Yeni başlatma maliyeti **mimari kurulumda yok**; tamamı **domain + uygulama + UI** üzerinde.

---

## 3. Domain Modeli (Backend Entity Haritası)

`server/src/CoreAlign.Domain/Entities/GlassEnclosure/` altına 22 entity. Hepsi `TenantEntity` türevi.

### 3.1 Katalog (Tenant'a Özel Master Data)

```
ProfileSystem            Sistem ailesi (örn. "SlideMaster Isıcamlı")
  ├─ brand_id            Marka (Albert Genau, Vizyon, ...)
  ├─ system_type         Folding | Sliding | HeatInsulatedSliding | Guillotine | Hinged | Fixed
  ├─ max_panel_width_mm
  ├─ max_panel_height_mm
  ├─ max_panel_weight_kg
  ├─ supported_glass_thicknesses_mm  (int[])
  ├─ certification_class             ("EU Class 2A" gibi metin)
  └─ thermal_u_value                 (opsiyonel, ısıcam için)

ProfileItem              Sisteme bağlı tek tek profiller
  ├─ system_id
  ├─ role                Top | Bottom | SideJamb | Mullion | Sash | Adapter | DripRail
  ├─ code, name
  ├─ stock_bar_length_mm (genelde 6000 veya 6500)
  ├─ weight_kg_per_meter
  ├─ cross_section_svg   (SVG path — 3D extrusion için)
  ├─ cross_section_dxf_url (opsiyonel — vendor verisi, daha hassas)
  ├─ parametric_description_json   (kalınlık/genişlik/cavity ölçüleri — extrude doğrulama için)
  ├─ default_color_id
  ├─ preferred_vendor_id           (BrandVendor üzerinden çözülür; null ise marka default)
  ├─ vendor_part_number            (tedarikçi sipariş kodu)
  ├─ lead_time_days                (vendor LeadTime'tan ayrışabilir — özel profil için uzun)
  └─ reorder_point_meters          (stok altı uyarı eşiği)

GlassType                Cam türü
  ├─ code, name
  ├─ thickness_mm
  ├─ structure           Tempered | Laminated | DoubleGlazed | TripleGlazed
  ├─ glass_layers_json   (örn. 4+12Ar+4 ısıcam)
  ├─ u_value, sound_db
  ├─ max_panel_area_m2
  └─ price_per_m2

ColorOption              Boya / anodizasyon seçenekleri
  ├─ ral_code            ("RAL 9016" gibi)
  ├─ finish_type         Anodized | PowderCoated | WoodLook
  ├─ hex_color           (3D render için)
  └─ price_modifier_percent

HardwareCategory         Hinge | Roller | Lock | Handle | Gasket | Brush | Bumper | WallBracket | Chain | DripCap
HardwareItem
  ├─ category
  ├─ brand_id
  ├─ code, name
  ├─ compatible_system_ids   (Guid[])
  ├─ unit                    Piece | Meter | Set
  ├─ unit_price
  └─ model_glb_url           (opsiyonel — 3D'de gerçek geometri)

HardwareKit              Sisteme + panel sayısına göre öntanımlı paket
  └─ items: HardwareKitItem
       ├─ hardware_item_id
       ├─ qty_formula              (DynamicExpresso ile çalışır: "panel_count * 2",
       │                            "panel_count - 1", "ceil(run_length_mm / 600)" gibi
       │                            erişilebilir değişkenler: panel_count, run_length_mm,
       │                            run_height_mm, opening_count_folding, opening_count_sliding,
       │                            opening_count_hinged, glass_thickness_mm)
       ├─ condition_expression     (opsiyonel — "system_type == 'Sliding'" gibi)
       └─ note                     (montaja yardımcı not)

GlassEnclosureSettings   Tenant başına ayarlar
  ├─ default_stock_bar_length_mm          (genelde 6000 / 6500)
  ├─ default_jumbo_glass_width_mm         (genelde 3210)
  ├─ default_jumbo_glass_height_mm        (genelde 2250)
  ├─ saw_kerf_mm                          (testere kalınlığı, ~5 mm)
  ├─ glass_kerf_mm                        (cam kesim payı, ~4 mm)
  ├─ guillotine_required                  (cam kesim makinesi gerçek serbest kesim yapamıyorsa zorunlu)
  ├─ default_waste_percent                (manuel ek fire payı)
  ├─ labor_cost_per_m2
  ├─ default_margin_percent
  ├─ field_tolerance_top_mm               (saha ölçüsü − üst toleransı, varsayılan 10)
  ├─ field_tolerance_side_mm              (yan toleransı, varsayılan 5)
  ├─ transport_rate_per_km
  ├─ transport_rate_per_kg
  ├─ scaffolding_required_from_floor      (örn. 5 — bu kat ve üstü ek ücret)
  ├─ scaffolding_rate_per_m2
  ├─ crane_required_from_floor            (asansöre sığmayan panel için cephe vinç)
  ├─ crane_rate_per_meter
  ├─ workshop_daily_capacity_m2           (üretim takvimi için)
  ├─ default_payment_terms_json           (peşin/3 taksit/6 taksit varsayılan)
  ├─ default_locale                       (tr-TR / en-US / ar-SA / de-DE)
  ├─ default_currency                     (TRY / EUR / USD / SAR)
  ├─ data_retention_days                  (KVKK için, varsayılan 730)
  ├─ whatsapp_business_phone_id           (Twilio veya WhatsApp Cloud API)
  ├─ notification_email_from
  └─ quote_share_token_ttl_days           (paylaşılan teklif geçerlilik süresi)

WindZone                 TS 498 / DIN 1055 bölge tablosu (seed: TR 1-4 bölgesi, kıyı/iç ayrımı)
  ├─ code                ("TR-Zone-3-Coast")
  ├─ wind_pressure_pa    (taban basınç değeri)
  └─ region_label_tr/en

ClimateZone              Bölge bazlı varsayılan öneriler (Akdeniz/Karadeniz/Marmara/Ege/İç/Doğu/G.Doğu)
  ├─ code
  ├─ avg_temperature_winter
  ├─ avg_humidity_percent
  ├─ corrosion_class     (C1-C5, ISO 12944) — sahil C4-C5
  ├─ recommends_double_glazing       (bool — soğuk bölge)
  ├─ recommends_corrosion_resistant_coating (bool — sahil)
  ├─ recommends_seismic_smaller_panel (bool — deprem bölgesi)
  └─ il_postal_prefix_list           (otomatik tespit için)

BrandVendor              Marka ↔ Tedarikçi köprüsü
  ├─ brand_id            (Albert Genau, Vizyon, …)
  ├─ vendor_id           (mevcut Vendor entity)
  ├─ default_lead_time_days
  ├─ default_payment_terms
  └─ is_preferred        (birden fazla tedarikçi olabilir)

DiscountRule             Proje düzeyi indirim motoru
  ├─ name, code          ("WINTER25", "Bayi-A")
  ├─ scope               CustomerGroup | Coupon | Volume | DateRange
  ├─ customer_group_id   (opsiyonel)
  ├─ coupon_code         (opsiyonel)
  ├─ min_area_m2         (opsiyonel — hacim indirimi)
  ├─ valid_from / valid_until
  ├─ discount_kind       Percent | FixedAmount
  ├─ discount_value
  └─ stackable           (diğer indirimle birleşir mi)

NotificationTemplate     Olay bazlı mesaj şablonu
  ├─ event_code          QuoteSent | QuoteViewed | QuoteAccepted | OrderConfirmed |
                         ProductionStarted | ProductionCompleted | InstallationScheduled | StockLow
  ├─ channel             Email | Sms | WhatsApp | InApp
  ├─ subject_template    (placeholder: {{customer_name}}, {{project_code}}, …)
  ├─ body_template
  └─ locale
```

### 3.2 Proje (Müşteriye Özel Tasarım)

```
GlassProject : TenantEntity
  ├─ code (auto: GE-2026-0001 — DocumentSequence)
  ├─ customer_id (mevcut Customer)
  ├─ project_name, site_address (AddressSnapshot)
  ├─ status        Draft | Surveyed | Quoted | Confirmed | InProduction | Ready | Installed | Cancelled
  ├─ created_by_user_id
  ├─ assigned_designer_id, assigned_salesperson_id   (rol bazlı atama)
  ├─ floor_number                       (kat — iskele/asansör ücreti için)
  ├─ building_height_m                  (zemin → balkon yüksekliği — wind load için)
  ├─ wind_zone_id                       (TS 498 bölgesi — adres'ten otomatik öneri, manuel override)
  ├─ climate_zone_id                    (Akdeniz/Karadeniz/... — adres'ten öneri)
  ├─ fire_safety_class                  (kamu binası ise EN 13501 gereksinim metni)
  ├─ scaffolding_required               (settings.threshold + floor_number → otomatik, override)
  ├─ crane_required
  ├─ total_area_m2, total_panels (cache)
  ├─ subtotal, discount_total, tax_total, grand_total, currency
  ├─ fx_rate_locked_at_utc              (teklif anındaki kur — TRY dışı için)
  ├─ wind_load_pa_calculated            (cache — IWindLoadCalculator çıktısı)
  ├─ weighted_u_value                   (cache — IThermalAcousticCalculator)
  ├─ weighted_sound_db                  (cache)
  ├─ valid_until_date
  ├─ current_scene_version              (en güncel sahne versiyonu)
  └─ notes

GlassProjectRun           Tek bir doğrusal/açılı segment (L/U cam balkonun her kenarı bir run)
  ├─ project_id
  ├─ order_index
  ├─ length_mm, height_mm
  ├─ origin_x, origin_y, rotation_deg   (3D plan koordinatları)
  ├─ profile_system_id
  ├─ color_id
  └─ has_top_drip, has_bottom_eski (boolean özellikler)

RunConnection             İki run'un birleşme noktası (L/U/poligon balkon için kritik)
  ├─ project_id
  ├─ run_a_id, run_b_id
  ├─ joint_angle_deg                (90 = L, 135 = oktagon kenarı)
  ├─ mitre_cut_deg                  (gönye kesim açısı — joint_angle / 2 varsayılan)
  ├─ uses_corner_post               (köşede dikme var mı, yoksa kavisli birleşim mi)
  └─ corner_profile_id              (varsa özel köşe profili)

GlassProjectPanel         Bir run içindeki tek panel (cam + opening)
  ├─ run_id
  ├─ panel_index
  ├─ width_mm (run uzunluğundan hesaplanır, manuel override mümkün)
  ├─ opening_type    Fixed | Folding | SlidingLeft | SlidingRight | Hinged | Guillotine
  ├─ glass_type_id
  ├─ has_handle, has_lock, has_brush_seal
  └─ notes

GlassProjectScene         3D sahnenin serileştirilmiş hali (versiyonlu, append-only)
  ├─ project_id
  ├─ version                        (1'den artan)
  ├─ label                          (opsiyonel — "Müşteri onayı v3")
  ├─ scene_json                     (brotli-sıkıştırılmış JSON, bytea)
  ├─ thumbnail_url
  ├─ camera_state_json
  ├─ saved_by_user_id
  ├─ saved_at_utc
  ├─ is_customer_approved           (share viewer onayı işaretler)
  └─ approval_signature_url         (varsa imza PNG)

GlassProjectChangeLog     Tasarım üzerinde yapılan değişiklik audit trail
  ├─ project_id
  ├─ scene_version_from, scene_version_to
  ├─ change_kind                    RunAdded | RunRemoved | RunResized | PanelAdded |
                                    PanelRemoved | OpeningTypeChanged | GlassChanged |
                                    SystemChanged | ColorChanged | HardwareChanged
  ├─ change_summary                 (kısa metin: "Run 2 length 3200 → 3400 mm")
  ├─ change_diff_json               (atomik fark — undo/redo + version diff için)
  ├─ user_id
  └─ created_at_utc

GlassProjectBOMLine       Hesaplanmış BOM (yeniden üretilebilir)
  ├─ project_id
  ├─ kind            ProfileCut | GlassPiece | HardwarePiece | Labor
  ├─ ref_id          (profile_item_id / glass_type_id / hardware_item_id)
  ├─ description
  ├─ quantity, unit
  ├─ unit_cost, line_cost
  └─ source          ("Run 1 / Panel 3" gibi izlenebilirlik)

GlassProjectCuttingPlan
  ├─ project_id
  ├─ plan_type        Profile1D | Glass2D
  ├─ plan_json        (nesting çıktısı)
  ├─ total_waste_mm2 / total_waste_mm
  └─ generated_at_utc

GlassProjectQuoteSnapshot Teklif anındaki donmuş kopyaa
  ├─ project_id
  ├─ pdf_url
  ├─ share_token (kısa URL + QR için)
  └─ accepted_at_utc

GlassProjectAttachment    Saha fotoğrafı, ölçü kroki taraması
  └─ url, kind, uploaded_by

FieldSurvey               Saha ölçü kaydı (PWA → server)
  ├─ project_id
  ├─ surveyed_by_user_id
  ├─ surveyed_at_utc
  ├─ gps_lat, gps_lng               (kayıt için, opsiyonel)
  ├─ floor_number, building_height_m (saha doğrulaması)
  ├─ slope_top_mm                   (üst kiriş eğimi — kritik panel ölçüsü etkisi)
  ├─ slope_bottom_mm                (alt kiriş)
  ├─ slope_left_mm, slope_right_mm  (yan duvar şakül kontrolü)
  ├─ raw_measurements_json          (lazer cihazından bluetooth ham veri)
  ├─ obstacles_json                 (kapı/pencere/kalorifer engelleri — overlay foto üzerine)
  ├─ photo_urls                     (string[])
  ├─ annotated_photo_urls           (FabricJS export — engel işaretli)
  ├─ status                         InProgress | Submitted | Approved | Rejected
  └─ notes

GlassProjectShareToken    Public read-only viewer linki
  ├─ project_id
  ├─ scene_version                  (donmuş — sonradan değişse de bu sürüm görünür)
  ├─ token (UUID + URL-safe)
  ├─ expires_at_utc                 (settings.quote_share_token_ttl_days)
  ├─ created_by_user_id
  ├─ view_count, last_viewed_at_utc (CRM telemetri)
  ├─ accepted_at_utc                (müşteri "kabul" tıkladıysa)
  ├─ rejected_at_utc, rejection_reason
  └─ signature_image_url            (canvas imza)
```

### 3.3 Üretim & Aktarım

```
GlassWorkOrder : TenantEntity
  ├─ project_id
  ├─ scheduled_start_date, scheduled_end_date
  ├─ assigned_team_id               (production team — User group)
  ├─ assigned_installer_id          (montaj ekibi)
  ├─ machine_id                     (kesim makinesi — kapasite çakışması için)
  ├─ workload_m2                    (atölye günlük kapasite hesabı için)
  ├─ status   Pending | Cutting | Assembling | Ready | InTransit | Installed | Defective
  ├─ checklists_json
  ├─ defect_notes                   (üretim hatası, taşıma hasarı, re-cut)
  └─ recut_count

GlassProjectOrderLink     Project → mevcut Order entity'sine köprü (sipariş aktarımı)
  ├─ project_id (unique)
  └─ order_id

GlassNotificationLog      Olay tabanlı bildirim geçmişi (audit + idempotency)
  ├─ project_id
  ├─ event_code                     QuoteSent | QuoteViewed | QuoteAccepted | QuoteRejected |
                                    OrderConfirmed | StockReserved | ProductionStarted |
                                    ProductionCompleted | InTransit | InstallationScheduled |
                                    InstallationCompleted | StockLow | PaymentDue
  ├─ channel                        Email | Sms | WhatsApp | InApp
  ├─ template_id                    (NotificationTemplate)
  ├─ recipient_kind                 Customer | Designer | Approver | Producer | Installer | Salesperson
  ├─ recipient_address              (email/phone/userId snapshot)
  ├─ payload_json                   (renderlanmış subject + body)
  ├─ provider_message_id            (Twilio/Cloud API ref)
  ├─ status                         Pending | Sent | Delivered | Failed | Read
  ├─ delivered_at_utc, read_at_utc
  └─ error_message                  (failure detayı)
```

### 3.4 Mevcut Modellere Dokunuş

- `Brand`: aynı entity kullanılır (Albert Genau, Vizyon vb. burada).
- `Vendor`: mevcut Vendor entity'si kullanılır; `BrandVendor` junction üzerinden marka → tedarikçi çözülür. Profile/Glass/Hardware stok altı düştüğünde `PurchaseOrder` draft otomatik üretilir (mevcut Purchasing flow'a delegasyon).
- `PurchaseOrder`: yeni satır türü gerekmez — `PurchaseOrderLine.Description` + `vendor_part_number`; modül `IPurchaseOrderSuggester` ile draft üretir, kullanıcı onaylar.
- `Order`: yeni satır türü gerekmez — `OrderLine.Description` + reference; ek olarak `GlassProjectOrderLink` ile geri izlenebilirlik.
- `Product` (opsiyonel B yolu): profil ve cam stok kalemlerini de Product olarak temsil edip mevcut stok hareketinden faydalanmak. **Tercih edilen:** Profile/Glass/Hardware ayrı entity (semantik temiz), stok takibi için Product ile junction (`linked_product_id` opsiyonel alan).
- `StockItem` / `StockMovement`: profil bar, jumbo cam, aksesuar her biri tek `StockItem` kaydı. Sipariş onayı → `Reservation`; üretim çıkışı → `StockMovement(Out)`; iskonto/fire → `StockMovement(Waste)`.
- `Customer`: `CustomerGroup` alanı yoksa eklenir (toptan/bayi/perakende ayrımı — discount engine için).
- `DocumentSequence`: `GE-{YYYY}-{####}` proje kod prefix'i seed.
- `Module` seed: `code='glass-enclosure', name='Cam Mekan', category='Manufacturing', isCore=false`. `ModulePricePlan` kayıtları billing tarafında.
- `User`: yeni rol/policy claim'leri eklenir — bkz. §10 Permission Matrix.

---

## 4. Uygulama Katmanı — CQRS Use-Case Listesi

`server/src/CoreAlign.Application/GlassEnclosure/`

### 4.1 Komutlar (Commands)

| Komut                                                                                   | Davranış                                                                                            |
| --------------------------------------------------------------------------------------- | --------------------------------------------------------------------------------------------------- |
| `CreateGlassProjectCommand`                                                             | Customer'a bağlı yeni Draft proje + ilk Run iskeleti + climate/wind zone otomatik öneri (adres'ten) |
| `UpdateGlassProjectHeaderCommand`                                                       | Adı, müşteri, site adresi, kat, bina yüksekliği, geçerlilik tarihi                                  |
| `AddRunCommand` / `RemoveRunCommand`                                                    | Plan üzerinde segment ekle/sil                                                                      |
| `UpdateRunCommand`                                                                      | length, height, system, color                                                                       |
| `AddRunConnectionCommand` / `UpdateRunConnectionCommand` / `RemoveRunConnectionCommand` | L/U/poligon balkonun köşe birleşimi + gönye açısı                                                   |
| `AddPanelCommand` / `RemovePanelCommand` / `UpdatePanelCommand`                         | Run içi panel CRUD                                                                                  |
| `BulkRebalancePanelsCommand`                                                            | Run uzunluğunu eşit panellere böl (otomatik genişlik)                                               |
| `SaveSceneStateCommand`                                                                 | Scene JSON + thumbnail PNG (binary), versiyon arttırır + ChangeLog yazar                            |
| `CompareSceneVersionsCommand`                                                           | İki versiyon arası diff (UI render için)                                                            |
| `RevertToSceneVersionCommand`                                                           | Eski versiyona dön + yeni versiyon olarak işaretle (geçmiş silinmez)                                |
| `RecomputeBOMCommand`                                                                   | Mevcut konfigürasyondan BOMLine yeniden üretir                                                      |
| `GenerateCuttingPlanCommand`                                                            | 1D + 2D nesting çalıştırır, plan kaydeder; `guillotine_required` settings'i dikkate alır            |
| `ComputeWindLoadCommand`                                                                | wind_zone + building_height + panel boyutları → cam kalınlığı önerisi/uyarı                         |
| `ComputeThermalAcousticCommand`                                                         | Tüm paneller için ağırlıklı U-value ve dB; teknik özet için                                         |
| `ValidateProjectCommand`                                                                | SceneValidator zinciri — wind/thermal/system compatibility/panel max/glass max — `ValidationResult` |
| `ApplyDiscountCommand`                                                                  | DiscountRule veya manuel iskonto uygula, proje toplamını yeniden hesapla                            |
| `LockFxRateCommand`                                                                     | Çoklu para teklifi için kuru dondur (valid_until_date'e kadar)                                      |
| `GenerateQuoteCommand`                                                                  | PDF üretir, snapshot oluşturur, `GlassProjectShareToken` üretir (kısa URL)                          |
| `RecordShareViewerActionCommand`                                                        | Public viewer'da görüntüleme / kabul / red / imza kaydı (anonymous endpoint)                        |
| `ConvertProjectToOrderCommand`                                                          | Project → Order + OrderLine'lar + StockAllocation + (yetersiz stok → PurchaseOrder draft)           |
| `ReleaseToProductionCommand`                                                            | WorkOrder oluştur + atölye takvimine yerleştir (capacity check)                                     |
| `RescheduleWorkOrderCommand`                                                            | Atölye/montaj tarihi değişikliği — bildirim tetikler                                                |
| `RecordDefectCommand`                                                                   | Üretim/taşıma/montaj hatası — recut emir akışı                                                      |
| `CreateFieldSurveyCommand`                                                              | PWA'dan saha ölçüsü kaydı (offline batch upload)                                                    |
| `SubmitFieldSurveyCommand`                                                              | Surveyor "tamam" — proje status `Draft → Surveyed`                                                  |
| `ApplyFieldSurveyToProjectCommand`                                                      | Saha ölçülerini run.length/height'a uygula (tolerans düşürerek)                                     |
| `ImportCatalogCommand`                                                                  | CSV/XLSX upload → mapping → dry-run → commit (Profil/Cam/Aksesuar)                                  |
| `SeedDemoCatalogCommand`                                                                | Onboarding wizard — Albert Genau + Vizyon hazır seed                                                |
| `UpsertProfileSystemCommand` / `UpsertGlassTypeCommand` / `UpsertHardwareItemCommand`   | Admin katalog CRUD                                                                                  |
| `UpsertProfileItemCommand`                                                              | Tek profil + cross_section_svg upload                                                               |
| `UpsertColorOptionCommand`                                                              | RAL koduna göre + hex önizleme                                                                      |
| `UpsertBrandVendorCommand`                                                              | Marka-Tedarikçi ilişkisi + lead time                                                                |
| `UpsertHardwareKitCommand`                                                              | Kit + qty_formula doğrulama (DynamicExpresso test)                                                  |
| `UpsertDiscountRuleCommand`                                                             | İndirim kuralı tanımı                                                                               |
| `UpsertNotificationTemplateCommand`                                                     | Olay bazlı mesaj şablonu                                                                            |
| `DispatchNotificationCommand`                                                           | Event geldiğinde template render + provider send (idempotent)                                       |
| `CloneProjectCommand`                                                                   | Benzer müşteri için kopyala                                                                         |
| `AnonymizeProjectCommand`                                                               | KVKK — müşteri verisini sil/anonimleştir, geçmiş finansal kayıt korunur                             |
| `ExportProjectDataCommand`                                                              | KVKK — proje + müşteri verisinin JSON export'u (data portability)                                   |

### 4.2 Sorgular (Queries)

| Sorgu                                                  | Sonuç                                                                                     |
| ------------------------------------------------------ | ----------------------------------------------------------------------------------------- |
| `GetGlassProjectsQuery`                                | Liste (filter: status, customer, tarih, designer, salesperson); paged                     |
| `GetGlassProjectByIdQuery`                             | Header + Runs + Panels + Scene meta                                                       |
| `GetGlassProjectSceneQuery`                            | Tam scene JSON (büyük) — lazy load                                                        |
| `GetSceneVersionsQuery`                                | Versiyon listesi + thumbnail grid (karşılaştırma için)                                    |
| `GetSceneVersionDiffQuery`                             | İki versiyon arası ChangeLog kayıtları                                                    |
| `GetProjectChangeLogQuery`                             | Tüm audit kayıt akışı                                                                     |
| `GetProfileSystemsQuery`                               | Filtre: brand, system_type, glass_thickness                                               |
| `GetProfileItemsBySystemQuery`                         | Sistem detayında profiller                                                                |
| `GetHardwareItemsQuery`                                | Filtre: compatible_system_id, category                                                    |
| `GetGlassTypesQuery` / `GetColorsQuery`                | Master data                                                                               |
| `GetClimateRecommendationQuery`                        | Adres'ten climate_zone tespiti + öneri seti (ısıcam/korozyon/sismik)                      |
| `GetWindZoneByAddressQuery`                            | Adres'ten TS 498 bölge tespiti (postakodu/il bazlı)                                       |
| `GetProjectValidationQuery`                            | Canlı uyarı listesi (wind/thermal/system/panel kuralları)                                 |
| `GetProjectBOMQuery`                                   | Kategoriye göre gruplu satırlar                                                           |
| `GetCuttingReportQuery`                                | Profil + cam plan + fire özeti                                                            |
| `GetCostAnalysisQuery`                                 | Malzeme/işçilik/iskonto/kar dağılımı (donut + table verisi)                               |
| `GetTechnicalSummaryQuery`                             | Wind/Thermal/Acoustic özet — teknik PDF + müşteri kartı için                              |
| `GetQuotePdfQuery`                                     | İmzalı download URL                                                                       |
| `GetShareViewerProjectQuery`                           | **Anonymous** — token'a göre frozen scene + price (auth bypass, tenant token'dan çözülür) |
| `GetProductionScheduleQuery`                           | Atölye takvimi (Gantt) — tarih aralığı + makine filtresi                                  |
| `GetWorkshopCapacityQuery`                             | Gün/hafta bazlı doluluk yüzdesi                                                           |
| `GetStockShortageForProjectQuery`                      | Stok yetersizliği listesi + tedarikçi önerisi + lead time                                 |
| `GetVendorSuggestionsQuery`                            | Profil/aksesuar için BrandVendor üzerinden tedarikçi sıralaması                           |
| `GetNotificationHistoryQuery`                          | Proje bildirim akışı + status                                                             |
| `GetFieldSurveyByIdQuery` / `GetSurveysByProjectQuery` | Saha ölçüm geçmişi                                                                        |
| `GetCatalogImportPreviewQuery`                         | Excel/CSV import dry-run sonuçları (validation + diff)                                    |
| `GetCatalogStatsQuery`                                 | Admin dashboard: kaç sistem/profil/cam/aksesuar; eksik vendor link                        |
| `GetOnboardingStatusQuery`                             | Tenant onboarding adım durumu (markalar seçildi mi, atölye ayarı yapıldı mı)              |

### 4.3 Servisler (Pure Logic — Test Edilebilir)

```
ICuttingOptimizer1D
  → IReadOnlyList<Bar> Plan(IEnumerable<Cut> cuts, int stockBarMm, int sawKerfMm, decimal kerfMm = 5)
  Algoritma: First-Fit Decreasing (kanıtlanmış 11/9 asimptotik oran) + lokal arama (2-swap)
  Çıktı: her stok bar için kesim sırası, fire mm, fire %

ICuttingOptimizer2D
  → SheetLayout Plan(IEnumerable<Rect> rects, Size jumbo, decimal kerfMm = 4, bool guillotineOnly)
  Algoritma:
    - guillotineOnly = false → Maximal Rectangles (Best Short Side Fit)
    - guillotineOnly = true  → Guillotine cut (recursive horizontal/vertical split) — gerçek cam
                               kesim makineleri için zorunlu; serbest kesim imkânsız
  Çıktı: jumbo başına yerleşim koordinatları, fire alan, fire %, kesim sırası (DXF layer ayrımı)

IBOMComposer
  → BOM Compose(GlassProject project, GlassEnclosureSettings settings)
  Adımlar:
    1. Her Run için profil ihtiyacı (top, bottom, jamb x2, mullion x (panel-1), sash x panel)
    2. Her Panel için cam (width, height, glass_type)
    3. HardwareKit eşleştirmesi (system + panel count + opening_types)
    4. İşçilik (settings.labor_per_m2 × total m²)
    5. Satırları unit_cost ile zenginleştir

ICostCalculator
  → CostBreakdown Calculate(BOM bom, GlassEnclosureSettings settings)
  Çıktı: malzeme, fire, işçilik, ek aksesuar, taban maliyet, kar marjı, satış fiyatı

ISceneValidator
  → ValidationResult Validate(GlassProject project)
  Kurallar (her biri ayrı IRule implementasyonu — kompozisyon, kolay test):
    - PanelMaxWidthRule          panel_width ≤ system.max_panel_width
    - PanelMaxWeightRule         panel_weight = glass.weight + sash.weight ≤ system.max_panel_weight
    - GlassThicknessSupportRule  glass.thickness ∈ system.supported_glass_thicknesses
    - GlassMaxAreaRule           panel_area ≤ glass.max_panel_area
    - SystemOpeningCompatRule    opening_type ∈ system.supported_openings
                                 (örn. KCS sistemiyle giyotin paneli karışmaz)
    - SystemGlassStructureRule   ısıcam sistemi single-layer cam reddeder
    - WindLoadRule               wind_load_pa > glass.allowable_pa → kalın cam öner
    - SeismicPanelRule           climate.recommends_seismic_smaller_panel && panel_area > 2 m² → uyarı
    - CorrosionRule              climate corrosion C4-C5 + color.finish_type=PowderCoated yetersizse uyarı
    - HingeWeightRule            menteşeli panel + glass_weight > hinge.max_load → daha güçlü menteşe öner
    - RunConnectionAngleRule     mitre_cut_deg ∈ [10, 80] dışı → manuel kontrol gereksin
    - CertificationRule          building.fire_safety_class gerektirir ama system.fire_class yetersiz → uyarı
    - ThermalRecommendRule       climate cold + glass.u_value > 2.0 → ısıcam öner (severity=Info)
  Çıktı: List<ValidationFinding> (severity: Error | Warning | Info, code, message_key, affected_run_id, affected_panel_id)

IWindLoadCalculator
  → WindLoadResult Calculate(WindZone zone, decimal buildingHeightM, IEnumerable<PanelDim> panels)
  Adım:
    1. q = zone.wind_pressure_pa × heightFactor(buildingHeightM)        TS 498 yapısal yük
    2. her panel için: F = q × cp × A   (cp = bina form katsayısı, varsayılan 1.0 saçaksız)
    3. cam dayanım tablosundan (vendor data) min thickness gerek
  Çıktı: { calculated_pressure_pa, per_panel_required_thickness_mm, suggested_glass_type_ids }

IThermalAcousticCalculator
  → ThermalAcousticResult Calculate(GlassProject project)
  Adım:
    1. weighted_u = Σ(panel_area × glass.u_value) / Σ(panel_area)
    2. weighted_db = 10 × log10(Σ(panel_area × 10^(glass.sound_db/10)) / Σ(panel_area))  (akustik ortalama)
    3. Profil thermal_bridge düzeltmesi (settings.profile_thermal_break_factor)
  Çıktı: { weighted_u, weighted_db, estimated_winter_energy_savings_kwh, estimated_db_reduction_vs_open }
  Not: kış faturası tahmini bilgilendirici — disclaimer Quote PDF'inde.

IDiscountEngine
  → DiscountResult Apply(BOM bom, Project project, IEnumerable<DiscountRule> activeRules)
  Sıralama:
    1. CustomerGroup (otomatik, customer.group_id'ye göre)
    2. Coupon (kullanıcı kod girdi)
    3. Volume (m² > eşik)
    4. Manual (kullanıcı override — yetkiye bağlı)
  stackable=false ise birinden sonra durur; aksi halde sırayla uygulanır.
  Çıktı: original_total, discount_lines[], discounted_subtotal, savings_percent

IClimateAdvisor
  → IReadOnlyList<ClimateAdvice> Suggest(Address siteAddress)
  Tablo aramayla (il/ilçe → ClimateZone) hızlı öner; ardından öneri listesi
  (örn. "Antalya kıyı → C5 korozyon sınıfı, anodize alüminyum öneri").

INotificationDispatcher
  → Task Dispatch(NotificationEvent evt)
  Adım:
    1. Tenant + recipient locale'a uygun NotificationTemplate ara
    2. Placeholder render (Scriban template engine — küçük + güvenli)
    3. Channel adapter'a yönlendir:
         IEmailSender (SendGrid/SMTP)
         ISmsSender (NetGSM/Iletimerkezi)
         IWhatsAppSender (Twilio veya WhatsApp Cloud API)
         IInAppNotifier (SignalR push)
    4. GlassNotificationLog'a status kaydet (idempotency: provider_message_id)
    5. Failure retry policy (Polly — exponential backoff, max 3)

IProductionScheduler
  → ScheduleSlot Allocate(WorkOrder wo, GlassEnclosureSettings settings)
  Algoritma:
    1. wo.workload_m2 hesapla
    2. settings.workshop_daily_capacity_m2 ile bölerek gün sayısı bul
    3. mevcut WorkOrder takvimi tara → ilk yeterli slot
    4. çakışma → conflict response
  Çıktı: scheduled_start, scheduled_end veya ConflictException(öneri tarih)

ICatalogImporter
  → ImportResult Run(Stream file, ImportMapping mapping, ImportMode mode)
  - mode = DryRun → sadece validate + diff (yeni/değişen/silinecek)
  - mode = Commit → transaction içinde upsert
  - Format adaptörleri: XlsxImportAdapter (EPPlus), CsvImportAdapter (CsvHelper)
  Çıktı: total_rows, succeeded, errors[], created_ids[], updated_ids[]

IPurchaseOrderSuggester
  → IReadOnlyList<PurchaseOrderDraft> Suggest(IEnumerable<StockShortage> shortages)
  Adım: shortage → ProfileItem.preferred_vendor → tedarikçi başına PO draft + lead_time
  Müşteri/kullanıcı manuel onaylar (mevcut Purchasing modülüne devir).

IFieldSurveyApplier
  → ApplyResult Apply(FieldSurvey survey, GlassProject project, FieldTolerance tolerance)
  - Ham saha ölçüsünden tolerance düşer (üst -10mm, yan -5mm settings)
  - Eğim varsa max(slope_top,slope_bottom) panel yüksekliğine etki
  - ChangeLog yazar
  - Validation tetikler (boyut max sınırı)

IShareTokenService
  → ShareToken Create(GlassProject project, int sceneVersion, TimeSpan ttl)
  → ShareView? Resolve(string token, string ipHash) — anonymous read; rate-limit IP başına 60/dak

IExpressionEvaluator
  → decimal Eval(string expr, IDictionary<string, object> vars)
  Wrapper: DynamicExpresso.Interpreter
  Sandbox: tip whitelisting (decimal, int, bool), allowedMethods (ceil, floor, max, min, sqrt)
  HardwareKit qty_formula + condition_expression için.

ITransportCostCalculator
  → decimal Calculate(Project project, GlassEnclosureSettings settings)
  Algoritma: (km_from_workshop × rate_per_km) + (total_weight_kg × rate_per_kg)
  Adres → mesafe: ilk fazda kullanıcı girer; sonradan distance API entegrasyonu opsiyon.

IInstallationCostCalculator
  → decimal Calculate(Project project, GlassEnclosureSettings settings)
  - Floor < scaffolding_required → 0 iskele
  - Floor ≥ scaffolding_required → m² × scaffolding_rate
  - Floor ≥ crane_required → toplam_yükseklik × crane_rate
  - Montaj işçilik (m² × labor_rate)

IQuotePdfGenerator
  → byte[] Render(GlassProject project, QuoteTemplate template)
  Stack: QuestPDF (mevcut .NET 10 uyumlu)

IDxfExporter / ICsvCutListExporter
  CNC için kesim listesi formatı (ilk fazda DXF + CSV; SoftTech SDF sonra)
```

### 4.4 Validation

`FluentValidation` ile her command:

- Pozitif boyutlar, max uzunluk sınırı (run ≤ 12000 mm, panel ≤ 1500 mm)
- Müşteri tenant'a ait mi (cross-tenant attack koruması — Customer.tenant_id == current_tenant)
- Anonymous share viewer rate-limit (60 istek/dak/IP)
- Excel/CSV import boyut limiti (10 MB, 10k satır)
- DynamicExpresso formula injection koruması — yalnızca whitelisted method
- Status geçişleri state machine (status_transition table):
  ```
  Draft     → Surveyed | Cancelled
  Surveyed  → Draft | Quoted | Cancelled
  Quoted    → Draft | Confirmed | Cancelled
  Confirmed → InProduction | Cancelled
  InProduction → Ready | Defective
  Ready     → Installed | InTransit
  InTransit → Installed | Defective
  Defective → InProduction (recut)  veya  Cancelled
  Installed → (terminal)
  Cancelled → (terminal)
  ```
- Her transition için RolePolicy şart (örn. `Confirmed → InProduction` yalnız Approver/Admin).
- Rol policy kontrolü `[Authorize(Policy="GlassEnclosure.Approver")]` + cross-check Application katmanında.

### 4.5 Test Standartları (CLAUDE.md 8.2)

`CoreAlign.Application.Tests/GlassEnclosure/`:

- `CuttingOptimizer1DTests` — bilinen referans örnekler (örn. 6000mm bar, [2400, 2200, 1800, 1600, 1000] → 1 bar 4 kesim + 1 bar 1 kesim)
- `CuttingOptimizer2DTests` — guillotine **on/off** karşılaştırma, kerf uygulaması, max kullanım %
- `BOMComposerTests` — 3 panel sliding KCS örneği → beklenen profil sayısı/uzunluğu
- `SceneValidatorTests` — her IRule için happy + failure; kompozit Validator
- `WindLoadCalculatorTests` — TS 498 referans örnekleri (1.bölge 25m bina, 4.bölge sahil 60m)
- `ThermalAcousticCalculatorTests` — bilinen U-value ve dB kombinasyonları
- `DiscountEngineTests` — stack ordering, customer group, kupon kombinasyonları
- `ProductionSchedulerTests` — çakışma detection, slot araması
- `ExpressionEvaluatorTests` — formula sandboxing (zararlı kod reddi), happy path
- `CatalogImporterTests` — XLSX validation, hatalı satır geri bildirimi, dry-run vs commit
- `FieldSurveyApplierTests` — tolerance uygulama, eğim hesabı
- `NotificationDispatcherTests` — template render, channel adapter mock, retry
- `ShareTokenServiceTests` — rate-limit, token resolve, expired token red
- `Handler testleri` — state machine geçişleri + RolePolicy ihlal reddi (xUnit + NSubstitute + WebApplicationFactory)

**Test stratejisi:**

- Pure servis testleri (Optimizer/Calculator/Validator) %95+ coverage
- Handler testleri integration (gerçek InMemory DbContext + outbox)
- E2E: F6'da Playwright ile Designer → Quote → Order happy path

---

## 5. Frontend Mimarisi — Feature-Sliced

```
src/features/glass-enclosure/
  api/
    glassProjectsApi.ts          axios CRUD
    profileSystemsApi.ts
    profileItemsApi.ts
    glassTypesApi.ts
    colorsApi.ts
    hardwareApi.ts
    hardwareKitApi.ts
    brandVendorApi.ts
    cuttingApi.ts
    quoteApi.ts
    discountApi.ts
    notificationApi.ts
    scheduleApi.ts
    fieldSurveyApi.ts
    shareApi.ts                  anonymous + RecordShareViewerAction
    versioningApi.ts             scene versions + diff + revert
    catalogImportApi.ts
    climateApi.ts
    windZoneApi.ts
    technicalSummaryApi.ts       wind/thermal/dB
    onboardingApi.ts
  hooks/
    glassProjectKeys.ts          tek queryKey faktörisi (CLAUDE.md cache rule)
    useGlassProjectQueries.ts
    useCatalogQueries.ts
    useCuttingQueries.ts
    useDiscountQueries.ts
    useScheduleQueries.ts
    useShareQueries.ts
    useVersionQueries.ts
    useNotificationQueries.ts
    useFieldSurveyQueries.ts
    useTechnicalSummaryQueries.ts
    useClimateQueries.ts
    useOnboardingQueries.ts
    useCatalogImportMutations.ts
  model/
    project.types.ts
    panel.types.ts
    scene.types.ts
    runConnection.types.ts
    validationFinding.types.ts
    discount.types.ts
    schedule.types.ts
    share.types.ts
    notification.types.ts
    fieldSurvey.types.ts
    changeLog.types.ts
    climate.types.ts
    windZone.types.ts
    technicalSummary.types.ts
    catalogImport.types.ts
    projectSchema.ts             zod
    panelSchema.ts
    runSchema.ts
    runConnectionSchema.ts
    fieldSurveySchema.ts
    importMappingSchema.ts
    designerStore.ts             Zustand — aktif proje canlı state + command pattern (undo/redo)
    fieldSurveyStore.ts          PWA offline batch state
  scene/
    builders/
      ProfileGeometry.ts         SVG/DXF cross-section → ExtrudeGeometry
      RunGroup.ts                top+bottom+mullion+jamb birleşimi
      RunConnectionMesh.ts       L/U köşe birleşimi (mitre + corner post)
      PanelMesh.ts               cam + opening hint
      HardwareInstanced.ts       menteşe/rulman GLB instancing
      AnnotationLayer.ts         ölçü çizgileri + m² etiket + panel no
      BuildingContext.ts         opsiyonel arka plan duvar wireframe + zemin
    materials/
      glassMaterial.ts           MeshPhysicalMaterial (transmission=1, ior=1.5, roughness=0.05, thickness=glassMm)
      glassMaterialEdit.ts       düşük maliyet translucent — edit modunda kullanılır
      aluminumMaterial.ts        MeshStandardMaterial + envMap (RAL hex)
      environment.ts             paylaşımlı HDRI/PMREMGenerator (tek instance)
      qualityPreset.ts           Low | Medium | High | Ultra — kullanıcı seçer veya cihazdan otomatik
    exporters/
      sceneSerializer.ts         scene → JSON (versiyonlu, brotli sıkıştırma)
      thumbnailExporter.ts       offscreen render → PNG dataURL
      dxfExporter.ts             2D profil dizilimini CAD'e
      glbExporter.ts             AR/VR için 3D model export
      usdzExporter.ts            iOS Quick Look (F8)
      svgPlanExporter.ts         üst görünüm vektör (PDF kapağı için)
    interactions/
      commandStack.ts            undo/redo command pattern
      selectionManager.ts        seçili panel/run/connection state
      cameraPresets.ts           top/front/iso/walkthrough
      presentationMode.ts        sahne dönüş animasyonu + UI gizleme
  ui/
    designer/
      GlassEnclosureDesigner.tsx   ana ekran (R3F canvas + sağ panel)
      PlanCanvas2D.tsx             üstten görünüm — run çiz/sürükle
      Toolbar.tsx                  araç çubuğu (yeni run, gizle, ölçü göster, kalite preset)
      QualityPresetSelector.tsx    Low/Medium/High/Ultra
      PresentationButton.tsx       müşteri sunum modu
      RunInspector.tsx             run parametreleri
      RunConnectionInspector.tsx   köşe açı + corner post seçimi
      PanelInspector.tsx           seçili panel
      ValidationPanel.tsx          uyarı listesi (Error/Warning/Info filtreli)
      VersionHistoryPanel.tsx     versiyon listesi + thumbnail grid
      VersionDiffView.tsx          v3 vs v5 yan yana 3D karşılaştırma
      ChangeLogDrawer.tsx          audit trail (kim ne zaman ne)
      DiscountPanel.tsx            iskonto kuralları + manuel override
      TechnicalSummaryCard.tsx     wind load + U-value + dB + uyarılar
      ClimateAdvisorBadge.tsx      adresten otomatik öneri (kıyı, soğuk, deprem)
      CostAnalysisCard.tsx         doughnut + table (recharts)
      CuttingReportView.tsx        profil 1D + cam 2D görsel + tablo + DXF/CSV indir
      QuotePreviewModal.tsx        PDF iframe + paylaş (WhatsApp/Mail/Link)
      ShareLinkPanel.tsx           token + QR + görüntülenme istatistiği
      StockShortageAlert.tsx       PO önerisi kartı
      ProjectStatusBadge.tsx
    catalog/
      ProfileSystemPicker.tsx      katalog seçim modal
      ProfileSystemList.tsx        admin
      ProfileItemEditor.tsx        SVG cross-section upload + parametric form
      GlassTypePicker.tsx
      GlassTypeEditor.tsx
      ColorPicker.tsx              RAL paleti + canlı önizleme
      HardwareSelector.tsx
      HardwareKitEditor.tsx        qty_formula playground + test runner
      BrandVendorEditor.tsx
      DiscountRuleEditor.tsx
      NotificationTemplateEditor.tsx
      CatalogImportWizard.tsx      Excel/CSV upload + mapping + dry-run + commit
      CatalogStatsCard.tsx
    schedule/
      ProductionScheduleView.tsx   Gantt benzeri haftalık görünüm
      MachineCalendar.tsx          tek makine takvimi
      CapacityHeatmap.tsx          günlük doluluk %
    notifications/
      NotificationHistoryList.tsx  proje bazlı bildirim akışı
      NotificationStatusBadge.tsx
    onboarding/
      OnboardingFlow.tsx           çok adımlı sihirbaz (brand select, workshop, demo)
      BrandSelectStep.tsx
      WorkshopSetupStep.tsx
      DemoSeedStep.tsx
      OnboardingCompleteStep.tsx
    common/
      LabelWithInfo.tsx            tooltip + i18n
      DimensionInput.tsx           mm input + cm/in toggle
      MoneyDisplay.tsx             tenant currency formatter
      RolePolicyGate.tsx           render guard for unauthorized UI
```

```
src/pages/glass-enclosure/
  GlassProjectsPage.tsx          liste/grid + arama + filtre
  GlassProjectDesignerPage.tsx   /dashboard/glass-enclosure/:id
  GlassProductionSchedulePage.tsx  atölye takvimi (rol gated: Producer/Admin)
  GlassCatalogPage.tsx           admin: sistem/cam/aksesuar/marka-tedarikçi/iskonto/şablon
  GlassOnboardingPage.tsx        ilk kurulum sihirbazı (tenant onboarding)
  GlassReportsPage.tsx           BI: aylık satış, en çok satan sistem, kıvrak müşteri
  GlassNotificationsPage.tsx     tenant-wide bildirim merkezi

src/pages/public/
  GlassProjectShareView.tsx      /share/glass/:token — anonymous, read-only 3D + fiyat + onay
```

### 5.0 PWA: Saha Ölçü Feature (Ayrı Bundle)

`src/features/glass-enclosure-field/` — masaüstü designer'dan **bağımsız** bir PWA:

```
src/features/glass-enclosure-field/
  api/
    fieldSurveyApi.ts            offline-aware (sync queue)
    bluetoothLaserApi.ts         Web Bluetooth API — Bosch GLM, Leica DISTO
  hooks/
    useOfflineSync.ts
    useLaserDevice.ts
    useCameraCapture.ts
  model/
    surveyDraft.types.ts
    syncQueueStore.ts            IndexedDB persistence
  ui/
    FieldSurveyApp.tsx           kompakt mobil layout
    SiteMeasureForm.tsx          ölçü girişi (laser otomatik doldurur)
    SlopeMeasureForm.tsx         eğim/şakül kontrolü (cihaz sensörü)
    PhotoAnnotator.tsx           FabricJS — engelleri foto üzerine işaretle
    GpsPicker.tsx                konum
    SyncStatusBar.tsx            offline-online geçiş + senkronizasyon
    FieldHomeScreen.tsx          atanan projeler listesi
    SurveyReviewScreen.tsx       gönderim öncesi özet
  pwa/
    manifest.webmanifest         display=standalone, theme dark, offline shell
    sw.ts                        Workbox — precache + background-sync queue
    icons/                       192/512/maskable
```

Servis worker stratejisi (Workbox):

- Precache: app shell, fontlar, ikonlar
- Runtime cache: API GET (NetworkFirst, 24h)
- Background sync: SaveFieldSurvey mutations → tekrar dene online olunca

Build: ayrı Vite entry — `vite.config.ts` → `build.rollupOptions.input.field = "field.html"`. Aynı backend, aynı auth.

### 5.1 3D Designer İç Akış

```
[Yeni Proje] → Müşteri seç + Sistem ön seç
   ↓
[Plan2D] kullanıcı kalemle run çizer (drag-to-draw)
   ↓
[3D'de canlı oluşur] — extrude geometry + glass material
   ↓
[RunInspector] uzunluk/yükseklik düzenle + panel sayısını öner ("önerilen 4 panel")
   ↓
[PanelInspector] her panele opening + cam tipi + aksesuar
   ↓
[ValidationPanel] kurallar canlı kontrol (kırmızı badge)
   ↓
[BOM hesapla] (debounced) — sağ alt: ön maliyet anlık
   ↓
[Kesim Raporu Oluştur] — server-side nesting + tablo + diagram
   ↓
[Teklif PDF] — QR + WhatsApp/E-Mail paylaş
   ↓
[Siparişe Çevir] — Order + OrderLine + WorkOrder
```

### 5.2 Performans Stratejisi (Three.js Glass Rendering)

**Sorun:** `transmission=1` her transmission objesi için ayrı render pass tetikler; bir cam balkonda 20+ panel olabilir.

**Çözümler:**

1. **Tek paylaşılan envMap** (PMREMGenerator + HDRI) → her panel ayrı capture yapmaz.
2. **`thickness` üzerinden refraction simüle et**, `dispersion` kullanma (daha hızlı).
3. **LOD**: Kamera uzaktayken `transmission=0` + alpha 0.4 düz translucent; yakına gelince physical material.
4. **Edit modunda düz translucent**, yalnız "Render Önizleme" butonunda full PBR.
5. **`InstancedMesh`** profillerde — aynı kesit tekrar tekrar olduğu için tek instanced çağrı.
6. **R3F `<Detailed>`** ile kamera uzaklığına göre geometri detay.
7. Aksesuarlar için **`<Bvh>`** raycast hızlandırma (drei).

### 5.3 Inputlar & Etkileşim

- **Mouse**: orbit (sağ tık pan, sol tık seç, tek panel tıklama → Inspector açılır).
- **Klavye**: `R` rotate, `M` move, `Del` sil, `Ctrl+D` panel duplicate, `Ctrl+Z/Y` undo/redo (designerStore command pattern).
- **Dokunmatik** (tablet/saha): pinch zoom, double-tap fit.
- **Lil-gui** (mevcut): debug paneli yalnız dev mode — `import.meta.env.DEV`.
- **Birim**: tüm boyutlar `mm` (state) — 3D dünyada `1 unit = 1 metre` → `mm/1000` scale.

### 5.4 i18n + Çoklu Dil & RTL

Her metin `t("GlassEnclosure.Foo")`. Ana namespace anahtarları:

```
GlassEnclosure.Title
GlassEnclosure.Designer.NewRun | AddPanel | AddConnection | UndoRedo | Presentation
GlassEnclosure.Designer.OpeningType.Folding | Sliding | Hinged | Fixed | Guillotine
GlassEnclosure.System.Folding | Sliding | HeatInsulated | Guillotine | Hinged | Fixed
GlassEnclosure.Panel.Width | Height | Glass | Hardware | Opening
GlassEnclosure.Validation.PanelTooWide | GlassThicknessMismatch | WeightExceeds |
                          WindLoadFail | SeismicTooLarge | CorrosionRisk |
                          HingeCapacityExceeded | SystemOpeningMismatch
GlassEnclosure.Report.CuttingPlan | CostAnalysis | Quote | TechnicalSummary | WindLoad
GlassEnclosure.Status.Draft | Surveyed | Quoted | Confirmed | InProduction |
                       Ready | InTransit | Installed | Defective | Cancelled
GlassEnclosure.Catalog.System | Profile | GlassType | Color | Hardware | Brand | Vendor |
                        Discount | Notification | Import
GlassEnclosure.Field.NewSurvey | LaserConnect | SlopeMeasure | SyncPending | OfflineMode
GlassEnclosure.Share.AcceptDesign | RejectDesign | RequestChanges | SignDocument
GlassEnclosure.Notification.QuoteSent | QuoteAccepted | OrderConfirmed | ProductionStarted
GlassEnclosure.Schedule.NoCapacity | Conflict | Reschedule | DailyCapacity
GlassEnclosure.Onboarding.WelcomeStep | BrandStep | WorkshopStep | DemoStep | Complete
```

**Dil destek matrisi (F7 sonrası):**

| Dil       | Kod   | RTL      | Faz | Pazar                      |
| --------- | ----- | -------- | --- | -------------------------- |
| Türkçe    | tr-TR | hayır    | F1  | Yerel                      |
| İngilizce | en-US | hayır    | F1  | Genel                      |
| Almanca   | de-DE | hayır    | F7  | Türk göçmen + ihracat      |
| Arapça    | ar-SA | **evet** | F7  | Körfez (Suudi, BAE, Katar) |
| Rusça     | ru-RU | hayır    | F8  | Rusya/Orta Asya            |

**RTL altyapısı:**

- `<html dir="rtl">` koşullu (i18n locale değişikliğinde)
- Tailwind `dir:` variantları (Tailwind v4 destekli)
- 3D scene: yatay aksis ters çevrilmez (fiziksel sahne) — yalnız UI çerçevesi RTL
- Icon set (Lucide) — yön içeren ikonlar (arrow-left/right) `rtl:rotate-180`

tr.json + en.json F1'de eş zamanlı; ar.json + de.json F7'de. Alfabetik gruplanır. ESLint kuralı: hard-coded string yasak (mevcut CLAUDE.md kuralı).

### 5.5 Sidebar Entegrasyonu

`Sidebar.tsx` `navigation` dizisine yeni grup:

```ts
{ section: 'MANUFACTURING' },
{ name: 'Glass Enclosure', icon: SquareStack, moduleCode: 'glass-enclosure',
  children: [
    { name: 'Projects', href: '/dashboard/glass-enclosure' },
    { name: 'New Design', href: '/dashboard/glass-enclosure/new' },
    { name: 'Catalog', href: '/dashboard/glass-enclosure/catalog' },
  ],
  hideWhenEmptyChildren: true,
},
```

`moduleCode` gating sayesinde abonelik olmayan tenant'ta görünmez.

### 5.6 Permission UI

Backend RolePolicy (bkz. §10) UI'da iki katmanda enforce edilir:

1. **Route gate** — `<ProtectedRoute requiredPolicies={["GlassEnclosure.Approver"]}>`
2. **Component gate** — `<RolePolicyGate policy="GlassEnclosure.PriceEdit"><EditPriceButton /></RolePolicyGate>` (yetkisi yoksa hiç render etmez)
3. **Action gate** — mutation hook'u optimistic UI'yi geri alır + toast (`safeRequestWithNotify` mevcut altyapı)

Salesperson `priceEdit` görmez ama `quoteCreate` görür. Producer `cuttingReport` indirir, fiyat satırını görmez. Installer yalnız WorkOrder checklist UI'sini açar.

### 5.7 Erişilebilirlik (a11y) ve Performans Bütçesi

- WCAG 2.1 AA hedef: kontrast oranı, klavye odak, ARIA label, screen reader landmark
- 3D canvas için klavye alternatifi: panel listesi yan panelde — ok tuşlarıyla seçim
- Performans bütçesi (Vite production build):
  - First Contentful Paint < 1.8s
  - Time to Interactive < 3.5s
  - Designer bundle ayrı chunk (lazy import — proje listesi sayfasında yüklenmez)
  - HDRI dosyası 2 MB altı (basis encoded)
  - GLB aksesuar modelleri lazy load (görünür panel için)

## 6. Üretim Çıktıları (Mesleki Profesyonellik)

### 6.1 Kesim Raporu (PDF + CSV)

**Profil 1D Bölümü:**

- Sisteme + renge göre gruplu
- Her stok bar (6000 mm) için sırayla kesim listesi (örn. `2400 | 1800 | 1700 | fire 95`)
- Toplam bar adedi, toplam fire mm, fire %
- Kesim makinesine CSV: `bar_no,position,length_mm,label,note`

**Cam 2D Bölümü:**

- Jumbo cam (varsayılan 3210×2250 mm) yerleşim diyagramı (SVG)
- Her dikdörtgen üzerinde panel referansı
- Toplam jumbo adedi, fire m²
- DXF export (her jumbo bir layer)

**Tasarım kalitesi:** A4 portrait, sol blok teknik veri tablo, sağ blok diyagram; CoreAlign header, proje kodu, müşteri, tarih, sayfa numarası.

### 6.2 Maliyet Analizi

| Kategori                                                 | Tutar | %    |
| -------------------------------------------------------- | ----- | ---- |
| Profil malzeme                                           | …     | …    |
| Cam malzeme                                              | …     | …    |
| Aksesuar (menteşe/rulman/conta/fırça/kilit)              | …     | …    |
| Fire maliyeti (profil + cam)                             | …     | …    |
| Atölye işçiliği (m² × birim)                             | …     | …    |
| Nakliye (km × ağırlık)                                   | …     | …    |
| Montaj işçiliği (m² × birim)                             | …     | …    |
| İskele (kat ≥ eşik ise)                                  | …     | …    |
| Cephe vinç (asansör yetersiz panel)                      | …     | …    |
| Taşıma + montaj sigortası                                | …     | …    |
| **Taban maliyet**                                        | …     | 100% |
| Kar marjı (config + manuel override)                     | …     | …    |
| Satış fiyatı (KDV hariç, indirimsiz)                     | …     | …    |
| **− İndirim** (CustomerGroup / Coupon / Volume / Manual) | (−)   | …    |
| **Satış fiyatı (KDV hariç, indirimli)**                  | …     | …    |
| Ödeme vade farkı (taksitli ise)                          | …     | …    |
| KDV (varsayılan %20 — TaxRate üzerinden)                 | …     | …    |
| **Müşteri toplam**                                       | …     | …    |

Donut chart + bar chart (recharts mevcut). Maliyet açılımı yetkili rollere (Designer/Approver/Admin) gösterilir; Salesperson yalnız taban+satış toplamını görür (bkz. §10 Permission Matrix).

### 6.2.1 Maliyet Açıklama Tooltipleri

Her satır için `?` ikonu — kullanıcı tıkladığında formülü gösterir:

- Profil malzeme: `Σ(profil.weight_kg_per_m × cut_length_m × profile.kg_price)`
- Cam: `Σ(panel_area_m2 × glass.price_per_m2)` + fire payı
- İskele: `floor_number ≥ scaffolding_required ? area_m2 × settings.scaffolding_rate : 0`
- Nakliye: `(km × settings.transport_rate_per_km) + (weight × settings.transport_rate_per_kg)`

Bu **müşteriye değil**, satış ekibine güven verir — "rakam nereden çıktı" sorusu kalmaz.

### 6.3 Teklif (Quote PDF)

- Marka şablonu (tenant `OrganizationName`, logo, IBAN, vergi no)
- Kapak: 3D scene PNG render (server-side puppeteer veya client thumbnailExporter çıktısı)
- Sayfa 2: ölçü tablosu + sistem özeti
- Sayfa 3: panel düzeni şeması (top view)
- Sayfa 4: fiyatlandırma + geçerlilik + ödeme şartları
- Son sayfa: imza alanı + QR (kısa URL — read-only viewer)
- Paylaş: WhatsApp `https://wa.me/?text=`, mailto, kısa link kopyala

### 6.4 Sipariş Aktarımı

`ConvertProjectToOrderCommand` akışı:

1. `BOM` mevcut & geçerli mi?
2. Yeni `Order` (mevcut entity): customer_id, currency, status=Confirmed
3. Her `BOMLine` → `OrderLine` (description = "SlideMaster Top 6000mm × Beyaz - 8 adet")
4. Mevcut `StockAllocation` ile profil/cam stoğu rezerve et — yetersizse PurchaseOrder önerisi
5. `GlassWorkOrder` oluştur, üretim takvimine düşür
6. Project status → `Confirmed`

### 6.5 Çizim Düzenleme & Şıklık

- **Annotations**: ölçü çizgileri (drei `Line` + `Text`); m² etiketleri; panel numarası
- **Materials**: cam reflektif HDRI, alüminyum brushed normal map, hafif AO
- **Lighting**: tek directional + ambient + IBL; gölge (PCFSoft)
- **Ortam**: bina arka planı opsiyonel (gerçek balkon hissi için duvar wireframe + zemin)
- **Export modları**: vektör 2D PDF (plan/önden görünüş) + PNG 4K + GLB + USDZ (iOS AR)
- **Sunum modu**: arayüz gizle, sol-sağ sahne dönüş animasyonu (müşteriye gösterim)
- **Kalite presetleri** (kullanıcı seçimi):
  - **Low** — translucent cam, instanced profil, no IBL → mobil/zayıf GPU 60 FPS
  - **Medium** — physical material düşük rezolüsyon, basit gölge → laptop 30+ FPS
  - **High** — full physical + IBL + SSAO → masaüstü 60 FPS, varsayılan
  - **Ultra** — Pathtracing önizleme (F8 — react-three-rapier yerine three-mesh-bvh + path-tracer) → render snapshot için

### 6.6 Teknik Özet Raporu (Yeni)

Quote PDF'in 3. sayfası — **rakipler bu raporu sunmuyor**, satış argümanı:

```
┌──────────────────────────────────────────────────────────────┐
│ TEKNİK ÖZET — Cam Mekan Projesi GE-2026-0142                 │
├──────────────────────────────────────────────────────────────┤
│ Yapısal Yük Hesabı (TS 498 Bölge 3 — Sahil)                  │
│   Hesaplanan rüzgar basıncı: 1280 Pa                         │
│   Maksimum panel ölçüsü:    1450 × 2200 mm                   │
│   Önerilen cam: 8 mm temperli (mevcut) ✓ uygun               │
│                                                              │
│ Termal Performans                                            │
│   Ağırlıklı U-değeri: 1.6 W/m²K (ısıcam)                     │
│   Tahmini yıllık ısı tasarrufu: ~ 480 kWh                    │
│   (450 m² yaşam alanı, İstanbul iklim verisi)                │
│                                                              │
│ Akustik Yalıtım                                              │
│   Ağırlıklı azaltma: 32 dB                                   │
│   Trafik gürültüsü (kapalıyken): ~ -22 dB                    │
│                                                              │
│ Standartlar                                                  │
│   Cam: TS EN 12150 (temperli) ✓                              │
│   Sistem: EU Class 2A sızdırmazlık ✓                         │
│   Korozyon: ISO 12944 C4 — anodize alüminyum ✓               │
└──────────────────────────────────────────────────────────────┘
*Disclaimer: Enerji tasarrufu tahmini bilgilendiricidir.
```

Bu blok bilgilendirme + sertifika kanıtı + müşteri güveni sağlar.

### 6.7 Saha Ölçü Raporu

PWA'dan toplanan veri tek sayfa PDF olarak da basılabilir (Surveyor + Designer arası iletişim):

- Ölçü zamanı + GPS
- Ham ölçüler (üst, alt, yan kirişler)
- Eğim/şakül değerleri (kritik — fabrikaya uyarı)
- Engelleri işaretlenmiş site fotoğrafı
- Tolerance düşürülmüş **sipariş ölçüsü** (otomatik hesap)
- Surveyor imzası

### 6.8 Müşteri Portal Görünümü (Share Viewer)

`/share/glass/:token` URL — **kimlik gerektirmez**, tenant context token'dan çözülür.

İçerik:

- 3D scene (read-only, döndür/zoom/pan)
- Renk/sistem seçimi kilitli — yalnız görüntüleme
- Fiyat (KDV dahil müşteri toplam) — açılım gösterimi opsiyonel ayar
- "Tasarımı Onaylıyorum" buton → kanvas imza + checkbox "Şartlarımı kabul ediyorum"
- "Değişiklik İste" buton → metin formu (Designer'a in-app notif)
- "PDF Al" — Quote indirme

Güvenlik:

- Rate limit 60 req/dk/IP
- Expiration check
- View telemetri (last_viewed_at, view_count → CRM)
- HTTPS zorunlu (Strict-Transport-Security)
- `frame-ancestors 'none'` — clickjacking koruması

### 6.9 Bildirim Akışı (Customer Journey Otomasyonu)

| Olay                    | Müşteriye                              | Ekibe                            |
| ----------------------- | -------------------------------------- | -------------------------------- |
| Teklif gönderildi       | WhatsApp + Email (link + 3D thumbnail) | Salesperson InApp "gönderildi"   |
| Teklif görüntülendi     | —                                      | Salesperson InApp "müşteri açtı" |
| Teklif kabul edildi     | Email "siparişiniz oluştu"             | Approver InApp + Email           |
| Sipariş üretime alındı  | SMS "üretim başladı, tahmini tarih X"  | Producer InApp                   |
| Stok eksik              | —                                      | Producer + Admin "PO öner"       |
| Üretim tamamlandı       | WhatsApp "montaj için randevu alalım"  | Salesperson InApp                |
| Montaj tamamlandı       | Email "teslim formu + garanti"         | Admin InApp                      |
| Ödeme vadesi yaklaşıyor | SMS hatırlatma                         | Accountant InApp                 |

Şablonlar `NotificationTemplate` üzerinden tenant özelleştirebilir.

---

## 7. Veri Akışı (Tek Bakışta)

```
                    ┌─────────────────┐
   Frontend (R3F)   │ designerStore   │  Zustand canlı state
                    │ (project draft) │
                    └────────┬────────┘
                             │ debounced 800ms
                             ▼
                    SaveSceneStateCommand ─► PostgreSQL (glass_project_scenes)
                             │
                             ▼
   Kullanıcı "Maliyet"    RecomputeBOMCommand
   butonuna basar         │
                          ▼
                  BOMComposer (Application)
                          │
                          ▼
              CostCalculator + (CuttingOptimizer1D|2D)
                          │
                          ▼
                  bom_lines + cutting_plan tabloları
                          │
                          ▼
            GetCostAnalysisQuery → UI (CostAnalysisCard)

   "Teklife Çevir"   GenerateQuoteCommand
                     │
                     ▼
                QuotePdfGenerator (QuestPDF)
                     │
                     ▼
              quote_snapshots + Storage (S3/Local)
                     │
                     ▼
                 share_token (kısa URL)

   "Siparişe Çevir"  ConvertProjectToOrderCommand
                     │
                     ▼
                Order + OrderLine + GlassProjectOrderLink + WorkOrder
                + StockAllocation (mevcut akış)
                + StockShortage → PurchaseOrderSuggester (draft PO)
                + ProductionScheduler.Allocate (atölye takvim slotu)


   Saha (PWA, opsiyonel offline)
                     │
                     ▼
              FieldSurvey IndexedDB (Workbox queue)
                     │
                     ▼  (online olunca)
              CreateFieldSurveyCommand (batch upload)
                     │
                     ▼
              SubmitFieldSurveyCommand (surveyor)
                     │
                     ▼
              ApplyFieldSurveyToProjectCommand (Designer onay)
                     │
                     ▼
              project.status: Draft → Surveyed
              run.length/height ← (saha ölçü − tolerance)


   Müşteri Portalı (anonymous)
       │
       ▼
   GET /share/glass/{token} → GetShareViewerProjectQuery
       │
       ▼
   Public read-only 3D render + price + accept/reject
       │
       ▼  müşteri imza atar
   RecordShareViewerActionCommand
       │
       ▼
   share_token.accepted_at_utc + signature_image_url
       │
       ▼
   Trigger: QuoteAccepted event


   Bildirim Hattı (event-driven)
       │
       ▼
   Domain Event (örn. QuoteAccepted)
       │
       ▼
   OutboxDrainBehavior → DispatchNotificationCommand
       │
       ▼
   NotificationDispatcher (template render — Scriban)
       │
       ▼
   ┌─────────────┬──────────────┬─────────────┬──────────┐
   │ IEmailSender│ ISmsSender   │IWhatsAppSend│IInAppNotf│
   │ (SendGrid)  │ (NetGSM)     │(Meta Cloud) │(SignalR) │
   └─────┬───────┴──────┬───────┴──────┬──────┴────┬─────┘
         ▼              ▼              ▼           ▼
   GlassNotificationLog (status: Pending→Sent→Delivered→Read)


   Versiyonlama
       │
       ▼
   SaveSceneStateCommand → glass_project_scenes (append-only, brotli)
                        + glass_project_change_logs (atomic diff)
       │
       ▼
   GetSceneVersionsQuery → VersionHistoryPanel (thumbnail grid)
       │
       ▼
   CompareSceneVersionsCommand → VersionDiffView (yan yana 3D)
       │
       ▼
   RevertToSceneVersionCommand → yeni versiyon olarak işaretle (eski versiyon korunur)
```

---

## 8. Fazlı Yol Haritası

| Faz                                               | Süre                | Çıktı                                                                                                                                                                                                                                                                                                                                                                                                                                         |
| ------------------------------------------------- | ------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| **F0 — Onay & Hazırlık**                          | 0.5 hafta           | §11 onay sorularının cevaplanması, mimari kararların kilitlenmesi, demo seed verisi toplama                                                                                                                                                                                                                                                                                                                                                   |
| **F1 — Domain & Katalog**                         | 3 hafta             | 30 entity (yeni: WindZone, ClimateZone, BrandVendor, DiscountRule, NotificationTemplate, RunConnection, ChangeLog, FieldSurvey, ShareToken, NotificationLog) + migrations + seed (Albert Genau SlideMaster + Vizyon Gold + 4 ısıcam + temel aksesuar + RAL CLASSIC 216 renk + TR WindZone tablosu + 7 ClimateZone) + Katalog CRUD UI + DiscountRule UI + NotificationTemplate editor + **Onboarding Wizard** + **CatalogImportWizard (XLSX)** |
| **F2a — Field Survey PWA**                        | 2 hafta             | Ayrı PWA bundle, Web Bluetooth lazer entegrasyon, offline-sync (Workbox + IndexedDB), foto-anote (FabricJS), eğim/şakül kayıt, surveyor ekibi UI                                                                                                                                                                                                                                                                                              |
| **F2b — Designer MVP**                            | 3 hafta             | Tek run + N panel + R3F render + scene save/load + zod doğrulama + ChangeLog audit + undo/redo (command stack) + ValidationPanel iskeleti                                                                                                                                                                                                                                                                                                     |
| **F3 — Katalog Bağlama & Multi-run + Validation** | 2.5 hafta           | Sistem/cam/renk/aksesuar picker + L/U cam balkon (RunConnection) + Tüm SceneValidator kuralları (12 kural) + ClimateAdvisor + WindZone otomatik öneri                                                                                                                                                                                                                                                                                         |
| **F4 — BOM, Kesim & Wind/Thermal Hesap**          | 2.5 hafta           | BOMComposer + CuttingOptimizer1D/2D (guillotine + non-guillotine) + WindLoadCalculator + ThermalAcousticCalculator + rapor görselleri + kesim PDF + Teknik Özet bölümü                                                                                                                                                                                                                                                                        |
| **F5 — Maliyet, İskonto & Teklif**                | 2.5 hafta           | CostCalculator (10 satırlık genişletilmiş tablo) + DiscountEngine + TransportCost + InstallationCost + QuestPDF şablonu (kapak/teknik/fiyat sayfası) + ShareToken üretici                                                                                                                                                                                                                                                                     |
| **F6 — Sipariş, Üretim, Vendor & Share Viewer**   | 3 hafta             | Order conversion + StockAllocation + WorkOrder + DXF/CSV export + **Anonymous Share Viewer** (3D + imza + kabul/red) + **Üretim Takvimi** (ProductionScheduler) + PurchaseOrderSuggester (stok altı PO draft)                                                                                                                                                                                                                                 |
| **F7 — Bildirim, KVKK & Versioning UI**           | 2 hafta             | NotificationDispatcher (Email/SMS/WhatsApp Cloud API + Twilio fallback) + GlassNotificationLog + olay tabanlı template render + VersionDiffView (yan yana 3D) + KVKK endpoint (Export/Anonymize) + data retention job                                                                                                                                                                                                                         |
| **F8 — Cila, Çoklu Dil & RTL**                    | 2 hafta             | LOD, env map, walkthrough mode, sunum modu, kalite presetleri, **AR-DE locale**, **RTL desteği**, a11y AA uyumluluk, performans bütçesi denetimi                                                                                                                                                                                                                                                                                              |
| **F9 — AR/VR + AI**                               | 4 hafta (opsiyonel) | iOS USDZ + Android Scene Viewer + WebXR Quest + GPT-4V fotoğraftan ölçü tahmini + sistem önerici (RAG) + fiyat anomali tespiti                                                                                                                                                                                                                                                                                                                |

**Toplam:** ~25 hafta MVP-to-Production (F0-F7) + 2 hafta cila (F8) + opsiyonel 4 hafta genişleme (F9).

**Kritik bağımlılıklar:**

- F0 → F1 (kararlar olmadan domain seed başlamaz)
- F1 katalog + F2a field birbirinden bağımsız → **paralel** geliştirilebilir
- F2b designer F1'in catalog kısmına bağımlı (sistem seçimi olmadan render anlamsız)
- F4 wind/thermal F1'in zone seed'ine bağımlı
- F6 share viewer F5 PDF üretimine bağımlı
- F8 RTL F7 sonrası — i18n stabilleşmeli

**Paralelleştirme şansı:** F2a + F2b paralel (2 dev), F4 + F5 paralel (1 backend + 1 frontend), F6 share viewer + F7 notification paralel.

**Ekipsel öneri:** 2 backend + 2 frontend + 1 designer + 0.5 DevOps. Solo geliştirme tahmini ~32 hafta.

---

## 9. Riskler & Önlemler

| Risk                                    | Etki                                               | Önlem                                                                                                                                            |
| --------------------------------------- | -------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------ |
| Transmission render maliyeti            | Düşük FPS                                          | LOD + tek envMap + edit modunda translucent + InstancedMesh + kalite preset                                                                      |
| Profil kesit verisi (SVG/DXF) eksikliği | Geometri yanlış                                    | F1'de 2 sistemle başla; vendor PDF kataloglarından elle SVG çıkar; sonradan importer ekle; **parametric_description_json** fallback              |
| Kesim algoritması yanlış fire           | Para kaybı                                         | FFD + bilinen test kümeleriyle unit test; kullanıcıya gösterilen "alternatif" plan (manuel müdahale); guillotine flag ile makine kısıtı eşleştir |
| **Guillotine kısıtı atlanırsa**         | Üretim raporu makinede çalışmaz, ham kayıp         | `settings.guillotine_required` zorunlu seçim; CuttingOptimizer2D iki ayrı algoritma branch'i; F4 test kapsamı                                    |
| **Saha eğim/şakül atlanırsa**           | Panel ölçüsü yanlış → müşteriye yanlış sipariş     | FieldSurvey'de zorunlu alan; üretim öncesi `IFieldSurveyApplier` doğrulaması; rapor PDF surveyor imzası                                          |
| **Wind load hesabı atlanırsa**          | Yasal/hukuki sorumluluk (yüksek katta cam kırılma) | `ComputeWindLoadCommand` Confirmed→InProduction transition'ında zorunlu; PDF teknik sayfasında imza                                              |
| Çok dilli ondalık ayraç                 | Maliyet hatası                                     | tüm hesap server-side `decimal`; UI sadece formatter; numberFormatting.ts mevcut                                                                 |
| Tenant verileri sızıntısı               | Güvenlik                                           | mevcut global query filter zaten otomatik; her endpoint policy + tenant context test; share token rate limit                                     |
| 3D state büyüklüğü                      | DB şişme                                           | scene_json'u brotli sıkıştır + versiyonla; eski versiyon (90 günden eski + customer_approved=false) arşivle                                      |
| CNC formatları çeşitliliği              | Müşteri başına özelleştirme                        | İlk fazda DXF + CSV; format adaptör pattern + plugin tabanlı eklenir                                                                             |
| QuestPDF lisans (commercial)            | Yasal                                              | community sürüm (Apache 2.0) yıllık ciro < 1M USD ise ücretsiz; aşılırsa lisans satın al **veya iText OSS** fallback (F0 karar)                  |
| Render kalitesi beklentisi              | Müşteri hayal kırıklığı                            | UI'da "Teknik Önizleme" rozeti; gerçek render export'unu Blender batch'e bağla (F9); kalite preset opsiyonu                                      |
| **WhatsApp Business API onay süreci**   | Faz 7 mesajlaşma çalışmaz                          | F0'da provider seç (Twilio veya Meta Cloud API); WhatsApp Business hesabı başvurusu F1 başında — onay 2-4 hafta sürer                            |
| **KVKK uyumluluk**                      | Yasal yaptırım                                     | F7'de Export + Anonymize endpoint; saklama süresi tenant ayar; site fotoğraflarına 30 gün retention default                                      |
| **Excel/CSV import veri kalitesi**      | Yanlış katalog → yanlış teklif                     | DryRun zorunlu, kullanıcı diff onaylamadan commit olmaz; hata satırı CSV indir; rollback transaction                                             |
| **DynamicExpresso formula güvenlik**    | Code injection (kullanıcı qty_formula yazıyor)     | Tip whitelisting, allowed method listesi, max execution time 100ms, sandbox                                                                      |
| **Stok rezervasyon race condition**     | Aynı stoğu iki proje rezerve eder                  | Mevcut StockAllocation row-level lock; outbox pattern; concurrency conflict 409 + UI retry                                                       |
| **WindZone/ClimateZone hatalı tespit**  | Yanlış öneri                                       | İl/postakodu ↔ Zone tablosu hata payı; kullanıcı override mümkün; öneri "info" seviyesi                                                          |
| **Saha PWA offline veri kaybı**         | Surveyor saatler süren ölçümü kaybeder             | IndexedDB persist + auto-save 5sn + Workbox background sync; submit öncesi local backup uyarı                                                    |
| **Customer rejection sonrası süreç**    | Kayıp lead, takipsiz fırsat                        | ShareToken reject → in-app notif + CRM lead status update; tekrar tasarım komutu otomatik öneri                                                  |
| **Üretim takvim çakışması**             | İki proje aynı slot                                | `IProductionScheduler` ilk uygun slot bulur; manuel reschedule UI; capacity heatmap                                                              |
| **Performans testi atlanması**          | Production'da yavaş designer                       | F8 sonrası performans bütçesi denetimi otomatik (Lighthouse CI); bundle size limiti                                                              |

---

## 10. CoreAlign Kurallarına Uyum Kontrol Listesi

- [x] Yorum yasağı — tüm kodda 0 yorum (yalnız `// WHY:` istisnası)
- [x] Lint sıfır warning — TreatWarningsAsErrors zaten açık
- [x] `console.*` yasağı — `logger` kullanılır
- [x] Hardcoded metin yok — `t()` + tr/en eş zamanlı
- [x] FSD katman yönü — `shared → features/glass-enclosure → pages/glass-enclosure → app`
- [x] Tailwind v4 + dark mode + responsive
- [x] safeRequest tüm API çağrılarında
- [x] Backend Clean + CQRS + FluentValidation + MediatR
- [x] Controller ≤ 10 satır gövde
- [x] ExceptionHandlingMiddleware tek hata noktası
- [x] snake_case DB + TenantEntity türevi + global query filter
- [x] Migration adı: `AddGlassEnclosureSchema`
- [x] Test: handler + optimizer + validator için xUnit + NSubstitute
- [x] Multi-tenant izolasyon — `Customer`, `Order` zaten korumalı

### 10.1 Permission Matrix (RBAC)

Backend policy adı (CoreAlign konvansiyonu — `GlassEnclosure.<Action>`); UI'da `RolePolicyGate` aynı isimleri kontrol eder.

| Aksiyon                                          | Salesperson | Designer | Approver | Producer | Installer | Surveyor | Admin |
| ------------------------------------------------ | :---------: | :------: | :------: | :------: | :-------: | :------: | :---: |
| `Project.View` (kendi)                           |      ✓      |    ✓     |    ✓     |    ✓     |     ✓     |    ✓     |   ✓   |
| `Project.View.All` (tüm tenant)                  |      —      |    —     |    ✓     |    ✓     |     —     |    —     |   ✓   |
| `Project.Create`                                 |      ✓      |    ✓     |    —     |    —     |     —     |    —     |   ✓   |
| `Project.Update` (Draft/Surveyed)                |      —      |    ✓     |    ✓     |    —     |     —     |    —     |   ✓   |
| `Project.Delete`                                 |      —      |    —     |    ✓     |    —     |     —     |    —     |   ✓   |
| `Project.Clone`                                  |      ✓      |    ✓     |    —     |    —     |     —     |    —     |   ✓   |
| `Designer.Open` (3D tasarımcı)                   |      —      |    ✓     |    ✓     |    —     |     —     |    —     |   ✓   |
| `Designer.PriceVisible` (maliyet detayını gör)   |   sınırlı   |    ✓     |    ✓     |    —     |     —     |    —     |   ✓   |
| `Designer.PriceEdit` (manuel override)           |      —      |    —     |    ✓     |    —     |     —     |    —     |   ✓   |
| `Designer.DiscountApply`                         |   sınırlı   |    ✓     |    ✓     |    —     |     —     |    —     |   ✓   |
| `Designer.DiscountOverride` (manuel)             |      —      |    —     |    ✓     |    —     |     —     |    —     |   ✓   |
| `Quote.Generate`                                 |      ✓      |    ✓     |    ✓     |    —     |     —     |    —     |   ✓   |
| `Quote.Send` (müşteriye)                         |      ✓      |    ✓     |    ✓     |    —     |     —     |    —     |   ✓   |
| `Quote.Accept` (manuel onay)                     |      —      |    —     |    ✓     |    —     |     —     |    —     |   ✓   |
| `Order.Convert` (Project → Order)                |      —      |    —     |    ✓     |    —     |     —     |    —     |   ✓   |
| `Production.Release` (üretime ver)               |      —      |    —     |    ✓     |    ✓     |     —     |    —     |   ✓   |
| `Production.Schedule`                            |      —      |    —     |    ✓     |    ✓     |     —     |    —     |   ✓   |
| `Production.UpdateStatus` (Cutting → Assembling) |      —      |    —     |    —     |    ✓     |     —     |    —     |   ✓   |
| `Production.RecordDefect`                        |      —      |    —     |    —     |    ✓     |     ✓     |    —     |   ✓   |
| `CuttingReport.Download`                         |      —      |    ✓     |    ✓     |    ✓     |     —     |    —     |   ✓   |
| `Installation.UpdateStatus`                      |      —      |    —     |    —     |    —     |     ✓     |    —     |   ✓   |
| `Installation.CompleteChecklist`                 |      —      |    —     |    —     |    —     |     ✓     |    —     |   ✓   |
| `FieldSurvey.Create`                             |      —      |    —     |    —     |    —     |     —     |    ✓     |   ✓   |
| `FieldSurvey.Submit`                             |      —      |    —     |    —     |    —     |     —     |    ✓     |   ✓   |
| `FieldSurvey.Approve`                            |      —      |    ✓     |    ✓     |    —     |     —     |    —     |   ✓   |
| `Catalog.View`                                   |      ✓      |    ✓     |    ✓     |    ✓     |     ✓     |    ✓     |   ✓   |
| `Catalog.Update`                                 |      —      |    —     |    —     |    —     |     —     |    —     |   ✓   |
| `Catalog.Import` (XLSX)                          |      —      |    —     |    —     |    —     |     —     |    —     |   ✓   |
| `DiscountRule.Update`                            |      —      |    —     |    —     |    —     |     —     |    —     |   ✓   |
| `NotificationTemplate.Update`                    |      —      |    —     |    —     |    —     |     —     |    —     |   ✓   |
| `BrandVendor.Update`                             |      —      |    —     |    —     |    —     |     —     |    —     |   ✓   |
| `Settings.Update`                                |      —      |    —     |    —     |    —     |     —     |    —     |   ✓   |
| `Settings.WindZone` (override)                   |      —      |    —     |    ✓     |    —     |     —     |    —     |   ✓   |
| `Anonymize` (KVKK)                               |      —      |    —     |    —     |    —     |     —     |    —     |   ✓   |
| `Export.ProjectData` (KVKK)                      |      —      |    —     |    ✓     |    —     |     —     |    —     |   ✓   |
| `Reports.View`                                   |   sınırlı   |    —     |    ✓     |    ✓     |     —     |    —     |   ✓   |

**Notlar:**

- "sınırlı" = aksiyon var ama bazı alanlar maskeli (örn. Salesperson maliyet açılımını görmez, yalnız net satış+iskonto)
- `Admin` her aksiyonu yapar ama UI'da "yıkıcı" aksiyonlar (Delete, Anonymize) için onay diyaloğu zorunlu
- Bir kullanıcı birden fazla rol taşıyabilir (örn. küçük firma sahibi: Designer+Approver+Admin)
- Backend her endpoint'te `[Authorize(Policy="GlassEnclosure.X")]` + cross-check Application katmanında (defense in depth)

### 10.2 Onay Adımları (Sales-to-Cash)

```
Salesperson → Project oluştur + Quote.Generate
                      ↓
                  Customer (share token)
                      ↓ Accept
              Approver onay (sistem otomatik veya manuel)
                      ↓
              Order.Convert + Production.Release
                      ↓
                  Producer
                      ↓ Status updates
                  Installer (saha)
                      ↓ CompleteChecklist
                  Closed
```

Her geçiş audit (kim/ne zaman) + bildirim (§6.9).

---

## 11. Onay Beklenen Kararlar (F0 Çıktısı)

Bu plan üzerinde başlamadan önce aşağıdaki kararları netleştirmek isterim:

### 11.1 Mimari Kararlar

1. **Profil/cam/aksesuar verisi:** F1'de Albert Genau + Vizyon kataloğunu manuel seed mi edeceğiz, yoksa elinde mevcut tedarikçi listen var mı? Excel/CSV varsa **CatalogImportWizard** ilk gün hazır olmalı.
2. **Stok bütünlüğü:** Profil/cam stoklarını mevcut `Product` + `StockItem` üzerinden mi yöneteceğiz (junction `linked_product_id`), yoksa modülün kendi stok tablosu olsun mu? Tercih: **junction** — mevcut Inventory dashboard'unda da görünür.
3. **Teklif PDF lisansı:** QuestPDF Community OK mi (yıllık ciro < 1M USD), yoksa fully OSS alternatif (örn. iText OSS, PdfSharpCore) tercih edilir mi?
4. **CNC hedefi:** İlk müşterinin makinesi hangi format kabul ediyor (DXF / CSV / Logikal SDF / Schueco proprietary / OptiCut)? F4 minimum hedef.
5. **Cam kesim makinesi:** Gerçek serbest kesim mi yoksa **guillotine-only** mi? `settings.guillotine_required` default değeri buna göre.
6. **Para birimi & vergi:** Çoklu para mı (TRY + EUR + USD + SAR), sadece TRY mi? KDV %20 default mi? Fx-rate kim sağlayacak (TCMB feed mi, manuel mi)?
7. **Tasarım fazı çıktısı:** F2b MVP'de tek run yeterli mi, yoksa L şeklinde (RunConnection) başlangıçtan birlikte mi? Tek run önerilir (2 hafta tasarruf).

### 11.2 Operasyonel Kararlar

8. **WhatsApp Business sağlayıcısı:** Twilio mu, Meta Cloud API mı? Onay süreci 2-4 hafta — F1 başında başvuru.
9. **SMS sağlayıcısı:** NetGSM, İletiMerkezi, Twilio? Türkiye yerel kontrolü için NetGSM/İletiMerkezi tercih.
10. **Email sağlayıcısı:** SendGrid mi, Postmark mı, SMTP mi? Şablon yönetimi tarafı.
11. **Tedarikçi & marka:** İlk müşterinin tedarikçi ağı ne? `BrandVendor` seed buna göre yapılır.
12. **WindZone tablosu kaynağı:** TS 498 manuel mi, yoksa **Türkiye Meteoroloji** ya da Eurocode-1 referans tablo mu? F1'de elle giriş kabul.
13. **ClimateZone tespiti:** İl + ilçe + postakodu cinsinden hangi granularite? Demo: il bazlı yeterli.

### 11.3 Tasarım Kararları

14. **Onboarding zorunlu mu?** Yeni tenant ilk girişte sihirbaz görsün mü (önerilen), yoksa "Skip" mümkün mü? Skip'te demo katalog otomatik yüklemez.
15. **Customer share viewer onayı:** Web imzası kanıt olarak yeterli mi (önerilen), yoksa yasal olarak ıslak imza/e-imza şart mı?
16. **KVKK saklama süresi:** Müşteri verisi için kaç gün default? Saha fotoğrafları için kaç gün? (önerilen: 730 gün proje, 30 gün fotoğraf — KVKK saklama amacı sınırı)
17. **Üretim takvimi capacity birimi:** Günlük m² mi (önerilen), saatlik panel mi, lineer metre mi? Bu fabrikaya göre değişir — settings'te seçenek.

### 11.4 Faz Önceliği

18. **F2a Field PWA ne kadar kritik?** Eğer müşteri başlangıçta sadece atölye senaryosu kullanacaksa F2a'yı F6 sonrasına ertelenebilir (3 hafta erken üretim).
19. **AI/AR fazı (F9) ne zaman?** MVP-sonrası "wow" özellik mi, yoksa hiç planda olmasın mı?

### 11.5 Ekip & Bütçe

20. **Geliştirici sayısı:** 1 solo mu (~32 hafta), 2 (back+front, ~20 hafta) mı, 4-5 kişilik tam ekip (~14-15 hafta) mi?
21. **3D asset (HDRI, vendor GLB, RAL renk paleti, profil DXF):** Lisanslı kütüphane bütçesi var mı? CC0 kaynakları yetmezse Vendor'lardan resmi izin gerekir.

---

**Onaylar gelince F0 → F1 → F2a/F2b paralel** akışına başlanır:

- F0: yarım hafta — kararlar + seed verisi toplama
- F1: 3 hafta — 30 entity + migration + seed + Katalog UI + Onboarding + ImportWizard
- F2a: 2 hafta — Field PWA (eğer önceliklenirse paralel)
- F2b: 3 hafta — Designer MVP

---

## 12. Açık Belirsizlikler & Kararlaştırılacak Detaylar

Bu plan bazı noktalarda **birden fazla geçerli yol** sunuyor; uygulamadan önce netleştirilmeli:

| #   | Konu                            | Seçenekler                                      | Önerilen                                            | Karar |
| --- | ------------------------------- | ----------------------------------------------- | --------------------------------------------------- | ----- |
| 1   | ProfileItem cross-section       | SVG path / DXF / parametric JSON                | İkisi de saklı: SVG ilk implementasyon, DXF opsiyon | ⏳    |
| 2   | CuttingOptimizer2D guillotine   | Settings flag / iki ayrı service                | **Settings flag** + tek service, branch             | ⏳    |
| 3   | HardwareKit qty_formula         | DynamicExpresso / regex switch / manual code    | DynamicExpresso (whitelist sandbox)                 | ⏳    |
| 4   | Quote PDF rendering             | QuestPDF / iText OSS / PdfSharpCore / Puppeteer | **QuestPDF Community** (lisans şartı uygun)         | ⏳    |
| 5   | RTL UI                          | Tailwind dir variants / RTL ayrı CSS            | **Tailwind v4 dir variants**                        | ⏳    |
| 6   | Anonymous share viewer security | Token + IP rate / Token + IP whitelist          | **Token + IP rate limit + CSP**                     | ⏳    |
| 7   | Scene JSON sıkıştırma           | Brotli backend / Brotli client / hiçbiri        | **Brotli backend (bytea)**                          | ⏳    |
| 8   | WhatsApp sağlayıcı              | Twilio / Meta Cloud API                         | Meta Cloud API (doğrudan, daha ucuz uzun vadede)    | ⏳    |
| 9   | Üretim Schedule UI              | Tam Gantt (frappe-gantt) / basit takvim         | **Basit haftalık takvim** F6, Gantt F8              | ⏳    |
| 10  | KVKK Anonymize davranışı        | Hard delete / Pseudonymize / Encrypt at rest    | **Pseudonymize** (audit korunur, KVKK uyumlu)       | ⏳    |

---

## 13. Geliştirme Başlangıç Checklist (F1 Kick-off)

Bu güncellenmiş planın **§11 kararları** netleşince aşağıdaki çıktılarla F1'e başlanır:

1. `apps/glass-enclosure-field` workspace eklenir (vite ayrı entry, PWA manifest, sw)
2. `server/src/CoreAlign.Domain/Entities/GlassEnclosure/` altına 30 entity (F1)
3. `server/src/CoreAlign.Infrastructure/Migrations/202606XX_AddGlassEnclosureSchema.cs`
4. `server/src/CoreAlign.Infrastructure/Seed/GlassEnclosureSeed.cs` — Albert Genau + Vizyon + RAL + WindZone + ClimateZone
5. `server/src/CoreAlign.Application/GlassEnclosure/Commands` + `Queries` + `Services` (CQRS bütünü)
6. `src/features/glass-enclosure/` + `src/features/glass-enclosure-field/`
7. `src/locales/{tr,en}/glassEnclosure.json` — bütün anahtarlar yerinde
8. Test: `server/tests/CoreAlign.Application.Tests/GlassEnclosure/`

Karar onayı geldiği anda F1 ile başlarım.

---

## 14. Mimari Kararlar (Sabitlenmiş — F1 Başlıyor)

§12'deki belirsizliklere uygulanan varsayılan kararlar. İhtiyaç halinde override edilir; aksi kararlaştırılana kadar buradakiler bağlayıcıdır.

### 14.1 Kararlar

| #   | Konu                      | Karar                                                                                       | Gerekçe                                  |
| --- | ------------------------- | ------------------------------------------------------------------------------------------- | ---------------------------------------- |
| 1   | ProfileItem cross-section | SVG path **primary**, DXF URL opsiyonel                                                     | F1 hızlı başlangıç, DXF F4 sonrası       |
| 2   | CuttingOptimizer2D        | Tek service + `guillotineOnly` parametre                                                    | Test edilebilir, makineye uyumlu         |
| 3   | HardwareKit formula       | DynamicExpresso + tip whitelist                                                             | Güvenli ifade motoru, .NET 10 uyumlu     |
| 4   | Quote PDF                 | **QuestPDF Community** (Apache 2.0)                                                         | Lisans uygun (ciro < 1M USD); modern API |
| 5   | RTL UI                    | Tailwind v4 `dir:` variants                                                                 | Mevcut altyapı yeterli                   |
| 6   | Scene compression         | Brotli, bytea kolonu                                                                        | Postgres native, 80%+ tasarruf           |
| 7   | WhatsApp sağlayıcı        | Meta Cloud API (Twilio fallback)                                                            | Düşük maliyet, doğrudan                  |
| 8   | Schedule UI               | F6 basit haftalık; F8 Gantt (frappe-gantt)                                                  | MVP'de yeterli                           |
| 9   | KVKK anonymize            | **Pseudonymize** (audit korunur)                                                            | Yasal + denetlenebilir                   |
| 10  | Currency                  | Multi-currency infra, TRY default; EUR/USD opsiyonel                                        | Customer.DefaultCurrency mevcut          |
| 11  | API namespace             | `/api/v1/glass-enclosure/...`                                                               | Mevcut `/api/v1/...` deseni              |
| 12  | File storage              | `IFileStorage` abstraction; F1 local fs (`server/storage/{tenantId}/...`), F7 S3-compatible | Aşamalı kompleksite                      |
| 13  | Background jobs           | `IHostedService` (Quartz/Hangfire YOK) — basit cron + Channel                               | Mevcut `ActivityLogWorker` deseni        |
| 14  | Image optimization        | SkiaSharp thumbnail (256x256 + 1024x1024)                                                   | .NET native, lisans bedava               |
| 15  | Telemetry                 | Serilog + ITelemetryRecorder (CRM olayları)                                                 | Mevcut log altyapısı                     |
| 16  | Test seed data            | Ayrı `GlassEnclosureTestSeed` (Application.Tests)                                           | Production seed'den ayrık                |
| 17  | Frontend bundle           | Designer + Field PWA + Share viewer ayrı lazy chunk                                         | İlk yük < 500KB                          |
| 18  | API versioning            | URL prefix `/api/v1/` (mevcut)                                                              | Konvansiyon korunur                      |
| 19  | Migration adı             | `YYYYMMDDHHMMSS_AddGlassEnclosureSchema_Initial`                                            | F1 tek migration                         |
| 20  | Permission policy adı     | `GlassEnclosure.<Resource>.<Action>`                                                        | Granular kontrol                         |

### 14.2 Domain Events Kataloğu

`server/src/CoreAlign.Domain/Events/GlassEnclosureEvents.cs`:

```
GlassProjectCreatedEvent              (TenantId, ProjectId, CustomerId, CreatedBy)
GlassProjectStatusChangedEvent        (TenantId, ProjectId, FromStatus, ToStatus, ChangedBy)
GlassProjectQuotedEvent               (TenantId, ProjectId, QuoteSnapshotId, ShareToken)
GlassProjectQuoteViewedEvent          (TenantId, ProjectId, ShareToken, IpHash)
GlassProjectQuoteAcceptedEvent        (TenantId, ProjectId, ShareToken, SignatureUrl)
GlassProjectQuoteRejectedEvent        (TenantId, ProjectId, ShareToken, Reason)
GlassProjectConfirmedEvent            (TenantId, ProjectId, OrderId, ConfirmedBy)
GlassWorkOrderReleasedEvent           (TenantId, WorkOrderId, ScheduledDate, AssignedTeamId)
GlassWorkOrderStatusChangedEvent      (TenantId, WorkOrderId, FromStatus, ToStatus)
GlassWorkOrderDefectReportedEvent     (TenantId, WorkOrderId, DefectNotes)
GlassFieldSurveySubmittedEvent        (TenantId, FieldSurveyId, ProjectId, SurveyedBy)
GlassFieldSurveyAppliedEvent          (TenantId, FieldSurveyId, ProjectId)
GlassSceneVersionSavedEvent           (TenantId, ProjectId, Version, SavedBy)
GlassStockShortageDetectedEvent       (TenantId, ProjectId, ShortageLines)
GlassInstallationCompletedEvent       (TenantId, ProjectId, InstalledBy)
```

Bu olaylar `OutboxDrainBehavior` ile transactional dispatch + `INotificationDispatcher` tarafından bildirim üretmek için tüketilir.

### 14.3 API Route Konvansiyonu

```
/api/v1/glass-enclosure/projects                       GET (list), POST (create)
/api/v1/glass-enclosure/projects/{id}                  GET, PUT, DELETE
/api/v1/glass-enclosure/projects/{id}/runs             POST (add)
/api/v1/glass-enclosure/projects/{id}/runs/{runId}     PUT, DELETE
/api/v1/glass-enclosure/projects/{id}/runs/{runId}/panels         POST
/api/v1/glass-enclosure/projects/{id}/runs/{runId}/panels/{panId} PUT, DELETE
/api/v1/glass-enclosure/projects/{id}/scene            GET, POST (save)
/api/v1/glass-enclosure/projects/{id}/scene/versions   GET
/api/v1/glass-enclosure/projects/{id}/bom              GET, POST (recompute)
/api/v1/glass-enclosure/projects/{id}/cutting-plan     GET, POST
/api/v1/glass-enclosure/projects/{id}/cost-analysis    GET
/api/v1/glass-enclosure/projects/{id}/technical-summary GET
/api/v1/glass-enclosure/projects/{id}/quote            POST
/api/v1/glass-enclosure/projects/{id}/discount         POST
/api/v1/glass-enclosure/projects/{id}/convert-to-order POST
/api/v1/glass-enclosure/projects/{id}/release-to-production POST

/api/v1/glass-enclosure/work-orders                    GET
/api/v1/glass-enclosure/work-orders/{id}/status        PUT
/api/v1/glass-enclosure/work-orders/{id}/defect        POST

/api/v1/glass-enclosure/field-surveys                  GET, POST (batch upload PWA)
/api/v1/glass-enclosure/field-surveys/{id}             GET, PUT
/api/v1/glass-enclosure/field-surveys/{id}/submit      POST

/api/v1/glass-enclosure/profile-systems                GET, POST
/api/v1/glass-enclosure/profile-systems/{id}           GET, PUT, DELETE
/api/v1/glass-enclosure/profile-items                  GET, POST, PUT, DELETE
/api/v1/glass-enclosure/glass-types                    GET, POST, PUT, DELETE
/api/v1/glass-enclosure/colors                         GET, POST, PUT, DELETE
/api/v1/glass-enclosure/hardware-items                 GET, POST, PUT, DELETE
/api/v1/glass-enclosure/hardware-kits                  GET, POST, PUT, DELETE
/api/v1/glass-enclosure/brand-vendors                  GET, POST, PUT, DELETE
/api/v1/glass-enclosure/discount-rules                 GET, POST, PUT, DELETE
/api/v1/glass-enclosure/notification-templates         GET, POST, PUT, DELETE
/api/v1/glass-enclosure/settings                       GET, PUT
/api/v1/glass-enclosure/wind-zones                     GET (master)
/api/v1/glass-enclosure/climate-zones                  GET (master)
/api/v1/glass-enclosure/climate/recommendation         GET ?address=...
/api/v1/glass-enclosure/wind-zone/by-address           GET ?address=...
/api/v1/glass-enclosure/onboarding/status              GET
/api/v1/glass-enclosure/onboarding/complete-step       POST
/api/v1/glass-enclosure/catalog/import                 POST (XLSX)
/api/v1/glass-enclosure/catalog/import/dry-run         POST

/api/v1/glass-enclosure/production-schedule            GET
/api/v1/glass-enclosure/reports/sales-summary          GET

/api/v1/share/glass/{token}                            GET (anonymous) — viewer
/api/v1/share/glass/{token}/action                     POST (anonymous, rate-limited) — accept/reject/sign
```

Tüm endpoint'ler `[Authorize(Policy = "GlassEnclosure.<X>")]` ile gated (anonymous viewer hariç).

### 14.4 Cache Strategy

**Backend (`IMemoryCache`):**

- `GlassEnclosure.WindZones` — TTL 1 saat (master data)
- `GlassEnclosure.ClimateZones` — TTL 1 saat
- `GlassEnclosure.ColorOptions.{tenantId}` — TTL 15 dakika
- `GlassEnclosure.ProfileSystems.{tenantId}` — TTL 15 dakika
- `GlassEnclosure.HardwareKits.{tenantId}` — TTL 15 dakika
- `GlassEnclosure.Settings.{tenantId}` — TTL 5 dakika
- Cache invalidation: ilgili Upsert command'inde explicit eviction

**Frontend (`httpCache.ts` mevcut):**

- `/api/v1/glass-enclosure/wind-zones` — TTL 1 saat + ETag
- `/api/v1/glass-enclosure/climate-zones` — TTL 1 saat + ETag
- `/api/v1/glass-enclosure/profile-systems` — TTL 5 dakika
- `/api/v1/glass-enclosure/projects/{id}/scene` — no cache (always fresh)

### 14.5 Background Jobs (IHostedService)

`server/src/CoreAlign.API/HostedServices/`:

| Job                         | Cron                       | Görev                                                                         |
| --------------------------- | -------------------------- | ----------------------------------------------------------------------------- |
| `GlassSceneRetentionJob`    | Günde 1 (03:00)            | 90+ gün eski + customer_approved=false sahneleri arşivle                      |
| `GlassNotificationRetryJob` | 5 dakikada 1               | `status=Failed && retries<3` log'larını yeniden dene                          |
| `GlassShareTokenCleanupJob` | Günde 1 (04:00)            | Expired share tokenleri sil                                                   |
| `GlassQuoteExpirationJob`   | Günde 1 (06:00)            | `valid_until_date` geçmiş Quoted projeleri "Expired" işaretle + email tetikle |
| `GlassKvkkRetentionJob`     | Haftada 1 (Pazar 02:00)    | Settings.data_retention_days üzeri pseudonymize                               |
| `GlassStockReorderJob`      | Saatte 1                   | Reorder point altı profil/cam için PO draft öner                              |
| `GlassThumbnailRegenJob`    | Yeni scene saved → Channel | Background thumbnail üretimi (UI bekletme)                                    |

### 14.6 Error Code Catalog

`CoreAlign.Domain/Exceptions/GlassEnclosure/`:

```
GE-001  ProjectNotFound
GE-002  CustomerMismatch                    (project.customer cross-tenant)
GE-003  ProjectStatusTransitionInvalid
GE-004  SystemNotCompatibleWithOpening
GE-005  PanelExceedsSystemMaxWidth
GE-006  PanelExceedsSystemMaxWeight
GE-007  GlassThicknessNotSupportedBySystem
GE-008  GlassAreaExceedsMax
GE-009  WindLoadInsufficientGlass
GE-010  HingeCapacityExceeded
GE-011  HardwareNotCompatibleWithSystem
GE-012  RunConnectionAngleInvalid
GE-013  CuttingPlanGenerationFailed
GE-014  CatalogImportValidationFailed
GE-015  ShareTokenExpired
GE-016  ShareTokenRateLimit
GE-017  FieldSurveyNotApplicable
GE-018  StockInsufficientForOrder
GE-019  WorkOrderScheduleConflict
GE-020  FormulaEvaluationFailed
GE-021  NotificationDeliveryFailed
GE-022  QuoteAlreadyAccepted
GE-023  KvkkAnonymizeFailed
GE-024  WindZoneNotFoundForAddress
GE-025  TenantOnboardingIncomplete
```

Her exception ExceptionHandlingMiddleware'de status koduna map edilir + `errorCode` field'ı ile frontend'e geçer.

### 14.7 Permission Policy İsim Listesi (Backend)

`Program.cs` `AuthorizationOptions` içine eklenecek policy'ler (§10.1 matrisinin kod karşılığı):

```
GlassEnclosure.Project.View
GlassEnclosure.Project.ViewAll
GlassEnclosure.Project.Create
GlassEnclosure.Project.Update
GlassEnclosure.Project.Delete
GlassEnclosure.Project.Clone
GlassEnclosure.Designer.Open
GlassEnclosure.Designer.PriceVisible
GlassEnclosure.Designer.PriceEdit
GlassEnclosure.Designer.DiscountApply
GlassEnclosure.Designer.DiscountOverride
GlassEnclosure.Quote.Generate
GlassEnclosure.Quote.Send
GlassEnclosure.Quote.Accept
GlassEnclosure.Order.Convert
GlassEnclosure.Production.Release
GlassEnclosure.Production.Schedule
GlassEnclosure.Production.UpdateStatus
GlassEnclosure.Production.RecordDefect
GlassEnclosure.CuttingReport.Download
GlassEnclosure.Installation.UpdateStatus
GlassEnclosure.Installation.CompleteChecklist
GlassEnclosure.FieldSurvey.Create
GlassEnclosure.FieldSurvey.Submit
GlassEnclosure.FieldSurvey.Approve
GlassEnclosure.Catalog.View
GlassEnclosure.Catalog.Update
GlassEnclosure.Catalog.Import
GlassEnclosure.DiscountRule.Update
GlassEnclosure.NotificationTemplate.Update
GlassEnclosure.BrandVendor.Update
GlassEnclosure.Settings.Update
GlassEnclosure.Settings.WindZone
GlassEnclosure.Anonymize
GlassEnclosure.Export.ProjectData
GlassEnclosure.Reports.View
```

### 14.8 Bağımlılıklar Eklenecek (NuGet + npm)

**Backend (CoreAlign.Application.csproj veya Infrastructure.csproj):**

- `QuestPDF` (2024.x — community)
- `DynamicExpresso.Core` (2.x)
- `EPPlus` (7.x — Polyform Non-Commercial veya commercial; alternatif `ClosedXML` MIT)
- `CsvHelper` (33.x)
- `Scriban` (5.x — template render, MIT)
- `Polly` (8.x — retry policy)
- `SkiaSharp` (2.88+ — thumbnail)

**Frontend (`package.json`):**

- `workbox-window` + `workbox-precaching` (PWA F2a)
- `idb` (IndexedDB wrapper, F2a)
- `fabric` (foto annotation, F2a)
- `frappe-gantt` (üretim takvim F8)
- `dompurify` (share viewer XSS, F6)

---

## Kaynaklar

- [Vizyon Cam Balkon Sistemleri](https://www.acvizyon.com/sistemliste/1/cam-balkon-sistemleri)
- [BKS Cam Balkon](https://bkscambalkon.com/)
- [BKS Sürgülü Sistemler](https://bkscambalkon.com/%C3%BCr%C3%BCnler/s%C3%BCrg%C3%BCl%C3%BC-sistemler)
- [Winsa Cam Balkon Sistemleri](https://www.winsa.com.tr/tr/urun/cam-balkon-sistemleri/winsa-cam-balkon-sistemleri-/)
- [Albert Genau SlideMaster](https://www.albertgenau.com/tr/Isicamli-Surme-Cambalkon)
- [Albert Genau Tiara Twinmax (Model Alüminyum)](https://www.modelaluminyuminsaat.com/markalarimiz/albert-genau/cam-balkon-katlanir-cam-sistemleri/kayar-katlanir-cam-balkon-sistemleri/tiara-twinmax-isi-camli-cam-balkon-sistemleri/)
- [Cam Balkon 2025 m² fiyatları (Korkmaz Haber)](https://korkmazhaber.com/cam-balkon-fiyatlari-25855.htm)
- [Katlanır Sürme 2025 Fiyat (Kupa Yapı)](https://www.kupayapi.com/2025/11/06/katlanir-surme-isicamli-cam-balkon-fiyatlari-ne-kadar/)
- [Cam Balkon Analiz Pro App](https://apps.apple.com/tr/app/cam-balkon-analiz-pro/id6446797086)
- [Cam Balkon Analiz Programı](https://www.cambalkonanaliz.com/)
- [Aluvector Çizim & Teklif](https://aluvector.com/)
- [Real Cam Balkon Programı](http://cambalkonhesapla.com/)
- [Master Cam Balkon](https://www.mastercambalkon.com/)
- [Technosoft PenCAD üretim yazılımı](https://technosoft.com.tr/)
- [PenCAD](https://pencad.net/)
- [Ayrıntı Shop — Aksesuar](https://www.ayrintishop.com/tr/cam-balkon-aksesuarlari--k)
- [Kobibest Katlanır Cam Aksesuar](https://www.kobibest.com/katlanir-cam-sistemleri)
- [Tema Alüminyum — sürme aksesuar](https://temaaluminyum.com.tr/en/product-category/sliding-system-accessories/)
- [Solarlux SL 25 balcony glazing](https://solarlux.com/en/systems/balcony-glazing-sl-25.html)
- [Solarlux SL 25XXL](https://solarlux.com/en/systems/balcony-glazing-sl-25xxl.html)
- [SUNFLEX sliding systems](https://www.sunflex-aluminiumsystems.com/products/sliding-systems)
- [Lumon Glazing](https://lumon.com/int/lumon-products/lumon-glazing/)
- [Three.js MeshPhysicalMaterial docs](https://threejs.org/docs/pages/MeshPhysicalMaterial.html)
- [Codrops — Glass & Plastic in Three.js](https://tympanus.net/codrops/2021/10/27/creating-the-effect-of-transparent-glass-and-plastic-in-three-js/)
- [Three.js Glass Transmission Tutorial](https://sbcode.net/threejs/glass-transmission/)
- [LogRocket — react-three-fiber configurator](https://blog.logrocket.com/configure-3d-models-react-three-fiber/)
- [Salsita 3D parametric configurator](https://blog.salsita-3d-configurator.com/implementing-a-challenging-product-configurator-with-3d-parametric-models/)
- [Wawa Sensei — Three.js product configurator](https://wawasensei.dev/tuto/how-to-use-three-js-to-create-a-3D-product-configurator)
- [First Fit Decreasing — Wikipedia/Grokipedia](https://grokipedia.com/page/First-fit-decreasing_bin_packing)
- [Bin Packing Problem — Wikipedia](https://en.wikipedia.org/wiki/Bin_packing_problem)
- [SciPy Book — Bin packing & cutting stock](https://scipbook.readthedocs.io/en/latest/bpp.html)
- [Pattern-based algorithms for cutting stock (ScienceDirect)](https://www.sciencedirect.com/science/article/abs/pii/S0305054814000525)
- [KopEksper Optimizasyon](https://www.notasyon.com/kopeksperpro.html)
- [OptiPanel](https://www.optipanel2d.com/p/teknik-ozellikler.html)
- [Perfect Cut Suite — cam kesim](http://www.cammakine.com/cam_kesim_programi.html)
