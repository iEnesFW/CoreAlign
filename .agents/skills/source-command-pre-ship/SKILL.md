---
name: source-command-pre-ship
description: Run CoreAlign pre-ship quality gates before declaring work complete. Use when the user invokes pre-ship, asks to prepare a release or handoff, or when a CoreAlign implementation needs final build, lint, typecheck, test, and migration verification.
---

# CoreAlign Pre-Ship

"Bitti" demeden önce CoreAlign kalite kapılarını ÇALIŞTIR ve sonucu PASS/FAIL olarak raporla. AGENTS.md §8 + §13.5 + §17 bağlayıcı. Kullanıcıyı manuel test eden kişi yerine koyma — gate'leri sen koştur, yeşil görmeden teslim etme.

Dokunulan alana göre koş (yalnız ilgili olanları; şüphedeysen hepsini):

**Backend değiştiyse**

```bash
dotnet build server/src/CoreAlign.API/CoreAlign.API.csproj
dotnet test server/tests/CoreAlign.Application.Tests
dotnet test server/tests/CoreAlign.Integration.Tests
```

- Yeni endpoint var mı? → cross-tenant izolasyon ({404,403}) + N+1 round-trip bütçesi testi eklendi mi (§14)?

**Frontend (admin SPA) değiştiyse**

```bash
npm run typecheck
npm run lint          # 0 warning
npm run test
```

- Portallar/mobil dokunulduysa: `npm --prefix apps/<name> run build` / `cd mobile && npx expo start` ile kontrol.

**Migration eklendi/değiştiyse**

- Aynı turda `dotnet ef database update` uygulandı mı (§4.2 apply-same-turn)?
- Tabula-rasa: `DROP DATABASE → CREATE → dotnet ef database update` temiz mi (§4.2/§17.2)? (`DROP DATABASE` onay ister — gerekçesini söyle.)

**Kapanış**

- Her kapı için PASS/FAIL ver. Kırmızı varsa kök-neden düzelt, tekrar koş (lint'i `@ts-ignore`/`eslint-disable` ile susturma — §1.2).
- §15: "Yeni bir invariant öğrendim mi?" → evetse `docs/INVARIANTS.md`'ye tek satır ekle.
