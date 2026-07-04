# Yol Haritası: E-Fatura Entegratör · Sipariş/Fatura/İrsaliye Döngüsü · Personel · Satın Alma (2026-07)

> **S1 DURUMU (2026-07-03): TAMAMLANDI.** RevertOrderToDraft (FSM + guard'lar + endpoint + FE), kısmi ödeme filtresi,
> tediye FE (PaymentCreateModal direction), klon UI (validator fix'iyle açıldı), customer_notes (entity + Phase114
> migration + endpoint'ler + panel UI). **Migration takibi (GÜNCEL):** `20260728000000_Phase114CustomerNotes` lokal
> Postgres'e UYGULANDI (2026-07-03'te bu makine native PostgreSQL 18'e geçti, startup `MigrateAsync` otomatik uygular).
>
> **S2 DURUMU (2026-07-04): TAMAMLANDI.** Çapraz bağlantı kolonları: `OrderSearchRow`/`OrderSummaryDto`'ya aktif
> (İptal/Void olmayan) fatura + irsaliye no/id scalar subquery ile eklendi — fatura iptalinde kolon kendiliğinden
> boşalır, sipariş durumu DEĞİŞMEZ (silme geri-bağlantısı bu tasarımla çözüldü, event handler gerekmedi);
> `InvoiceSearchRow`/`InvoiceSummaryDto`'ya OrderNumber eklendi. Liste kolonları tıklanabilir (`?focus=` deep-link,
> InvoicesPage'e focus/selected desteği eklendi — DocumentChain'in `?selected=` linkleri de artık çalışıyor).
> Sipariş/fatura oluşturma tam sayfaya taşındı: `/dashboard/orders/new` + `/dashboard/invoices/new`
> (OrderFormModal + CreateStandaloneInvoiceModal `presentation="page"` çift-mod; düzenleme modal'da kaldı,
> taslak-otokayıt korunuyor). N+1 bütçeleri korunuyor (scalar subquery tek round-trip);
> `OrderInvoiceCrossLinkIntegrationTests` iptal-sonrası boşalmayı da kanıtlıyor. Ek bugfix: `fx-rates/resolve`
> query-bound DateTime Kind=Unspecified → Npgsql 500 (handler + repository çift normalizasyon, INVARIANTS'a işlendi).
>
> **S3 DURUMU (2026-07-04): D1 TAMAMLANDI (§1.3-D1).** (A) GİB kod tabloları: `withholding_tax_codes` (52 kod: 601-627
> kısmi + 801-825 tam tevkifat) + `vat_exemption_codes` (88 kod: 201-250 kısmi istisna, 301-351 tam istisna, 701-704
> ihraç-kayıtlı) — `IGlobalReadable`, Phase115 migration (Postgres'e UYGULANDI), her-açılışta idempotent seed
> (`GibCodeSystemDataSeeder`), GİB "UBL-TR Kod Listeleri V 1.42/Mart 2026" ile birebir doğrulandı; `/master-data/{withholding-tax-codes,vat-exemption-codes}` endpoint'leri + FE dropdown'ları (fatura + sipariş satır editörü, oran koddan otomatik). Tevkifat artık KDV×pay/payda kesri. (B) Invoice e-belge alanları
> (profil, GİB durum kodu, red sebebi, sentAt, lastSync) + terminal-korumalı durum FSM (`ApplyEInvoiceStatus`);
> `EFaturaReconciliationJob` stub'ı gerçek invoice sorgusuna bağlandı. (C) `CheckTaxpayerAsync` capability
> (provider+dispatcher+gateway) + issue akışında otomatik e-Fatura/e-Arşiv yönlendirme (VKN mükellefse e-Fatura).
> (D) UBL-TR zenginleştirme: ProfileID (profil), InvoiceTypeCode senaryosu (TEVKIFAT/ISTISNA/SATIS),
> `WithholdingTaxTotal` kod bloğu, satır iskonto `AllowanceCharge`, istisna `TaxExemptionReasonCode/Reason`.
> **Kalan (S4+):** EDM/Payflex provider'ları (NDA API dokümanı bekliyor).
> Testler: +18 Application (2202), +5 Integration (240), UBL builder 3 yeni senaryo; tümü yeşil.
>
> **S5 DURUMU (2026-07-04): TAMAMLANDI (§1.5 Gelen Faturalar).** Yeni `IncomingInvoices` modülü: `incoming_invoices`
> inbox tablosu (Phase116, Postgres'e UYGULANDI, `(TenantId, Ettn)` unique idempotency), durum FSM
> (Yeni→İncelendi/İşlendi/YokSayıldı, terminal-korumalı). `IEFaturaDispatcher.ListReceivedAsync` eklendi (Nilvera
> zaten destekliyor); `IncomingInvoiceFetchJob` (Hangfire günlük, PushScope tenant döngüsü) provider'dan çeker,
> Ettn'e göre dedupe eder. "Sisteme işle" akışı: gönderen VKN'den tedarikçi çöz (`Vendor.GetByTaxNumberAsync`)
> veya yoksa VKN'den hızlı-oluştur → draft VendorBill (mevcut AP/GL zinciri). "Yoksay" akışı. `IncomingInvoicesController`
> (liste/detay/process/ignore), FE Gelen Faturalar sayfası + Sidebar. **NOT:** provider'ın inbox DTO'su yalnız 5 alan
> (Ettn/VKN/no/tarih/durum) döndürüyor — tutar yok; "sisteme işle" formunda kullanıcı tutarı girer, satır-eşleştirme
> provider "belge detayı çekme" yeteneği geldiğinde eklenecek (bilinçli kapsam sınırı). **Firma Profili** zaten
> mevcut (`/settings/company` + `CompanyProfileSection`, Tenant kimliği VKN/vergi dairesi/MERSIS/adres) — yeniden
> yapılmadı. Testler: +10 Application (2212), +5 Integration (245).
>
> **S6 DURUMU (2026-07-04): TAMAMLANDI (§3 İade oluştur).** Returns backend'i zaten baştan sona vardı (Phase33:
> CreateReturnRequest/Approve/Reject/Cancel/Receive + otomatik kredi notu + stok geri-yükleme + COGS ters kaydı,
> 17+ Application testi). TEK boşluk admin'de **"İade Oluştur" UI'ıydı** — `useCreateReturnRequest` hook'u + tip +
> endpoint hazır ama hiç UI'dan çağrılmıyordu. Eklendi: `CreateReturnModal` (iade edilebilir satırlar =
> quantityShipped−quantityReturned, neden dropdown, miktar) + Order detay panelinde "İade Oluştur" butonu
> (order.status ∈ Shipped/PartiallyShipped/Delivered/Closed/Returned), oluşturunca `/dashboard/returns/{id}`'e gider;
> i18n Returns.create.\* tr+en. Tarayıcıda demo'da uçtan uca doğrulandı (sevk edilmiş sipariş → RTN-2026-00001
> "Talep Edildi"). Returns entegrasyon testleri eklendi (auth-deny, liste-OK, satırsız-red, olmayan-onay-404,
> uygun-olmayan-sipariş-400). **Düzeltildi:** uygun olmayan siparişte (Draft/geçersiz satır) iade oluşturma artık
> temiz 400 dönüyor — handler uygunluk + satır-üyelik kontrolünü belge-sırası tüketiminden ÖNCE yapıyor
> (aksi halde reddedilecek istek RMA numarasını harcıyor ve sequence hatası domain guard'ını maskeleyip 500'e
> dönüşüyordu). **Erteleme:** Refund adımı (Refunded durumu / `MarkRefunded` komutu) hâlâ ulaşılamaz — ödeme-oluşturma
> kararı gerektiriyor (S7+).
>
> **S7 DURUMU (2026-07-04): KISMİ — FE cila dilimi TAMAMLANDI (§4 Personel).** Keşif, HR/Payroll'un Phase92'de
> baştan sona kurulu olduğunu gösterdi (Employee CRUD + yaşam döngüsü, dönem-bazlı toplu bordro çalışması "Yeni
> Dönem→Hesapla", bordro yazdırma, parametreler, tam FSM + GL muhasebeleştirme, 20+ Application testi) — "ham"
> DEĞİL. Gerçek boşluklar ek özelliklerdi. Bu turda saf-FE, hesap-motoruna-dokunmayan dilim yapıldı:
> (1) **vCard indir** — EmployeeDetail'de "vCard İndir", `.vcf` RFC 6350 vCard 3.0 (ad/ünvan/firma=tenant/tel/e-posta,
> PII yok), FE Blob, sıfır backend/migration; (2) **Maaş Değiştir UI** — mevcut ama kullanılmayan `useUpdateBaseSalary`
> hook'u bir modala bağlandı (aktif personelde); (3) **Personel detay overview zenginleştirildi** (SGK teşvik,
> engellilik, bakmakla yükümlü, eş çalışıyor, SGK muaf, emekli, SGK sicil no); (4) ölü `PayslipLinesTable.tsx` silindi.
> Tarayıcıda uçtan uca doğrulandı (vCard içeriği geçerli, maaş 500k→550k güncellendi). typecheck+lint+vitest(464) 0.
> **Ertelenen (büyük, standalone, hesap-motoruna dokunuyor):** PDKS/mesai (gün-çalışıldı/fazla-mesai girişi —
> Payslip.DaysWorked hep 30 varsayılıyor, girişi yok; roadmap'te gelecek ay-sonu job'u), personel avans defteri
> (şu an sadece kesinti tipi), taban maaş geçmişi/leave takvimi.

> Kullanıcı geri bildirim taramasından (2026-07-02) çıkan **büyük kapsamlı** işlerin karar ve planlama dokümanı.
> Küçük/orta düzeltmeler (P0 çökme, 403, dark mode, i18n, cache) ayrıca uygulandı — bu doküman tek oturumda bitmeyecek,
> sprint'lere bölünmesi gereken kalemleri kapsar. Her bölüm: mevcut durum → karar → iş dilimleri.

---

## 1. E-Fatura / E-Arşiv / E-İrsaliye Entegratörü (EDM, Payflex, ...)

### 1.1 Mevcut altyapı (YENİDEN KURMA — üzerine inşa et)

| Parça                 | Yerİ                                                                       | Durum                                                                                                                                                                                                            |
| --------------------- | -------------------------------------------------------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Provider soyutlaması  | `Application/Providers/EFatura/IEFaturaProvider.cs`                        | **Hazır.** Capability bayrakları: `CanIssue/CanCancel/CanCreditNote/CanQueryStatus/CanListReceived/CanWebhook`. `EFaturaDocumentType`: Invoice/Despatch(e-irsaliye)/EArchive/ProducerReceipt/SelfEmployedReceipt |
| İlk somut provider    | `Infrastructure/Providers/EFatura/Nilvera/*`                               | Var (token manager + webhook verifier + DTO'lar). EDM/Payflex için şablon                                                                                                                                        |
| Tenant kimlik profili | `Domain/Entities/TenantProviderConfig` + `TenantProviderConfigsController` | **Hazır.** `ProviderName`, `IsDefault/IsEnabled`, `EncryptedCredentialsJson` (at-rest şifreli), health alanları                                                                                                  |
| Credential şifreleme  | `IProviderCredentialProtector` + provider `UnprotectCredentials`           | Hazır — creds asla düz saklanmaz                                                                                                                                                                                 |
| Webhook doğrulama     | `IProviderWebhookVerifier` + `provider_webhook_inbox`                      | Hazır                                                                                                                                                                                                            |

**Karar:** EDM ve Payflex birer `IEFaturaProvider` implementasyonu olarak eklenir; tenant, Yönetim > Sağlayıcılar
ekranından profilini seçer ve giriş bilgilerini doldurur. Yeni mimari icat edilmez.

### 1.2 Yönetim panelinde tenant'tan toplanacak profil alanları

Entegratörden bağımsız çekirdek (hepsi `EncryptedCredentialsJson` içinde, alan bazlı):

- Entegratör seçimi (EDM / Payflex / Nilvera / ...), test–prod ortam anahtarı
- API kullanıcı adı + şifre (bazılarında ek API key / WS sertifikası)
- Firma VKN/TCKN, ünvan, vergi dairesi
- **GİB etiketleri (alias/URN):** e-fatura gönderici birim (GB) + posta kutusu (PK) etiketleri — belge tipi başına ayrı olabilir
- Belge tipi başına **seri/prefix** (e-Fatura, e-Arşiv, e-İrsaliye ayrı seri ister; `document_sequences` mevcut altyapısı belge tipi başına genişletilir)
- Varsayılan senaryo/profil: `TICARIFATURA` vs `TEMELFATURA` (e-fatura), e-arşiv gönderim şekli (ELEKTRONIK/KAGIT)
- İmza/mali mühür bilgisi entegratörde tutuluyorsa yalnız referansı

> EDM/Payflex API dokümanları NDA'lı portal üzerinden veriliyor; kesin alan adları sözleşme sonrası doküman ile
> netleşir. Yukarıdaki küme üç büyük entegratörün ortak paydası — profil formu bu çekirdekle kurulup provider'a özel
> alanlar `CredentialsJson` şemasına eklenir (migration gerektirmez).

### 1.3 Eksik olan gerçek işler (dilimler)

1. **UBL-TR zenginleştirme (D1):** `EFaturaDocument` bugün minimal (satır: miktar/ad/fiyat/KDV). Gerekenler:
   satır bazında **tevkifat kodu+oranı**, **istisna kodu**, iskonto, birim kodu (UN/ECE), alıcı adres/iletişim,
   sipariş/irsaliye referansları, senaryo+fatura tipi (SATIS/IADE/TEVKIFAT/ISTISNA/OZELMATRAH), döviz + kur.
2. **Mükellef sorgusu (D1):** alıcı VKN'si e-fatura mükellefi mi → e-Fatura, değilse e-Arşiv. Entegratör API'si sağlar;
   `IEFaturaProvider`'a `CheckTaxpayerAsync` capability'si eklenir; fatura ISSUE akışı otomatik yönlendirir.
3. **Belge durum makinesi (D1):** `Invoice`'a e-belge alanları (ETTN/UUID, profil, gönderim durumu, GİB yanıt kodu,
   reddetme sebebi). Webhook + polling ile durum güncelleme (`NotificationStatusUpdater` deseni).
4. **Tevkifat + istisna kod tabloları (D1):** aşağıda §1.4.
5. **EDM provider (D2), Payflex provider (D3):** Nilvera dosya düzeni birebir şablon (TokenManager + Provider + DTOs + WebhookVerifier).
6. **E-İrsaliye (D4):** Shipments modülü mevcut; `EFaturaDocumentType.Despatch` yolu + irsaliye UBL mapping + taşıyıcı/araç plaka alanları.
7. **Gelen faturalar (D5):** §1.5.

### 1.4 Tevkifat ve istisna kod tabloları

**Tasarım:** iki global lookup tablosu (reference data, `IGlobalReadable` benzeri — tenant'sız):

- `withholding_tax_codes` (kod `varchar(3)` unique, ad, pay/payda → oran, geçerlilik tarihi aralığı, aktif)
- `vat_exemption_codes` (kod, ad, KDVK madde referansı, tam/kısmi istisna türü, aktif)

Fatura satırı/başlığı bu kodlara FK ile bağlanır; UBL üretiminde kod+oran otomatik yazılır. UI: satır tevkifatı ve
fatura istisna tipi **dropdown** (kod + açıklama), oran koddan otomatik gelir.

**Seed stratejisi:** GİB "UBL-TR Kod Listeleri" resmî yayınından (kısmi tevkifat 6xx serisi — örn. 601 yapım işleri,
602 etüt/plan-proje/danışmanlık, 606 işgücü temini, 612 temizlik/çevre/bahçe, 613 servis taşımacılığı ...; tam
tevkifat 7xx serisi; istisna 3xx serisi — örn. 301 mal ihracatı, 302 bavul ticareti, 303 hizmet ihracı, 304 roaming,
uluslararası taşımacılık, dahilde işleme vb.). **Oranlar dönemsel değişiyor** (2021'de birçok kısmi tevkifat oranı
güncellendi) → seed, implementasyon sprint'inde güncel GİB listesiyle birebir doğrulanarak yazılır; go-live öncesi
SMMM onayı şart (payroll'daki `PayrollParameters` disipliniyle aynı). Kod listesi versiyonlanabilir olmalı
(geçerlilik aralığı kolonu bunun için).

### 1.5 Gelen Faturalar (entegratörden çekilen)

- **Yeni modül `Application/IncomingInvoices`:** `incoming_invoices` inbox tablosu (ETTN, gönderici VKN/ünvan, tutar,
  tarih, XML/PDF referansı, durum: Yeni/İncelendi/Sisteme İşlendi/Yok Sayıldı). Hangfire job'u `CanListReceived`
  destekleyen provider'dan periyodik çeker (`ListReceivedAsync` hazır).
- **"Sisteme işle" akışı:** gelen fatura → mevcut **VendorBill** oluşturma (satın alma modülü zaten VendorBill +
  GL + AP entegrasyonuna sahip). Gönderici VKN → Vendor eşleştirme (yoksa hızlı vendor oluştur).
- **Gelen fatura stokları:** satırlar tenant'ın kendi ürünleri olmayacağı için satır-eşleştirme ekranı:
  (a) mevcut ürüne bağla, (b) "stoksuz gider satırı" olarak işle, (c) yeni ürün oluştur. Eşleştirme
  `incoming_invoice_line_mappings` ile hatırlanır (aynı tedarikçi + aynı ürün adı → sonraki faturada otomatik öneri).
- **Muhasebe etkisi:** VendorBill'e dönüştüğü anda mevcut zincir çalışır (AP 320, ledger, ödeme akışı) — yeni GL kodu yazılmaz.

### 1.6 "Kendi firmam" görünümü

**Karar önerisi:** tenant'ın kendi firması **cari listesinde GÖSTERİLMEZ** (kendi kendine cari = merge/rapor
kirliliği). Bunun yerine ayrı **"Firma Profili"** paneli: firma kimlik bilgileri (VKN, adres, GİB etiketleri),
gelen/giden e-belge özetleri, banka hesapları (yeni `BankAccount` master'ı zaten var), borç/alacak özetleri
(AP/AR raporlarından). Bu panel, e-fatura profil ayarlarının da doğal evi.

---

## 2. Sipariş ↔ Fatura ↔ İrsaliye Yaşam Döngüsü

### 2.1 Durum hiyerarşisi ve düzenleme kilidi

Mevcut FSM: `Draft → Submitted → Approved → (Confirmed | Allocated → Picking → Packed) → Shipped → Delivered → Closed`
(+ Cancelled/Returned). Kullanıcının istediği kurallar için dilimler:

- **Onay sonrası içerik kilidi:** `UpdateOrderCommand` handler'ına guard — `Status != Draft` iken satır/tutar
  değişikliği reddedilir (409 + açıklayıcı mesaj). UI'da alanlar readonly.
- **"Taslağa çek" (RevertToDraft):** yeni `RevertOrderToDraftCommand`. Guard zinciri: faturası varsa → "önce faturayı
  iptal edin/silin" (409, fatura no ile), sevkiyatı varsa → "önce sevkiyatı iptal edin", stok rezervasyonu varsa
  release. Confirmed'dan dönüşte stok restore zaten `Cancel` yolunda var — aynı restore mantığı revert'te de çalışır.
- Her yeni geçiş + reddedilen geçiş için ayrı test (proje kuralı §14.4).

### 2.2 Tek tık fatura/irsaliye + çapraz bağlantı

- Backend `GenerateInvoiceFromOrderCommand` **zaten var**; irsaliye için `CreateShipmentFromOrderCommand` benzeri eklenir.
- **Liste kolonları:** sipariş tablosuna "Fatura No" / "İrsaliye No" kolonları (`OrderSearchRow`'a slim projection ile,
  N+1'siz — scalar subquery). Fatura/irsaliye listelerine de "Sipariş No".
- **Tıkla-git:** çapraz numara tıklanınca ilgili modüle route + `?selected={id}` query param → sayfa açılışta o kaydı
  seçili/üstte gösterir (mevcut detail-panel deseni).
- **Silme geri-bağlantısı:** fatura/irsaliye silinir/iptal edilirse siparişteki durum/işaret otomatik geri alınır
  (`InvoiceCancelled/Deleted` event → order handler). "Faturaya gönderildi" görünümü kalmaz.
- **Muhasebe tekilleştirme:** bugün zaten doğru — sipariş GL'e/bakiyeye YAZMAZ; yalnız fatura yazır. Yani
  sipariş+faturası bakiyeye çift düşmüyor. UI'da bunu netleştirmek için müşteri hareketlerinde sipariş satırı
  "bilgi", fatura satırı "bakiye etkisi" olarak rozetlenir.

### 2.3 Klonlama

`CreateOrderFromPreviousCommand` **zaten yazılmış** (bu sprintte validator fix'iyle önü açıldı — boş numarayla sequence
tüketiyor). Kalan: UI butonu (sipariş satırı aksiyonu "Klonla" → modal önceki verilerle + yeni otomatik numara) ve
aynısının fatura + irsaliye için komut kopyaları.

### 2.4 Kısmi ödeme

Backend **hazır**: `InvoiceStatus.PartiallyPaid` + `RecordInvoicePaymentCommand` (`POST /invoices/{id}/payments`,
over-pay reddi dahil). Eksik yalnız FE: fatura durum filtrelerine "Kısmi Ödendi" chip'i + fatura detayında "Ödeme Gir"
modalı (tutar girilir, kalan gösterilir). Küçük iş.

### 2.5 Modal → sayfa kararı

**Karar: EVET, sipariş ve fatura oluşturma tam sayfaya taşınmalı** (`/dashboard/orders/new`, `/dashboard/invoices/new`).
Gerekçe: satır kaleminin tek satırda (stok no · ad · miktar · birim · fiyat · iskonto · KDV · tevkifat · tutar)
gösterilebilmesi için ~1100px+ genişlik gerekiyor; e-fatura alanları (senaryo, istisna, etiket) eklenince modal
taşacak. Mevcut `OrderFormModal` form gövdesi component olarak korunur, route'lu sayfa sarmalayıcıya taşınır
(draft-autosave zaten var — sayfa geçişinde veri kaybolmaz). Tedarikçi/ürün gibi kısa formlar modalda kalır.

### 2.6 Tediye (giden ödeme) girişi

Backend `PaymentDirection` zaten iki yönlü (`CustomerReceipt` + tediye yönü) ve `CreatePaymentCommand.Direction`
parametreli. Eksik FE: müşteri cari ekranındaki "Tahsilat gir"in yanına "Tediye gir" (aynı modal, direction farklı).
Tedarikçi tarafında `VendorPayment` zaten ayrı modül. Küçük iş.

### 2.7 Notlar → "Not Ekle"

Müşteri panelindeki Notlar bölümüne append-only not ekleme: `customer_notes` tablosu (TenantEntity: müşteriId, metin,
yazan kullanıcı, tarih) + slim endpoint çifti + panelde listeleme/ekleme. (Alternatif: mevcut Collaboration/Tags
altyapısı incelenip nota uyarlanabilir — implementasyon sprint'inde karar.)

---

## 3. İadeler: "İade Oluştur" akışı

Mevcut: Returns modülü (Approve/Reject/Receive[+otomatik credit note]/Cancel) canlı; admin SPA'da **oluşturma UI'sı yok**
(iade API/portal kaynaklı doğuyor). Plan:

- "İade Oluştur" butonu → iade edilebilir kaynak seçimi: müşteri seçilir → faturalanmış/sevk edilmiş satırları listelenir
  (kalan iade edilebilir miktarlarıyla; daha önce iade edilenler düşülür) → satır+miktar+sebep seçilir → `CreateReturnRequest`.
- Muhasebe döngüsü zaten kurulu: Receive → credit note → GL reverse + müşteri ledger + stok girişi. Yeni GL yazılmaz.
- Kaynak belgenin durumu otomatik güncellenir (Order → Returned / kısmi iade işareti).

---

## 4. Personel Modülü Genişletmesi

Mevcut çekirdek **güçlü**: Türk statutory bordro motoru (`IPayrollCalculationService`, kümülatif vergi merdiveni,
SGK/damga/istisna), `PayrollRun` FSM (Draft→Calculated→Approved→Posted→Paid), GL konsolide fiş, PII maskeleme.
Eksik olan **yüzeyler**:

1. **Ödenek/avans fişleri sayfası:** `Payslip` zaten immutable snapshot; avans/icra `OtherDeductions` +
   335 kalıntı-settlement mekanizması INVARIANTS'ta tanımlı. Yeni `employee_advances` (avans talebi → onay → maaştan
   kesinti planı) + fiş listesi/raporu.
2. **Toplu maaş ödemesi:** `PayrollRun.Paid` geçişi var; eksik banka ödeme listesi çıktısı (XLSX/banka formatı,
   `BankAccount` master'ı + xlsx export altyapısı mevcut) + tek tıkla tüm personele DR335/CR102 ödeme fişi.
3. **PDKS / giriş-çıkış + fazla mesai:** yeni `employee_time_entries` (gün, giriş, çıkış, kaynak: manuel/import).
   Ay sonu job'u normal süre üstünü fazla mesaiye çevirir (1.5x/2x katsayıları `PayrollParameters`'a eklenir),
   bordro hesaplamasına `OvertimeHours` girdisi olarak akar. CSV/Excel import (Imports modülü mevcut).
4. **v-Card:** personel detayında "vCard indir" — `.vcf` üretimi (ad, ünvan, telefon, e-posta, firma) + QR görseli.
   Saf FE/hafif backend, migration'sız.
5. **Özlük görünümü:** bordro geçmişi, SGK/vergi kesinti dökümü, izin (gelecek), alacak/borç (avans) tek sayfada.

> Sıra önerisi: 2 → 1 → 3 → 5 → 4 (toplu ödeme en yüksek değer/en düşük risk; PDKS en büyük yeni yüzey).

---

## 5. Satın Alma (Tedarikçiler dahil) Denetimi

Modül sanılandan olgun (requisition→PO→GRN(QC-hold'lu)→3-way-match→VendorBill→AP aging→VendorPayment, GL zinciri
doğrulanmış). Denetim kapsamı:

1. **Uçtan uca smoke:** PR→PO onay→mal kabul→(QC)→bill→match→ödeme akışını demo tenant'ta koştur; her adımda
   GL/AP/stok etkisini doğrula (çoğu integration testli — UI yolunu da tıkla).
2. **Bilinen açıklar (INVARIANTS'tan):** GRN reversal'ın orijinal maliyetle değerlemesi (value-exact `ApplyReversalAsync`
   iyileştirmesi), `FinanceManager` rolünün gerçekten seed edilmesi ya da kaldırılması, PO kapanışında çoklu-fiyat
   katmanlı GR-IR write-off hassasiyeti.
3. **Tedarikçiler UI:** dark mode taraması (bu sprintte), vendor detayında AP özet kartları (bakiye = vendor ledger,
   müşterideki snapshot-drift dersinin aynısı burada da doğrulanır), tediye/ödeme geçmişi sekmeleri.
4. **Fatura tarafıyla köprü:** gelen e-faturalar (§1.5) VendorBill'e aktığında 3-way-match zinciriyle uyum testi.

---

## 6. Önerilen Sprint Dilimleri

| Sprint | Kapsam                                                                                                     | Bağımlılık            |
| ------ | ---------------------------------------------------------------------------------------------------------- | --------------------- |
| S1     | §2.1 FSM kilidi + taslağa çek; §2.4 kısmi ödeme FE; §2.6 tediye FE; §2.7 not ekle; §2.3 klon UI            | —                     |
| S2     | §2.2 çapraz bağlantı + tek tık fatura/irsaliye + silme geri-bağlantısı; §2.5 sipariş/fatura sayfaya taşıma | S1                    |
| S3     | §1.3-D1 UBL zenginleştirme + kod tabloları + mükellef sorgusu + belge durum makinesi                       | —                     |
| S4     | §1.3-D2 EDM provider + yönetim profili UI                                                                  | S3 + EDM API dokümanı |
| S5     | §1.5 gelen faturalar + §1.6 firma profili                                                                  | S4                    |
| S6     | §3 iade oluştur                                                                                            | S1                    |
| S7     | §4 personel (toplu ödeme → avans → PDKS)                                                                   | —                     |
| S8     | §5 satın alma denetimi + e-irsaliye (D4)                                                                   | S3                    |

> Not: S3 entegratörden bağımsız ilerleyebilir (UBL/kodlar GİB standardı). EDM/Payflex API dokümanları temin edilir
> edilmez S4 başlar; doküman gelmeden provider yazmak spekülatif olur.
