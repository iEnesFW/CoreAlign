# AI Helper — Kurulum & Ön-Gereksinimler

> AI Helper RAG yardımcısının çalışması için yerel/sunucu servis kurulumu. Mimari: `docs/modules/ai-helper.md`. Kural özeti: `CLAUDE.md §0.2`.

## Bileşenler

- **PostgreSQL** (mevcut `corealign` DB) — bilgi tabanı tabloları (`ai_kb_documents`, `ai_kb_chunks`) burada. **pgvector GEREKMEZ** (embedding'ler `real[]` kolonda, cosine app tarafında).
- **Ollama** — LLM + embedding runtime (yerel, ücretsiz). Varsayılan modeller: `qwen2.5:7b` (chat) + `bge-m3` (embedding).

## Geliştirme kurulumu (Windows)

1. Ollama kur: `winget install --id Ollama.Ollama -e`
2. Modelleri indir: `ollama pull qwen2.5:7b` ve `ollama pull bge-m3`
3. Doğrula: `ollama list`; servis `http://localhost:11434` ayakta olmalı (`ollama serve` arka planda otomatik başlar).
4. Config: `appsettings.json` `AiHelper` bloğu varsayılan `BaseUrl=http://localhost:11434` ile gelir; ekstra gizli değer gerekmez.
5. DB: ek adım yok — migration ana `corealign` DB'sine uygulanır (`real[]` kolon, extension gerektirmez).

## Sunucu / Docker kurulumu

1. `docker-compose.full.yml` içinde `ollama` servisi tanımlıdır (`image: ollama/ollama`).
2. Ayağa kaldır: `docker compose -f docker-compose.full.yml up -d`
3. Modelleri çek (ilk sefer): `docker exec corealign-ollama ollama pull qwen2.5:7b` ve `docker exec corealign-ollama ollama pull bge-m3`
4. `api` servisi `AiHelper__BaseUrl=http://ollama:11434` ile bağlanır (compose'da set edilir).

## Model / sağlayıcı değiştirme (swappable)

- **Model:** `appsettings` `AiHelper:ChatModel` / `EmbeddingModel` (örn. hız için `qwen2.5:3b`, kalite için `qwen2.5:14b`). Embedding modeli değişirse boyut değişebilir → `EmbeddingDimensions` güncellenir ve **KB yeniden index'lenir** (vektör uzayı değişir).
- **Sağlayıcı (Ollama → bulut/GPU):** `AiHelper:Provider` + Infrastructure DI'daki `IAiChatProvider`/`IAiEmbeddingProvider` kaydı. Yeni sağlayıcı = yeni Infrastructure adaptörü + tek DI satırı. Frontend değişmez.

## Donanım notu

- GPU yoksa CPU çıkarımı: 7B makul ama yavaş olabilir; streaming ile ilk token hızlı gelir. Yük/eşzamanlılık artarsa GPU'lu makineye/buluta config ile geçilir.

## CPU performans ayarı (GPU'suz)

GPU yokken iki bağımsız maliyet var ve ayrı kollardan ayarlanır:

- **CPU doygunluğu ("makineyi kilitliyor"):** Ollama varsayılan olarak tüm çekirdekleri kullanır → çıkarım sırasında sistemin geri kalanı (API/DB/diğer işler) tıkanır. `AiHelper:NumThreads` ile çekirdek sayısını sınırla (`0`=Ollama'ya bırak; örn. 8 çekirdekli makinede `6` → 2 çekirdek sisteme kalır). Her model boyutunda işe yarar.
- **Gecikme (yavaş cevap):** baskın maliyet çoğu zaman üretim değil, **bağlamın prompt-eval'i**dir. `MaxContextChunks` düşür (8→5 ölçülen: ~68s→~25s, kalite korunur) ve/veya daha küçük `ChatModel`. `MaxOutputTokens` yalnız uzun cevaplarda etkili.
- **Model boyutu:** `qwen2.5` aile boyutları 0.5/1.5/3/**7**/**14**/32/72 — 7 ile 14 arası ara boy yoktur. 14b kalite iyidir ama CPU'da ~2× ağırdır. CPU'da pratik öneri **7b**; 7–14 arası "orta" bir model isteniyorsa farklı aile gerekir (ör. `gemma2:9b` daha hafif, `mistral-nemo:12b` 14b'ye yakın) — Türkçe'de Qwen güçlüdür, aile değişiminde kaliteyi doğrula.
- **Bu makine (dev, 8C/16T, GPU yok):** user-secrets ile `NumThreads=6` + `MaxContextChunks=5` + `ChatModel=qwen2.5:7b` → CPU ~%45 (max ~%53, sistem responsive) + ~25s warm. Repo varsayılanları (`NumThreads=0`, `MaxContextChunks=8`) korunur; sunucu da CPU-only ise oraya da aynı override önerilir.

## Açma/kapama (dev PC) + kayıt

- **Aç/kapat:** `AiHelper:Enabled` (varsayılan `true`). `false` yapıldığında `/ask` ve `/reindex` anında **503** döner (Ollama'ya HİÇ gitmez → model yüklenmez → RAM/CPU tüketimi olmaz) ve widget üç yüzeyde de `GET /api/v1/ai-helper/status`'u okuyup kendini gizler. Dev'de kapatmak: `dotnet user-secrets set "AiHelper:Enabled" "false" --project server/src/CoreAlign.API` + yeniden başlat; açmak: `dotnet user-secrets remove "AiHelper:Enabled"`. RAM'i tamamen boşaltmak için ayrıca Ollama servisini durdurabilir / `OLLAMA_KEEP_ALIVE`'ı kısaltabilirsin (model keep-alive sonrası kendiliğinden boşalır).
- **Kayıt (analiz için):** her cevap `ai_helper_query_logs`'a yazılır (PII-safe, never-throws writer): soru, **cevap metni**, locale, tenant, route, getirilen chunk'lar + skorları (JSON), efektif model/chunk-sayısı/top-score ve **`conversation_id`** (panel oturumu boyunca sabit → çok-turlu konuşmalar gruplanır). Geri-bildirim (👍/👎) `ai_helper_feedback`'te `answer_id` ile cevaba bağlı. Canlı metrikler `/metrics` (`aihelper_*`). İleride: en çok başarısız/👎 alan sorular + düşük top-score + boş-bağlam trace'leri SQL ile incelenip içerik açığı sürülür.

## Sorun giderme

- `/api/v1/ai-helper/ask` 5xx + "connection refused": Ollama ayakta değil veya `BaseUrl` yanlış → `ollama list` / servis kontrol.
- "model not found": ilgili `ollama pull <model>`.
- Yavaş cevap: önce `MaxContextChunks` düşür (en büyük kazanç), sonra daha küçük `ChatModel` / `MaxOutputTokens`. Bkz. "CPU performans ayarı".
- Çıkarım tüm CPU'yu yiyor / makine donuyor: `AiHelper:NumThreads` ile çekirdek sınırla (örn. `6`). Bkz. "CPU performans ayarı".

## Faz 2/3 bileşenleri

- **Route/i18n extractor:** `npm run gen:ai-kb` (`scripts/ai-kb/extract-frontend.mjs`) Sidebar nav + i18n etiketlerinden her route için `docs/ai-kb/{tr,en}/generated/nav-*.md` (deep-link'li) üretir. Route/etiket değişince yeniden çalıştırın (ideali CI/build adımı); üretilen dosyalar reindex'te otomatik ingest edilir.
- **Zamanlı yeniden-index:** Hangfire `ai-kb-reindex` job'u her gün 06:00'da `ReindexAsync` çağırır (sunucuda Ollama erişilebilir olmalı). Manuel tetik: dev'de anonim, prod'da TenantAdmin ile `POST /api/v1/ai-helper/admin/reindex`.
- **Sektörel içerik:** `docs/ai-kb/{tr,en}/sector/*.md` (muhasebe + cam). Yeni konu = yeni `.md` + reindex; kod gerekmez.
- **Portallar:** Müşteri Portalı (`apps/customer-portal`, sky teması) ve B2B Portal (`apps/b2b`, amber teması) kendi `src/widgets/AiHelper/AiHelperWidget.tsx`'lerini taşır; relative `/api/v1/ai-helper/ask` kullanır (reverse-proxy arkasında, aynı backend + aynı KB).

## Kapsam & derin tarama (config — kolayca açılıp kapanır)

`appsettings`/user-secrets `AiHelper` bloğu:

- `MaxContextChunks` (8): cevap başına bağlam parça sayısı. CPU'da gecikmenin en büyük kolu (düşür = daha hızlı prompt-eval).
- `NumThreads` (0): Ollama çıkarımının kullanacağı CPU çekirdek sayısı; `0`=Ollama'ya bırak (tüm çekirdekler). GPU'suz makinede sistemi responsive tutmak için sınırla.
- `IngestModuleDocs` (true): `docs/modules/*.md` indekslenir.
- `IngestSourceCode` (false): açılırsa `.cs/.ts/.tsx` dosyaları da indekslenir (derin teknik "bu nasıl çalışıyor" soruları). ⚠️ İlk reindex CPU'da çok uzun sürebilir (binlerce dosya); `SourceCodeRoot` ile daralt, `SourceCodeExtensions`/`SourceCodeExcludes`/`MaxIngestFileBytes` ile ayarla.
- `ModuleDocsRoot` / `SourceCodeRoot`: boşsa `ContentRoot`'tan türetilir (`docs/modules`, repo kökü).
- **Görünürlük:** anonim (login öncesi) yalnız public yardım/navigasyon/sektör/i18n görür; modül-dokümanları + kaynak-kodu (`ModuleDoc` tipi) yalnız giriş yapmış kullanıcılara.

**Guardrail:** bağlam yetersizse asistan genel ERP bilgisiyle de yardım eder + ilgili sayfaya yönlendirir ("bulamadım" yerine); ama context'te olmayan CoreAlign'a özgü özellik/alan/route uydurmaz.

**İçeriği üret + yükle:** `npm run gen:ai-kb` (Sidebar/i18n → `docs/ai-kb/{shared/i18n,tr,en}/generated`); sonra API çalışırken `POST /api/v1/ai-helper/admin/reindex` (dev'de anonim) veya günlük Hangfire `ai-kb-reindex` job'u.
