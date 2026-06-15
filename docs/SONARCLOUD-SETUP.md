# SonarQube Cloud (eski adıyla SonarCloud) — Kurulum

> Tek manuel parça bu. CI adımı **token-guard'lı** (`if: ${{ secrets.SONAR_TOKEN != '' }}`) — sen token'ı eklemeden hiçbir şey kırılmaz, gate yeşil kalır. Aşağıdaki A bölümünü (ya da ücretsiz alternatif B'yi) bir kez yaparsın.

## 0. Önce maliyet (oku, sonra karar ver)

- **Public repo →** ücretsiz, sınırsız.
- **Private repo (CoreAlign böyle) →** ücretsiz sadece **50.000 satıra (LoC)** kadar. CoreAlign büyük bir ERP + frontend monorepo; bu limiti **muhtemelen aşar**. Aşarsan **Team plan** gerekir (~$10/ay'dan başlar, LoC arttıkça artar).
- **Ücretsiz alternatif (B):** Self-host **SonarQube Community Edition** — Docker ile (zaten Docker kullanıyorsun), sınırsız LoC, C# + TS destekler. Tek eksiği: PR/branch bazlı analiz yok (yalnız ana dal analizi); o ücretli/Cloud ister.
- Önerim: önce **A** ile dene; 50k'yı aşıp ücret çıkarsa **B**'ye geç.

---

## A. SonarQube Cloud (SaaS) — adımlar

1. **Giriş:** https://sonarcloud.io → **Log in** → **GitHub ile** giriş yap, yetki ver.
2. **Organization:** Sağ üstte **+ → Create Organization** (ya da Analyze new project akışı seni yönlendirir). GitHub organizasyonunu/hesabını içe aktar veya manuel oluştur. **Organization Key**'i not al.
3. **Proje:** CoreAlign reposunu seç/import et. Sana bir **Project Key** verilir — not al.
4. **Analiz yöntemi:** **"With GitHub Actions"** (CI-based) seç. SonarCloud `SONAR_TOKEN` kullanmanı söyler.
5. **Token üret:** Sağ üst avatar → **My Account → Security → Generate Tokens** → bir isim ver (ör. `corealign-ci`) → **Generate** → değeri **kopyala** (bir daha gösterilmez).
   - Not: organization-scoped token Team/Enterprise ister; **ücretsiz tier'da kullanıcı/proje token'ı yeterli.**
6. **GitHub secret:** Repo → **Settings → Secrets and variables → Actions → New repository secret** → Name: `SONAR_TOKEN`, Value: token → **Add secret**.
7. **Hepsi bu.** CI'daki Sonar adımı artık çalışır (CC kurdu). Push sonrası SonarCloud panosunda ilk analiz görünür.

> CC'nin kuracağı CI şunu kullanır (bilgi için):
>
> - **C#:** `dotnet-sonarscanner begin /k:"<ProjectKey>" /o:"<OrgKey>" /d:sonar.host.url="https://sonarcloud.io" /d:sonar.token="$SONAR_TOKEN"` → `dotnet build`/`test` → `dotnet-sonarscanner end /d:sonar.token="$SONAR_TOKEN"`
> - **JS/TS:** `SonarSource/sonarqube-scan-action` ⚠️ Eski `SonarSource/sonarcloud-github-action` **deprecated** — kullanılmaz.
> - `sonar-project.properties`: `sonar.projectKey`, `sonar.organization`, coverage yolları (.NET cobertura + JS lcov).

---

## B. Self-host SonarQube Community (ücretsiz, sınırsız LoC)

1. `docker-compose.full.yml`'a `sonarqube:community` + ayrı Postgres servisi ekle (CC verir).
2. `docker compose up -d sonarqube` → http://localhost:9000 → `admin/admin` ile gir, şifreyi değiştir.
3. **My Account → Security → Generate Token** → kopyala.
4. CI secret'ları: `SONAR_TOKEN` + `SONAR_HOST_URL` (sunucunun adresi). **Önemli:** CI runner'ı bu sunucuya erişebilmeli; erişemiyorsa (lokal-only) Cloud daha pratiktir.

---

## Özet

- Sen: **A.1–A.6** (ya da **B**). 2–5 dakika.
- Token yoksa: CI yine yeşil, Sonar adımı atlanır.
- Detaylı CI/dosya kurulumu: `docs/quality-gates-spec.md` §8.
