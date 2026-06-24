# AI Helper — Tasarım & Uygulama Planı

> Durum: Faz 0 (iskelet derlendi) → Faz 1 (çekirdek) başlıyor. Bu doküman bağlayıcı tasarımdır; değişiklik istemde gerekçeyle geçer.
> İlgili kurallar: `CLAUDE.md` §0.2 (AI Helper ön-gereksinimleri), §2 (FSD), §3 (Clean Arch/CQRS), §3.4 (hata/observability), §4 (DB), §4.7 (multi-tenant), §8.0 (auth); `docs/INVARIANTS.md`; kurulum: `docs/ai-helper-setup.md`.

## 0. Amaç & kapsam

CoreAlign içine, kullanıcının **sorusunu anlayan → ilgili bilgiyi bulup analiz eden → o an için en uygun cevabı ÜRETEN** bir AI yardımcı widget'ı eklenir. Sağ-altta yüzer; **login öncesi ve sonrası** görünür. Diller: **TR + EN**.

Yaklaşım **RAG (Retrieval-Augmented Generation)**'tir: model yeniden eğitilmez (fine-tuning yok); bilgi (site kodu + dokümanlar + sektörel içerik) **soru anında bağlam olarak** verilir. "Öğretmek" = bilgi tabanını kurmak ve güncel tutmak.

Çekirdek ilke: **sağlayıcı-bağımsız.** LLM/embedding sağlayıcısı ve modeli config + DI ile değiştirilebilir; iş mantığı ve frontend buna dokunmaz. Başlangıç: kendi sunucuda **Ollama** (ücretsiz, veri içeride). İleride bulut/GPU = sadece config.

Başlangıç kapsamı: **admin SPA (`src/`)** + onun public sayfaları (landing/login/register). Müşteri/B2B portalları sonraki faz. Ekran-bağlamı: **route bazlı** (DOM okuma sonraki faz).

## 0.1 Ortam tespiti & kararlar (2026-06, bu PC)

- **PostgreSQL 18.3 yerelde çalışıyor** (`postgresql-x64-18` servisi), DB `corealign`, 417 tablo (EF migrations uygulanmış). → Mevcut DB kullanılır; bağlantı user-secrets'te.
- **pgvector PG18'de YOK; Docker da kurulu DEĞİL.** → **Vektör deposu pgvector'a bağımlı olmaz.** Embedding'ler native `real[]` kolonda saklanır; benzerlik (cosine) `IKnowledgeRetriever` arkasında hesaplanır. Yardım KB'si küçük ölçekli (yüzler–binler chunk) olduğundan metadata-filtreli aday kümesi + cosine sıralaması yeterli ve hızlıdır. pgvector/HNSW, pgvector-yetenekli (Linux/Docker) dağıtımda `IKnowledgeRetriever`'ın alternatif implementasyonu ile **config'den** devreye alınır — extension gerektirmeyen yol her Postgres'te çalışır (taşınabilirlik > pgvector).
- **Donanım:** Ryzen 7 7700 (8c/16t), 31 GB RAM, kullanılabilir ayrık GPU yok → CPU çıkarımı. Dev'de `qwen2.5:7b` makul; smoke testte gerekirse `qwen2.5:3b`.
- **Ollama dev'e winget ile** kurulur; sunucu/diğer ortamlar için `docker-compose.full.yml`'a `ollama` servisi eklenir; ön-gereksinim `CLAUDE.md §0.2` + `docs/ai-helper-setup.md`'de.

## 1. Mimari özeti

```
[KAYNAKLAR: site kodu(route/i18n/docs) + makaleler + sektörel bilgi]
      │  HAZIRLIK: parçala (chunk) → embedding
      ▼
[Vektör deposu: PostgreSQL real[] kolon + cosine (pgvector'a yükseltilebilir)]
      ▲  anlamsal arama (scope + tenant + locale filtreli)
      │
[KULLANICI: soru + o anki route]
      ▼
[POST /api/v1/ai-helper/ask  (.NET, slim controller → Application servisi)]
   1) soruyu embed et   2) ilgili parçaları çek (retriever)
   3) prompt = soru + parçalar + ekran bağlamı + guardrail
   4) LLM'e gönder → SSE ile token-token stream
      ▼
[LLM SAĞLAYICI — DEĞİŞTİRİLEBİLİR (IAiChatProvider/IAiEmbeddingProvider)]
   şimdi: Ollama (yerel) · sonra: OpenAI/Gemini/Azure → yalnız config+DI
      ▼
[Widget (sağ-alt): akıcı cevap + kaynak deep-link'leri]
```

## 2. Sağlayıcı soyutlaması & config (swappable — birinci sınıf gereksinim)

Soyutlamalar (Application katmanı, sağlayıcıdan habersiz) — **uygulandı (Faz 0):**

- `IAiChatProvider.StreamAsync(AiChatRequest, CancellationToken) : IAsyncEnumerable<AiChatDelta>`
- `IAiEmbeddingProvider.EmbedAsync(IReadOnlyList<string>, CancellationToken) : IReadOnlyList<float[]>` + `Dimensions`
- `IKnowledgeRetriever.RetrieveAsync(...) : IReadOnlyList<KnowledgeChunk>` (Faz 1)

Infrastructure implementasyonları: `OllamaChatProvider`, `OllamaEmbeddingProvider` (Faz 0, uygulandı), `PostgresKnowledgeRetriever` (Faz 1; native `real[]` + cosine). Tek config bloğu (`appsettings` `AiHelper`), secret'lar user-secrets/env ile:

```
"AiHelper": {
  "Provider": "Ollama",
  "BaseUrl": "http://localhost:11434",
  "ChatModel": "qwen2.5:7b",
  "EmbeddingModel": "bge-m3",
  "EmbeddingDimensions": 1024,
  "MaxContextChunks": 6,
  "MaxOutputTokens": 800,
  "Temperature": 0.2,
  "RequestTimeoutSeconds": 120,
  "PublicRateLimitPerMinute": 10,
  "AuthedRateLimitPerMinute": 30
}
```

Sağlayıcı/model değiştirmek = bu blok + DI'daki tek kayıt. Frontend yalnız kendi endpoint'imize konuşur → sağlayıcı değişimi frontend'e görünmez.

## 3. Backend modülü — `Application/AiHelper`

- CQRS: `AskAiHelperRequest` (soru + locale + route context + isPublic) → Application servisi orkestrasyon (retrieve → prompt → stream). Akış (streaming) doğası gereği transactional pipeline'a girmez; **slim controller** Application servisini doğrudan çağırır (CLAUDE.md §3.3).
- Endpoint sözleşmesi:
  - `POST /api/v1/ai-helper/ask` — gövde: `{ question, locale, routePath, routeContext? }`. Yanıt: **SSE** (`text/event-stream`): `event: token` (delta) + `event: sources` + `event: done` (+ `event: error`).
  - `[AllowAnonymous]` varyantı public KB ile sınırlı + sıkı rate-limit; authenticated istek tenant+rol scope'lu retrieval kullanır.
- Hata/observability: `ExceptionHandlingMiddleware` zinciri, `X-Correlation-Id`, PII/secret log'a yazılmaz; 5xx detayı sızmaz.

## 4. Veritabanı — extension-free vektör deposu

- Yeni tablolar (yeni Phase## migration, ileri-tarihli, idempotent, apply-same-turn):
  - `ai_kb_documents`: `id`, `source_type` (route|i18n|module_doc|article|sector), `source_ref`, `title`, `locale`, `scope` (public|tenant|role), `tenant_id` (nullable; public/global = boş), `content_hash`, timestamps.
  - `ai_kb_chunks`: `id`, `document_id` (FK → documents, Cascade), `ordinal`, `content` (text), `embedding real[]`, `locale`, `scope`, `tenant_id`, `token_count`, timestamps.
  - (Faz 3) `ai_helper_conversations`/`ai_helper_messages`: analitik/iyileştirme; PII'siz.
- **Vektör tipi `real[]`** (float4 dizisi) — pgvector gerektirmez, her Postgres'te çalışır. EF/Npgsql `float[]` ↔ `real[]` map'ler.
- Index: retrieval filtre kolonları (`tenant_id`, `scope`, `locale`) btree (tenant-leading). ANN index YOK (extension-free); aday kümesi metadata ile daraltılır, cosine sıralaması bellekte (bounded). Büyük ölçek/pgvector dağıtımında HNSW'li `PgVectorKnowledgeRetriever` config ile devreye alınır.
- Multi-tenant: retrieval `locale=@l AND (scope='public' OR (scope IN ('tenant','role') AND tenant_id=@tenant))`. Cross-tenant sızıntı testi zorunlu.
- Migration disiplini: Phase## adlandırma (en son Phase'den sonra), idempotent raw-SQL guard'ları, `--connection` ile uygula (design-time factory 'design' auth fail eder), snapshot reconcile + `has-pending`=No changes.

## 5. Ingestion (öğretme) pipeline — re-runnable

Kaynaklar → chunk → embed → upsert (content_hash ile değişmeyeni atla):

1. **Site kodu/route/i18n:** Node extractor (`scripts/ai-kb/extract-frontend.mjs`) `Sidebar.tsx` (yapısal nav: labelKey+href+icon) + route tablosu + `tr.json`/`en.json` → JSON; .NET ingestion bunu okur. Deep-link'ler buradan → her zaman doğru.
2. **Modül dokümanları:** `docs/modules/*`, ADR, runbook (post-login KB).
3. **Sektörel/curated içerik:** `docs/ai-kb/{tr,en}/*.md` (muhasebe/cam/makaleler) — kullanıcı yazar, scope etiketli.

Çalıştırma: tekrar-çalıştırılabilir .NET komutu; zamanlanmış yeniden-index **Hangfire** ile. Hassas içerik (token/tenant verisi/legal tam metin) KB'ye **girmez** (yalnız link).

## 6. Frontend widget — `src/widgets/AiHelper/`

- **Mount:** `App.tsx`'te `BrowserRouter` _içinde_ ama `Routes` _dışında_ (mevcut `AppToaster`/`ConflictResolutionHost`/`OnboardingTourHost` deseni) → her üç kabukta da görünür + router context (route-awareness) alır. Koşullu render (createPortal değil) → theme+i18n miras. z-index **40–50** (asla `z-[60]`+). Print rotalarında (`/invoices/:id/print`, `/payslips/:id/print`) gizli; customer-portal mobil alt-nav (z-30) için konum ofseti.
- **Yapı:** launcher butonu hafif/eager; panel + stream mantığı **ilk açılışta lazy** (bundle-gate: main ≤800KB).
- **State:** Zustand store (open/close), `useAuthStore` → public/full mod, `useLocation` → route context.
- **Stream:** `fetch` + `ReadableStream` ile SSE tüketimi; token-token render; cevap markdown + kaynak deep-link butonları.
- **Kurallar:** `AiHelper.*` i18n (5 locale parity; gerçek tr+en) — **widget arayüz metinleri**; KB içeriği i18n değil veri (tr/en). `shared/ui` + `lucide-react` + `primary-*` token + `dark:` + responsive + `logger` + `safeRequest`.

## 7. Güvenlik & multi-tenant guardrail

- Frontend asla LLM'e doğrudan gitmez; yalnız kendi endpoint'imize.
- Login öncesi: yalnız `scope='public'` KB, sıkı rate-limit, tenant verisi yok.
- Login sonrası: retrieval tenant + rol ile filtreli; `IgnoreQueryFilters` kullanılmaz.
- Prompt guardrail: "yalnız verilen bağlamdan cevapla; bağlamda yoksa bilmiyorum de ve ilgili sayfaya yönlendir; kaynak göster" → halüsinasyon kontrolü.
- API key/secret yalnız sunucuda; PII log'a yazılmaz; rate-limit → 429; timeout + output-token cap → DoS koruması.

## 8. Donanım & model seçimi

- **Dev PC:** Ryzen 7 7700 (8c/16t), 31 GB RAM, kullanılabilir GPU yok → CPU. `qwen2.5:7b` (Q4) + `bge-m3` embedding. Smoke testte hız için gerekirse `qwen2.5:3b`.
- **Hedef sunucu (henüz yok, tahmini 12 vCPU/64 GB, GPU yok):** RAM bol → 7B–14B; CPU → streaming ile algılanan gecikme düşer. Model tek satır config; sıkışırsa büyüt/küçült.

## 9. Fazlar & teslimatlar

- **Faz 0 — iskelet (TAMAM):** provider soyutlaması + Ollama adaptörleri + Options + DI; build yeşil.
- **Faz 1 — çekirdek:** entity+migration (KB tabloları) + retriever + ingestion (site/i18n/docs) + `/ask` (retrieve→generate→SSE) + widget MVP (mount, stream, TR/EN, public+full).
- **Faz 2 — bağlam & izolasyon:** route ekran-bağlamı + kaynak linkleri + public/post-login ayrımı + tenant scoping + rate-limit.
- **Faz 3 — kalite & içerik:** sektörel KB içeriği + prompt/eval kalite turu + testler (vitest + .NET) + bundle/observability + Hangfire yeniden-index.

## 10. Açık kalemler

- Ollama dev kurulumu winget ile (arka planda) + model pull (bge-m3, qwen2.5:7b).
- Embedding boyutu modele bağlı (bge-m3 = 1024) → `vector`/`EmbeddingDimensions` buna göre. Model değişirse re-index gerekir (boyut/uzay değişir).

## 11. Kurallara uyum kontrol listesi

- [ ] Yorum yok; `console.*` yok (`logger`); `@ts-ignore`/suppress yok.
- [ ] Görünür metin `t("AiHelper.*")`; tr+en (+ar/de/ru) senkron.
- [ ] FSD: widget `widgets/`'te, features import edebilir; shared→features yok.
- [ ] Slim controller; mantık Application'da; DTO↔entity sızıntısı yok.
- [ ] N+1 yok; retrieval aday kümesi bounded; pagination/limit bilinçli.
- [ ] Multi-tenant: KB tenant-aware; cross-tenant izolasyon testi.
- [ ] Migration: idempotent, Phase## sıralı, apply-same-turn, snapshot drift yok.
- [ ] Dark mode + responsive; bundle-gate (lazy panel).
- [ ] Hata: `ApiResponse`/SSE-error; 5xx sızdırmaz; correlation id.
