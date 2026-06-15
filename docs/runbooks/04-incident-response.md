# Runbook 04 — Incident Response

> **Last verified:** 2026-06-01

## Severity matrisi

| Sev       | Tanım                                    | Örnek                                                  | Response time  | İletişim                                           |
| --------- | ---------------------------------------- | ------------------------------------------------------ | -------------- | -------------------------------------------------- |
| **Sev-1** | Toplam erişim yok / veri kaybı riski     | Tüm tenant login fail, DB down, ödeme tamamen kırık    | < 15 dk        | Slack `#incidents` + telefon + status page kırmızı |
| **Sev-2** | Bir özellik kullanılamaz, alternatif yok | e-Fatura submit fail, dealer portal 500'lerle dolu     | < 1 saat       | Slack `#incidents` + status page sarı              |
| **Sev-3** | Düşük etkili, alternatif var             | Tek bir tenant'ta yavaşlık, isteğe bağlı özellik bug'ı | < 8 saat       | Slack `#engineering`                               |
| **Sev-4** | Kullanıcı görmez                         | Log noise, izleme uyarısı                              | Sonraki sprint | Issue tracker                                      |

## On-call rotation

| Hafta           | Birincil   | İkincil    |
| --------------- | ---------- | ---------- |
| Pazartesi-Pazar | Mühendis A | Mühendis B |
| Sonraki hafta   | Mühendis B | Mühendis C |
| ...             | ...        | ...        |

(Gerçek rotasyon `https://pagerduty.com/schedules/PXXXX` veya kurumsal scheduling tool'unda tutulur.)

**Eskalasyon zinciri**: Birincil 15 dk içinde response vermezse İkincil çağrılır. İkincil 15 dk içinde response vermezse Engineering Manager (telefon).

## İlk müdahale (TRIAGE — 5 dk)

1. **Alarm aldın**. Slack'e şu mesajı at:
   > 🚨 Investigating: <kısa belirti>. ETA on update: 15 dk.
2. **Etki kapsamı**:
   - Sentry: hangi error class, kaç event/dakika?
   - Grafana: HTTP 5xx oranı, hangi endpoint?
   - Tüm tenant mı, bir tenant mı?
   - Tüm replica mı, bir replica mı?
3. **Hızlı kontrol**:
   ```bash
   curl -fsS https://api.corealign.io/health/ready | jq
   # Healthy mi?
   kubectl get pods -n corealign
   # Restart loop var mı?
   ```
4. **Severity ata**: matristen birini seç. Yanlış da olsa ata — sonra ayarlanır.

## Stabilizasyon (FIX — 30 dk hedef)

Önce **etkiyi durdur**, sonra root cause'a in.

### Yangın söndürme stratejileri (öncelik sırasına göre)

1. **Rollback** ([02-rollback.md](02-rollback.md)) — yeni deploy sonrası başlayan sorun için varsayılan.
2. **Trafik azalt**: Rate limit'i agresifleştir, suspicious tenant'ı isolate et.
3. **Restart**: `kubectl rollout restart deployment/corealign-api -n corealign` — bazen yeter (memory leak, stuck connection pool).
4. **Scale up**: Replica sayısını artır, geçici yük taşı.
5. **Feature flag off**: Bozuk feature'ı tenant config'ten kapat.
6. **DB connection pool**: `dotnet trace collect` ile pool exhaustion'ı doğrula, gerekirse pool size artır.

## Communication

| Süre      | Aksiyon                                             |
| --------- | --------------------------------------------------- |
| T+0       | İlk Slack mesajı (investigating)                    |
| T+15      | Durum güncellemesi (root cause hipotezi, ETA)       |
| T+30      | Güncelleme (ya fix uygulandı, ya hâlâ devam ediyor) |
| Her 30 dk | Sev-1/2 devam ediyorsa güncelleme                   |
| Resolved  | Final mesaj + postmortem ETA                        |

Müşteri-etkili Sev-1 ise **status page** (StatusPage.io / Better Uptime / kendi sayfa) anında güncellenir.

## Postmortem template

Sev-1/2 sonrası 5 iş günü içinde yazılır. Blameless format.

```markdown
# Postmortem: <kısa başlık>

**Date**: 2026-MM-DD
**Severity**: Sev-1
**Duration**: HH:MM (T+0 → T+resolved)
**Author**: <isim>
**Reviewers**: <isimler>

## Özet

Bir paragraf: ne oldu, kim etkilendi, nasıl çözüldü.

## Etki

- Etkilenen tenant sayısı:
- Etkilenen kullanıcı sayısı:
- Para kaybı (varsa):
- Mevcut SLO ihlali:

## Zaman çizelgesi (UTC)

| T+    | Olay                                  |
| ----- | ------------------------------------- |
| 00:00 | <ilk belirti>                         |
| 00:05 | İlk Sentry alarm                      |
| 00:12 | On-call mühendis triage başladı       |
| 00:25 | Rollback başladı                      |
| 00:30 | Rollback tamamlandı, metrikler normal |
| 00:35 | Status page güncellendi (resolved)    |

## Root cause

Detaylı teknik açıklama. 5 Whys uygula.

## Ne işe yaradı?

- Hızlı detection (Sentry alarm 2 dk içinde geldi)
- Runbook'lar yardımcı oldu
- Rollback prosedürü test edilmişti

## Ne işe yaramadı?

- Migration test edilmemişti (staging'de aynı sorun yok)
- Alarm noise nedeniyle ilk Sentry alarm gözden kaçtı
- vb.

## Action items

| #   | Eylem                                           | Sahip  | Hedef tarih | Önlemeyi sağladığı sorun                                 |
| --- | ----------------------------------------------- | ------ | ----------- | -------------------------------------------------------- |
| 1   | Staging'e production-benzeri data yükle         | <isim> | YYYY-MM-DD  | Pre-deploy migration smoke test eksikliği                |
| 2   | Sentry alarm gruplaması iyileştir               | <isim> | YYYY-MM-DD  | Alarm yorgunluğu                                         |
| 3   | Migration için canary deploy stratejisi tasarla | <isim> | YYYY-MM-DD  | Multi-tenant şema değişikliği büyük blast radius'a sahip |

## Lessons learned

- Genelleştirilebilir öğrenmeler.
- Kültür / proses değişiklikleri.
```

## Drilling / chaos engineering

Çeyrekte bir:

- **Game day**: Random pod kill, network latency injection, DB failover simulation.
- **Rollback drill**: Önceki release tag'e gerçekten dön, sonra geri.
- **Backup restore drill**: [05-backup-restore.md](05-backup-restore.md).

## Sık karşılaşılan senaryolar

| Belirti                                       | İlk yapılacak                                                                 |
| --------------------------------------------- | ----------------------------------------------------------------------------- |
| Tüm API 500 + Sentry'de DbException dalgası   | DB connection / pool kontrolü                                                 |
| Login 401 dalgası, dün düne kadar çalışıyordu | JWT key rotation problemli olabilir, [07-key-rotation.md](07-key-rotation.md) |
| Sadece bir tenant yavaş                       | Tenant-spesifik N+1, log query duration; geçici cache TTL düşür               |
| Outbox backlog şişiyor                        | OutboxProcessor restart, error log incele                                     |
| e-Fatura submit fail dalgası                  | Gateway durumu kontrolü (Stub → Veriban geçişi yapıldı mı?)                   |
| Sentry'de aniden 1000+ event                  | Filtre / rate limit Sentry tarafında, kod düzeltmesi öncesi                   |
