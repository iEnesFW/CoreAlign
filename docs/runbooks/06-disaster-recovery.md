# Runbook 06 — Disaster Recovery

> **Last verified:** 2026-06-01

Bölge çapında felaket / büyük outage durumunda CoreAlign'ı yeniden ayağa kaldırma rehberi. **RTO 2 saat, RPO 15 dakika.**

## Tanımlar

- **RPO (Recovery Point Objective)**: en fazla 15 dakika veri kaybı kabul edilebilir.
- **RTO (Recovery Time Objective)**: hizmet 2 saat içinde geri yükselmeli.
- **Felaket türleri**:
  1. AWS region tamamen down (bölge bazlı outage)
  2. DB veri bozulması (silent corruption keşfi)
  3. Ransomware / yetkisiz erişim → tüm production'ın yeniden inşası
  4. Insan hatası: production DB drop / büyük veri silme

## Yedekleme topolojisi (mevcut)

```
┌─────────────────────────────────────────────┐
│ Region eu-central-1 (PRIMARY)               │
│  ┌──────────────────┐  ┌─────────────────┐  │
│  │ RDS PostgreSQL   │  │ K8s app pods    │  │
│  │ Multi-AZ enabled │  │ 3 replicas      │  │
│  └────────┬─────────┘  └─────────────────┘  │
│           │ daily snapshot                  │
│           │ WAL archive every minute         │
└───────────┼─────────────────────────────────┘
            ↓
┌─────────────────────────────────────────────┐
│ S3 bucket (cross-region replication ON)     │
│  s3://corealign-backups (eu-central-1)      │
│  s3://corealign-backups-dr (eu-west-1)      │
└─────────────────────────────────────────────┘
```

Cross-region S3 replication açık. DR region (eu-west-1) sadece backup tutuyor — application orada **standby değil** (cold). Active-passive kurulumu NICE-023 sonrası gündeme gelir.

## Senaryo 1: AWS region down

eu-central-1 erişilmez.

### Karar (5 dk)

- AWS health dashboard kontrolü
- Tahmini downtime > 2 saat ise → DR failover başlat
- Daha kısa ise → bekle (DR failover'in kendi maliyeti var)

### DR failover (60-90 dk)

1. **DR region'a RDS yarat** (eu-west-1):

   ```bash
   aws rds restore-db-instance-from-db-snapshot \
     --db-instance-identifier corealign-dr \
     --db-snapshot-identifier <latest-cross-region-replicated-snapshot> \
     --region eu-west-1
   ```

   - Multi-AZ enabled, instance class production ile aynı.
   - Snapshot 24 saatten yeniyse veri kaybı 24 saat içinde (kötü senaryo).

2. **WAL-G ile en güncel point-in-time'a ilerlet** (RPO 15 dk hedefi için):

   ```bash
   # DR region'da yeni RDS instance bir replica olarak başlatılmış olabilir
   # WAL-G replay süresi WAL hacmine bağlı (genelde dakikalar)
   ```

3. **K8s cluster DR region'da yarat** (Terraform `dr-cluster.tf` veya benzeri):

   ```bash
   terraform -chdir=infra/dr apply
   ```

4. **Secret'ları DR region'a kopyala**:

   ```bash
   # AWS SSM parameter store cross-region copy
   for param in /corealign/prod/db-conn /corealign/prod/jwt-secret /corealign/prod/iyzico-api-key; do
     value=$(aws ssm get-parameter --name "$param" --with-decryption --region eu-central-1 --query 'Parameter.Value' --output text)
     aws ssm put-parameter --name "$param" --type SecureString --value "$value" --region eu-west-1 --overwrite
   done
   ```

5. **Application deploy** (mevcut latest image):

   ```bash
   kubectl --context=corealign-dr apply -f k8s/
   kubectl --context=corealign-dr set image deployment/corealign-api api=ghcr.io/<owner>/corealign-api:<current-tag>
   ```

6. **DNS güncelle** (Route53):

   ```bash
   # api.corealign.io → DR ALB
   aws route53 change-resource-record-sets \
     --hosted-zone-id <ZONE_ID> \
     --change-batch file://dns-failover-to-dr.json
   ```

   TTL 60s; propagation 1-2 dk.

7. **Smoke test** ([01-deployment.md](01-deployment.md) adım 6).

8. **Communication**: status page güncelle, müşterilere e-posta.

### Failback (eu-central-1 ayağa kalkınca)

- DR region'da işlenen değişiklikleri primary'e WAL stream ile geri taşı.
- Trafiği yavaş yavaş primary'e döndür (canary).
- DR region'ı tekrar standby moda al.

## Senaryo 2: DB veri bozulması

Silent corruption keşfedildi (örn. checksum hatası, anormal data).

1. **Trafiği durdur**: API replica'larını 0'a çek veya healthcheck'i fail et.
2. **Forensic**: hangi tablolar, hangi tenant, ne zaman başladı? Audit log + Sentry yardımcı olur.
3. **Restore window'u belirle**: corruption başlangıcından önceki en yakın temiz snapshot.
4. **Yeni instance'a restore** ([05-backup-restore.md](05-backup-restore.md) Senaryo 2).
5. **Lost data**: snapshot ile şimdi arasındaki transaction'lar manuel olarak yeniden işlenir (audit log + email log yardımıyla).
6. **DNS swap**: yeni instance'ı primary yap.
7. **Trafiği aç**.

## Senaryo 3: Ransomware / yetkisiz erişim

1. **DERHAL trafiği kes** (DNS 0.0.0.0'a yönlendir veya Cloudflare under-attack mode).
2. **Tüm credential'ları döndür** ([07-key-rotation.md](07-key-rotation.md)):
   - JWT signing key
   - DB password
   - Iyzico API key + secret
   - SMTP password
   - Sentry DSN
   - AWS access keys (eğer compromised ise)
3. **Etki kapsamı**: data exfiltration var mı? CloudTrail + DB audit log → forensic ekip.
4. **KVKK bildirimi**: 72 saat içinde KVKK Kurulu'na bildirim zorunludur (kişisel veri ihlali).
5. **Sıfırdan rebuild**:
   - Compromise edildiği bilinen tarihten önceki son temiz backup'a restore.
   - Yeni VPC + yeni cluster + yeni RDS — eski infrastructure üzerinde çalışma.
   - Tüm secret'lar yeni vault entries'le set edilir.
6. **Müşteri iletişimi**: status page + e-posta + (Sev-1 ise) basın.
7. **Postmortem**: tüm action item'lar yüksek öncelik.

## Senaryo 4: Insan hatası

> "Production'da yanlışlıkla `DELETE FROM customers WHERE tenant_id = ...` çalıştırdım"

1. **Panik yapma**. Backup'lar var.
2. **Trafiği etkilenmiş tenant için durdur** (mümkünse).
3. **Point-in-time restore** ([05-backup-restore.md](05-backup-restore.md) Senaryo 1) — corruption öncesi.
4. **Etkilenen tabloları diff'le** + manuel SQL ile yamamla.
5. **Postmortem aksiyon**: bu komutun bir daha çalışmasını engelleyecek guard (örn. `psql --single-transaction` zorunlu, DESTRUCTIVE komutlar için review zorunlu).

## DR test (yılda 2 kez)

- Game day senaryosu seç (yukarıdaki 4'ten biri).
- Production'a değil staging/DR'a uygula.
- Saatleri ölç: detection → restore → trafiği yönlendirme → smoke pass.
- 2 saatlik RTO hedefini karşılıyor mu?
- Bulguları `docs/runbooks/dr-drills/YYYY-MM-DD.md` dosyasına yaz.

## Gereksinimler / dış bağımlılıklar

| Bağımlılık              | Tek nokta arızası mı?              | Yedeği                                                 |
| ----------------------- | ---------------------------------- | ------------------------------------------------------ |
| AWS region eu-central-1 | Evet (single region)               | DR region eu-west-1, manuel failover                   |
| RDS PostgreSQL          | Multi-AZ ile mitigated             | Snapshot + WAL replicate                               |
| Iyzico                  | Evet                               | Tek payment gateway. Stripe / PayTR fallback NICE item |
| Sentry                  | Hayır (cloud SaaS, kendi DR'ı var) | —                                                      |
| ghcr.io                 | Evet                               | DockerHub mirror düşünülebilir                         |
| TCMB FX feed            | Hayır                              | Internal cache 24 saat                                 |

## Eksikler / improvement queue

- **Cross-region active-passive**: şu an cold standby. DR-warm için NICE-023 (per-tenant DB) sonrası tekrar değerlendir.
- **Automated DR failover script**: şu an manuel. Terraform + Ansible playbook ile otomatize edilebilir.
- **Drill frequency**: yılda 2 azdır; çeyrekte 1'e çıkarılmalı.
- **Backup encryption key escrow**: WAL-G libsodium key'i ayrı bir vault'ta (cross-account) tutulmalı.
