# Runbook 05 — Backup & Restore

> **Last verified:** 2026-06-01

## Politika

| Tip                              | Sıklık             | Retention                    | Storage                                           |
| -------------------------------- | ------------------ | ---------------------------- | ------------------------------------------------- |
| Full logical dump (`pg_dump`)    | Her gece 02:00 UTC | 30 gün                       | S3 `s3://corealign-backups/full/` (KMS encrypted) |
| WAL archiving (WAL-G)            | Sürekli            | 7 gün point-in-time recovery | S3 `s3://corealign-backups/wal/`                  |
| Encrypted snapshot (RDS managed) | Her gün            | 7 gün otomatik               | RDS otomatik                                      |
| Manuel snapshot (release öncesi) | Her release        | Manuel temizlik              | Adlandırma: `pre-release-vX.Y.Z`                  |
| Restore drill                    | Aylık 1 kere       | —                            | Staging'e restore + smoke test                    |

**RPO** (Recovery Point Objective): 15 dakika (WAL archive sayesinde).
**RTO** (Recovery Time Objective): 2 saat (point-in-time restore + replica warmup).

## Full backup (cron)

```bash
#!/usr/bin/env bash
# /etc/cron.daily/corealign-backup
set -euo pipefail

DATE=$(date -u +%Y%m%d-%H%M)
BACKUP_FILE="/tmp/corealign-${DATE}.dump"

PGPASSWORD="${DB_PASSWORD}" pg_dump \
  --host "${DB_HOST}" \
  --port "${DB_PORT:-5432}" \
  --username corealign \
  --dbname corealign \
  --format custom \
  --compress 9 \
  --jobs 4 \
  --file "${BACKUP_FILE}"

aws s3 cp "${BACKUP_FILE}" \
  "s3://corealign-backups/full/corealign-${DATE}.dump" \
  --sse aws:kms \
  --sse-kms-key-id "${KMS_KEY_ID}"

rm "${BACKUP_FILE}"

# Retention: 30 gün önceki silinir
aws s3 ls s3://corealign-backups/full/ | \
  awk -v d="$(date -u -d '30 days ago' +%Y%m%d)" '$1 < d {print $4}' | \
  xargs -I{} aws s3 rm "s3://corealign-backups/full/{}"
```

## WAL archiving (point-in-time recovery)

PostgreSQL config:

```conf
# postgresql.conf
wal_level = replica
archive_mode = on
archive_command = 'wal-g wal-push %p'
```

WAL-G environment:

```bash
WALG_S3_PREFIX=s3://corealign-backups/wal
AWS_ACCESS_KEY_ID=...
AWS_SECRET_ACCESS_KEY=...
WALG_LIBSODIUM_KEY=<32-byte hex>  # encryption
```

Periyodik base backup (haftada bir):

```bash
wal-g backup-push /var/lib/postgresql/17/main
```

## Restore

### Senaryo 1: Tek tablo / belirli veri (point-in-time)

```bash
# 1. Yeni geçici DB instance oluştur
createdb -h <restore-host> corealign_recovery

# 2. WAL-G ile en yakın hedef zamana restore
wal-g backup-fetch /var/lib/postgresql/17/recovery LATEST
# postgresql.conf:
#   restore_command = 'wal-g wal-fetch %f %p'
#   recovery_target_time = '2026-06-01 14:30:00 UTC'
# Postgres'i restore mode'da başlat

# 3. İhtiyacın olan tabloyu dump et
pg_dump -h <restore-host> -U corealign -d corealign_recovery \
  --table=invoices --data-only --format=plain > affected_invoices.sql

# 4. Production'a uygula (KARELERINE BAKARAK)
psql -h <prod-host> -U corealign -d corealign -f affected_invoices.sql
```

### Senaryo 2: Full disaster — DB tamamen kayıp

```bash
# 1. Yeni RDS instance oluştur (boş)
aws rds restore-db-instance-from-db-snapshot \
  --db-instance-identifier corealign-prod-new \
  --db-snapshot-identifier corealign-prod-2026-06-01-02-00

# 2. WAL-G ile point-in-time'a ilerlet (gerekirse)

# 3. DNS/config güncelle
kubectl set env deployment/corealign-api \
  ConnectionStrings__DefaultConnection="Host=corealign-prod-new.xxxxx.eu-central-1.rds.amazonaws.com;..." \
  -n corealign

# 4. Application restart
kubectl rollout restart deployment/corealign-api -n corealign

# 5. Smoke test (01-deployment.md adım 6)
```

### Senaryo 3: Tek tenant geri yükleme

```bash
# Tüm tenant_id = X verisini eski snapshot'tan al, mevcut DB'ye merge et
# DİKKAT: bu özel iş, manuel SQL gerektirir. Genel script yok.
# Adım:
# 1. Snapshot'tan tek tenant'a ait satırları DUMP
# 2. Mevcut DB'de tenant'ın o tablolarındaki satırları sil
# 3. DUMP'ı uygula
# Transaction içinde yap, foreign key sıralaması önemli
```

## Monthly restore drill

**Her ayın ilk Çarşamba:**

1. Production'un dünkü full backup'ını staging'e indir.
2. Staging DB'sini wipe et + restore et.
3. Migration'ları staging'de tetikle (eğer prod sürümünden geride ise).
4. Staging API'ye smoke test çalıştır (01-deployment.md adım 6).
5. Süreyi ölç → 2 saat RTO hedefi içinde mi?
6. Bulguları `docs/runbooks/restore-drills.md` (NEW — drill geçmiş kaydı) dosyasına yaz.

## Backup verification

Backup yedeğinin geçerli olduğunu doğrula:

```bash
# Header check
pg_restore --list /tmp/corealign-20260601-020000.dump | head -20
# Beklenen: TOC entries, no error

# Test restore (RAM disk veya geçici instance)
pg_restore --jobs 4 --no-owner --no-privileges \
  --dbname corealign_test_restore \
  /tmp/corealign-20260601-020000.dump

# Row count sanity
psql -d corealign_test_restore -c '
SELECT
  (SELECT COUNT(*) FROM tenants) AS tenants,
  (SELECT COUNT(*) FROM users) AS users,
  (SELECT COUNT(*) FROM orders) AS orders,
  (SELECT COUNT(*) FROM invoices) AS invoices;
'
```

## Encryption

- **At rest**: RDS KMS-encrypted; S3 SSE-KMS; WAL-G libsodium key.
- **In transit**: SSL connection enforced (`sslmode=require`).
- **Backup key rotation**: yılda bir; eski backup'lar eski key ile decrypt edilebilmeye devam eder.

## Logical replication (geleceğe yönelik)

Multi-region veya read replica için PostgreSQL logical replication ayarı. Şu an aktif değil. INFRA-012 (Redis) sonrası gündeme alınır.

## Sorun giderme

| Belirti                       | Yapılacak                                                          |
| ----------------------------- | ------------------------------------------------------------------ |
| `pg_dump` "out of disk"       | Geçici dump dosyası için ayrı disk mount et                        |
| WAL archive geride kalıyor    | `archive_command` exit code kontrol, S3 erişim sorunu mu?          |
| Restore "FATAL: lock_timeout" | Mevcut connection'ları kıs: `SELECT pg_terminate_backend(pid)`     |
| Snapshot restore yavaş        | Önce snapshot'tan instance oluştur, sonra point-in-time WAL replay |
