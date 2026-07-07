# Kapsamlı Kod Denetimi (2026-07-06) — Remediation Durumu

> Bu belge, `KAPSAMLI_KOD_DENETIMI_2026-07-06.md` raporundaki bulguların ele alınma durumunu
> özetler. Her bulgu gerçek koda karşı **doğrulandı**, ardından sağlam olanlar önceliğe göre
> uygulandı; her düzeltme build + lint + test yeşil olarak ayrı commit'lendi. Doğrulama sonucu
> "bug değil" çıkanlar ve bilinçli ertelenenler gerekçesiyle listelendi.

**Doğrulama:** Application testleri 2286/2286, dokunulan Integration alanları 112/112, backend
build 0 warning/0 error, frontend `tsc -b` temiz, `has-pending-model-changes` = "No changes".

---

## ✅ Uygulanan düzeltmeler

### §1 — Para · Muhasebe (batch 1-2'de kısmen; bu turda tamamlandı)

- **§1-1** VoidPayment retry çift-ters-çevirme — terminal guard (batch 1).
- **§1-2** FX kur cache tenant-scope'suz — tenant-segment'li cache key (batch 1).
- **§1-3** `journal_entries` denge CHECK — `ck_journal_entries_balanced` (Posted → debit=credit), **Phase127** idempotent migration.
- **§1-4** GL `NextJournalNumberAsync` advisory-lock'suz — `IDocumentSequenceRepository.AcquireLockAsync` (ConsumeAsync ile aynı `docseq:` kilidi) numaralamadan önce alınıyor.
- **§1-5** `UnapplyPayment` manuel SaveChanges + non-idempotent — pipeline envelope'una taşındı (manuel save kaldırıldı) + uygulama-yok → mevcut durumu döndürür (idempotent replay). _(Not: unapply'da müşteri ledger reversal event'i hâlâ yok — ayrı takip.)_
- **§1-6** Percent CHECK'leri — `line_discount/tax_rate/withholding_rate_percent` (invoice/order/quote lines), `tax_rate_percent` (PO/vendor-bill lines), `tax_rates.rate_percent` → `BETWEEN 0 AND 100`, **Phase127**.

### §2 — Sipariş · Teklif · Sevkiyat · İade (batch 1-2)

- **§2-1/§2-2** Shipment `Cancel()` FSM guard'ını baypaslıyordu — `EnsureTransitionAllowed` ile.
- **§2-3** Hasarlı iade (`Restockable=false`) satılabilir stoğa dönüyordu — restock snapshot `.Where(Restockable)`.
- **§2-4** `GetById` 200-null (404 yerine) — GetById handler'ları NotFound fırlatır.
- **§2-5** Reorder mutlak iskonto/maliyet alan kaybı — kopya alanları eklendi.
- **§2-11** ConvertQuoteToOrder satır maliyeti 0 — `unitCostSnapshot` product AvgCost'tan.

### §3 — MRP

- **§3-3** Legacy MrpService talep geçmişi `UpdatedAtUtc` yerine `Order.OrderDate` ile bucket'lanıyor (data-loader ile hizalı).
- **§238** MRP write endpoint'leri (commit/release/firm-planned/dismiss/generate-suggestions) `[Authorize(Roles=TenantAdmin)]` — production-order işlemleriyle tutarlı.
- **§214** MrpService dashboard/suggestion tüm katalogu `ToListAsync` ile çekiyordu → keyset-batch stream (`StreamCandidatesAsync`, 500'lük) + slim kolon projeksiyonu (`MrpProductRow`); yalnız aday kümesi biriktiriliyor. Aday kümesi birebir aynı (batch-boundary testiyle kilitlendi); `productIds.Contains` dev IN-listesi üretmiyor.
- **§220** MRP preview/carry-forward `int.MaxValue` sayfa boyutu → `IPlannedProductionOrderRepository.ListByRunAsync` (500'lük keyset accumulation). Adversarial review (3-lens → verify) + MRP integration testleri (gerçek repo, 20 test) ile doğrulandı.

### §4 — E-Fatura · Bildirim · E-posta

- **§4-1** `IEmailService` 7-metot no-op → gerçek SMTP (`IEmailSender→SmtpEmailSender` + `EmailOptions` register; resilient never-throw, enumeration-safe; reset/verify linkleri `AppBaseUrl`; güvenlik-uyarısı alıcısı chain'den geçirildi).
- **§4-2** E-Fatura satıcı kimliği hardcoded — tenant vergi kimliğinden (`ITenantRepository`); eksikse submission Failed.
- **§4 (ORTA)** `EmailQueuedOutboxHandler` + `IEmailRenderer` DI'da register — planlı e-postalar artık dead-letter olmuyor.
- **§4 (YÜKSEK)** Zamanlı denetim export'u dosyayı teslim etmiyordu — `EmailMessage`/`EmailQueuedPayload` attachment desteği + ScheduledAuditExportJob byte'ları 10 MB cap ile ekliyor.

### §5 — Auth · Gizlilik (KVKK) · Consent

- **§5-1** DSAR access/portability iş yapmadan "Completed" — gerçek `PersonalDataExportDto` üretimi (`BuildExportAsync`) + admin indirme endpoint'i (`GET /admin/privacy/requests/{id}/export`); işleme gerçek `DataExportFileId` marker'ı set ediyor.
- **§5-2** Consent gönderim öncesi kontrol edilmiyor — `NotificationDispatcher` marketing consent gate'i (`MarketingConsentPurpose` set ise geçerli/geri-çekilmemiş consent yoksa blokla; transactional gönderimler etkilenmez).
- **§5-3** Erasure PII eksik — `Employee.Anonymize()` (IBAN/SGK/TCKN/e-posta temizle + soft-delete) + `Payslip.Anonymize()`; `EraseUserCascadeAsync` UserId→Employee→Payslip zincirini anonimleştirir.
- **§5-4** DSAR IDOR/cross-tenant — subject-user tenant'a bağlanıyor (submit + tüm process yolları); `User` global-filter dışı olduğundan açık tenant kontrolü.
- **§5 (ORTA)** AI Helper reindex Dev'de anonim — `[AllowAnonymous]` kaldırıldı, `[Authorize(Roles=TenantAdmin)]` + rate-limit.

### §7 — Frontend

- **§7 (ORTA)** Ölü i18n anahtarları (`warehousesComingSoon`, `mailNotice`) 5 locale'den silindi; `.gitattributes` (`* text=auto eol=lf`) eklendi.

### §6 — Cam · Bordro (batch 3)

- **§6-1** Glass Panel/Run/WorkOrder concurrency token EF'de bağlı değil — config'e `IsConcurrencyToken`.
- **§6-2** Glass BOM çok-para-birimli toplam FX'siz — `IFxRateProvider` ile proje para birimine çevrim.
- **§6-3** Bordro SGK muafiyet (`SgkExempt`) hesapta yok sayılıyor — ayrı SGK matrahı (`SgkGrossSalary`).
- **§3-1/#190** `Product.StockQuantity` concurrency token'sız — `Product : IHasConcurrencyToken` (Phase126).

---

## 🔍 Doğrulandı — bug değil (bilinçli kapatıldı)

- **§3-2 "MRP release Make kayboluyor"** — Backend **iki-sink** mimarisi doğru: `MrpPlannedOrder`=Buy (release→requisition), `PlannedProductionOrder`=Make (ayrı `ReleaseProductionOrderAsync` yolu, frontend `useReleaseProductionOrder`). Make kalemleri kaybolmuyor. Frontend `ReleaseResult` tipindeki 3 fantom alan (`productionOrderIds`/`requisitionsCreated`/`productionOrdersCreated`) backend'in hiç dönmediği + hiçbir yerin okumadığı ölü alanlardı → tip gerçek sözleşmeye hizalandı.
- **§5 "consent boş Guid ile yazılıyor"** — `CurrentUserAccessor.UserId` unauthenticated'ta `null` döner (Guid.Empty DEĞİL) ve `ConsentsController.Capture` bilinçli `[AllowAnonymous]` (fingerprint tabanlı anonim rıza). `UserIdOrThrow()` bu meşru akışı kırardı → dokunulmadı.

---

## ⏸️ Ertelenen (gerekçeli)

| Bulgu                                                                           | Gerekçe                                                                                                                                                                                                                                                                           |
| ------------------------------------------------------------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| **§4 (YÜKSEK)** Zamanlı rapor teslimi render/attach etmiyor                     | Built-in `reportKey` için generic rapor-yürütme pipeline'ı yok (yalnız custom rapor `RunCustomReportQuery` ile render olur). Feature-boyutu iş, "fix" değil. Denetim-export teslimi (attachment mekanizması) yapıldı.                                                             |
| **§4 (YÜKSEK/ORTA)** S3/Azure Blob + Redis stub                                 | Harici SDK + altyapı kararı gerektirir (raporun kendisi "SDK ekle veya NFS'i belgele" diyor). Startup fail-fast zaten var.                                                                                                                                                        |
| **§208 (YÜKSEK)** ProductionExecutionService N+1                                | N+1 costing/allocation **chokepoint'i** içinde (her bileşen issue'u gerçek bir mutasyon: FIFO-consume + GL); batch varyantı yüksek-riskli chokepoint değişimi, seyrek yazma yolunda (küçük N) orantısız. Invariants chokepoint'e dokunmaya karşı uyarıyor.                        |
| **§226 (ORTA)** PO per-line teslim tarihi                                       | Yeni kolon + migration + loader değişimi (ERP-MRP-002 dökümante açık).                                                                                                                                                                                                            |
| **§232 (ORTA)** MRP NetChange = Regenerative                                    | Bu run-scoped model'de gerçek incremental mümkün değil (kalıcı plan tablosu gerektirir); dökümante sınır.                                                                                                                                                                         |
| **§244 (ORTA)** GenerateRequisitionSuggestions idempotency                      | `(tenant, asOfDate)` durable guard — legacy MrpService yolu; endpoint artık TenantAdmin-gated.                                                                                                                                                                                    |
| **§256 (DÜŞÜK)** stock_items FILLFACTOR                                         | Trivial idempotent `ALTER`; bir sonraki DB-hardening migration'ıyla.                                                                                                                                                                                                              |
| **§250 (DÜŞÜK)** operationId ölü kontrat                                        | İmza temizliği; FSM-doğal idempotency zaten yeterli.                                                                                                                                                                                                                              |
| **§5 (ORTA)** `/me/erase` MFA step-up                                           | `RequireRecentMfaAttribute` MFA-kayıtsız kullanıcıya da 428 döndürür → frontend 428 step-up interceptor'ı canlı olmadan eklenirse **her MFA'sız kullanıcı kendi hesabını silmekten kilitlenir** (memory'deki dökümante lockout riski). Frontend interceptor'la birlikte açılmalı. |
| **§1-5 ledger reversal**                                                        | `Payment.Unapply` reversal event'i yaymıyor → müşteri ledger'ı unapply'da ters çevrilmiyor; ayrı event+handler gerektirir (double-reversal riski, dikkatli analiz).                                                                                                               |
| **§2-6/§2-8/§2-9, §6 glass FE window.prompt/negatif-guard/FSM Ready→Defective** | ORTA/DÜŞÜK; ayrı dilimler.                                                                                                                                                                                                                                                        |

---

_Uygulanan tüm düzeltmeler `glass-panel-shapes-arc` dalında, her biri ayrı commit + test._
