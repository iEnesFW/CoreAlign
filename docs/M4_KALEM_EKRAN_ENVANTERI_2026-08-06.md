# M4 · Kalem Girişli Ekranların Sipariş Şablonuna Hizalanması — Adım 0 Envanter

**Tarih:** 2026-08-06 · **Statü:** Adım 0 = SADECE envanter. Bu doküman hiçbir kod değişikliği içermez.
**Kaynak:** repo taraması (satır sayıları ve özellik tespitleri gerçek dosyalardan okundu).

---

## 0. Yönetici özeti

Kalem girişi yapan **11 ekran** var. Bunlardan **yalnız 1 tanesi** (`OrderFormModal`) modern form altyapısını kullanıyor; diğer 10'u ham `useState` dizisiyle kalem yönetiyor.

| Ölçüt                                         | OrderFormModal     | Diğer 10 ekran           |
| --------------------------------------------- | ------------------ | ------------------------ |
| react-hook-form + `useFieldArray`             | ✅                 | ❌ hiçbiri               |
| zod şeması (`zodResolver`)                    | ✅                 | ❌ hiçbiri               |
| Taslak otomatik kaydetme (`useDraftAutosave`) | ✅ (tek kullanıcı) | ❌ hiçbiri               |
| Kirli-form kapatma koruması (`useModalClose`) | ✅                 | 4/10                     |
| İki adımlı sihirbaz                           | ✅                 | ❌ hiçbiri               |
| Başlık indirimi / navlun / yuvarlama          | ✅                 | 0-1 kısmi                |
| Tevkifat (withholding)                        | ✅                 | 1/10 (standalone fatura) |
| Cam ölçü → m² türetme                         | ✅                 | ❌                       |

Yani "sipariş şablonuna hizalama" pratikte **bir ekranın yeteneklerini on ekrana taşımak**tır — ve bu yüzden görev "YÜKSEK REGRESYON RİSKİ" etiketini hak ediyor: sipariş ekranı kullanıcının en yoğun kullandığı ekran ve iskeleti çıkarırken bozulmamalı.

---

## 1. Ekran envanteri

### 1.1 Referans (şablon kaynağı)

| Ekran                       | Dosya                                   | Satır    | Notlar                                                                                                                                     |
| --------------------------- | --------------------------------------- | -------- | ------------------------------------------------------------------------------------------------------------------------------------------ |
| **Sipariş oluştur/düzenle** | `features/orders/ui/OrderFormModal.tsx` | **1250** | Tek referans. `OrderLineEditor.tsx` ayrı bileşen. 8 lookup sorgusu (müşteri, ürün, KDV, depo, tevkifat, ödeme koşulu, fiyat listesi, UoM). |

⚠️ 1250 satır CLAUDE.md §1.6'nın "300 satırı aşan component parçalanır" kuralını **çoktan aşmış** durumda. Bu, M4'ün yalnız yeniden kullanım değil aynı zamanda bir **borç ödeme** işi olduğu anlamına gelir.

### 1.2 Hizalanacak ekranlar

| #   | Ekran                      | Dosya                                                                     | Satır | Kalem yönetimi  | Eksik olan                                          |
| --- | -------------------------- | ------------------------------------------------------------------------- | ----- | --------------- | --------------------------------------------------- |
| 1   | Standalone fatura          | `pages/invoices/components/CreateStandaloneInvoiceModal.tsx`              | 511   | `useState` × 10 | rhf/zod, taslak, sihirbaz, başlık indirimi/navlun   |
| 2   | Tekrarlayan fatura şablonu | `pages/invoices/components/RecurringInvoiceFormModal.tsx`                 | 542   | `useState`      | rhf/zod, ürün seçici, KDV lookup                    |
| 3   | Alacak dekontu             | `features/invoices/ui/IssueCreditNoteModal.tsx`                           | 257   | `useState`      | rhf/zod (kalemler kaynak faturadan gelir — kısıtlı) |
| 4   | Satınalma siparişi         | `features/purchasing/ui/PurchaseOrderFormModal.tsx`                       | 331   | `useState` × 8  | rhf/zod, taslak, başlık indirimi, tevkifat          |
| 5   | Tedarikçi faturası         | `features/purchasing/ui/VendorBillModals.tsx`                             | 735   | `useState`      | rhf/zod; 3-way-match satır bağlama var (özel)       |
| 6   | Yevmiye fişi               | `features/accounting/ui/JournalEntryFormModal.tsx`                        | 345   | `useState` × 8  | rhf/zod; **borç=alacak** kuralı özel                |
| 7   | Stok fişi                  | `features/inventory/ui/StockVoucherModal.tsx`                             | 355   | `useState` × 7  | rhf/zod, taslak                                     |
| 8   | Cam levha mal kabul        | `features/glass-plates/ui/ReceiveGlassPlatesModal.tsx`                    | 305   | `useState`      | rhf/zod                                             |
| 9   | Satınalma talebi (MRP)     | `features/mrp/ui/PurchaseRequisitionForm.tsx`                             | 172   | `useState` × 4  | rhf/zod                                             |
| 10  | Sevkiyat oluştur           | `features/orders/ui/CreateShipmentModal.tsx`                              | 209   | `useState`      | rhf/zod (kalemler siparişten gelir — kısıtlı)       |
| 11  | **B2B sipariş**            | `apps/b2b/src/features/orders/NewOrderForm.tsx` (+ `OrderLineEditor.tsx`) | 306   | `useState` × 5  | ayrı yüzey — **çapraz import YASAK**, kopya gerekir |

---

## 2. `OrderFormModal`'ın çıkarılabilir iskeleti

Aşağıdakiler ekrandan bağımsız, **yeniden kullanılabilir** parçalardır:

| Parça                                                                                                              | Bugün nerede                                                                        | Çıkarılacak yer                                              | Zorluk            |
| ------------------------------------------------------------------------------------------------------------------ | ----------------------------------------------------------------------------------- | ------------------------------------------------------------ | ----------------- |
| Modal kabuğu + kirli-form kapatma + backdrop                                                                       | satır 166-167 (`useModalClose` + `useBackdropClick`, ikisi de zaten `shared/hooks`) | `shared/ui/DocumentFormLayout`                               | kolay             |
| İki adımlı sihirbaz başlığı (adım rozeti, `aria-current="step"`, adım-1 doğrulamadan adım-2'ye geçmeme)            | satır 174-181, 542-560                                                              | `shared/ui/FormWizardSteps`                                  | kolay             |
| Bölüm kabuğu (`sectionWrapperCls`/`HeaderCls`/`TitleCls`/`BodyCls`)                                                | satır 80-86                                                                         | `DocumentFormLayout.Section`                                 | kolay             |
| Taslak otomatik kaydetme + "taslağı geri yükle" bandı                                                              | satır 174-193 (`useDraftAutosave`, `ORDER_DRAFT_KEY`)                               | zaten shared hook; **anahtar parametreleştirilecek**         | kolay             |
| Kalem tablosu iskeleti (satır ekle/sil, yeni satıra odak, `useFieldArray`)                                         | satır 169, 194-268                                                                  | `shared/ui/DocumentLineTable` (generic `<T>`)                | **orta**          |
| Toplam paneli (ara toplam → satır indirimi → başlık indirimi → KDV → tevkifat → navlun → yuvarlama → genel toplam) | `useWatch` türevleri                                                                | `shared/ui/DocumentTotals` + **saf** `computeDocumentTotals` | **orta — dikkat** |
| Hızlı-ekle (ödeme koşulu / fiyat listesi)                                                                          | satır 173 + `MasterDataQuickModal`                                                  | zaten shared                                                 | kolay             |

⚠️ **Toplam hesabı en riskli parça.** Sipariş toplamı bugün hem client'ta (önizleme) hem server'da (`Order.Recalculate`) hesaplanıyor; ortak bileşene çıkarılırken client tarafı **birebir aynı** kalmalı, yoksa kullanıcı önizlemede bir rakam görüp faturada başkasını görür. Öneri: önce saf `computeDocumentTotals` fonksiyonu çıkarılıp **mevcut sipariş ekranının çıktısına karşı** testle kilitlenmeli (altın-değer testi), sonra bileşen değiştirilmeli.

---

## 3. Ekrana özgü, ORTAKLAŞTIRILAMAYACAK olanlar

Bunları zorla ortak bileşene sokmak, bileşeni herkesin korktuğu bir "her şeyi yapan" parçaya çevirir:

- **Yevmiye fişi**: satır seviyesinde borç/alacak ve `Σborç = Σalacak` kuralı; ürün/KDV kavramı yok.
- **Tedarikçi faturası**: satırın PO satırına bağlanması + `PoUnitCost` snapshot'ı (3-way-match).
- **Stok fişi / cam levha kabulü**: fiyat yok, depo/lot/seri var.
- **Sevkiyat & alacak dekontu**: kalemler kaynak belgeden **türer**, kullanıcı satır ekleyemez (yalnız miktar kısar).
- **Cam ölçü satırı** (en/boy/adet → m²): bugün `OrderLineEditor`'a özel; faturaya taşınacaksa `GlassLineMath` ikizinin de taşınması gerekir (M8 dersi).

---

## 4. Sipariş ekranını bozmama stratejisi

Adım 1'in tek başarı ölçütü: **sipariş ekranı birebir aynı davranır.**

1. **Önce saf mantık, sonra JSX.** `computeDocumentTotals` ve satır-ekleme davranışı önce saf fonksiyona çıkarılıp mevcut değerlerle testlenmeli.
2. **Yeni bileşen ilk gün sadece sipariş ekranında.** Diğer 10 ekran Adım 2+'da.
3. **Görsel/etkileşim doğrulaması tarayıcıda:** taslak geri yükleme, iki adım geçişi, satır ekle/sil, yeni satıra odak, kirli-form kapatma uyarısı, toplam rakamları — ekran görüntüsü + değer karşılaştırması. (Bu projede `npm run designer:probe` altyapısı bunu yapabiliyor; sipariş ekranı için aynı harness kullanılabilir.)
4. **Geri alma kolaylığı:** Adım 1 tek commit olmalı; davranış farkı görülürse tek `revert` yeter.

---

## 5. Önerilen adım sırası (her biri ayrı teslim)

| Adım  | Kapsam                                                                                                                          | Risk                        | Not                                                                                    |
| ----- | ------------------------------------------------------------------------------------------------------------------------------- | --------------------------- | -------------------------------------------------------------------------------------- |
| **1** | `DocumentFormLayout` + `FormWizardSteps` + `DocumentLineTable` + `computeDocumentTotals`; **yalnız** OrderFormModal'a uygulanır | **yüksek** (referans ekran) | 1250 satır → hedef <400; davranış birebir                                              |
| **2** | Standalone fatura + tekrarlayan fatura şablonu                                                                                  | orta                        | en çok kazanç: rhf/zod + taslak + tevkifat tutarlılığı                                 |
| **3** | Satınalma siparişi + satınalma talebi                                                                                           | orta                        | PO'nun tevkifat/başlık indirimi eksiği kapanır                                         |
| **4** | Stok fişi + cam levha kabulü                                                                                                    | düşük                       | fiyatsız varyant → `DocumentLineTable`'ın "fiyat kolonu opsiyonel" yeteneğini kanıtlar |
| **5** | Sevkiyat + alacak dekontu (türetilmiş kalem modu)                                                                               | düşük                       | salt-okunur kalem + miktar kısma                                                       |
| **6** | B2B `NewOrderForm` (**kopya**, çapraz import yok)                                                                               | orta                        | admin `src/`'ten import YASAK; b2b kendi `shared/`'ına kopyalanır                      |
| —     | Yevmiye fişi                                                                                                                    | —                           | **kapsam dışı** önerilir (borç/alacak modeli farklı)                                   |

---

## 6. Ölçülebilir hedefler

- `OrderFormModal` 1250 → **< 400 satır** (CLAUDE.md §1.6).
- Kalem yöneten ekranlarda `useState` dizisi sayısı 10 → 0 (yevmiye hariç).
- Taslak otomatik kaydetme kullanan ekran 1 → en az 5.
- Toplam hesabı yazıcı sayısı: FE'de 1 (saf fonksiyon), BE'de 1 (`Recalculate`) — bugün FE'de ekran başına ayrı.

---

## 7. Bu envanterin kanıtları

- 11 ekranın dosya yolu ve satır sayısı `wc -l` ile ölçüldü.
- `useFieldArray` / `zodResolver` / `ProductPicker` / `CurrencySelect` / `useState` sayımları `grep -c` ile ekran başına çıkarıldı: `useFieldArray` ve `zodResolver` yalnız `OrderFormModal`'da bulundu.
- `useDraftAutosave`'in tek tüketicisi `OrderFormModal`; `useModalClose` 8, `useBackdropClick` 5 tüketici.
- Adım 0 kapsamı gereği **hiçbir dosya değiştirilmedi**.
