---
description: 'Bitti demeden önce zorunlu doğrulama kapıları (build + lint + typecheck + test + migration).'
---

"Bitti" demeden önce CoreAlign kalite kapılarını ÇALIŞTIR ve sonucu PASS/FAIL olarak raporla. CLAUDE.md §8 + §13.5 + §17 bağlayıcı. Kullanıcıyı manuel test eden kişi yerine koyma — gate'leri sen koştur, yeşil görmeden teslim etme.

Dokunulan alana göre koş (yalnız ilgili olanları; şüphedeysen hepsini):

**Backend değiştiyse**

```bash
dotnet build server/src/CoreAlign.API/CoreAlign.API.csproj   # 0 warning (TreatWarningsAsErrors)
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
