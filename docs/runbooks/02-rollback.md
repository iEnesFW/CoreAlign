# Runbook 02 — Rollback

> **Last verified:** 2026-06-01

Üretim deploy'u kötü gittiyse hızlıca eski sürüme dön. **Hız > kibarlık** — önce yangını söndür, sonra postmortem yap.

## Karar matrisi

| Belirti                                             | Aksiyon                                                     |
| --------------------------------------------------- | ----------------------------------------------------------- |
| Error rate %1'in üzerinde + spike Sentry'de görüldü | **HEMEN rollback**                                          |
| p99 latency 2x arttı + healthcheck OK               | Gözlemle, 5 dk içinde düşmezse rollback                     |
| Yeni feature bug'lı ama kritik akışlar çalışıyor    | Hot-fix branch + cherry-pick + yeni release                 |
| Migration sonrası DB schema bozuk                   | **STOP** — DB rollback gerekir, application rollback yetmez |

## 1. Application rollback (kod)

### Docker Compose

```bash
cd /opt/corealign
# Önceki tag'i bul
LAST_TAG=$(docker compose -f docker-compose.full.yml config | grep image | grep corealign-api | grep -oP 'v\d+\.\d+\.\d+' | sort -V | tail -2 | head -1)
echo "Rolling back API to $LAST_TAG"

# Tag'i değiştir
sed -i "s|corealign-api:v[0-9.]*|corealign-api:$LAST_TAG|g" docker-compose.full.yml
docker compose -f docker-compose.full.yml pull
docker compose -f docker-compose.full.yml up -d --force-recreate
```

### Kubernetes

```bash
# Hızlı rollback — son başarılı revision'a dön
kubectl rollout undo deployment/corealign-api -n corealign
kubectl rollout status deployment/corealign-api -n corealign --timeout=2m

# Belirli bir tag'e dönmek için:
kubectl set image deployment/corealign-api api=ghcr.io/<owner>/corealign-api:v1.1.9 -n corealign
```

Aynı işlemi 3 SPA için tekrar et: `corealign-admin`, `corealign-customer-portal`, `corealign-b2b`.

## 2. Verify

```bash
curl -fsS https://api.corealign.io/health/ready | jq
# Sentry → Issues → Resolved bekleniyor: yeni event azalmalı
# Grafana → latency düşmeli
```

## 3. Database migration rollback

**ÖNEMLİ**: EF migration `Down()` methodu **veri kaybına yol açabilir** (column drop, table drop). Migration rollback yapmadan önce:

1. Mevcut DB snapshot al:
   ```bash
   pg_dump -h <host> -U corealign corealign -F c -f /tmp/before-rollback-$(date +%s).dump
   ```
2. `Down()` migration kodunu oku ve veri etkisini analiz et:
   ```bash
   cat server/src/CoreAlign.Infrastructure/Persistence/Migrations/<TIMESTAMP>_<NAME>.cs
   ```
3. Eğer geri alınamayacak veri varsa (örn. `DropColumn`), **rollback yapma** — bunun yerine:
   - Eski API kodunu deploy et (column yeni şemada var ama yeni kod kullanmıyor → uyumlu)
   - Bug fix'le ileri git
   - **Forward-only migration** yaklaşımı genelde daha güvenli

### Rollback kararı veriyorsan:

```bash
# Önce hedef migration adını bul
dotnet ef migrations list \
  --project server/src/CoreAlign.Infrastructure \
  --startup-project server/src/CoreAlign.API

# Hedef migration'a geri dön (Down çalışır)
dotnet ef database update <PREVIOUS_MIGRATION_NAME> \
  --project server/src/CoreAlign.Infrastructure \
  --startup-project server/src/CoreAlign.API \
  --connection "<DB_CONN>"
```

## 4. Replicas senkronizasyonu

Multi-replica deployment'larda rollback sonrası **tüm pod'ların yeni image ile çalıştığından emin ol**:

```bash
kubectl get pods -n corealign -l app=corealign-api -o wide
# Tüm pod'lar yeni image SHA'sini göstermeli
```

## 5. Communication

- **Sev-1/2 ise** Slack `#incidents` kanalına anons:
  > ⚠️ Rollback: v1.2.0 → v1.1.9 production'da. Sebep: <kısa açıklama>. Detaylar gelecek.
- 15 dakika içinde durum güncellemesi.
- 24 saat içinde postmortem ([04-incident-response.md](04-incident-response.md)).

## 6. Gözle önemli metrikler (rollback sonrası 30 dk)

- HTTP 5xx oranı → düştü mü?
- p95/p99 latency → eski baseline'a döndü mü?
- Sentry yeni event rate → durdu mu?
- Login success oranı → normal mi?
- Outbox backlog → eriyor mu yoksa büyüyor mu? (`SELECT COUNT(*) FROM outbox_messages WHERE processed_at_utc IS NULL`)

## 7. Postmortem

Sev-1/2 sonrasında her zaman blameless postmortem. Şablon: [04-incident-response.md#postmortem-template](04-incident-response.md#postmortem-template).

## Sorun giderme

| Belirti                                                       | Yapılacak                                                                    |
| ------------------------------------------------------------- | ---------------------------------------------------------------------------- |
| Rollback sonrası hâlâ 5xx                                     | DB schema rollback eksik OR cache stale OR config drift                      |
| Eski image registry'de yok                                    | Manuel build + push: `docker build -t ghcr.io/.../corealign-api:emergency .` |
| EF migration rollback `Sequence contains no matching element` | Hedef migration adı yanlış — `dotnet ef migrations list` ile doğrula         |
