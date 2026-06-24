---
description: 'Migration üretme protokolü (ileri-tarihli ID, idempotent, aynı-tur uygula, snapshot drift, tabula-rasa).'
---

Migration: $ARGUMENTS

CLAUDE.md §4.2 / §4.12 / §12 / §17 — battle-tested, atlama:

1. **Build'li üret** (asla `--no-build` — stale assembly boş migration üretir):
   `dotnet ef migrations add <Ad> -p server/src/CoreAlign.Infrastructure -s server/src/CoreAlign.API -o Persistence/Migrations`
2. **ID'yi en son Phase'den SONRAYA rename et** (dosya adı + `.Designer.cs` + class adı + `[Migration("...")]`). EF wall-clock ID verir; proje ileri-tarihli `PhaseNN` kullandığı için yeni migration yanlış sıralanır. Snapshot doğru — koru.
3. **İçerik standardı:** para `decimal(18,4)`, zaman `timestamptz`, FK + index, tenant-leading unique, CHECK constraint (§4.3-4.7). Yıkıcı/raw adımları idempotent yaz (`IF NOT EXISTS`/`IF EXISTS`).
4. **Çakışma kontrolü:** `has-pending-model-changes` temiz mi? Başka ajan snapshot tutuyorsa §12.9 (el-yazımı idempotent + snapshot'a DOKUNMA + blocker'a not). `IGlobalReadable` entity ise tenant-FK exclusion'ını doğrula (§4.12).
5. **Aynı turda uygula:** `dotnet ef database update`. Sonra **tabula-rasa:** `DROP DATABASE → CREATE → update` temiz mi? (`column already/does not exist` çıkarsa düzelt.) `DROP DATABASE` onay ister.
6. **Drift yok:** raw-SQL-only index (GIN/BRIN/partition) modele bildir veya `docs/RAW_SQL_INDEX_REGISTRY.md` + INVARIANTS. Bitince **`/pre-ship`**.
