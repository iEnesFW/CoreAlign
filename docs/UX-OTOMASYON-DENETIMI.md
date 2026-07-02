# CoreAlign — Modül Modül UX & Otomasyon Denetimi

> Kapsamlı kod taraması (frontend `src/` + backend `server/src/`) sonucu hazırlanmıştır.
> Amaç: kullanıcı deneyimini rahatlatacak ve döngüyü (müşteri → stok → sipariş → fatura → MRP → fiş → banka) otomatize edecek **somut** fırsatları modül bazında listelemek.
>
> Etiketler: **[HK]** Hızlı kazanım (≈0.5–2 gün) · **[O]** Orta (≈3–7 gün) · **[B]** Büyük (1+ hafta).
> Not: "bu oturumda eklendi" işaretliler son çalışmada yapıldı.

---

## 0. Yönetici Özeti

İyi haber: altyapı çok sağlam. DDD/CQRS, çok-kiracılı (multi-tenant) izolasyon, **DocumentSequence ile otomatik numaralandırma**, outbox ile olay yayılımı, optimistic concurrency, audit log, bildirim kanalları (InApp/Email/SMS/Push/WhatsApp), e-fatura entegrasyonu hep mevcut. Çoğu eksik **"backend hazır ama UX'e bağlanmamış"** ya da **"tek tek manuel, otomatikleştirilebilir"** kategorisinde — yani sıfırdan yazmak değil, **bağlamak ve cilalamak** gerekiyor.

**En yüksek etkili 10 fırsat (özet):**

1. Numara önizlemesi: her oluşturma formunda "Otomatik · ORD-2026-0031" rozeti; manuel numara alanını gizle. **[HK]**
2. Düşük stok → tek tıkla satınalma talebi/siparişi (MRP zaten öneri üretiyor). **[O]**
3. Tahsilatta FIFO (en eski açık faturadan) otomatik kapama. **[O]**
4. Otomatik gecikme hatırlatma (dunning) zamanlayıcısı — şu an UI iskeleti var, çalışmıyor. **[O]**
5. Banka ekstresi içe aktarma + otomatik eşleştirme (parser altyapısı var, UI yok). **[B]**
6. Kredi limiti aşımında sipariş bloğu (CreditLimitGuard var, otomatik tetiklensin). **[O]**
7. Liste sayfalarında toplu seçim + toplu aksiyon (DataTable seçim altyapısı hazır, handler yok). **[O]**
8. Çapraz-varlık global arama (Cmd-K şu an sadece menü; müşteri/sipariş/fatura numarası aratılsın). **[O]**
9. Belge zinciri görünümü: Teklif → Sipariş → Sevkiyat → Fatura → Tahsilat → İade tek ekranda. **[O]**
10. Tedarikçi varsayılanlarının satınalma formuna otomatik akışı (müşteride bu oturumda yapıldı; tedarikçide de yapılmalı). **[O]**

---

## 1. Numara Otomasyonu (yatay — senin başlıca isteğin)

**Mevcut durum:** `DocumentSequence` sistemi olgun ve **zaten otomatik**: 22 belge tipi, kiracı-bazlı, **yıllık sıfırlama**, biçim şablonu (`{P}-{Y}-{N}`), ayarlanabilir önek/padding, eşzamanlılık kilidi. Oluşturma handler'ları numara **boş gelirse otomatik üretiyor** (sipariş, fatura, ödeme, PO, GRN, PR, bordro, GL fişi vb.). Ayar ekranı: Ayarlar → Numara Formatı.

Tipler: `CustomerCode, ProductSku, OrderNumber, InvoiceNumber, CreditNoteNumber, DebitNoteNumber, PaymentNumber, ShipmentNumber, JournalNumber, SubscriptionOrderNumber, QuoteNumber, ReturnRequestNumber, PurchaseOrderNumber, VendorPaymentNumber, GlassProjectCode, StockCountNumber, PurchaseRequisitionNumber, MrpPlanRunNumber, GoodsReceiptNumber, EmployeeNumber, PayrollRunNumber, PayslipNumber`.

**Yani "bir öncekinin sonraki sayısıyla yeni açılması" zaten teknik olarak çözülmüş.** Asıl boşluk **UX tarafında**:

1. **Numara önizleme rozeti.** Form açılınca kullanıcı bir sonraki numarayı görmüyor. Her oluşturma ekranına salt-okunur "Otomatik · `ORD-2026-0031`" rozeti koy (ufak bir "next number" sorgu ucu ile). Güven verir, "numara ne olacak?" kaygısını siler. **[HK]**
2. **Manuel numara alanlarını gizle/kaldır.** Bazı admin formlarında hâlâ elle numara girilebiliyor (handler boşsa otomatik üretse de). Manuel girişi varsayılan olarak kapat; sadece "Gelişmiş → numarayı elle gir" altında bırak ve girilirse audit'e düş. Sıra bozulmasını ve duplicate riskini engeller. **[HK]**
3. **`DebitNote` ve `ProForma` tipleri yarım.** Enum'da var, handler yok. Borç dekontu ve proforma akışlarını tamamla. **[O]**
4. **Numara "deliği" raporu.** İptal/silme sonrası sıra atlamalarını gösteren küçük bir denetim görünümü (resmî numara sürekliliği için Türkiye'de önemli). **[O]**

---

## 2. Müşteri / CRM

**Mevcut durum:** Zengin. Kredi limiti guard'ı, müşteri **birleştirme (merge)**, çoklu adres/iletişim, toplu CSV içe aktarma, ekstre, grup/etiket, aktivite zaman çizelgesi var. Ticari koşulların (para birimi, vade, fiyat listesi, iskonto) yeni belgelere otomatik akışı **bu oturumda eklendi**.

1. **Kredi limiti otomatik blok.** `CreditLimitGuard` var ve faturada kontrol ediliyor; ama **sipariş oluşturmada sert/uyarı blok** akışı net değil. Müşteri açık bakiyesi + yeni sipariş > limit ise: uyar (soft) veya engelle (hard, ayardan seçilir) + "limit artışı talep et" aksiyonu. **[O]**
2. **Otomatik duplicate tespiti.** Merge var ama **önleyici** tespit yok. Müşteri oluştururken vergi no / e-posta / unvan benzerliğiyle "Bunu mu demek istediniz?" önerisi. İçe aktarımda da kod/vergi-no çakışmasını yakala. **[O]**
3. **Müşteri ekstresi otomasyonu.** Şu an talep üzerine. "Aylık ekstreyi otomatik e-postala" planı + PDF şablonu. **[O]**
4. **Dinamik segmentler.** Grup/etiket var ama kural-bazlı segment yok (örn. "son 90 günde > 50k alan", "60+ gün geciken"). Segment kur → toplu aksiyon (kampanya, hatırlatma, fiyat listesi atama). **[O]**
5. **Müşteri kartında "sonraki en iyi aksiyon".** Gecikmiş fatura varsa "Hatırlat", limit doluysa "Limit gözden geçir", uzun süre sipariş yoksa "Tekrar kazan". Küçük ama deneyimi çok artırır. **[O]**
6. **360° önizleme zenginleştirme.** LTV, ödeme alışkanlığı (ort. gün), ürün eğilimi, sipariş sıklığı sparkline'ları. **[O]**
7. **"Son Hareketler" iyileştirmeleri** (bu oturumda statü çevirisi + satır→ilgili sayfaya arama ile yönlendirme eklendi). Sıradaki: ödeme satırlarını da tıklanabilir yap, hareket tipine ikon/renk tutarlılığı. **[HK]**

---

## 3. Satış — Sipariş / Teklif / İade

**Mevcut durum:** Ana admin uygulamasında Sipariş (`OrderFormModal`), Teklif (`CreateQuoteModal` + Teklifler sayfası) ve İadeler menüsü **var**. Backend FSM zengin: Draft→Submit→Approve→Allocate→Pick/Pack/Dispatch→Ship→Deliver→Close; ayrıca `ConvertQuoteToOrderCommand`, `GenerateInvoiceFromOrderCommand`, RMA akışı hazır. (Not: customer-portal ve b2b uygulamalarında teklif/iade UI'ı yok — bunlar müşteri-yüzü, çoğu senaryoda gerekmeyebilir.)

### Sipariş

1. **Statü akışı çok tıklamalı.** Draft→...→Invoice ~8-10 ayrı tıklama/ekran. Çözüm: sipariş detayında **"Sonraki Adımlar" kartı** (tek butonla bir sonraki statü) + bileşik aksiyonlar ("Onayla & Rezerve Et", "Sevk Et & Faturala"). Her biri tek backend transaction. **[O]**
2. **Toplu iş yok.** Listede çoklu seçip "Seçilenleri onayla / rezerve et / sevk et". DataTable seçim altyapısı zaten var. **[O]**
3. **Sipariş → Fatura tek tık.** `GenerateInvoiceFromOrderCommand` var; sipariş detayına "Fatura Oluştur" butonu + oluşan faturaya bağlantı (linked invoice). **[HK]**
4. **Teklif → Sipariş tek tık.** Backend hazır; teklif detayına "Siparişe Dönüştür" butonu, satır/fiyat/adres taşınsın. **[HK]**
5. **"Tekrarla / Reorder".** Geçmiş siparişten tek tıkla aynı kalemlerle yeni taslak. Tekrarlayan müşterilerde büyük zaman tasarrufu. **[O]**
6. **Sipariş şablonları.** Sık tekrarlayan sipariş setlerini şablon olarak kaydet. **[O]**
7. **Taslak otomatik kaydetme.** Form kapanınca veri kaybı; localStorage'a 30 sn'de bir "Kaydediliyor…". **[HK]**
8. **Kalem editörü hızlandırma:** satır kopyala/çoğalt, SKU ile arama, klavye (Tab→Enter ile satır ekle), panodan yapıştır (Excel'den kalem listesi), stok uygunluğunu picker'da göster ("12 adet mevcut"). **[O]**
9. **Stok rezervasyon görünürlüğü.** Allocate edilince hangi depodan ne kadar düşüldü; yetersizse uyarı + kısmi rezerv. **[O]**

### Teklif

10. **Teklifte iskonto/fiyat listesi alanı yok.** Bu oturumda para birimi oto-default + fiyat listesinden kalem fiyatı eklendi; ama teklif modelinde **iskonto alanı yok**. Header/line iskonto + müşterinin varsayılan iskontosunu teklife de uygula (küçük backend + model eklemesi). **[O]**
11. **Teklif yaşam döngüsü UI.** Gönder/Kabul/Ret durumları + geçerlilik tarihi sayacı + "süresi doluyor" uyarısı + müşteriye e-posta ile teklif gönderme. **[O]**

### İade (RMA)

12. **Sipariş detayında "İade Oluştur".** Satır seç, miktar, sebep (Hatalı/Yanlış ürün/Hasarlı/Vazgeçti), stoklanabilir mi toggle'ı. RMA numarası otomatik. **[O]**
13. **İade alındığında otomatik:** stok geri girişi + kredi notu üretimi (backend destekliyor) tek akışta. **[O]**

### Çapraz

14. **Belge zinciri görseli.** Sipariş üstünde Teklif→Sipariş→Sevkiyat→Fatura→Tahsilat→İade bağlantı haritası; tek tıkla geçiş. Kullanıcı "bu sipariş faturalandı mı, ödendi mi?" sorusunu anında görür. **[O]**

---

## 4. Faturalama

**Mevcut durum:** Çok yönlü. Siparişten üret, **standalone manuel fatura**, kredi notu, e-fatura outbox (Nilvera/Foriba/GİB), müşteri snapshot'ı, vade çözümü, kredi kontrolü hep var. Numara otomatik.

1. **Toplu faturalama yok.** "Sevk edilmiş tüm siparişleri faturala" (tarih aralığı seç → toplu üret). Ay sonu için kritik. **[O]**
2. **Tekrarlayan / abonelik faturası yok.** `RecurringInvoiceTemplate` + zamanlanmış üretim (sıklık, kalemler, vade kuralı). SaaS/bakım/periyodik satışlar için. **[B]**
3. **Proforma & borç dekontu** akışları yarım (bkz. §1.3). **[O]**
4. **Fatura PDF şablon editörü.** Şu an sabit düzen; QuestPdf altyapısı var. Logo/alan/dipnot özelleştirme. **[O]**
5. **"Faturayı tahsil et" kısayolu.** Fatura detayından tek tıkla ödeme oluştur (tutar/para birimi ön-dolu). **[HK]**
6. **Vade/gün hesaplayıcı.** Oluştururken "Net 30 → Vade: 29 Oca" canlı göster. **[HK]**

---

## 5. Tahsilat / Ödeme

**Mevcut durum:** Ödeme oluştur→onayla→uygula akışı, kısmi ödeme, çoklu faturaya uygulama, fazla ödeme (`UnappliedAmount`) takibi, müşteri ledger'ı, yaşlandırma uç noktası mevcut. Olaylar AR alt-defterine otomatik düşüyor.

1. **FIFO / en-eski-önce otomatik kapama yok** — en büyük el emeği. "Açık faturaları otomatik kapat" seçeneği: açık faturaları tarihe göre sırala, tükenene dek yukarıdan uygula. ~%40 daha hızlı tahsilat girişi. **[O]**
2. **Fazla ödeme → sonraki faturaya otomatik aktarım yok.** `UnappliedAmount` kalıyor ama otomatik ilerlemiyor; sonraki faturada öner. **[O]**
3. **Silme/şüpheli alacak (write-off) akışı yok.** `WriteOffInvoiceCommand`: faturayı kapat, fark için kredi notu/ayar fişi, eşik üstü onaya tabi. **[O]**
4. **Mahsup/avans yönetimi.** Müşteri avansı (peşin tahsilat) → ileride faturayla mahsup. **[O]**

---

## 6. Banka / Hazine

**Mevcut durum (kritik boşluk):** Parser altyapısı **var** (`IBankReconciliationProvider`: MT940, CSV, CAMT.053, Garanti, İş Bankası) ama **operasyon katmanı ve UI yok**. Banka hesabı master'ı, ekstre içe aktarma, eşleştirme, mutabakat, GL kaydı, nakit pozisyon — hiçbiri uçtan uca bağlı değil.

1. **Banka hesabı master'ı.** Kiracı bazlı banka hesapları (IBAN, para birimi, açılış bakiyesi). **[O]**
2. **Ekstre içe aktarma + otomatik eşleştirme.** Dosya yükle → parse → satırları referans/tutar+tarih ile ödeme/faturaya otomatik eşle; belirsizleri manuel onaya bırak. **[B]**
3. **Mutabakat ekranı.** Eşleşen/eşleşmeyen, "temizlenmiş vs bekleyen", tek tıkla "bu ayı mutabakatla". **[B]**
4. **Nakit pozisyon paneli.** Banka + kasa + beklenen tahsilat/ödeme → 30/60/90 gün nakit akış projeksiyonu. **[O]**
5. **Tahsilat-gateway işi var** (`PaymentReconciliationJob` Iyzico/PayTR/Stripe'ı yokluyor) ama bu **banka mutabakatı değil**; ikisini ayır ve banka tarafını da kur. **[O]**

---

## 7. Stok / Envanter / Depo

**Mevcut durum:** StockItem (onHand, reserved, availableToPromise, reorderPoint, min/max, avgCost), hareketler (Receipt/Issue/Transfer/Adjustment/CountVariance), lot/seri, "reorder altı" filtresi, MRP'nin `shouldReorder`+`suggestedQty` hesabı var.

1. **Aktif düşük-stok uyarısı yok.** UI durumu gösteriyor ama uyarı/bildirim yok. Ürünler/Stok sayfasında sabit "X kalem reorder altında" bandı + bildirim. **[HK]**
2. **Düşük stok → tek tıkla talep/sipariş.** MRP öneri üretiyor; "Öneriyi PR'a/PO'ya çevir" butonunu öne çıkar (bkz. §10). **[O]**
3. **Depolar arası transfer önerisi.** Bir depoda fazla, diğerinde eksikse otomatik transfer öner. **[O]**
4. **Stok sayımı UI'ında otomatik numara.** Backend numaralıyor; sayım modalına entegre değil. **[HK]**
5. **Negatif stok / rezerv ihlali guard'ları + uyarıları.** Sevkiyat öncesi "yetersiz stok" net mesajı. **[O]**
6. **Barkod ile stok işlemleri** (sayım/transfer/sevk). **[O]**

---

## 8. Satınalma — Tedarikçi / SAS / Tedarikçi Faturası / 3'lü Eşleştirme

**Mevcut durum:** Talep→PO→Mal kabul→Fatura akışı tam; 3'lü eşleştirme (miktar %0, fiyat %5 tolerans) **var ve varsayılan açık**; numaralar otomatik. Tedarikçi master'ı zengin (defaultCurrency, paymentTerms, sınıflandırma, ledger).

1. **Tedarikçi varsayılanları PO'ya otomatik akmıyor.** Müşteride bu oturumda yaptığımız desenin tedarikçi eşleniği: PO açarken para birimi/vade/teslim deposu/fiyatı tedarikçiden ön-doldur. **[O]**
2. **Talep → PO tek tık.** `ConvertRequisition` var ama tedarikçi/para birimi elle seçiliyor; `preferredSupplierId`'den otomatik öner ve satır içi dönüştür. **[O]**
3. **Satış talebinden PO (demand-driven).** Açık siparişler stok açığı yaratıyorsa otomatik satınalma önerisi. **[O]**
4. **Toplu PO onayı** ve **toplu mal kabul** (tek tedarikçi/depo için gruplu). **[O]**
5. **Tedarikçi yaşlandırma (AP aging) raporu.** Ledger var ama yaşlandırma kovaları/aksiyon yok (müşteri tarafıyla paritede değil). **[O]**
6. **Tedarikçi performans karnesi.** Zamanında teslim %, kalite, fiyat trendi. **[O]**
7. **3'lü eşleştirme görünürlüğü.** Eşleşmeyenleri (PO≠GRN≠Fatura) tek ekranda topla, fark sebebi + onay. **[O]**
8. **Blanket/çerçeve sipariş** ve **RFQ/teklif toplama** akışları yok. **[B]**

---

## 9. Mal Kabul / Fişler

**Mevcut durum:** Mal kabul (GRN) satır-satır miktar, kısmi kabul, idempotent (çift kayıt engeli), stok + GL otomatik posting; ters çevirme guard'lı.

1. **Barkod ile kabul.** Barkod oku → miktar otomatik, sonraki satıra geç. **[O]**
2. **Eksik/fazla kabul toleransı.** Şu an fazla kabul UI'da bloklu; ürün/global tolerans + uyarı modu. **[O]**
3. **Kalite/muayene beklemesi.** Kabul edilen mal doğrudan stoğa giriyor; "muayenede" ara durumu (cam/temper için önemli). **[O]**
4. **ASN (ön sevkiyat bildirimi) eşleştirme** ve **toplu kabul** (çoklu PO). **[O]**
5. **Fiş tiplerinde otomatik numara önizlemesi** (bkz. §1.1). **[HK]**

---

## 10. MRP / Üretim

**Mevcut durum:** Gelişmiş. Dashboard (reorder adayları, bekleyen talepler, açık PO'lar), 30 günlük stok projeksiyonu, 90 günlük talep tahmini, **öneri üretimi** (`GenerateRequisitionSuggestionsAsync` — tedarikçiye göre gruplayıp PR oluşturuyor), pegging, değişiklik etki analizi, planlı üretim emri (Firmed/Released), CRP hesaplayıcı (kodda var ama akışa bağlı değil).

1. **Öneri → eyleme tek tık.** Öneriler PR oluşturuyor ama PO'ya çevirme manuel. MRP dashboard'a "Tümünü PR yap / seçilenleri PO yap" toplu aksiyonu. **[O]**
2. **Planlı üretim emri otomatik firm/release.** "Vade öncesi N gün otomatik firm" politikası + zamanlı serbest bırakma. **[O]**
3. **CRP'yi MRP koşusuna bağla** (sonlu kapasite). Kapasite çakışmalarını göster. **[B]**
4. **Çok seviyeli BOM patlatma.** `ProductComponent` var ama MRP net ihtiyaç hesabına bağlı değil; cam/doğrama montaj ağacı için gerekli. **[B]**
5. **Tedarikçi bazlı tedarik süresi** (sadece `product.LeadTimeDays` kullanılıyor; tedarikçi varyasyonu yok). **[O]**
6. **MRP koşu zamanlaması.** Gecelik otomatik koşu + "değişti" bildirimi. **[O]**

---

## 11. Muhasebe / GL

**Mevcut durum (dikkat):** Faturadan/ödemeden **müşteri alt-defteri (CustomerLedgerEntry)** otomatik düşüyor, ama UI'daki GL kart'ı (`GlPostingCard`) **temsilî** — gerçek GL hesap kaydı (JournalEntry) atılmıyor. Outbox/olay altyapısı hazır.

1. **Gerçek GL posting.** Gelir/KDV/iskonto/navlun GL hesap eşlemesi (Ayarlar'da var) ile fatura/ödeme onayında **JournalEntry üret**. Alt-defter ↔ GL mutabakatı + denetim izi. **[B]**
2. **Ödeme GL kaydı** ve **iptal/void için ters fiş**. **[O]**
3. **Dönem kilidi** GL posting'de de uygulansın (şu an sadece fatura oluşturmada). **[O]**
4. **GL mutabakat raporu** (alt-defter bakiyesi = GL kontrol hesabı). **[O]**

---

## 12. Yatay Platform (tüm modülleri etkiler)

1. **Çapraz-varlık global arama.** Cmd-K şu an yalnız ~15 menü öğesi. Müşteri (ad/kod), fatura (numara), sipariş, ürün (SKU) aratılabilsin; sonuçtan kayda git. En çok "rahatlatan" özelliklerden. **[O]**
2. **Toplu seçim + toplu aksiyon.** DataTable checkbox altyapısı hazır; handler yok. Sil/dışa aktar/etiketle/durum değiştir toolbar'ı. **[O]**
3. **Kayıtlı filtreler / görünümler.** Her yenilemede filtre sıfırlanıyor. localStorage + isimli görünümler ("Bekleyen onaylarım", "Gecikenler"). **[O]**
4. **Kolon özelleştirme** (göster/gizle/sırala) + localStorage. **[O]**
5. **Satır içi düzenleme** (müşteri adı, e-posta, limit gibi hızlı alanlar) — modal yerine. **[O]**
6. **XLSX dışa aktarma.** Şu an sadece CSV; ClosedXml altyapısı var, liste sayfalarına "Excel'e aktar" butonu. **[HK]**
7. **Zamanlanmış hatırlatma/bildirim.** Bildirim kanalları var ama "fatura vadesi yaklaşıyor", "teklif süresi doluyor", "stok kritik" gibi **zamanlanmış** tetikleyiciler yok. Bir reminder scheduler (BackgroundService) + kullanıcı tercihleri. **[O]**
8. **Klavye kısayolları.** Ctrl-N (yeni), Ctrl-S (kaydet), Ctrl-P (yazdır), "/" ile arama + kısayol cheat-sheet. **[O]**
9. **Optimistic UI.** Mutasyonlar sunucu cevabını bekliyor; güvenli işlemlerde anında geri bildirim. **[O]**
10. **Erişilebilirlik turu.** İkonlara aria-label, kontrast denetimi (axe/Lighthouse), landmark'lar. **[O]**
11. **Onboarding/karşılama akışı.** Tur altyapısı var ama yeni-kiracı karşılama sihirbazı yok. **[O]**
12. **Dark mode + i18n cila.** Çoğu yer tamam (chart'lar bu oturumda düzeltildi); kalan ham/çevrilmemiş anahtarları ve eksik dark varyantları tara. **[HK]**

---

## 13. Önceliklendirilmiş Yol Haritası

### A) Hızlı Kazanımlar (önce bunlar — düşük risk, yüksek hissedilen değer)

- Numara önizleme rozeti + manuel numara alanını gizleme (§1.1, §1.2)
- Sipariş→Fatura ve Teklif→Sipariş tek-tık butonları (§3.3, §3.4)
- "Faturayı tahsil et" + vade hesaplayıcı (§4.5, §4.6)
- Düşük-stok bandı + stok sayımı oto-numara (§7.1, §7.4)
- Taslak otomatik kaydetme (§3.7)
- XLSX dışa aktarma butonları (§12.6)
- Dark mode / i18n kalan cila (§12.12)

### B) Orta (döngüyü gerçekten otomatize eder)

- Düşük stok / MRP önerisi → tek-tıkla PR/PO (§7.2, §10.1, §8.2)
- FIFO otomatik tahsilat kapama (§5.1)
- Otomatik gecikme hatırlatma (dunning) zamanlayıcısı (§12.7 + AR)
- Kredi limiti otomatik blok (§2.1)
- Tedarikçi varsayılanları → PO (§8.1)
- Toplu seçim/aksiyon + kayıtlı görünümler + çapraz arama (§12.1–3)
- Belge zinciri görünümü (§3.14)
- Sipariş "Sonraki Adımlar" kartı + bileşik aksiyonlar (§3.1)

### C) Büyük (stratejik)

- Banka ekstresi içe aktarma + otomatik eşleştirme + mutabakat (§6)
- Gerçek GL posting + mutabakat (§11)
- Tekrarlayan/abonelik faturalama (§4.2)
- Çok seviyeli BOM + sonlu kapasite MRP (§10.3, §10.4)

---

### Notlar

- Bu rapor "ne eksik" kadar "ne **zaten var**"ı da gösteriyor; çoğu madde sıfırdan yazım değil, mevcut backend'i UX'e **bağlama** işi — bu yüzden hızlı ilerlenir.
- İstersen herhangi bir maddeyi seç, birlikte uçtan uca tasarlayıp uygulayalım (backend derlemesini sen yaparsın, ben sandbox'ta sözdizimi doğrularım).
