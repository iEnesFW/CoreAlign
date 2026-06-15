# CoreAlign Runbooks

> **Last verified:** 2026-06-01

Operasyonel runbook'lar burada toplanır. Her runbook, belirli bir konuda **adım adım, kopyalanıp çalıştırılabilir** rehber içerir. Bir on-call mühendisinin gece yarısı uykudan kalktığında, runbook'a göre tek başına ilerleyebilmesi hedeflenir.

## İçindekiler

| #   | Runbook                                         | Konu                                                                    |
| --- | ----------------------------------------------- | ----------------------------------------------------------------------- |
| 01  | [deployment.md](01-deployment.md)               | İmaj build + tag + push, env config matrisi, smoke test                 |
| 02  | [rollback.md](02-rollback.md)                   | İmaj rollback + EF migration rollback stratejisi                        |
| 03  | [db-migration.md](03-db-migration.md)           | Migration uygulama yolları (auto, manuel, bundle) ve multi-replica race |
| 04  | [incident-response.md](04-incident-response.md) | Severity matrisi, on-call rotasyonu, postmortem şablonu                 |
| 05  | [backup-restore.md](05-backup-restore.md)       | `pg_dump` cron, S3 upload, monthly restore drill                        |
| 06  | [disaster-recovery.md](06-disaster-recovery.md) | RPO/RTO hedefleri, region failover, full rebuild                        |
| 07  | [key-rotation.md](07-key-rotation.md)           | JWT, Iyzico, DB password, SMTP, Sentry DSN rotasyonu                    |

## Naming convention

- Dosya adı: `NN-konu.md` (NN = iki haneli sıra numarası).
- Her dosya en üstte `> **Last verified:** YYYY-MM-DD` satırı içerir.
- Komutlar **çalıştırılabilir** olmalı — ortama özel değer varsa `<PLACEHOLDER>` ile işaretlenir ve dosyanın başında açıklanır.

## Sürdürülebilirlik

- Her sprint sonunda runbook'lar **gözden geçirilir**. Eskimişse `Last verified` tarihi güncellenir veya içerik düzeltilir.
- Yeni bir operasyonel prosedür (yeni dış servis entegrasyonu, yeni süreç) eklendiğinde **aynı sprint içinde** ilgili runbook güncellenir.
- Postmortem'lerden çıkan aksiyonlar, ilgili runbook'lara işlenir.

## On-call için hızlı erişim

| Senaryo                              | Git                                                |
| ------------------------------------ | -------------------------------------------------- |
| Üretime deploy ediyorum              | [01-deployment.md](01-deployment.md)               |
| Deploy bozuldu, geri almak istiyorum | [02-rollback.md](02-rollback.md)                   |
| Veritabanı migration nasıl uygulanır | [03-db-migration.md](03-db-migration.md)           |
| Üretimde Sev-1 alarm aldım           | [04-incident-response.md](04-incident-response.md) |
| Yedekten geri yükleme yapacağım      | [05-backup-restore.md](05-backup-restore.md)       |
| Bölge bazlı felaket yaşandı          | [06-disaster-recovery.md](06-disaster-recovery.md) |
| Bir secret/anahtar değiştireceğim    | [07-key-rotation.md](07-key-rotation.md)           |
