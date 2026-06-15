# CLAUDE.md — Eklenecek Kalıcı Bölümler

> Bu içerik mevcut `CLAUDE.md`'nin **sonuna** eklenir. Stil mevcut dosyayla aynı: kısa, emir kipi, non-negotiable. Numaralandırma mevcut son bölümü takip eder (11'den başlar). Bu bölümler her task'ta bağlayıcıdır.

---

## 11. Öngörülü Davranış (Foresight) — Anahtar İlke

Her tablo, sorgu, endpoint ve sayfa **"1 yıl sonra burada 10M satır / 100× trafik olursa ne olur?"** testinden geçer. Tasarımı bugünün veri hacmi değil, makul en kötü gelecek hacmi belirler.

1. **Sınırsız sorgu yok.** Liste dönen her query'de zorunlu pagination. Büyüyen/büyük tablolarda `OFFSET` yerine **keyset (seek) pagination** (`WHERE id > @cursor ORDER BY id LIMIT n`). `ToList()` / `Take(int.MaxValue)` ile sınırsız çekim yasak (madde 4.4 pekiştirir).
2. **Filtre/sıralama server-side ve indexli.** "Hepsini çek, frontend'de filtrele" yasak. Filtrelenen/sıralanan her kolonun index'i olur.
3. **N+1 öngörüsü.** Koleksiyon erişiminde baştan projection ya da `Include` + `AsSplitQuery`. "Sonra optimize ederim" yok — ilk yazımda doğru.
4. **Frontend ölçek.** Uzun listelerde virtualization (ör. `@tanstack/react-virtual`). Sonsuz büyüyen client state yok; sayfalı/sanal.
5. **Hot-path öngörüsü.** Çok yazılan/okunan tablolar (audit log, stock ledger, activity) için baştan doğru index; gerekiyorsa partition/archival kararını `docs/INVARIANTS.md`'ye not düş.
6. **Öngörü ≠ over-engineering.** YAGNI ile denge kuralı: _ucuzsa ve geri dönüşü pahalıysa_ baştan yap (index, pagination, doğru tip, tenant filter); _pahalıysa ve belirsizse_ genişleme dikişi (seam) bırak ve nedenini not et, ama spekülatif soyutlama üretme.

---

## 12. DB Yaşam Döngüsü ve Evrim — Tablolar Çürümez

Bir tablo bir istek için doğar; aylar sonra başka yöne evrilebilir. **Evrilen tablo ilk hâlinde bırakılmaz.**

1. **Doğuştan doğru.** Yeni tablo: tüm FK'lere index, sık `WHERE/JOIN/ORDER BY` kolonlarına index, doğru unique constraint'ler, doğru tipler (`decimal(18,4)` para ya da `bigint` minor-unit, `timestamptz`, GUID `id`), bilinçli nullability. Madde 4 standartları **aynı migration'da** karşılanır.
2. **Her şema değişiminde index revizyonu.** Bir tabloyu değiştiren task, o tablonun mevcut index/constraint'lerini **gözden geçirir**; sorgu paterni değiştiyse eski index'i düzeltir veya yenisini ekler. Kural: **"dokunduğun tabloyu bulduğundan daha iyi bırak."**
3. **Index disiplini.** Composite index kolon sırası selectivity'ye göre. Çok okunan dar sorgular için covering index. Soft-delete varsa kısmi index (`WHERE deleted_at_utc IS NULL`). JSONB filtreleniyorsa GIN. Gereksiz/çoğullanan index temizlenir (yazma maliyeti).
4. **Query plan farkındalığı.** >10k satır beklenen yeni/değişen sorguda `EXPLAIN ANALYZE` ile plan kontrol edilir; beklenmeyen seq scan varsa index eklenir. Sonuç PR/özet notuna yazılır.
5. **Zero-downtime migration.** Büyük/canlı tabloda yıkıcı değişiklik adımlanır: önce nullable kolon ekle → backfill (ayrı veri komutu) → constraint/NOT NULL. Tek migration içinde uzun kilit tutan işlem yok. Yıkıcı/geri alınamaz migration → madde 7 onayı.
6. **Standart tutarlılığı.** Her tabloda `created_at_utc`, `updated_at_utc`; soft-delete kullanılıyorsa `deleted_at_utc`. `snake_case` + çoğul tablo (madde 4.1).

---

## 13. İlk Seferde Doğru (First-Pass Quality) — Tekrar Döngüsünü Önle

Amaç: kodu baştan mimariye/performansa/kurala uygun yazmak; kullanıcıyı tekrar tekrar test/düzeltme döngüsüne sokmamak.

1. **Önce tara, sonra yaz.** Yeni iş öncesi benzer mevcut feature okunur ve **aynı pattern** uygulanır. Yeni pattern icat etme; tutarlılık > yaratıcılık.
2. **Mimariyi yazarken uygula.** Dependency yönü (FSD/Clean), layer sorumluluğu, DTO/`IQueryable` sızıntısı yok, slim controller — sonradan düzeltilecek borç olarak değil, ilk yazımda doğru.
3. **Performansı baştan koy.** Doğru async/await (sync-over-async yok), read'lerde `AsNoTracking`, projection, N+1 yok, gereksiz materialization yok.
4. **Hata yönetimini baştan koy.** `safeRequest`/middleware sarmalı, exception yutma yok, iş mantığında try/catch yok (madde 2.4 / 3.4).
5. **Bitmeden kendin doğrula.** "Bitti" demeden önce **madde 8 checklist'ini sen koştur** ve gate'leri lokalde çalıştır: `dotnet build` (0 warning), arch testler, `npm run lint` / `typecheck` / `test`. **Yeşil görmeden teslim etme.**
6. **Testi sen çalıştır.** Yazdığın test ve gate'leri sen koştur, yeşil sonucu kullanıcıya sun. Kullanıcı senin yerine manuel test eden kişi değildir — onu döngüye sokma.

---

## 14. Genişletilmiş Test Direktifleri

Madde 8.2 / 8.2.1 geçerli; ek olarak:

1. **Her yeni endpoint → integration test** (gerçek DB): happy path + auth reddi + **tenant izolasyonu** (başka tenant'ın verisi sızmıyor) doğrulanır.
2. **N+1 regression guard.** Kritik liste/detay query'lerinde SQL round-trip sayan test (DbCommand interceptor ile). Round-trip sayısı beklenenden artarsa test kırılır.
3. **Mutation skoru düşmez.** Yeni Application kodu Stryker eşiğinin altına çekemez.
4. **State machine** her geçiş + her reddedilen geçiş için ayrı test (sipariş/fatura/ödeme FSM'leri).
5. **Para/stok mutasyonu** testi: yuvarlama, negatif stok reddi, idempotency (madde 16), eşzamanlı güncelleme çakışması.
6. **Frontend:** yeni zod schema/util → happy + ≥2 failure; kritik etkileşimli component → React Testing Library.

---

## 15. Invariants Log — Kendi Kendini Güncelleyen Kurallar

Prompt'ta unutulsa bile atlanmaması gerekenler kalıcı hafızada tutulur: **`docs/INVARIANTS.md`**. Bu, projenin "bir daha aynı hatayı yapma" defteridir.

1. **Oku (task başında).** Her göreve başlarken `docs/INVARIANTS.md` okunur; ilgili invariant'lar uygulanır.
2. **Ekle (task sonunda).** Tekrar eden bir hata, bir "şunu unutma" durumu veya bir tuzak ortaya çıktığında agent bunu **kendi** ekler. Tek satır: neden + kural.
3. **Format:** `- [ALAN] Durum → Kural`.
   - `- [DB] customers.email aramada full-scan yapıyordu → normalized_email + ix index zorunlu`
   - `- [API] liste endpoint'i pagination'sız döndü → tüm liste endpoint'leri zorunlu sayfalı`
   - `- [TENANT] yeni repo sorgusu global filter'ı bypass etti → her sorgu tenant filter ile doğrulanır`
4. **Kapanış kontrolü.** Görev sonunda agent açıkça sorar: **"Yeni bir invariant öğrendim mi?"** Evetse ekler. Tekrarlayan ihlal kök-neden olarak buraya yazılır — böylece bir daha atlanmaz.
5. **Alan etiketleri:** `[ARCH] [DB] [PERF] [SECURITY] [API] [FRONTEND] [TEST] [I18N] [TENANT] [BUILD]`.
6. **Çatışma çözümü.** Bir invariant CLAUDE.md ile çelişirse CLAUDE.md kazanır; invariant güncellenir veya silinir.

---

## 16. ERP Doğruluk Kuralları (Öngörünün ERP'ye Uygulanışı)

Para ve stok hatası kabul edilemez; eşzamanlılık ve retry senaryoları **baştan** düşünülür.

1. **Optimistic concurrency.** Yarışabilen kayıtlarda (stok, hesap bakiyesi, fatura/ödeme durumu) `xmin`/rowversion concurrency token kullanılır. Çakışmada **409**, sessiz overwrite yok.
2. **Idempotency.** Para/stok hareketi yaratan komutlar (ödeme uygula, sipariş onayla, stok düş) idempotency key ile çalışır; retry çift kayıt üretmez.
3. **Transaction sınırı.** Çok-tablolu tutarlılık (sipariş + stock ledger + fatura) tek transaction / UnitOfWork içinde; yarım kalmış durum bırakılmaz.
4. **Cache disiplini.** Cache key **tenant-scoped**; cross-tenant okuma imkânsız. Yazma sonrası ilgili key invalidate edilir; stale para/stok gösterilmez. TTL bilinçli seçilir.
5. **Audit.** Para/stok/yetki değişiklikleri audit'lenir (kim, ne zaman, eski → yeni).
6. **Decimal & TZ.** Para her zaman `decimal(18,4)` ya da minor-unit `bigint`; `float`/`double` yasak. Zaman `timestamptz`, UTC saklanır, yalnız sınırda dönüştürülür.
