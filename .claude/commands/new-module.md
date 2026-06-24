---
description: 'Yeni FSD+CQRS modülü iskeleti (doğru yüzey + TenantEntity + handler + UI + i18n).'
---

Yeni modül: $ARGUMENTS

ÖNCE en yakın mevcut modülü oku ve AYNI pattern'i uygula (§13.1 — tutarlılık > yaratıcılık). §0.1 indeksinden ilgili alanın tuzaklarını al.

**Backend (§3/§4)**

- `Domain` entity → `TenantEntity` türet (TenantId + Id(UUIDv7) + timestamps + tenant FK + auto-filter otomatik gelir). Yarışabilirse §4.6 concurrency token.
- EF Configuration (snake_case convention; FK+index; doğru tip: para `decimal(18,4)`, zaman `timestamptz`).
- Migration: §4.2/§4.12 (build'li üret → ileri-tarihli ID'ye rename → idempotent → aynı turda `database update` → tabula-rasa).
- `Application/<Modul>/{Commands,Queries,Validators,DTOs,Handlers}` + Repository (pure data access, N+1 yok) + slim Controller.

**Frontend (§2)**

- DOĞRU yüzey seç (§0: admin `src/` / `apps/customer-portal` / `apps/b2b` / `mobile/`).
- `features/<x>/{api,hooks,model,ui}` + `pages/<x>` + i18n tr+en. Dark + responsive. `primary-*` token.

**ERP doğruluk (§16):** para/stok mutasyonu varsa idempotency + transaction sınırı + audit. Bitince **`/pre-ship`** + §15 invariant kontrolü.
