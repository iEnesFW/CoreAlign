# Runbook 07 — Key & Secret Rotation

> **Last verified:** 2026-06-01

CoreAlign'ın kullandığı her secret'ın rotation prosedürü. Genel kural: **dual-key window** (eski + yeni aynı anda geçerli) ile zero-downtime rotation.

## Rotation cadence

| Secret                        | Rotasyon sıklığı         | Compromise sonrası | Sorumluluk                     |
| ----------------------------- | ------------------------ | ------------------ | ------------------------------ |
| JWT signing key               | 6 ayda bir               | Anında             | Platform team                  |
| Iyzico API key + secret       | Yılda bir                | Anında             | Platform team + Iyzico support |
| DB password                   | 6 ayda bir               | Anında             | Platform team + DBA            |
| SMTP password                 | Yılda bir                | Anında             | Platform team                  |
| Sentry DSN                    | Compromise olursa        | Anında             | Platform team                  |
| OTLP exporter token           | Compromise olursa        | Anında             | Platform team                  |
| Azure Key Vault / AWS SSM key | Otomatik (cloud-managed) | —                  | —                              |

## JWT signing key rotation

CoreAlign'ın JWT'leri HMAC-SHA256 ile imzalanır. `JwtOptions:SecretKey` config key'inde tutulur.

**Bugünkü durum**: tek key. Dual-key rotation için aşağıdaki yaklaşımı uygula (henüz yapılmadıysa AUTH-012 item'ı olarak roadmap'te var).

### Manuel rotation (dual-key destekleniyorsa)

1. Yeni key üret (≥ 64 karakter, base64):
   ```bash
   openssl rand -base64 96 | tr -d '\n'
   ```
2. Vault'a yeni key'i `Jwt:NextSecretKey` olarak ekle (eski hâlâ `Jwt:SecretKey`).
3. API replica'larını rolling restart et — artık yeni key'i de tanırlar (sadece eski ile imzalanır).
4. **24 saat bekle** — bu süre içinde verilen tüm refresh token'lar eski key ile imzalanmıştır; süreleri dolar.
5. `Jwt:SecretKey` = yeni key (eski silinir).
6. API'leri tekrar rolling restart et — artık sadece yeni key kullanılır.

### Acil rotation (compromise)

1. Yeni key üret + vault'a koy.
2. `Jwt:SecretKey` = yeni key (eski iptal).
3. API restart.
4. **Tüm refresh token'lar geçersiz** — kullanıcılar yeniden login olmalı.
5. Kullanıcılara bildirim e-postası (KVKK gereği if breach).

## Iyzico API key + secret rotation

1. **Iyzico merchant portal**'a giriş.
2. "API Keys" sayfasında yeni key pair üret.
3. **ÖNEMLİ**: Iyzico eski key pair'i hemen iptal etmez. Genelde 24 saat overlap window var.
4. Vault'ta yeni key + secret'ları set et:
   ```bash
   aws ssm put-parameter --name /corealign/prod/iyzico-api-key --type SecureString --value "<new>" --overwrite
   aws ssm put-parameter --name /corealign/prod/iyzico-secret-key --type SecureString --value "<new>" --overwrite
   ```
5. API rolling restart.
6. Test: küçük tutarlı bir test payment çek.
7. **Webhook signature**: yeni secret ile imzalanan webhook'lar gelmeye başlar. Eski secret de overlap süresince geçerli. CoreAlign'ın signature verification kodu tek secret kullanır — overlap süresince kısa downtime risk var.
   - **Mitigation**: `Iyzico:SecretKey` yanında `Iyzico:PreviousSecretKey` config slot'u tut, ikisini de denesin (bugün henüz desteklenmiyor → backlog item).
8. Iyzico portal'da eski key'i manuel olarak iptal et.

## DB password rotation

PostgreSQL `corealign` user'ı için.

1. Yeni şifre üret:
   ```bash
   openssl rand -base64 32 | tr -d '\n='
   ```
2. **Önce app'in yeni şifreyi tanıdığından emin ol**: vault'ta yeni şifreyi `ConnectionStrings:DefaultConnection_New` olarak set et. (Bunu kullanan kodu hazırlamadıysan, bu adım skip; doğrudan rotation yap + downtime kabul et.)
3. PostgreSQL'de password değiştir:
   ```sql
   ALTER USER corealign WITH PASSWORD '<new-password>';
   ```
4. Vault'ta `ConnectionStrings:DefaultConnection` güncelle.
5. K8s'te secret update:
   ```bash
   kubectl create secret generic corealign-db \
     --from-literal=connection-string="Host=...;Username=corealign;Password=<new>;..." \
     --dry-run=client -o yaml | kubectl apply -f -
   ```
6. API rolling restart — eski connection'lar pool'dan dropla:
   ```bash
   kubectl rollout restart deployment/corealign-api -n corealign
   ```
7. Verify: `/health/ready` yeşil mi?

## SMTP password rotation

1. Email provider (SendGrid / Mailgun / kendi SMTP) portal'ından yeni şifre üret.
2. Vault'ta `Email:Smtp:Password` güncelle.
3. API rolling restart.
4. Test: tetikle bir password reset → e-posta geliyor mu?

## Sentry DSN rotation

Sentry DSN compromise olduysa (örn. public repo'ya sızdı):

1. Sentry organization → Project Settings → Client Keys → revoke + create new.
2. Vault'ta `Sentry:Dsn` güncelle (backend ve 3 SPA için ayrı).
3. SPA için: `VITE_SENTRY_DSN` build-time env, yeni release çıkarman gerek.
4. Eski DSN'den gelen event'ler Sentry tarafında otomatik drop.

## Vault provider (Azure KV / AWS SSM) key rotation

Bu cloud-managed. Detay sağlayıcıya özgü:

- **AWS SSM**: KMS key rotation otomatik (yıllık). Parametre değerleri etkilenmez.
- **Azure Key Vault**: Soft-delete + purge protection açık olmalı.

CoreAlign tarafında vault provider'a erişim için kullanılan IAM role / service principal:

- En az 6 ayda bir credential rotate edilmeli.
- AWS IAM access key rotation: `aws iam create-access-key` → secret update → `aws iam delete-access-key <old>`.

## Audit

Her rotation sonrası:

- `LoginAuditLog` veya `ActivityLog`'a admin action olarak yazılır (mümkünse).
- Rotation kaydı: `docs/runbooks/rotation-history.md` (NEW — bu dosya henüz yok ama oluşturulabilir).
- Bir sonraki rotation tarihi takvime girilir (PagerDuty schedule reminder).

## Common failure modes

| Sorun                                                       | Çözüm                                                                                                                  |
| ----------------------------------------------------------- | ---------------------------------------------------------------------------------------------------------------------- |
| JWT rotation sonrası tüm kullanıcılar 401                   | Dual-key window olmadan rotate edildi. Kullanıcılar yeniden login olmalı. Mitigation: AUTH-012 (kid claim + dual key). |
| Iyzico rotation sırasında webhook signature fail            | Overlap window yetersiz. Iyzico support'a yaz.                                                                         |
| DB rotation sonrası "FATAL: password authentication failed" | App pod'lar eski password'ü cache'lemiş. Force restart.                                                                |
| SMTP password rotation sonrası emails not sent              | EmailService outbox queue'sunda backlog birikmiş; restart sonrası flush olur.                                          |

## Compliance

KVKK + GDPR perspektifinden:

- Encryption-at-rest key rotation yılda 1 zorunlu.
- Kullanıcı şifre rotation zorlanmamalı (NIST 800-63B'ye aykırı) ama tenant admin kendi politikasını set edebilmeli (AUTH-006 item).
