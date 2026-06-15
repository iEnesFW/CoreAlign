# Runbook 03 — Database Migration

> **Last verified:** 2026-06-01

EF Core migration'ları üretimde uygulamak için üç yaklaşım var. **Çoğu zaman: Yaklaşım B (migration container).** Auto-migrate sadece tek replica + dev/staging için.

## Yaklaşımlar

### A. Auto-migrate on startup (DEV/STAGING ONLY)

`appsettings.json` içinde `Database:AutoMigrate=true` veya CLI flag:

```bash
dotnet run --project server/src/CoreAlign.API -- --migrate
```

`Program.cs` zaten bu flag'i destekliyor (`MigrateAsync()` → `Environment.Exit(0)`).

**Tehlike**: Multi-replica deployment'larda her replica aynı anda migration uygulamaya çalışır → race. Production'da **kullanma**.

### B. Migration container (RECOMMENDED for prod)

CI/CD pipeline'ı şu sırayla:

1. **Önce** sadece migration container'ı çalıştır:

   ```bash
   docker run --rm \
     -e ConnectionStrings__DefaultConnection="$DB_CONN" \
     ghcr.io/<owner>/corealign-api:v1.2.0 \
     --migrate
   ```

   - Process zero exit code ile biter.
   - **Sonra** application replica'larını deploy et.

2. **Sonra** application'ı deploy et:
   ```bash
   kubectl set image deployment/corealign-api api=ghcr.io/<owner>/corealign-api:v1.2.0 -n corealign
   ```

Bu sıralama: yeni şema önce kurulur, yeni kod sonra çalışır. Eski kod yeni şemayı görür ama kullanmazsa sorun olmaz (forward-compatible migration kuralı).

### C. SQL bundle (manuel DBA)

Bazı kurumsal müşteriler EF Core'un DB'ye doğrudan bağlanmasını istemez. Bu durumda script üret:

```bash
dotnet ef migrations script <FROM_MIGRATION> <TO_MIGRATION> \
  --project server/src/CoreAlign.Infrastructure \
  --startup-project server/src/CoreAlign.API \
  --output migrations.sql \
  --idempotent
```

`--idempotent` → script kendisi `__EFMigrationsHistory` tablosuna bakar, daha önce uygulanmış migration'ları atlar.

DBA bu script'i kendi tooling'i ile uygular. Sonra application deploy edilir.

## Multi-replica race koruması

K8s'te basit çözüm: **migration için ayrı bir Job kullan**:

```yaml
apiVersion: batch/v1
kind: Job
metadata:
  name: corealign-migrate-v1-2-0
spec:
  backoffLimit: 0
  template:
    spec:
      restartPolicy: Never
      containers:
        - name: migrate
          image: ghcr.io/<owner>/corealign-api:v1.2.0
          args: ['--migrate']
          envFrom:
            - secretRef:
                name: corealign-db
```

`Job` tamamlanmadan Deployment rollout başlatma. ArgoCD / Flux gibi GitOps tool'ları bunu doğal olarak destekler (`waveSync` annotation).

Tek host docker-compose için sıralama bash'te garanti edilir:

```bash
docker compose run --rm api --migrate && \
docker compose -f docker-compose.full.yml up -d
```

## Migration yazma kuralları

1. **Forward-compatible**: Eski kod yeni şemada çalışabilmeli. Yeni column ekliyorsan `nullable` veya `default` koy.
2. **Eski column'u hemen drop etme**: 2 release boyunca tut (drop'u ayrı bir migration'a koy).
3. **Index oluştururken büyük tablolarda**: `migrationBuilder.Sql("CREATE INDEX CONCURRENTLY ...")` (PostgreSQL özelliği — long-running, locking yok).
4. **Veri taşıma**: ayrı bir handler/job yaz, migration içinde minimum SQL.
5. **`ExecuteUpdate` / `ExecuteDelete`**: idempotent yaz.

## Yerelden migration üretme

```bash
# 1. Entity'leri / config'i değiştir
# 2. Migration oluştur
dotnet ef migrations add Phase29YourFeature \
  --project server/src/CoreAlign.Infrastructure \
  --startup-project server/src/CoreAlign.API

# 3. Üretilen .cs dosyasını incele — istenmeyen değişiklik var mı?
#    (Bazen EF, snapshot drift'ten dolayı başka entity'lerden de değişiklik bundle eder)
# 4. Migration body'sini gerekirse manuel düzelt
# 5. Test:
dotnet ef database update --connection "Host=localhost;..."
# 6. Geri al ve tekrar uygula → idempotent mı kontrol et
dotnet ef database update <PREVIOUS_NAME>
dotnet ef database update
```

## Snapshot drift sorunları

Paralel branch'lerde aynı zamanda migration oluşturulduysa snapshot çakışabilir. Belirti: yeni migration `dotnet ef migrations add` çalıştırınca, başka entity'lerden alakasız değişiklikler bundle ediyor.

Çözüm:

```bash
# Snapshot'ı sıfırla, son migration'dan yeniden üret
dotnet ef migrations remove --project server/...
# Conflict'i çöz, sonra:
dotnet ef migrations add YourMigrationName --project server/...
```

Snapshot dosyası: `server/src/CoreAlign.Infrastructure/Persistence/Migrations/CoreAlignDbContextModelSnapshot.cs`. Her migration'ın `.Designer.cs` dosyası o anki snapshot'ın kopyasıdır. Snapshot ve son Designer **aynı** olmalı.

## Verify

```bash
# Hangi migration'lar uygulanmış?
psql -h <host> -U corealign -d corealign -c \
  'SELECT "MigrationId", "ProductVersion" FROM "__EFMigrationsHistory" ORDER BY "MigrationId" DESC LIMIT 10;'

# Beklenen pattern: 20260601...
```

## Rollback

[02-rollback.md](02-rollback.md) — özellikle "Database migration rollback" bölümü. Forward-only tercih edilir.

## Mevcut migration phase'leri

| Phase | İçerik                                                                   |
| ----- | ------------------------------------------------------------------------ |
| 1-13  | Core domain (Customers, Orders, Invoices, Inventory, Accounting, Outbox) |
| 14    | CustomerTags                                                             |
| 15    | Collaboration (Comment + Notification)                                   |
| 16-17 | Billing modules + Iyzico                                                 |
| 18    | B2B Identity (DealerAccount/User, CustomerUser)                          |
| 19    | Dealer order approval flow                                               |
| 20    | Webhook idempotency (ProcessedWebhookEvent)                              |
| 21-22 | Notification outbox uniqueness                                           |
| 23    | Dealer product visibility                                                |
| 24    | KVKK + survey applied                                                    |
| 25    | Password history                                                         |
| 26    | KVKK enhancements (Customer.IsAnonymized + IP hashing)                   |
| 27    | UserConsent                                                              |
| 28    | Tenant legal contacts (DPO email vb.)                                    |

## Background jobs (Hangfire) and tenant scope

Recurring jobs registered via Hangfire (`outbox-drain`, `token-cleanup`, `log-ip-anonymize`)
run OUTSIDE the HTTP request pipeline, so `ITenantContext.CurrentTenantId` is `null`
inside them. Repositories that rely on the global query filter must call
`IgnoreQueryFilters()` whenever a job needs to iterate rows across all tenants —
the `MaintenanceDataAccess` helper does this for `TwoFactorChallenges` and
`ActivityLogs`. New job logic that scans tenant-owned tables must follow the same
pattern: load via `IgnoreQueryFilters()` and never call code paths that throw
`MissingTenantContextException`.
