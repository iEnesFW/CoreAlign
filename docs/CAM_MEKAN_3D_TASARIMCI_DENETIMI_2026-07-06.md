# CoreAlign — 3D CAD "Cam Mekan" Tasarımcı Modülü Derin Denetimi

**Tarih:** 2026-07-06
**Kapsam:** `src/features/glass-enclosure` (157 dosya), `src/shared/three-engine`, `server/src/CoreAlign.Application/GlassEnclosure` (BOM/quote/cutting/stok yolları).
**Yöntem:** 4 paralel derin-denetim ajanı, çekirdek dosyalar (geometri, çizim araçları, autofill, kompozisyon, panolar, sektörel, render) satır-satır okundu. En kritik bulgular gerçek koda karşı elle doğrulandı. .NET SDK bu ortamda olmadığından backend statik analizle incelendi.

> **Önemli düzeltme (kural drift'i):** CLAUDE.md'deki "three.js r128 sınırı / CapsuleGeometry yasak / OrbitControls CDN'de yok" notu **BAYAT**. Repo gerçekte `three@0.183.0` + `@react-three/fiber@9` + `@react-three/drei@10` kullanıyor; bu API'ler mevcut. Kısıt diye kendini frenleme — modern three.js/drei özellikleri (Fullscreen, InstancedMesh, drei `<Bvh>`, `<Detailed>` vb.) kullanılabilir.

---

## Yönetici Özeti — modülün gerçek durumu

Geometri **matematiği sağlam ve iyi test edilmiş** (arc chord-invariant modeli, extrude winding, tessellation, curvedExtrude/curvedSlab testleri geçer). Yani temel doğru. Sorun **mimari ve ürün-bütünlüğü katmanında** ve senin tespitlerinle birebir örtüşüyor. Beş sistemik kök-neden, gözlemlediğin 12 belirtinin çoğunu üretiyor:

1. **Çizim = yalnız "açıklık/delik".** Bir yüzeye çizim daima malzemeden çıkarma (hole/recess) veya çıkıntıdır; çizilen bölgeye **cam kanat/bölme/serbest cam** koyma yolu yoktur. Gerçek freehand (basılı-tut-akıt) de yoktur — araç poligon + tek yay. Kavisli/bent yüzeye çizim ya bloklu ya sadece delik. → Tespit #1.
2. **Cam-host bağı kalıcı bir veri ilişkisi değil.** Doldurulan cam host duvara **parent edilmiyor**; bağ her sürüklemede geometrik örtüşmeden yeniden türetiliyor, **tek yönlü** (duvar→cam) ve **yalnız sürükleme** yolunda geçerli. Inspector'dan ölçü/konum değiştirmek, run'ı taşımak, resize ve çatı bu bağı hiç uygulamıyor → cam havada kalıyor. → Tespit #2, #4.
3. **Gerçek bina/kompozisyon primitifi yok.** L-duvar var ama kapalı 4-duvar "bina" objesi yok (şablon 4 grupsuz duvar üretiyor); "seçili duvarların üstünü otomatik çatıyla kapat" aksiyonu yok. → Tespit #3, #4.
4. **Tasarım ↔ ticaret kopuk.** 3D'de yerleştirilen **donanım backend'e yapısal yazılmıyor** (BOM/teklif/kesime girmiyor); **canlı maliyet önizlemesi backend BOM'dan farklı hesaplıyor** (cam alanı, profil, FX, arc). Yani ekranda gördüğün fiyat gerçek teklif değil. → Tespit #8.
5. **Sahne yaşam döngüsü kararsız.** Görünüm değişince `key={view}` tüm WebGL sahnesini remount ediyor; her mutasyon proje sorgusunu invalidate edip refetch→`loadProject` tetikliyor; `useViewerAppearance` her render'da yeni obje döndürüyor. Üçü birden "sayfa rebuild oluyor" hissini üretiyor. → Tespit #12.

Bunlara ek: gerçek tam ekran yok (#9), firma-düzeyi default marka/cam yok (#10), marka→cam→stok referansı backend'de kurulu ama frontend uyum-validasyonu uygulamıyor (#11), sağ/sol panel organizasyonu ve validasyon zayıf (#6/#7), stok rezervasyonu yok, nesting şekilli paneli bounding-box sayıyor (fire yanlış).

**Önem etiketleri:** KRİTİK = veri kaybı/yanlış fiyat/çalışmayan çekirdek akış · YÜKSEK = ciddi işlevsel eksik · ORTA = doğruluk/UX/ölçek · DÜŞÜK = cila/borç. Her başlıkta ilgili **(Tespit #N)** işaretli.

---

## 1. Çizim Araçları & Geometri (Tespit #1, #5)

### [KRİTİK] Yüzeye çizim yalnız "delik/oyuk" üretir — kanat, bölme veya yeni cam alanı çizilemez (Tespit #1: doğrulandı)

- **Konum:** `scene/DesignerCanvas.tsx:699-711` (`commitPenFace`), `model/wallFeatureGeometry.ts:321` (`ComposedFeatureKind = 'plug'|'protrude'|'outline'|'none'`)
- **Teşhis:** Bir wall/slab yüzeyine kalemle çizilen her kontur zorunlu olarak bir `SceneWallFeature` (opening) olur; mode yalnız `hole`/`recess`/`protrude`. Çizilen bölgeye **cam kanat/bölme/alt-panel** ya da serbest cam kontürü koyma yolu yok. "Buraya sabit cam + yanına sürme kanat" gibi cephe alt-bölümlemesi imkânsız.
- **Çözüm:** `commitPenFace`'e `penIntent: 'opening' | 'glassPanel' | 'divide'` ekle. `glassPanel` için mevcut `buildCurvedShapedGeometry`/`PolygonSurfaceObject` altyapısı (outline→şekilli cam) kullanılır; feature-CSG yerine yeni bir cam objesi commit et. `divide` host run'ın panel dizisini çizim çizgisinden böler (`panelDistribution` var).

### [KRİTİK] Genişlet (stretch) L/çokgen/üçgen tabanda yalnız kalınlık+kot çıkarır; seçilen yan cepheyi genişletmez (Tespit #5: kısmen — kutu/arc'ta var, L/çokgen'de yok)

- **Konum:** `scene/builders/PolygonSurfaceObject.tsx:236-262` (`stretchFaces` yalnız `top`+`bottom`)
- **Teşhis:** Kutu/arc gövdeler (wall/slab/run — `WallObject.tsx:1736`, `SlabObject.tsx:1139`, `RunGroup.tsx:390`, `ArcRunGroup.tsx:375`) düzgün per-face yönlü stretch alır. Ama serbest poligon (L/üçgen/çokgen) için stretch tool YALNIZ top/bottom üretir; yan kenarlar için `StretchFaceDef` yok — kontur değişimi ayrı "Q vertex-edit" moduna itilir. Senin "L'de sol tarafa tıklayınca sol cepheyi genişlet" beklentin bu asimetri yüzünden çalışmıyor.
- **Çözüm:** `PolygonSurfaceObject`'te her poligon kenarı için bir `StretchFaceDef` üret (kenar orta noktası=centerM, kenar normali=axis, uzunluk=widthM, gövde yüksekliği=heightM); `onCommit(delta)` kenarın iki verteksini normal yönünde öteleyip `updateSurface({points})` çağırsın. Böylece "hangi cepheye tıklarsam o cephe" L/çokgen/üçgen için de gelir.

### [YÜKSEK] Kavisli (arc) ve eğimli (bent/L) yüzeye çizim bloklu veya sadece delik (Tespit #1'i pekiştirir)

- **Konum:** `scene/DesignerCanvas.tsx:702` (curved wall → yalnız `hole`), `:716-727` (bent wall → toast + return), `:772-781` (barrel/pitch slab → return)
- **Teşhis:** En çok cam-mekan sektöründe kullanılan kavisli cephe/çatıya kapı/pencere/bölme çizilemiyor: kavisli duvarda yalnız `hole`, bent (L) duvara çizim tamamen reddediliyor (`ArcNoFeature` toast), kubbe/beşik çatıya çizim tamamen reddediliyor.
- **Çözüm:** Bent wall için isabeti `computeBendLegs`'in ürettiği düz leg'e yönlendir (o leg'in düz-yüzey feature yoluna). Barrel/pitch slab için yükseklik-haritalı CSG cutter ekle veya düz projeksiyonla recess destekle. Kavisli duvarda `commitFeatureDepth` clamp'ini gevşetip recess+depth aç.

### [YÜKSEK] Gerçek "serbest çizim" yok — kalem poligonal (tıkla-köşe) + tek shift-yay; sürekli freehand stroke desteklenmiyor (Tespit #1: doğrulandı)

- **Konum:** `scene/interaction/PenController.tsx:189-211` (`handleClick`=köşe ekle), `:138-146` (pointer-down yalnız shift ise yay)
- **Teşhis:** İsim "serbest çizim" olsa da davranış CAD-poligon: her tıklama köşe koyar. Basılı-tut-akıt freehand yolu yok — pointer-move yalnız önizler, nokta biriktirmez. Oysa `wallFeatureGeometry.ts:35` (`FREE_SAMPLE_STEP_MM=25`) + `simplifyFreePoints` (RDP) bir freehand altyapısını ima ediyor ama PenController beslemiyor.
- **Çözüm:** PenController'a "freehand" alt-modu ekle: pointer-down+move'da noktaları `FREE_SAMPLE_STEP_MM` aralıkla biriktir, pointer-up'ta `simplifyFreePoints` uygula. Poligon modu "click-to-place" olarak kalsın; toolbar'dan mod seçimi.

### [YÜKSEK] Kenar (segment) snap'i hesaplanıyor ama kullanılmıyor — yalnız köşe + grid + shift-açı (Tespit #5 bağlamı)

- **Konum:** `scene/DesignerCanvas.tsx:143-146` (`segments` üretiliyor) vs `PenController.tsx:28-39, 70-82` (yalnız `.points` + grid)
- **Teşhis:** `buildPlanSnapTargets` hem `points` hem `segments` (kenar çizgileri) üretir; PenController yalnız köşe noktalarına yapışır, **kenara/çizgiye hizalı çizemezsin** — mimari çizimde en kritik snap türü yok. Perpendicular/tangent/intersection snap'leri de yok, snap-tipi UI'ı da yok.
- **Çözüm:** `PenController.resolve`'a nokta-doğru izdüşümü ile kenar snap ekle (köşe önce, kenar sonra); `SnapGuideOverlay` ile snap-tipini göster. İleride grid/endpoint/midpoint/edge/perp toggle'lı bir snap-tipi UI.

### [YÜKSEK] Footprint köşe-genişletme rotasyonu sabit tutar — L/açılı/trapez gövde köşeden şekillendirilemez (Tespit #5)

- **Konum:** `scene/interaction/footprintCorners.ts:55-97` (`resizeBoxFromCorner` her zaman dikdörtgen döndürür)
- **Teşhis:** Köşe sürükleme daima eksen-hizalı dikdörtgen üretir; trapez/paralelkenar/L imkânsız. "Duvarı köşesinden çekince yamuk yapabilme" gibi düzenleme yok.
- **Çözüm:** Kutu gövdeler için mevcut davranış makul; serbest-köşe gerekiyorsa gövdeyi `PolygonSurfaceState`'e "çevir" (convert-to-polygon) aksiyonu ekle → `SurfaceVertexHandles` her köşeyi bağımsız hareket ettirir. Alternatif: `resizeQuadFromCorner` (4 serbest köşe).

### [ORTA] Kopyala-dizi (array), simetri/ayna, offset gibi üretkenlik primitifleri hiç yok

- **Konum:** `model/designerStore.ts` (arama: `mirror`/`array`/`symmetr` → yok; yalnız undo/redo/paste var)
- **Teşhis:** Simetrik cam-mekan (iki yana simetrik köşe cam) veya eşit aralıklı tekrarlı panel grubu elle tek tek üretilmek zorunda. Sektörel araç seti olgunluğu düşük.
- **Çözüm:** Store'a `duplicateSelection(offsetMm)`, `mirrorSelection(axis)`, `arraySelection({count, stepMm|angleDeg, pivot})` ekle; hepsi `pushHistory` ile undo'ya bağlı. Dönüşümler `planTransform` yardımcılarından türetilir.

### [ORTA] Kalem yay çiziminde canlı ölçü (yarıçap/açı) gösterilmiyor

- **Konum:** `scene/interaction/PenController.tsx:148-157` (arc dalı yalnız `arcPreview`) vs `scene/interaction/penArc.ts:21-33` (`arcMetricsFromBulge` mevcut ama bağlı değil)
- **Teşhis:** Shift+sürükle yay çizerken yarıçap/açı görünmüyor; kavis yarıçapı üretim için kritik.
- **Çözüm:** `handleMove` arc dalında `arcMetricsFromBulge(...)` çağır, apex'te drei `<Html>` etiketi (`R … mm · … °`) göster (`DragReadoutOverlay` deseni var).

### [ORTA] `resolveArc` sweep'i 360°'ye izin veriyor → sıfır-kiriş dejenerasyonu / kapalı halka riski

- **Konum:** `model/arcGeometry.ts:30` (`MAX_SWEEP_RAD = Math.PI*2`), `:63`, `:160`
- **Teşhis:** `deriveArcFromSweep` 359°'ye kırpar (doğru) ama `resolveArc`/`computeArcLayout` 360°'ye kadar kabul eder; sweep→360°'de kiriş `2r·sin(180°)=0` → uçlar çakışır, sıfır-uzunluk gövde. `isRealArc` bunu yakalamaz.
- **Çözüm:** `MAX_SWEEP_RAD`'i tüm türetme yollarında 359°'ye sınırla; `isRealArc`'a üst-sınır ekle.

### [DÜŞÜK] Diğerleri (özet)

- **Kalem grid snap 50mm sabit**, handle araçları 10mm — tutarsız çözünürlük (`PenController.tsx:21` vs `FootprintCornerHandles`/`SurfaceVertexHandles` 10mm). → `gridStepMm` store ayarına bağla.
- **`sanitizeFreeOutline` kendini-kesen konturu 24 nokta kırpma ile onarır, başarısızsa sessiz reddeder** (`wallFeatureGeometry.ts:241-254`); zemin çiziminde hiç onarım yok. → iterasyonu nokta sayısına orantıla + kurtarma.
- **Zemin çizimi daima `floor` (kot 0, 120mm)** (`DesignerCanvas.tsx:907-915`); çatı/kot çizim anında seçilemiyor. → `activeSurfaceKind`/`defaultElevationMm` bağla.
- **`filletedShapeMm` gerçek yay değil parabolik** (`surfaceFeatureShapes.ts:60`); büyük yarıçapta CNC yarıçapı tutmaz. → `absarc` kullan.
- **Kalem yayında `Ctrl+Z` devrede değil, yalnız Backspace** (`PenController.tsx:124-128`); iki geri-al modeli tutarsız.

---

## 2. Camla Doldur + Bina / Duvar / Çatı Kompozisyonu (Tespit #2, #3, #4)

### [KRİTİK] Duvarı inspector'dan (ölçü/konum) düzenleyince bağlı cam havada kalıyor (Tespit #2b + #4: doğrulandı)

- **Konum:** `model/designerStore.ts:728-737` (`updateWall` yalnız patch merge — **doğrulandı**), `ui/WallInspector.tsx:75-97`, `designer/panels/TransformToolbar.tsx:236-266`
- **Teşhis:** `updateWall` bağlı run'ları hiçbir zaman yeniden konumlamaz. Inspector'dan X/Y/uzunluk/rotasyon değiştirince duvar hareket eder, cam geride kalır. Sürükleme yolu (`WallObject onCommitMove`) camı taşır ama **numerik alan yolu taşımaz** — iki yol tutarsız.
- **Çözüm:** Tek `moveWallWithAttachments(wallId, patch)` yolu kur; hem sürükleme hem inspector bunu kullansın. Geometri/konum değişince `findAttachedRunIds` ile bağlı run'ları aynı delta ile kaydır.

### [KRİTİK] Cam-host bağı kalıcı değil — sürükleme başında örtüşmeden türetiliyor; bir kez kopunca yakalanmaz (Tespit #2b kök-neden)

- **Konum:** `model/wallAttachment.ts:32-79` (`pointAttached`/`findAttachedRunIds`), `scene/builders/WallObject.tsx:591-601, 705-726`
- **Teşhis:** Bağlanma bir veri ilişkisi değil; her sürüklemede `findAttachedRunIds(wall, sceneRuns)` ile o an örtüşen run'lar yakalanır. Cam bir kez ayrılırsa (yukarıdaki bug, ya da camı tek taşıma) sonraki duvar sürüklemesinde artık örtüşmediği için **yakalanmaz** — kopukluk kalıcılaşır; reload/undo sonrası da tümüyle örtüşme-tabanlı.
- **Çözüm:** Cam run'a opsiyonel `hostWallId` ekle; autofill/hole-fill sırasında set et. Taşıma/döndürme/ölçü değişiminde host'u örtüşme yerine bu alandan çöz. En sağlamı: doldurulan camı host duvarın three group'una **child** yap (yerel koordinat) — host'un her transform'u camı otomatik taşır.

### [KRİTİK] Cam run taşınınca host duvar takip etmiyor (bağ tek yönlü) (Tespit #2b simetrik hali)

- **Konum:** `scene/builders/RunGroup.tsx:282-298`, `scene/builders/ArcRunGroup.tsx:186-266`
- **Teşhis:** Duvar→run bağı var ama run→duvar yok; run sürüklemesi yalnız çoklu-seçim üyelerini taşır. Hole-fill camını tek taşıyınca duvar geride kalır (delikli obje + cam ayrışır — senin tarif ettiğin durum).
- **Çözüm:** Bağı simetrik yap (parent'lama veya iki yönlü co-move cluster). Cam host group'una child edilirse bu kendiliğinden çözülür.

### [YÜKSEK] "Dört duvar seç → üste otomatik çatı ekle" aksiyonu YOK (Tespit #3: doğrulandı)

- **Konum:** `designer/panels/SelectionToolbar.tsx:91-197` (çoklu-seçim toolbar'ı), `scene/interaction/PlacementController.tsx:411-421, 66-70`
- **Teşhis:** Çoklu-duvar toolbar'ında hizala/dağıt/birleştir/eşitle/**camla doldur**/köşe-dolgu/grupla var ama "üstünü çatıyla kapat" yok. Çatı ayrı `roof` aracıyla **elle** ve **sabit 3000×2000mm** gelir; seçili duvarların ayak-izini kapsamaz.
- **Çözüm:** Çoklu-seçim toolbar'ına "Üstünü çatıyla kapat" ekle: seçili duvarların birleşik ayak-izi bbox'ını (veya convex hull) hesaplayıp duvarların max top-height'ında bir `roof` slab üret. Bbox versiyonu ucuz ve #3'ü doğrudan çözer.

### [YÜKSEK] Kapalı 4-duvar bina primitifi yok; "oda" şablonu 4 grupsuz bağımsız duvar üretiyor (Tespit #4: doğrulandı)

- **Konum:** `model/templates.ts:107-127` (`room-door`), `hooks/useTemplateInsert.ts:42-48` (`groupId` atanmıyor)
- **Teşhis:** 4-duvar+kapı şablonu 4 ayrı `SceneWallState` ekler, `groupId` vermez. Kutuyu tek obje seçemez/taşıyamaz/döndüremez/büyütemezsin, üstüne "çatı ekle" diyemezsin. L-wall tek parça ama çok-segmentli kapalı kutu yok.
- **Çözüm:** Hızlı: `room-door`/`u-walls`/`l-walls` duvarlarına ortak `groupId` ata (birlikte taşı/sil/resize + grup "çatıyla kapat"). Doğru: plan-footprint (poligon) + yükseklik taşıyan "Bina/Oda" entity'si; duvarları footprint kenarlarından türet. `groupId` düzeltmesi bile #4'ün büyük kısmını açar.

### [YÜKSEK] İki-duvar-arası camla doldur, `connectorLeavesOutward` kapısıyla U/karşılıklı düzende reddediliyor (Tespit #2a: doğrulandı)

- **Konum:** `model/multiAutofill.ts:403-413` (`connectorLeavesOutward`), `:445`, `:29-31` (`OUTWARD_DOT_MIN=-0.35`, `CORNER_ANGLE_TOLERANCE_DEG=60`)
- **Teşhis:** Her serbest-uç çifti önce outward-yön testinden geçer; iki duvarın uçları **birbirine bakıyorsa** (U/kanal) outward vektörler bağlantıya ters düşer, dot çok negatif → çift daha geometri denenmeden atılır, "doldurulacak boşluk yok" toast'ı çıkar. `MIN_GAP_MM=300`/`MAX_GAP_MM=60000` ve açı toleransı ile birleşince "çoğu durumda çalışmıyor" algısını üretiyor.
- **Çözüm:** Kullanıcı iki duvarı bilinçli seçtiyse niyet net — outward testini çoklu-modda gevşet/kaldır, yalnız gövde-penetrasyon + gap kontrolüyle düz connector üret. Toast'ta red nedenini (açı/uzaklık/yön/örtüşme) ayrıştır.

### [YÜKSEK] Çatı slab'ı taşınınca altındaki duvarlar/camlar takip etmiyor (ve tersi)

- **Konum:** `scene/builders/SlabObject.tsx:484-502` (yalnız çoklu-seçim taşır), `WallObject onGestureStart` (slab'ı yakalamaz)
- **Teşhis:** Çatı-duvar arasında bağ yok; çatıyı taşıyınca birleşim kayar, duvarı taşıyınca çatı yerinde kalır. Kompozisyon bütünlüğü sürüklemede korunmuyor.
- **Çözüm:** Çatıya host-ilişkisi (hangi duvarların üstünde) tanımla; grup mekanizmasını (`groupId`) slab'ları da kapsayacak şekilde genişlet.

### [YÜKSEK] Kavisli duvar/run'da hizala-birleştir-eşitle fantom düz kirişi kullanıyor

- **Konum:** `model/multiAlign.ts:32-53` (`alignTargetCenter`/`alignTargetEndpoints` arc'ı yok sayar, `originX+lengthMm·cos(rot)`)
- **Teşhis:** `wallAttachment`/`multiAutofill` arc'ın gerçek ucunu (`arcEndLocal`) kullanırken hizalama/uç-uca birleştirme fantom düz uç kullanır; arc'ın gerçek ucu ~0.3·R sapar → kavisli parçalar yanlış konumlanır.
- **Çözüm:** `alignTargetCenter/Endpoints`'i `isRealArc` durumunda `arcEndLocal` ile hesapla (attachment koduyla aynı desen).

### [ORTA] Diğerleri (özet)

- **Duvarı uzatınca/resize edince bağlı cam ölçeklenmez** (`FootprintCornerHandles.tsx:131-163`, `footprintCorners.ts:55-97`); origin kayınca cam taşar. → resize commit'inde bağlı run'ı da güncelle. **(Tespit #4)**
- **Tek duvarın açık YÜZÜNÜ camla doldurma yok, yalnız delik/açıklık** (`wallAutofill.ts:139-268`). → "duvar önüne cephe camı" seçeneği ekle. **(Tespit #2a)**
- **Kavisli duvarda açıklık/autofill devre dışı; kavise çevirince kapı/pencere silinir** (`WallInspector.tsx:71-73, 120-141`). → arc-aware açıklık editörünü tamamla (offset=developed arc-length; `computeOpeningEdges` zaten bekliyor).
- **Autofill panel sayısı 20'de clamp** (`wallAutofill.ts:126-137`); geniş cephede üretilemez tek-parça cam. → fiziksel max-panel-width'e bağla.
- **Autofill run'ları tek tek server CRUD; yarıda kalınca öksüz run + köşe bağı eksik** (`useWallAutofill.ts:35-215`). → bulk/tek-transaction endpoint.
- **Beşik/tonoz çatı duvar tepesiyle kenar hizası garantisi yok** (`PlacementController.tsx:114-151`, sabit boyut). → "çatıyla kapat" aksiyonu boyutu bbox'a eşitlesin.
- **`resizeBoxFromCorner` minMm=50** (`footprintCorners.ts:87-88`) — üretilemez ölçü; başka yerde 100. → duvar/slab min 100.
- **Autofill "boşluk yok" toast'ı opak** (`useWallAutofill.ts:174-184`) — gerçek red nedenini gizliyor. → neden-kodu döndür.

---

## 3. Sağ / Sol Panel UX + Donanım + Cam Tipleri (Tespit #6, #7)

### [KRİTİK] 3D'de yerleştirilen donanım backend'e yapısal yazılmıyor — BOM/teklif/kesime girmiyor (Tespit #6/donanım: kopuk)

- **Konum:** `hooks/useDesignerEntityActions.ts:44-62` (`toPanelInput` `hardware`'i **atlıyor** — doğrulandı), `model/project.types.ts:212-227` (`AddPanelInput`'ta `hardware` alanı yok)
- **Teşhis:** `HardwareManager` ile panele eklenen her donanım (`SceneHardwareItem[]`) yalnız `sceneJson` blob'unda yaşıyor; `toPanelInput` yapısal DTO'ya koymuyor, backend `GlassProjectPanelDto`'da `hardware` yok. BOMPanel server-hesaplı DTO'dan çalıştığından **3D'de yerleştirilen donanım malzeme listesine/teklife/kesime hiç yansımıyor** — müşteriye çıkan fiyat gerçek donanımı içermiyor.
- **Çözüm:** Panel donanımını katalog `HardwareItem`'a bağlı yapısal satırlara (`hardwareItemId, quantity, position`) taşı, `AddPanelInput`'a ekle ve BOM'a besle. Serbest `SceneHardwareItem` yalnız görsel; `hardwareItemId` referansı ticaret için zorunlu.

### [KRİTİK] İki paralel/çelişkili donanım modeli: `hasHandle/hasLock/hasBrushSeal` (bool) vs `hardware[]` (nesne) (Tespit #6: doğrulandı)

- **Konum:** `ui/PanelInspector.tsx:440-463`, `model/project.types.ts:523-543`, `scene/builders/PanelFittings.tsx:13-64` vs `scene/builders/HardwareObject.tsx`
- **Teşhis:** Aynı "Donanım" sekmesinde iki sistem: (1) üç bool toggle → sabit konumlu, düzenlenemez kol/kilit/fitil; (2) `HardwareManager` → serbest nesneler. Panele hem `hasHandle=true` hem `Handle` nesnesi eklenebilir → iki kol render, BOM'a çift yansır. `Lock`/`GasketStrip` iki sistemde çakışır.
- **Çözüm:** Tek modele indir — bool'ları arka planda `hardware[]`'e gerçek nesne + `hardwareItemId` ekleyen "hızlı ekle" kısayoluna çevir; `PanelFittings` ve `HardwareObject` render yollarını birleştir.

### [YÜKSEK] Donanım seçimi bağlamsız; adet / kanat tarafı / kilit tipi / menteşe adedi yok (Tespit #6/donanım özellikleri)

- **Konum:** `ui/HardwareManager.tsx:36-47` (13 kind filtresiz), `ui/HardwareInspector.tsx:54-66`, `scene/builders/PanelFittings.tsx:22-23` (menteşe/kol tarafı sabit-kod)
- **Teşhis:** Sürme panele `Hinge`, sabit panele `Roller`, cam kanada hat/köşe donanımı (`CornerJoint/DripProfile`) eklenebiliyor. Menteşe adedi, makara sayısı, kilit tipi (tek/çok nokta/yer), DIN sol/sağ menteşe tarafı, kol yüksekliği/model yok. `HardwareInspector` yalnız renk + 6 boyut/offset.
- **Çözüm:** Öneriyi `panel.openingType`'a göre filtrele; `HardwareInspector`'a `quantity`, `catalogItemId` (marka/model), `hingeSide/mountSide` ekle; panele `hingeSide: 'left'|'right'`.

### [YÜKSEK] Panel cam TİPİ var ama renk/ton/kaplama yok; profil–cam kalınlık uyumu doğrulanmıyor (Tespit #6/cam: kısmen)

- **Konum:** `ui/PanelInspector.tsx:422-438`, `model/glassEnclosure.types.ts:138-154`, `model/catalogValidation.ts:4-23`
- **Teşhis:** Cam tek `<select>` (isim+kalınlık+U+dB). Ton/renk (füme/bronz/reflekte), low-e yüzey pozisyonu, buzlu/desenli yok. Panelin cam kalınlığı hattın `supportedGlassThicknesses`'ine karşı kontrol edilmiyor (`catalogValidation` yalnız genişlik/yükseklik/ağırlık) — profilin taşımadığı kalınlık uyarısız seçilebiliyor.
- **Çözüm:** Cam listesini `supportedGlassThicknesses` ile filtrele + inline uyarı; `GlassTypeDto`'ya `tint/finish` ekle; `runViolatesCatalog`'a kalınlık + açılım uyumu ekle.

### [YÜKSEK] Hat inspector'ında "Cam" sekmesi boş; "tüm panellere cam uygula" yok (Tespit #6: doğrulandı)

- **Konum:** `ui/RunInspector.tsx:349-356` ("panel başına seçilir" ipucu), `ui/PanelInspector.tsx:422-438`
- **Teşhis:** 6-8 panelli cephede her paneli tek tek seçip aynı camı atamak ağır; sektörde hat genelde tek cam spesifikasyonu taşır. Toplu atama yok.
- **Çözüm:** Hat "Cam" sekmesine "Hat geneli cam" `<select>` + "Tüm panellere uygula" (mevcut `rebalance` deseni); panel override korunur.

### [YÜKSEK] Sağ panelde üç seçim-özeti çakışıyor; sekme yapısı mantıksız (Tespit #6: doğrulandı — "zor anlaşılır, organize değil")

- **Konum:** `designer/panels/InspectorPanel.tsx:65-185, 53-63, 201-291`, `SelectionSummary.tsx:24-89`, `TransformToolbar.tsx:446-484`
- **Teşhis:** Panel genişliği/cam/donanım üç yerde görünebiliyor (Inspector + SelectionSummary + TransformToolbar), genişlik iki yerde farklı min/max ile düzenlenebiliyor. Sekmeler tutarsız: panel "general" tek alan (`OpeningType`), run "hardware" sekmesi profil/çerçeve alanları taşıyor (donanım değil), panel yoksa `hardware`/`glass` boş kutu.
- **Çözüm:** Tek kaynak: `TransformToolbar` yalnız taşı/döndür/boyut; `InspectorPanel` tüm özellikler; `SelectionSummary`'yi kaldır veya collapsed-rail'e al. Panel sekmelerini birleştir ("Cam & Açılım" / "Ölçü & Şekil" / "Donanım"); run "hardware"ı "Çerçeve & Profil" yap; boş sekmede yönlendirme.

### [YÜKSEK] Profil sistemi seçiminde systemType/açılım kısıtı gizli; panel açılım tipleri profilden bağımsız

- **Konum:** `ui/RunInspector.tsx:121-182`, `model/glassEnclosure.types.ts:198-217`, `ui/PanelInspector.tsx:20-27`
- **Teşhis:** Profil `<select>` yalnız isim; `systemType` (Folding/Sliding/Guillotine/Hinged/Fixed) ve `supportedOpenings` gösterilmiyor. Panel açılım butonları profilin `supportedOpenings`'inden bağımsız → giyotin-only sisteme "Folding" panel atanabiliyor (sektörel doğruluk hatası).
- **Çözüm:** Option'lara `systemType` etiketi; panel açılım butonlarını `supportedOpenings`'e göre disable/filtre et.

### [ORTA] Sol araç paleti aşırı yüklü; sahne ağacı/outliner yok (Tespit #7: doğrulandı)

- **Konum:** `designer/panels/ToolPalette.tsx:50-92, 162-212` (17+ ikon tek bar), `designer/panels/RunsPanel.tsx:77-99` (yalnız `scene.runs`), `LayersControl.tsx:6-11`
- **Teşhis:** 17+ araç tek yatay barda, gruplama/etiket yok, dokunmatikte taşar. Ana liste yalnız hatları gösterir; duvar/döşeme/yüzey/bağlantı listede yok — seçmenin tek yolu 3D'de tıklamak. CAD'de beklenen outliner yok.
- **Çözüm:** Paleti kategorilere böl (Seç/Dönüştür · Çiz · Yerleştir · Eylem), sık aksiyonu öne al. `RunsPanel`'i gruplu outliner'a çevir (Hatlar/Duvarlar/Döşemeler/Yüzeyler + görünürlük + kilit — `locked` alanı zaten var), 3D ile senkron seçim.

### [ORTA] Diğerleri (özet)

- **Panel şekillendirme yalnız tek-panelli hatta** (`PanelInspector.tsx:54-55, 217`); kış bahçesi alınlık (trapez) çok-panelli cephede verilemiyor. → hat-düzeyi üst-eğim parametresi.
- **`HardwareInspector` offset clamp'siz** (`:85-101`) — donanım camın dışına/havaya; `TransformToolbar` clamp'liyor (tutarsız). → ortak clamp.
- **`RunConnectionInspector` persist etmiyor** (`:57-60`) — diğer inspector'lar ediyor; köşe/mitre yalnız autosave'e bağlı. → persist modelini birörnek yap.
- **`PanelInspector` cam listesi `isActive` filtresiz** (`:429-435`) — pasif cam atanabiliyor; RunInspector filtreliyor. → `.filter(isActive || seçili)`.
- **Boyut input'larında üst sınır/validasyon tutarsız** (`PanelInspector.tsx:184-215` width max 3000 sabit, height max yok; donanım sınırsız). → profil `maxPanelWidth/Height`'e bağla + inline uyarı.
- **`hasBrushSeal` panel başına** (`PanelInspector.tsx:453-457`) — fitil sürekli/profil düzeyinde olmalı; `GasketStrip` ile çift model. → hat/profil düzeyine taşı.

### [DÜŞÜK] i18n/tema hijyeni

- `InspectorPanel.defaultInspectorLabel` (`:293-312`) ve çok sayıda inline `defaultValue` **hardcoded Türkçe**; en.json eksikse İngilizce kullanıcı Türkçe görür (§1.4/§2.5). → fallback'leri İngilizce yap, tr/en tamlığını denetle.
- Boş-durum ikonları ham emoji (🪟/📐) (`InspectorPanel.tsx:124-127` vb.) — dark kontrast garantisi yok. → lucide + token.
- Renk paleti RAL/finish/fiyat etkisini göstermiyor (`RunInspector.tsx:138-182`). → seçili rengin RAL+finish+fiyat özeti.

---

## 4. Sektörel: Stok · Marka · Cam · Teklif · Fire · Plaka · Defaults (Tespit #8, #10, #11)

### [KRİTİK] Canlı maliyet önizlemesi backend BOM'dan tamamen farklı hesaplıyor — ekrandaki fiyat gerçek teklif değil (Tespit #8: doğrulandı)

- **Konum:** `model/costCalculator.ts:74-217` (frontend) vs `server/src/CoreAlign.Application/GlassEnclosure/Services/IBOMComposer.cs` (backend); tüketim `ui/LiveCostPreview.tsx`
- **Teşhis:** `LiveCostPreview` frontend hesabını, "Quote"/BOM backend'i kullanır ve **yapısal olarak uyuşmazlar**: (a) profil — frontend TÜM profillerin **ortalamasını**, backend rol-eşleşen `representativeProfile`'ı; (b) cam alanı — frontend **dikdörtgen** (`widthMm/1000 * h`), backend gerçek siluet `NetAreaMm2` (trapez/arch/yuvarlak köşe); (c) **FX yok** — frontend farklı para birimli kalemleri çevirmeden topluyor, backend `_fx.ConvertAsync`; (d) **arc yok** — frontend `BentGlassCostFactor`/`BendRailFeePerM` (ray bükme işçiliği) içermiyor. Aynı projede farklı fiyat.
- **Çözüm:** Canlı önizlemeyi backend'e taşı (debounce'lu `POST .../bom/preview` — kaydetmeden hesaplayan hafif endpoint) VEYA `costCalculator.ts`'i backend `BOMComposer`'ın birebir aynası yap (per-role profil, `NetAreaMm2`, FX, arc factor). Tek doğruluk kaynağı = backend.
- **Not:** Backend BOM'un kendisi de çok-para-birimli kalemleri FX'siz topluyor (ayrı KRİTİK — bkz. genel denetim raporu §6-2, `IBOMComposer.cs:127,188-190,225`). İkisi birlikte düzeltilmeli.

### [KRİTİK] ConvertToOrder stok REZERVE/DÜŞÜM yapmıyor — iki teklif aynı stoğu sipariş edebilir (Tespit #8/stok: doğrulandı)

- **Konum:** `server/src/CoreAlign.Application/GlassEnclosure/Handlers/CommerceHandlers.cs:287-414`, `server/src/CoreAlign.Application/Stock/Availability/StockAvailabilityService.cs:143-150` (salt-okuma)
- **Teşhis:** Convert sırasında `CheckAsync` yalnız kısayı raporluyor (shortage → 409); order line'ları oluştuktan sonra **hiçbir yerde stok düşülmüyor/rezerve edilmiyor** (glass akışında `Reserve/StockMovement/Decrement` yok). İki proje aynı `on_hand`'i "yeterli" görüp ikisi de sipariş oluşturur (over-commit). `Reserved` okunuyor ama bu akış artırmıyor.
- **Çözüm:** Convert (zaten `ITransactionalRequest`) içinde order line başına `StockItem.Reserve(qty)` çağır, aynı transaction'da persist et; iptalde geri al. Proje→order link zaten var (tekrar convert'te rezerve etme — idempotent).

### [YÜKSEK] Firma-düzeyi default marka/cam/profil-sistem yok; yeni run kataloğun ilk elemanıyla açılıyor (Tespit #10: doğrulandı)

- **Konum:** `GlassEnclosureSettings` (DefaultBrandId/DefaultGlassTypeId/DefaultProfileSystemId **yok**; waste/margin/currency var), `src/pages/glass-enclosure/GlassProjectDesignerPage.tsx:620,636` (`profileSystems[0]`, `colors[0]`), wizard marka/cam sormuyor
- **Teşhis:** `Plan2DAddRun` her yeni run'a kataloğun **ilk** profil sistemini ve **ilk** rengini atıyor; wizard da marka/cam seçtirmiyor. Firma tek marka çalışsa bile her seferinde elle değiştirmek gerekiyor.
- **Çözüm:** `GlassEnclosureSettings`'e `DefaultBrandId/DefaultProfileSystemId/DefaultGlassTypeId` ekle (migration + settings UI). Yeni run/panel'de `settings.default*` → yoksa `[0]` fallback. Wizard'a opsiyonel "varsayılan sistem/cam" adımı; proje düzeyinde override.

### [YÜKSEK] Marka→profil→cam→stok referansı backend'de KURULU ama frontend uyum-validasyonu uygulamıyor (Tespit #11: kısmen — bağ var, kullanılmıyor)

- **Konum:** `ProfileSystem.cs:8` (`BrandId`), `GlassType.LinkedProductId`, `HardwareKit.systemId`, BOM line `ProductId` (stok bağı) vs `model/catalogValidation.ts:4-23` (yalnız boyut/ağırlık)
- **Teşhis:** Veri modeli kopuk değil — zincir mevcut. Ama kullanım katmanı zayıf: cam kalınlığının sisteme uygunluğu (`SupportedGlassThicknessesJson`), açılış tipi uyumu (`SupportedOpeningsJson`), donanım-sistem uyumu doğrulanmıyor. Cam/donanım "yanlış markaya" seçilebilir.
- **Çözüm:** `runViolatesCatalog`'a kalınlık ∈ `SupportedGlassThicknessesJson` ve açılış ∈ `SupportedOpeningsJson` kontrolleri ekle; UI'da cam/sistem seçicilerini seçili markaya/sisteme göre filtrele — mevcut referans bağı kullanıcıya yansısın.

### [ORTA] 2D nesting şekilli paneli bounding-box sayıyor → arch/trapez panelde fire gizleniyor, utilization şişiyor (Tespit #8/fire: kısmen)

- **Konum:** `server/src/CoreAlign.Infrastructure/GlassEnclosure/Cutting/MaxRectsGlass2DOptimizer.cs:68-72,127-134`, `cutting/placedPanelOutline.ts:41-55` (rounded köşe rect fallback), `cutting/Glass2DNestingViewer.tsx:166-185`
- **Teşhis:** Nesting gerçek MaxRects (cam blank için makul) ama `UsedAreaMm2` her panelin **bounding dikdörtgenini** kullanılmış sayar; arch/raked/üçgen panelde kırpılabilir üçgen offcut fire olarak görünmez, utilization iyimser raporlanır, o offcut'tan ikinci parça çıkarma fırsatı kaçar. Görselde yuvarlak köşe hiç çizilmiyor.
- **Çözüm:** Raporda "net cam alanı (kesim)" ile "blank alanı (satın alma)" ayır; utilization'ı net alandan da ver. Uzun vade: arch/raked köşe offcut'larını serbest-dikdörtgen havuzuna ekleyen post-pass; görselde gerçek poligonu (rounded dahil) çiz.

### [ORTA] Diğerleri (özet)

- **KDV %20 üç ayrı yerde hardcoded** (`IBOMComposer.cs`, `CommerceHandlers.cs:223`, `costCalculator.ts`/`LiveCostPreview.tsx:33`); ihracat/muafiyet yok. → `Tax` modülü/müşteri vergi kodundan çöz; convert'te order line'a `taxRateId` bağla.
- **`TechnicalSummary` (canlı) cam alanını dikdörtgen alıyor** (`ui/TechnicalSummary.tsx:31`); arc developed-length ve şekilli net alan yok → yanlış m²/kg. → `arcGeometry`+panel şekil yardımcılarıyla hesapla.
- **Nesting kerf (kesim payı) yok, hedef utilization %85 sabit** (`cutting/Optimize2DButton.tsx:31-38`); cam kesiminde kerf fire/yerleşimi doğrudan etkiler. → kerf'i settings/optimize formuna ekle.
- **Arc panel rounded köşe kesim silueti rect fallback** (`placedPanelOutline.ts:47-55`) — operatör yanlış blank görebilir. → köşe yarıçapı desteği + görselde çiz.
- **Nesting SVG etiketleri sabit koyu renk** (`Glass2DNestingViewer.tsx:191-209`, `CuttingReportView.tsx:266,278`) — dark-mode'da okunmuyor (§2.2). → `currentColor`/CSS var.

---

## 5. Render · UX · Performans (Tespit #9, #12)

### [YÜKSEK] "Sayfa rebuild oluyor" hissinin ÜÇ kök nedeni (Tespit #12: doğrulandı)

1. **`<DesignerCanvas key={view} />`** (`designer/panels/CanvasPanel.tsx:209-210` — **doğrulandı**): 2d/3d/split arası her geçişte tüm three.js sahnesi, kamera, materyaller sıfırdan mount olur (WebGL context kopar-kur, çok pahalı).
2. **Her mutasyon proje sorgusunu invalidate ediyor** (`hooks/useGlassProjectQueries.ts:36-39` → `GlassProjectDesignerPage.tsx:440-442` `useEffect(loadProject)`): run/panel eklenince `invalidateProject` → detay refetch → `projectQuery.data` yeni referans → `loadProject` tetiklenir. `typedSceneEqual` çoğu tam-reset'i önlüyor ama refetch + diff "rebuild" hissi + gereksiz ağ üretiyor.
3. **`useViewerAppearance` her render'da yeni obje döndürüyor** (`model/viewerAppearance.ts:100-108`, memoize yok): `DesignerCanvas` tüm `scene`'e abone olduğundan her düzenlemede `appearance` yeni referans → `<Sky>/<Environment>/GroundPlane` reconcile.

- **Çözüm:** (1) `key={view}` yerine panelleri `display:none`/görünürlükle sakla → Canvas tek sefer mount. (2) Mutasyon `onSuccess`'lerinde `detail` invalidate etme (autosave zaten persist ediyor); `loadProject`'i yalnız `project.id`/`currentSceneVersion` değişince çağır. (3) `appearance`'ı `useMemo([preset])` ile sabitle.

### [YÜKSEK] `SceneViewport` ölçülene kadar Canvas'ı hiç render etmiyor → ilk açılışta boş alan + pop-in (Tespit #12)

- **Konum:** `src/shared/three-engine/SceneViewport.tsx:150-182` (`measured` gate, ResizeObserver)
- **Teşhis:** Container ölçülene kadar `<Canvas>` yok → boş → sonra ani pop-in. "Önce yükleniyor gibi, sonra düzeliyor" tarifinin ilk-açılış kısmı.
- **Çözüm:** İlk ölçümde iskelet/placeholder göster (boş bırakma); mümkünse ilk boyutu SSR/CSS'ten kestir.

### [YÜKSEK] Gerçek tam ekran yok; "focusMode" yalnız designer'ın yan panellerini gizliyor, app navbar/sidebar kalıyor (Tespit #9: doğrulandı)

- **Konum:** `src/App.tsx:254-257` (route app `<Layout>` içinde), `designer/layout/DesktopLayout.tsx:57,67-68,94-102` (`focusMode` yalnız `leftWidth/rightWidth`=0)
- **Teşhis:** Designer global navbar+sidebar altında; canvas o kadar daralıyor. `focusMode` (Maximize2) yalnız runs/inspector panellerini gizler, app chrome'unu değil. Default fullscreen de yok.
- **Çözüm:** (a) Fullscreen API (`requestFullscreen()`) ile canvas konteynerini gerçek tam ekrana al; veya (b) designer route'unu app `<Layout>` dışına taşı (kendi minimal shell'i + "Geri"); veya (c) `focusMode`'da app navbar/sidebar'ı da gizleyen global "immersive" bayrağı. Geçişi animasyonlu ve **canvas'ı remount etmeden** yap (yukarıdaki #12 çözümüyle uyumlu). Senin "geçiş dikkatlice tasarlanmalı" uyarına uygun: layout genişlik değişince canvas remount olmamalı, yalnız resize almalı.

### [ORTA] `DesignerCanvas` 1877 satır — God-component; tüm sahne+etkileşim tek dosyada, `scene`'in tamamına abone

- **Konum:** `scene/DesignerCanvas.tsx` (1877 satır, `:297-357` ~40 store selektörü)
- **Teşhis:** §1.6 (300 satır üstü parçalanır) ağır ihlali; tek parça `scene` aboneliği en küçük değişimde tüm ağacı yeniden değerlendiriyor (performans #8 + flicker #12'ye katkı).
- **Çözüm:** Render katmanlarını (`RunsLayer/WallsLayer/SlabsLayer/SurfacesLayer`) ve interaction controller'larını ayrı `React.memo` bileşenlere böl; her biri kendi dilim-selektörüne abone olsun.

### [ORTA] Çift persist stratejisi: blob-autosave + CRUD-sync birlikte, her sync `getById`+diff çekiyor

- **Konum:** `hooks/useSceneAutosave.ts:7,15-40` (1200ms blob save) + `hooks/useSceneSync.ts:126-249` (her sync `getById`+diff, sonda tekrar `getById`+`loadProject`)
- **Teşhis:** Autosave sahneyi blob kaydederken undo/redo/autofill CRUD ile senkronize edip her seferinde taze `getById` çekip diff yapıyor; sık düzenlemede çok GET + tam diff = ağ/CPU + flicker (#12). İki yol aynı state'i güncelliyor (yarış riski).
- **Çözüm:** Tek strateji seç. Bu UI için blob-autosave daha basit/az yarışlı (server sceneJson otorite; designer'daki CRUD run/panel endpoint'lerini kaldır, undo/redo blob replay eder, `getById`-diff kalkar).

### [DÜŞÜK] `DesignerCanvas.tsx` içinde mojibake (bozuk Türkçe) default string'ler

- **Konum:** `scene/DesignerCanvas.tsx:1768-1776, 1859-1869` (`YapÄ±ÅŸtÄ±r`, `tÄ±kla`)
- **Teşhis:** `defaultValue` metinleri UTF-8 bozulmuş; fallback tetiklenirse kullanıcı bozuk metin görür, dosya kodlaması kirlenmiş.
- **Çözüm:** Düzgün Türkçe ile yeniden yaz, anahtarları tr/en'de doğrula, dosyayı UTF-8 kaydet.

---

## 6. Öncelikli Yol Haritası

**Faz 1 — "kalp" akışını sağlamlaştır (kullanıcı en çok bunları hissediyor):**

1. **Cam-host bağını kalıcı yap** (`hostWallId` + parent'lama) → taşıma/ölçü/resize/çatı hepsinde cam host'u takip etsin. Tek `moveWallWithAttachments` yolu. (§2 — Tespit #2, #4)
2. **Sahne remount/flicker'ı bitir:** `key={view}` kaldır (görünürlükle sakla), mutasyon invalidate'ini kes, `useViewerAppearance` memoize. (§5 — Tespit #12)
3. **Gerçek tam ekran** (Fullscreen API veya immersive route), remount'suz geçiş. (§5 — Tespit #9)

**Faz 2 — kompozisyon & çizim gücü (senin bina-yapma senaryon):** 4. **Kapalı 4-duvar bina primitifi** (en az `groupId` ile grup; ideal footprint entity) + **"seçili duvarların üstünü çatıyla kapat"** aksiyonu. (§2 — Tespit #3, #4) 5. **İki-duvar-arası fill'i aç** (outward kapısını gevşet) + red nedenini göster. (§2 — Tespit #2a) 6. **Çizim niyeti** (`opening | glassPanel | divide`) — yüzeye cam kanat/bölme çizebilme; **freehand modu**; **kavisli/bent yüzeye çizim**. (§1 — Tespit #1) 7. **Genişlet tool'u L/çokgen/üçgen'de yüz-bazlı** (`PolygonSurfaceObject`'e per-edge StretchFaceDef). (§1 — Tespit #5)

**Faz 3 — ticaret bütünlüğü (fiyat doğruluğu):** 8. **Donanımı backend'e yapısal yaz** (`hardwareItemId` + BOM'a besle); iki donanım modelini birleştir. (§3 — Tespit #6) 9. **Canlı maliyeti backend BOM ile tek kaynağa indir** (bom/preview endpoint); BOM FX'i düzelt. (§4 — Tespit #8) 10. **ConvertToOrder'da stok rezervasyonu.** (§4 — Tespit #8)

**Faz 4 — sektörel & UX cilası:** 11. **Default marka/cam** + **marka→cam→kalınlık uyum validasyonu** (§4 — Tespit #10, #11); **hat-geneli cam ataması**, cam rengi/ton, profil systemType/açılım kısıtı, menteşe adedi/tarafı (§3 — Tespit #6). 12. **Sağ panel tek-kaynak + mantıklı sekmeler**, **sol outliner** (§3 — Tespit #6, #7); **nesting net-alan/kerf/şekilli görsel** (§4 — Tespit #8). 13. **Üretkenlik primitifleri** (array/mirror/offset), **edge-snap**, **DesignerCanvas parçalanması**, **çift persist tekilleştirme** (§1, §5).

---

_Bu rapor statik analiz + hedefli doğrulama ile üretildi (4 derin ajan, çekirdek dosyalar satır-satır). En kritik iddialar (`key={view}` remount, `toPanelInput` donanım düşürme, `updateWall` bağlı-cam taşımama, hardcoded seller/FX) gerçek koda karşı elle doğrulandı. Geometri matematiği testlerle sağlam; sorunlar mimari/ürün-bütünlüğü katmanında ve senin 12 tespitinin tümü kodda karşılık buldu._
