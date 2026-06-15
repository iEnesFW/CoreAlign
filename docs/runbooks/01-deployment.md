# Runbook 01 — Deployment

> **Last verified:** 2026-06-01

Bu runbook üretim deployment akışını anlatır. Tüm adımlar bir release etiketi (`v*.*.*`) push edildikten sonra tetiklenir.

## Ön koşullar

- `main` branch'te green build (CI yeşil).
- Tüm migration'lar yerel olarak test edilmiş.
- `docs/secrets-inventory.md` üzerindeki tüm secret'lar hedef ortamda set edilmiş.
- DNS + TLS sertifikası hazır (`docs/DEPLOY_SUBDOMAINS.md`).

## 1. Release tag oluştur

```bash
git tag -a v1.2.0 -m "Release v1.2.0 — sprint 2 compliance + observability"
git push origin v1.2.0
```

GitHub Actions `release.yml` workflow'u devreye girer → 4 Docker imaj build edilir, `ghcr.io/<owner>/corealign-{api,admin,customer-portal,b2b}:v1.2.0` etiketiyle push edilir.

## 2. Ortam değişkenleri (env matrix)

| Ortam      | API URL                            | DB                           | Vault                         | Sentry environment |
| ---------- | ---------------------------------- | ---------------------------- | ----------------------------- | ------------------ |
| Dev        | `http://localhost:5178`            | `corealign-postgres` (local) | None                          | `Development`      |
| Staging    | `https://api.staging.corealign.io` | RDS `corealign-staging`      | AWS SSM `/corealign/staging/` | `Staging`          |
| Production | `https://api.corealign.io`         | RDS `corealign-prod`         | AWS SSM `/corealign/prod/`    | `Production`       |

`docs/secrets-inventory.md` her secret için sensitivity tier'ı listeler. Production secret'ları için Azure Key Vault / AWS SSM kullan; **asla** `appsettings.Production.json` içinde değer commit etme.

## 3. Database migration

**ÖNCE migration'ı çalıştır, SONRA yeni API imajını deploy et.** Aksi takdirde eski şema üzerinde yeni kod çalışır → 5xx fırtınası.

```bash
# Migration-only image
docker run --rm \
  -e ConnectionStrings__DefaultConnection="$DB_CONN" \
  ghcr.io/<owner>/corealign-api:v1.2.0 \
  --migrate
```

`--migrate` flag'i migration'ı uygular ve **process exit eder** (web sunucu başlatmaz). Detay: [03-db-migration.md](03-db-migration.md).

## 4. Deploy

### Docker Compose (single-host)

```bash
cd /opt/corealign
git pull
docker compose -f docker-compose.full.yml pull
docker compose -f docker-compose.full.yml up -d
```

### Kubernetes

```bash
kubectl set image deployment/corealign-api api=ghcr.io/<owner>/corealign-api:v1.2.0 -n corealign
kubectl set image deployment/corealign-admin admin=ghcr.io/<owner>/corealign-admin:v1.2.0 -n corealign
kubectl set image deployment/corealign-customer customer=ghcr.io/<owner>/corealign-customer-portal:v1.2.0 -n corealign
kubectl set image deployment/corealign-b2b b2b=ghcr.io/<owner>/corealign-b2b:v1.2.0 -n corealign
kubectl rollout status deployment/corealign-api -n corealign --timeout=5m
```

## 5. Health check verification

```bash
# Liveness
curl -fsS https://api.corealign.io/health/live | jq
# Beklenen: {"status":"Healthy"}

# Readiness (DB dahil)
curl -fsS https://api.corealign.io/health/ready | jq
# Beklenen: {"status":"Healthy","results":{"npgsql":{"status":"Healthy"}}}

# Prometheus metrics
curl -fsS https://api.corealign.io/metrics | head -20
# Beklenen: Prometheus exposition formatı (HELP/TYPE satırları)
```

## 6. Smoke test (post-deploy sanity)

```bash
# 1. Admin login
curl -fsS -X POST https://api.corealign.io/api/v1/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@<tenant-domain>","password":"<password>"}' | jq .accessToken

# 2. Tenant dashboard (token gerekir)
TOKEN="<paste from step 1>"
curl -fsS https://api.corealign.io/api/v1/dashboard \
  -H "Authorization: Bearer $TOKEN" | jq

# 3. Bir sipariş oluştur (cURL veya admin SPA üzerinden)
# 4. Faturayı issue et → e-Fatura outbox tetiklenir mi? Log'larda "Submitted to Stub" görmelisin.
# 5. Customer portal login + onay akışı çalışıyor mu?
# 6. b2b portal login + sipariş oluşturma → onaya gidiyor mu?
```

## 7. Sentry release tag

Source map upload otomatik olarak `release.yml` içinde yapılır. Manuel tetiklemek için:

```bash
npx @sentry/cli releases new v1.2.0 --org corealign --project corealign-api
npx @sentry/cli releases files v1.2.0 upload-sourcemaps dist/ --org corealign --project corealign-admin
npx @sentry/cli releases finalize v1.2.0
```

## 8. Cache invalidation (gerekiyorsa)

Distribured cache henüz yok (Redis = INFRA-012). Bugün her API replica kendi `IMemoryCache`'ini taşıyor. Deploy sonrası 30-300 saniye içinde otomatik invalidate olur (DashboardCacheService 30s TTL, LookupCacheService 5m TTL).

## 9. Monitoring

- **Sentry**: `https://sentry.io/organizations/corealign/issues/?environment=Production` — yeni release'de error rate spike var mı?
- **Prometheus / Grafana**: `https://grafana.corealign.io/d/corealign-api` — HTTP latency p99, request rate, EF Core query duration.
- **Logs**: Serilog file sink → log shipper (Vector/Fluentbit) → Loki. Query: `{app="corealign-api"} |= "ERROR"`.

## 10. Rollback decision

Eğer:

- Error rate %0.1'in üzerine çıktıysa,
- p99 latency 2 katından fazla arttıysa,
- 5xx oranı %1'i geçtiyse,

→ Hemen [02-rollback.md](02-rollback.md) uygula.

## Sorun giderme

| Belirti                   | Olası neden                                                  | Yapılacak                                                     |
| ------------------------- | ------------------------------------------------------------ | ------------------------------------------------------------- |
| `/health/ready` 503 döndü | DB bağlantı yok                                              | Connection string + DB erişimi kontrol et                     |
| `/metrics` 404            | OTel kayıt edilmedi                                          | `Program.cs` `MapPrometheusScrapingEndpoint()` çağrılıyor mu? |
| Sentry'de event yok       | DSN yanlış veya SendDefaultPii=false ile PII tamamen silindi | `Sentry:Dsn` config, BeforeSend callback debug                |
| 401 dalgası               | JWT key rotation eksik                                       | [07-key-rotation.md](07-key-rotation.md)                      |
| e-Fatura outbox başarısız | Stub gateway → gerçek entegratör değişiminde config          | `EInvoice:Provider` config                                    |
