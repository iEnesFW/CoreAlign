---
description: "Yeni API endpoint'i için CoreAlign checklist'i (slim controller → Application → tenant → test → i18n)."
---

Yeni endpoint: $ARGUMENTS

CLAUDE.md §3 / §6 / §14 sırasıyla — atlama:

1. **Yönel.** Hangi modül (§0.1 indeks)? O satırın tuzaklarını + "önce oku"yu aç. `docs/INVARIANTS.md`'yi ilgili etiketlerle tara.
2. **Application.** `Command`/`Query` + FluentValidation `Validator` (+ ≥2 test: red & green) + `DTO` (entity SIZDIRMA) + `Handler`. Para/stok/durum mutasyonu → `ITransactionalRequest` + idempotency (durable natural key tercih, §3.9/§16).
3. **Controller.** Slim (≤10 satır gövde) — sadece bind + dispatch + return. `[Authorize]` bilinçli. Yanıt `ApiResponse<T>`. Hata = uygun exception fırlat (status KODLAMA).
4. **Tenant.** Sorgu global query filter'dan geçiyor mu? `GetById` eksik kayıtta `NotFoundException` (200-null leak YOK).
5. **Concurrency/para.** Yarışabilen aggregate doğru token (§4.6: `IXminConcurrency` / `IHasConcurrencyToken`). Para `decimal(18,4)`.
6. **Test (Integration).** happy + auth-reddi + **cross-tenant izolasyon** (TenantA token, TenantB resource → {404,403}) + **N+1 round-trip bütçesi** (tight 3-4).
7. **Frontend.** Doğru yüzey (§0) → `features/<x>/api` + `hooks/use<X>Queries` + UI. Görünen metin `t()` + tr&en senkron.
8. **`/pre-ship`.**
