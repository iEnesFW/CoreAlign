# CoreAlign — Quality Gates Kurulum Spec'i

> Bu, Claude Code'un uygulayacağı teknik kurulum reçetesidir. Master prompt buna referans verir. Her bölüm: **Amaç → Dosya → İçerik → Doğrula → Commit** akışını izler.

## 0. Genel kurallar (CC için bağlayıcı)

- **Branch:** Tüm iş `chore/quality-gates` branch'inde. Repo'da başka bir agent çalışıyor olabilir — bu branch izolasyonu çakışmayı önler.
- **Commit:** Her gate **ayrı, küçük commit**. Mesaj: `chore(quality): <gate>`. Build kırıkken commit yok.
- **Sürüm pinleme YAPILMADI (bilerek).** Paket sürümlerini **sen** güncel-stabil ve toolchain-uyumlu (.NET 10 SDK, ESLint 9 flat config, Node 22) seçersin ve `restore`/`install` + `build` ile doğrularsın. Ben uyumu senin makinende doğrulayamadığım için sürüm yazmadım — yanlış pin'den iyidir.
- **Central Package Management kontrolü:** `Directory.Packages.props` varsa (CI cache anahtarı buna bakıyor, muhtemelen var) sürümler orada `<PackageVersion Include="..." Version="..." />` ile tanımlanır; `<PackageReference>`'a `Version` yazma. Yoksa `PackageReference`'a doğrudan `Version` ekle.
- **Severity kalibrasyonu (kritik):** `TreatWarningsAsErrors` açık → analyzer "warning" = build kırar. Bu yüzden başlangıçta `AnalysisMode=Recommended` kullan (All değil); gürültülü stil kurallarını `.editorconfig`'de `suggestion`/`silent`, gerçek bug/perf/mimari kurallarını `warning` yap.
- **İhlaller DÜZELTİLİR, baseline'a ATILMAZ** (kullanıcı kararı). Bir kural çok gürültülü/değersizse: kapatma gerekçesini `docs/INVARIANTS.md`'ye yaz ve kullanıcıya bildir.
- **Çok büyük tek-seferlik düzeltme çıkarsa** (ör. 500+ ihlal tek kural): durma, o kuralı geçici `suggestion`'a çek, kalanları düzelt, kullanıcıya "şu kuralda N ihlal var, kademeli temizlik öneriyorum" diye raporla.

---

## 1. Backend Mimari Testleri (NetArchTest)

**Amaç:** CLAUDE.md'deki mimari kuralları (madde 3.1 bağımlılık yönü, slim controller, DTO sızıntısı) çalışan teste çevir.

**Dosya:** Yeni proje `server/tests/CoreAlign.Architecture.Tests/`, `.sln`'e eklenir.

**csproj:** xUnit + FluentAssertions + `NetArchTest.Rules`. Domain/Application/Infrastructure/API projelerine ProjectReference. `TreatWarningsAsErrors` ve global usings mevcut test projeleriyle aynı.

**Kurallar (her biri bir `[Fact]`):**

```csharp
public class LayeringTests
{
    static readonly Assembly Domain = typeof(CoreAlign.Domain.Common.TenantEntity).Assembly;
    static readonly Assembly Application = typeof(/* bir Application marker tipi */).Assembly;
    static readonly Assembly Api = typeof(/* Program ya da bir Controller */).Assembly;

    [Fact]
    public void Domain_hicbir_ust_katmana_bagimli_olamaz()
    {
        var r = Types.InAssembly(Domain)
            .ShouldNot().HaveDependencyOnAny(
                "CoreAlign.Application", "CoreAlign.Infrastructure", "CoreAlign.API")
            .GetResult();
        r.IsSuccessful.Should().BeTrue(string.Join(", ", r.FailingTypeNames ?? []));
    }

    [Fact]
    public void Application_infrastructure_veya_api_ye_bagimli_olamaz()
    {
        var r = Types.InAssembly(Application)
            .ShouldNot().HaveDependencyOnAny("CoreAlign.Infrastructure", "CoreAlign.API")
            .GetResult();
        r.IsSuccessful.Should().BeTrue(string.Join(", ", r.FailingTypeNames ?? []));
    }

    [Fact]
    public void Controller_lar_dogrudan_DbContext_e_bagimli_olamaz()
    {
        var r = Types.InAssembly(Api)
            .That().HaveNameEndingWith("Controller")
            .ShouldNot().HaveDependencyOn("CoreAlign.Infrastructure.Persistence")
            .GetResult();
        r.IsSuccessful.Should().BeTrue(string.Join(", ", r.FailingTypeNames ?? []));
    }
}
```

**Ek kurallar (yaz):** Handler'lar `sealed` ve `IRequestHandler<,>` implemente eder; Command/Query/Validator isim sonekleri; `CoreAlign.Domain.Entities` tipleri API assembly'sinde public dönüş tipinde geçmez (DTO sızıntısı); `IQueryable` API katmanında public imzada görünmez.

**Doğrula:** `dotnet test CoreAlign.sln` bu projeyi kapsar (mevcut CI'daki test adımı yeter; ek CI işi gerekmez).

**Commit:** `chore(quality): architecture tests (NetArchTest)`

---

## 2. Roslyn Analyzer'lar (çözüm geneli)

**Amaç:** SOLID ihlali, code smell, async/dispose/perf tuzaklarını derlemede yakala.

**Dosya 1 — `Directory.Build.props` (MERGE, mevcut `NuGetAuditSuppress` korunur):**

```xml
<Project>
  <PropertyGroup>
    <EnableNETAnalyzers>true</EnableNETAnalyzers>
    <AnalysisLevel>latest</AnalysisLevel>
    <AnalysisMode>Recommended</AnalysisMode>
    <EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>
  </PropertyGroup>

  <ItemGroup>
    <!-- CPM aktifse Version'ı Directory.Packages.props'a taşı -->
    <PackageReference Include="SonarAnalyzer.CSharp" PrivateAssets="all" />
    <PackageReference Include="Roslynator.Analyzers" PrivateAssets="all" />
    <PackageReference Include="Meziantou.Analyzer" PrivateAssets="all" />
  </ItemGroup>

  <ItemGroup>
    <NuGetAuditSuppress Include="https://github.com/advisories/GHSA-37gx-xxp4-5rgx" />
    <NuGetAuditSuppress Include="https://github.com/advisories/GHSA-w3x6-4m5h-cxqf" />
    <NuGetAuditSuppress Include="https://github.com/advisories/GHSA-9mv3-2cwr-p262" />
  </ItemGroup>
</Project>
```

**Dosya 2 — kök `.editorconfig` (yoksa oluştur, varsa MERGE).** Başlangıç kalibrasyonu: gerçek-değerli kuralları `warning`, gürültülü stil/dok kurallarını kıs. Örnek:

```ini
[*.cs]
# Gerçek bug/perf/async — build'i kırsın
dotnet_diagnostic.CA2007.severity = none      # ConfigureAwait (ASP.NET Core'da gereksiz)
dotnet_diagnostic.CA1848.severity = suggestion # LoggerMessage delegasyonu (kademeli)
dotnet_diagnostic.S2589.severity = warning     # her zaman true/false koşul
dotnet_diagnostic.S3776.severity = warning     # cognitive complexity
dotnet_diagnostic.S1172.severity = warning     # kullanılmayan parametre
dotnet_diagnostic.MA0051.severity = warning    # method too long
# Gürültü — kıs
dotnet_diagnostic.S125.severity = suggestion   # yorumlu kod (madde 1 zaten yasaklıyor)
csharp_using_directive_placement = outside_namespace:suggestion
```

> CC: `AnalysisMode=Recommended` ile çıkan listeyi gör, **gerçek ihlalleri düzelt**, sadece açıkça gürültü olanları `.editorconfig`'de kıs ve gerekçeyi not et. Hedef: `dotnet build -c Release` **0 warning**.

**Doğrula:** `dotnet build CoreAlign.sln -c Release` temiz.
**Commit:** `chore(quality): roslyn analyzers + editorconfig calibration`

---

## 3. EF Core N+1 / Performans Guard'ları

**Amaç:** N+1 ve sıralamasız limit gibi pattern'leri runtime'da değil testte/derlemede yakala.

**A) DbContext uyarı yükseltmesi** (`AddDbContext`/`OnConfiguring`, Infrastructure):

```csharp
options.ConfigureWarnings(w =>
{
    w.Throw(RelationalEventId.MultipleCollectionIncludeWarning);
    w.Throw(CoreEventId.RowLimitingOperationWithoutOrderByWarning);
});
```

**B) Query sayan interceptor + test temeli** (Integration.Tests): `DbCommandInterceptor` ile `ReaderExecuting` sayılır; bir test temeli round-trip sayısını assert eder.

```csharp
public sealed class QueryCountingInterceptor : DbCommandInterceptor
{
    public int Count { get; private set; }
    public override InterceptionResult<DbDataReader> ReaderExecuting(
        DbCommand c, CommandEventData e, InterceptionResult<DbDataReader> r)
    { Count++; return base.ReaderExecuting(c, e, r); }
}
```

Örnek test: "müşteri listesi N kayıt için tek sorgu atar" → `interceptor.Count.Should().BeLessThanOrEqualTo(2)`.

**Doğrula:** Integration testleri Postgres servisiyle geçer (CI'da zaten Postgres var).
**Commit:** `chore(quality): EF N+1 guards + query-count test base`

---

## 4. Mutation Testing

**Amaç:** "Testlerim gerçekten yakalıyor mu?" — coverage %'sinden dürüst ölçüm. Yavaştır → **nightly**.

**Backend — Stryker.NET:** `dotnet-tool` manifest'e ekle. `stryker-config.json` (Application projesini hedefler):

```json
{
  "stryker-config": {
    "project": "CoreAlign.Application.csproj",
    "test-projects": [
      "server/tests/CoreAlign.Application.Tests/CoreAlign.Application.Tests.csproj"
    ],
    "thresholds": { "high": 80, "low": 60, "break": 50 },
    "reporters": ["progress", "html", "cleartext"]
  }
}
```

**Frontend — StrykerJS:** `@stryker-mutator/core` + `@stryker-mutator/vitest-runner`. `stryker.conf.json` (src/ hedef, vitest runner, aynı eşikler).

**Doğrula:** Lokal `dotnet stryker` / `npx stryker run` çalışır, skor üretir.
**Commit:** `chore(quality): mutation testing (Stryker.NET + StrykerJS)`

---

## 5. Güvenlik & Bağımlılık

**Amaç:** Açık paket ve kod-seviyesi güvenlik bulgularını CI'da yakala.

- **`.github/workflows/codeql.yml`** — `csharp` + `javascript-typescript` dilleri, push/PR + haftalık schedule.
- **`.github/dependabot.yml`** — `nuget`, `npm`, `github-actions` ekosistemleri, haftalık.
- **CI adımları:** `dotnet list package --vulnerable --include-transitive` (bulguda fail) ve `npm audit --audit-level=high`.

**Doğrula:** Workflow'lar sözdizimi geçerli; audit komutları lokalde koşar.
**Commit:** `chore(quality): codeql + dependabot + dependency audit`

---

## 6. Frontend FSD Sınır Zorlaması

**Amaç:** CLAUDE.md 2.1 katman yönünü (shared → features → widgets → pages → app) ve feature→feature import yasağını makineyle zorla.

**A) ESLint (flat config, `eslint.config.js` MERGE):** `eslint-plugin-boundaries` ile elements = `app/pages/widgets/features/shared`; kurallar yönü ve feature→feature'ı yasaklar. Plugin flat-config'de sorun çıkarırsa **fallback:** `import/no-restricted-paths` zone'ları. Monorepo: `apps/*` her biri kendi `src/` köküyle ayrı değerlendirilir.

**B) dependency-cruiser — `.dependency-cruiser.cjs`:**

```js
module.exports = {
  forbidden: [
    {
      name: 'fsd-no-upward',
      severity: 'error',
      from: { path: 'src/shared' },
      to: { path: 'src/(features|widgets|pages|app)' },
    },
    {
      name: 'fsd-no-feature-to-feature',
      severity: 'error',
      from: { path: 'src/features/([^/]+)' },
      to: { path: 'src/features/(?!$1)([^/]+)' },
    },
    { name: 'no-circular', severity: 'error', from: {}, to: { circular: true } },
  ],
};
```

Script: `"depcruise": "depcruise src --config .dependency-cruiser.cjs"`. CI'da frontend job'a adım ekle.

**Doğrula:** `npm run lint` + `npm run depcruise` mevcut kodda geçer (ihlal varsa düzelt).
**Commit:** `chore(quality): FSD boundaries (eslint-boundaries + dependency-cruiser)`

---

## 7. Sıkı TypeScript Lint

**Amaç:** Tip güvenliği + complexity + smell'leri yakala.

**`eslint.config.js` (MERGE):** `typescript-eslint` `strictTypeChecked` + `stylisticTypeChecked` (parserOptions.project gerekir), `eslint-plugin-sonarjs` recommended, `eslint-plugin-unicorn` recommended (mantıksız kuralları kapat: ör. `unicorn/prevent-abbreviations` off). Type-checked lint yavaştır → CI'da yeterli kaynak/zaman.

**Doğrula:** `npm run lint` + `npm run typecheck` temiz (çıkan gerçek ihlaller düzeltilir).
**Commit:** `chore(quality): strict typescript-eslint + sonarjs + unicorn`

---

## 8. SonarCloud (dış pano)

**Amaç:** C# + TS tek panoda; duplication, complexity, hotspot, coverage trendi.

**Dosya — `sonar-project.properties`:** projectKey, organization, `sonar.sources`, exclusions (`**/bin/**,**/obj/**,**/node_modules/**,**/dist/**`), coverage yolları: .NET cobertura (`**/coverage.cobertura.xml`) + JS lcov (`**/lcov.info`).

**CI:** `.NET` tarafı `dotnet-sonarscanner begin/end` ile build+test'i sarar; JS tarafı SonarCloud için lcov üretir. Sonar adımı **token guard**'lı: `if: ${{ secrets.SONAR_TOKEN != '' }}` — secret yoksa CI yeşil kalır, pas geçer.

**⚠️ Kullanıcının yapacağı 2 manuel adım (CC yapamaz):**

1. SonarCloud'da organization + project oluştur (projectKey'i `sonar-project.properties`'e yaz).
2. GitHub repo secret: `SONAR_TOKEN` ekle.

**Doğrula:** `sonar-project.properties` geçerli; CI adımı secret yokken pas geçer, varken çalışır.
**Commit:** `chore(quality): sonarcloud config + ci wiring (token-guarded)`

---

## 9. CI Değişiklikleri (`.github/workflows/ci.yml` MERGE)

Mevcut job'ları **bozma** (frontend lint/typecheck/test/build, backend build/test, coverage-gate %60). Ekle:

- **Frontend job'a:** `npm run depcruise` ve `npm audit --audit-level=high` adımları.
- **Backend job'a:** `dotnet list package --vulnerable --include-transitive` adımı (arch testler + analyzer'lar zaten build/test içinde çalışır).
- **Yeni:** `codeql.yml` (ayrı workflow).
- **Yeni:** `mutation.yml` (ayrı, `schedule` nightly + manuel `workflow_dispatch`; Stryker.NET + StrykerJS; yavaş olduğu için PR'da değil).
- **Sonar:** ana CI'a token-guard'lı job/step (bkz. 8).
- Coverage eşiğini **şimdi yükseltme** (%60 kalsın); zamanla artırma notunu `docs/quality-gates.md`'ye yaz.

**Doğrula:** `ci.yml` sözdizimi geçerli (CC `act`/lint ile veya gözle); push'ta yeşil.
**Commit:** `chore(quality): ci wiring for new gates`

---

## 10. Dokümantasyon

**Dosya — `docs/quality-gates.md`:** her gate ne yakalar, **lokal nasıl çalıştırılır**, yaygın ihlallerin çözümü, Sonar kurulum adımları, coverage eşiğini yükseltme planı.

**Dosya — `docs/INVARIANTS.md`:** iskelet + ilk birkaç madde (CLAUDE.md 15'e göre). Başlık + format açıklaması + boş alan etiketleri.

**Commit:** `docs(quality): quality-gates guide + invariants log skeleton`

---

## 11. Yürütme Sırası (commit zinciri)

1. `chore: branch chore/quality-gates`
2. Mimari testleri (§1)
3. Roslyn analyzer + editorconfig + **ihlal düzeltme** (§2)
4. EF N+1 guard + query-count test (§3)
5. FSD boundaries + **ihlal düzeltme** (§6)
6. Strict TS lint + **ihlal düzeltme** (§7)
7. Mutation testing config (§4)
8. CodeQL + Dependabot + audit (§5)
9. SonarCloud config (§8)
10. CI wiring (§9)
11. Docs + INVARIANTS (§10)
12. CLAUDE.md'ye `CLAUDE-additions.md` bölümlerini ekle (madde 11–16)

Her adım sonunda ilgili build/lint/test **yeşil**. Bitişte tam doğrulama raporu (bkz. master prompt "Done").
