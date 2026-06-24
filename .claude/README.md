# `.claude/` — CoreAlign Claude Code yapılandırması

Bu klasör, CoreAlign'a özel Claude Code varlıklarını **repoyla birlikte taşınacak** şekilde tutar. Amaç: CoreAlign'ın Claude hafızası ve yapılandırması projeye izole, versiyon-takipli ve makineden bağımsız olsun.

İçerik:

| Yol             | Ne                                                                                                      |
| --------------- | ------------------------------------------------------------------------------------------------------- |
| `settings.json` | CLAUDE.md izolasyonu (`claudeMdExcludes`) + güvenlik izinleri (`permissions`) + format hook'u (`hooks`) |
| `commands/`     | Slash komutları: `/pre-ship`, `/new-endpoint`, `/new-module`, `/db-migration`                           |
| `agents/`       | `corealign-reviewer` — değişiklik-sonrası bağımsız §8 kod incelemesi                                    |
| `hooks/`        | `format-edited.mjs` — düzenleme sonrası prettier (PostToolUse)                                          |
| `memory/`       | Canlı memory store'un git-aynası (aşağıya bkz.)                                                         |

## Otomatik yüklenen bağlam (önemli)

Claude Code her oturumda **yalnız `CLAUDE.md` + onun `@import` ettiği dosyaları** instruction olarak yükler. CoreAlign'da `CLAUDE.md` üstte `@docs/INVARIANTS.md`'yi import eder → "bir daha aynı hatayı yapma" defteri her oturumda yüklüdür. Başka hiçbir doküman (bu `memory/` dahil) otomatik yüklenmez; `CLAUDE.md` §0.1 indeksindeki "önce oku"ları ajan kendi açar. Kalıcı **kural** bu yüzden `CLAUDE.md`/`docs/INVARIANTS.md`'ye yazılır, `memory/`'ye değil.

## `memory/` — Claude kalıcı hafıza aynası (mirror)

Claude Code, proje hafızasını **kullanıcı ev dizininde** çalışma-dizini slug'ına göre tutar; repo klasörünün fiziksel içinde değil. CoreAlign için **canlı (canonical) store**:

```
%USERPROFILE%\.claude\projects\D--CoreAlign\memory\
```

Buradaki `memory/` o canlı store'un **git-takipli aynasıdır** — repoyla taşınır, geçmişi görünür, başka makinede klonlandığında elde edilir. Her dosya tek bir kalıcı bilgi taşır (frontmatter + gövde); `MEMORY.md` indeksdir.

### Senkron (canlı store ⇄ ayna)

**Canlı → ayna (commit öncesi, en sık yön):**

```powershell
Copy-Item "$env:USERPROFILE\.claude\projects\D--CoreAlign\memory\*" "D:\CoreAlign\.claude\memory\" -Force
```

**Ayna → canlı (taze klon / yeni makinede ilk kurulum):**

```powershell
New-Item -ItemType Directory -Force "$env:USERPROFILE\.claude\projects\D--CoreAlign\memory" | Out-Null
Copy-Item "D:\CoreAlign\.claude\memory\*" "$env:USERPROFILE\.claude\projects\D--CoreAlign\memory\" -Force
```

> Tek doğruluk kaynağı **canlı store**'dur (Claude oraya yazar). Bu ayna onun snapshot'ıdır; çakışmada canlı store kazanır, aynayı güncelle.

## Komutlar, ajanlar, hooks, izinler

- **Slash komutları** (`commands/*.md`) — dosya adı = komut adı. `/pre-ship` (bitti-öncesi gate'ler), `/new-endpoint`, `/new-module`, `/db-migration`. CLAUDE.md §18.2.
- **Reviewer subagent** (`agents/corealign-reviewer.md`) — "bitti" demeden önce `> corealign-reviewer ile incele` gibi çağır; §8 + kritik invariant'ları bağımsız denetler, kod yazmaz. §18.3.
- **Güvenlik izinleri** (`settings.json` → `permissions`) — yıkıcı komutlar (`rm -rf`, `git push --force`, `git reset --hard`, `dotnet ef database drop`, `dropdb`, `docker compose down`) **onay (`ask`) ister**; salt-doğrulama komutları (build/test/lint/typecheck/git status·diff·log) `allow`. §7'yi mekanik zorlar. Not: Bash izin kalıpları **prefix** eşler (`Bash(rm -rf:*)`); `psql -c "DROP DATABASE …"` gibi iç-SQL kalıpları yakalanmaz — yıkıcı SQL'i elle dikkatle çalıştır.
- **Format hook'u** (`hooks/format-edited.mjs`, `settings.json` → `hooks.PostToolUse`) — Edit/Write sonrası ilgili frontend dosyasına `npx prettier --write` (asla işlemi başarısız etmez; `node_modules`/`dist`/`bin`/`obj` atlar). Lint debt birikmesini önler. `.cs` formatı build/CI'da kalır.

## İzolasyon — 3 katmanlı mimari

İki sistem `D:\` altında **tam izole** workspace'lerdir: **CoreAlign** (`D:\CoreAlign`) ve **diğerleri** (`D:\` = Omnisight/EMCM, Services, alarm-services...).

| Katman       | Dosya                             | Kapsam                                            |
| ------------ | --------------------------------- | ------------------------------------------------- |
| Evrensel     | `%USERPROFILE%\.claude\CLAUDE.md` | Her iki sistemde geçerli; proje-özel isim **yok** |
| CoreAlign    | `D:\CoreAlign\CLAUDE.md`          | Yalnızca CoreAlign (otomatik yüklenir)            |
| Diğer sistem | `D:\CLAUDE.md`                    | Omnisight/EMCM vb.; `D:\` workspace'inde yüklenir |

**İzolasyon nasıl garanti edildi:** Claude Code, CLAUDE.md'leri çalışma dizininden **üst klasörlere doğru** yükler. `D:\CoreAlign`, `D:\`'nin alt klasörü olduğu için `D:\CLAUDE.md` normalde sızardı; `settings.json` içinde **`claudeMdExcludes`** ile `D:\CLAUDE.md` bloklandı.

> Doğrulama: `D:\CoreAlign` workspace'inde `/context` (veya `/memory`) → yüklenen CLAUDE.md listesinde `D:\CLAUDE.md` **görünmemeli**, `@docs/INVARIANTS.md` **görünmeli**.

Kural çekirdeği: `D:\CoreAlign\CLAUDE.md`; tekrar-etme defteri: `docs/INVARIANTS.md`; hata yönetimi rehberi: `docs/modules/error-handling.md`.
