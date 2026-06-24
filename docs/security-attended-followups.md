# Security — Attended / Ops-Gated Follow-Ups

Bu doküman, 2026-06-22/23 güvenlik sprintinde **bilinçli olarak ertelenen** kalemleri ve her birinin **turnkey uygulama prosedürünü** içerir. Bunlar ya bir **ops/deploy kararı** (DB rol/parola/bağlantı değişimi, persisted-key onayı) gerektirir ya da en **yüksek-riskli yüzeyi** (admin primary login, global apiClient) tek seferde test ederek değiştirmeyi gerektirir. Kod tarafı hazır olan yerlerde mekanizma mevcut; aşağıdakiler son adımlardır.

Tamamlanan güvenlik işi: `.claude/memory/project_corealign_security_sprint.md` + `docs/INVARIANTS.md` (alan-bazlı şifreleme + bağımlılık-CVE + model-cache-key invariant'ları).

---

## 1. PII / TOTP-secret eager backfill (C-2 / H-3 mevcut verinin şifrelenmesi)

**Durum:** Mekanizma CANLI. `ResilientEncryptedStringConverter` yeni yazımları şifreliyor; mevcut plaintext satırlar okuma sırasında passthrough ile çalışmaya devam ediyor ve **sonraki yazımda** otomatik şifreleniyor. Şifreli kolonlar (`users.two_factor_secret_key`, `employees.iban/sgk_registration_no`, `payslips.national_id`, `vendor_bank_accounts.iban/account_number/swift`) Phase106 ile `text`'e genişletildi.

**Geriye kalan (ops):** Mevcut (eski) satırların **eager** şifrelenmesi.

**Ön-koşul (ZORUNLU):** Hedef ortamda DataProtection anahtarları KALICI olmalı — `DataProtection:KeyDirectory` (dosya sistemi/paylaşımlı volume) ve/veya `DataProtection:ProtectionCertificateThumbprint` set + yedekli. Aksi halde ephemeral key restart'ta döner ve şifrelenmiş veri kalıcı olarak okunamaz hale gelir. (Dev için `appsettings.Development.json`'da `App_Data/dataprotection-keys` set edildi + `.gitignore`'landı.)

**Prosedür:**

1. Ortamda persisted key'i doğrula (yukarıdaki config + yedek).
2. Backfill, naive "load + SaveChanges" ile ÇALIŞMAZ: CLR değeri değişmediği için EF write atlar (converter tetiklenmez). Doğru yol: her satır için ilgili property'yi `context.Entry(entity).Property(x => x.Field).IsModified = true` ile işaretle → SaveChanges → ConvertTo şifreler. Cross-tenant tarama için `IgnoreQueryFilters()` (User tenant-filtresiz; Employee/Payslip/VendorBankAccount tenant-scoped → PlatformAdmin/sistem context). 10k'lık batch'lerle.
3. İdempotent: tekrar çalıştırma çift-şifreleme yapmaz (okuma decrypt → plaintext → yazma tek-şifreleme). Önce küçük bir tenant'ta DryRun/sayım ile doğrula.

---

## 2. Admin SPA 2FA UI (step-up MFA Slice 2 — admin yüzeyi)

**Durum:** Backend TAM (enroll/verify/disable/backup-codes/challenge/step-up endpoint'leri canlı + IDOR-safe + test edildi; `RequireRecentMfaAttribute` + `mfa_verified_at` claim mevcut). **customer-portal** login-challenge eklendi (enrolled müşteri giriş yapabilir). **Admin SPA'da 2FA UI YOK** (enroll UI yok → aktif footgun yok; yalnız bilinçli raw-API enrollment riski var, o da API challenge endpoint'i ile tamamlanabilir).

**Neden ertelendi:** Admin login akışı (zod-doğrulamalı response + react-hook-form + 5-dil i18n) ve global `apiClient` interceptor'ı uygulamanın **en kritik/yüksek-riskli** yüzeyleri. Aceleci gözetimsiz değişiklik tüm admin'leri kilitleyebilir ("don't break functionality"). Odaklı, tam-test edilmiş bir frontend dilimi olarak yapılmalı.

**Turnkey adımlar (admin `src/`):**

1. `features/auth/model/auth.types.ts` + `authResponseSchemas.ts`: `AuthResponse`'a `requiresTwoFactor?: boolean` + `twoFactorChallengeToken?: string`; `user` nullable-tolerant (challenge öncesi null gelir).
2. `features/auth/api/authApi.ts`: `completeTwoFactorChallenge(challengeToken, code)` + enroll/verify/disable/regenerate/stepUp sarmalları (generated `EMCM.Client.ts`'te metotlar zaten var).
3. `features/auth/hooks/useAuth.ts` `useLogin`: `response.data.requiresTwoFactor` ise `setAuth` ÇAĞIRMA (null user'a NPE'yi önler) → challenge state'i yüzeye çıkar; `useCompleteTwoFactorChallenge` ekle.
4. `LoginForm.tsx`: `requiresTwoFactor` → 6-haneli kod ekranı → challenge → başarıda normal login (portal `LoginForm.tsx` referans deseni).
5. **Güvenlik ayarları sayfası**: enroll (QR — `otpauth://` URI'yi göster veya küçük bir QR; yeni dep eklersen bundle bütçesine dikkat) + verify + backup codes + disable (parola). UserId body'de DEĞİL, JWT'den (backend zaten böyle).
6. `shared/api/apiClient.ts`: HTTP **428** + body `code="MFA_REQUIRED"` → step-up TOTP modalı → `POST /auth/2fa/step-up` → elevated access token'ı authStore'a koy → orijinal isteği BİR kez retry. (Bu, `[RequireRecentMfa]`'lı 2 Privacy endpoint'ini — KVKK erasure/DSR — UI'dan kullanılabilir yapar.)
7. i18n tr+en (zorunlu) + ar/de/ru fallback.
8. Slice 3 (enforcement): hazır olunca `[RequireRecentMfa]`'yı hassas mutasyonlara ekle (payment void/apply, journal post, fiscal close, role/active değişimi, vendor bank account) — `[Authorize(Roles="TenantAdmin")]` ile BİRLİKTE, yerine değil. Tenant feature-flag arkasında aç (tenant admin'leri enroll OLANA kadar açma).
9. Test: LoginForm challenge (RTL), apiClient 428 interceptor, e2e enroll→logout→challenge→step-up.

---

## 3. RLS (Row-Level Security) enforcement flip — H-8

**Durum:** RLS **TAM KURULU ama DORMANT**. Policy'ler ~150 tenant-FK tablosunda mevcut (Phase85 + Phase94 re-run; partition tabloları Phase86/90/95). GUC: `app.tenant_id` (izolasyon) + `app.rls_bypass` (policy'lerde `OR current_setting('app.rls_bypass', true)='1'` zaten gömülü). `TenantRlsConnectionInterceptor` `app.tenant_id`'yi her connection-open'da set ediyor ama yalnız `Database:EnableRls=true` iken EKLENİYOR (hiçbir config'de set değil). Uygulama Postgres **superuser `postgres`** ile bağlanıyor → superuser RLS'i `FORCE` ile bile BYPASS eder. Yani bugün RLS hiçbir koruma sağlamıyor; tek aktif izolasyon EF global query filter'ları.

**Neden ertelendi:** Flip bir **ops/deploy** kararıdır (yeni DB rol parolası + runtime connection identity değişimi + staging doğrulaması). Kod groundwork'ü (sistem-scope bypass) flip OLMADAN hiçbir aktif güvenlik sağlamaz ama 8+ background job'a regresyon yüzeyi ekler → spekülatif uygulamadan kaçınıldı.

**Turnkey adımlar:**

1. **Sistem-scope bypass kodu:** `ITenantContext`/yeni `ISystemScope`'a ambient bayrak ekle; `TenantRlsConnectionInterceptor` bayrak set'liyken AYRICA `set_config('app.rls_bypass','1',false)` çalıştırsın (policy zaten kabul ediyor). Tüm cross-tenant sistem geçişlerini bu scope'a sar: OutboxProcessor drain, `WarrantyExpiryNotifier`, retention/cleanup job'ları, `MrpWeeklyJob`, `ScheduledAuditExport`, report scheduler'lar, PlatformAdmin all-tenant okumaları, `PartitionMaintenanceHostedService`. (`.IgnoreQueryFilters()` yalnız EF filtresini kaldırır, DB RLS'i KALDIRMAZ → bypass GUC şart, yoksa 0 satır.)
2. **Runtime identity:** `corealign_app` (Phase85'te parolasız LOGIN, non-superuser) rolüne parola ver; runtime `ConnectionStrings:DefaultConnection`'ı buna çevir.
3. **Migration'ı AYRI privileged bağlantıyla çalıştır:** startup `MigrateAsync` owner/`postgres` ile (DDL non-owner'da çalışmaz) — ayrı migration-connection veya deploy-time out-of-process.
4. **Partition rollover:** `corealign_ensure_future_partitions` (ve partition-rebuild fonksiyonları) `SECURITY DEFINER SET search_path=public, pg_temp` yap (non-owner `CREATE TABLE ... PARTITION OF` çalışsın) — yeni PhaseNN migration.
5. `Database:EnableRls=true`.
6. **Test (Postgres-only, non-owner rol):** tenant A connection yalnız A satırlarını görür; `app.tenant_id` boş → 0 satır; cross-tenant INSERT/UPDATE `WITH CHECK` ile reddedilir; sistem-scope (rls_bypass) tüm tenant'ları görür; background job sistem-scope ile cross-tenant satırları döndürür (0-satır tuzağı regresyon guard'ı). SQLite bunu KANITLAYAMAZ (RLS yok) → guarded Npgsql integration test.

`docs/DB_RECONCILE_FOLLOWUP.md` + `docs/SCALE_READINESS_ROADMAP` (O2) ile aynı yönde.
