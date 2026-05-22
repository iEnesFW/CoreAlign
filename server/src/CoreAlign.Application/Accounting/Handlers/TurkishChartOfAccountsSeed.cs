using CoreAlign.Domain.Enums;

namespace CoreAlign.Application.Accounting.Handlers;

/// <summary>
/// Tek Düzen Hesap Planı (TDHP) — Turkish official chart of accounts. This
/// seed covers the standard main / sub headers down to the most-used postable
/// leaves; tenants extend it as needed. Codes follow the canonical pattern:
///   1 → 1xx → 1xx.xx → 1xx.xx.xxx (deeper levels are tenant-specific).
/// </summary>
internal static class TurkishChartOfAccountsSeed
{
    internal sealed record Entry(string Code, string Name, AccountType Type, int Level, bool IsPostable);

    /// <summary>
    /// Returns the parent code by stripping the last segment of a dotted code.
    /// "100.01" → "100", "100" → "1" (single-char top class), "1" → null.
    /// </summary>
    public static string? ParentCodeOf(string code)
    {
        var dot = code.LastIndexOf('.');
        if (dot > 0) return code[..dot];
        // Class-level rollup: "100" rolls up under "1" (Dönen Varlıklar).
        if (code.Length > 1 && code.All(char.IsDigit)) return code[..1];
        return null;
    }

    public static IReadOnlyList<Entry> Entries { get; } = new List<Entry>
    {
        // ============ 1xx DÖNEN VARLIKLAR ============
        new("1",      "Dönen Varlıklar",                              AccountType.Asset, 1, false),
        new("10",     "Hazır Değerler",                               AccountType.Asset, 2, false),
        new("100",    "Kasa",                                          AccountType.Asset, 3, true),
        new("101",    "Alınan Çekler",                                 AccountType.Asset, 3, true),
        new("102",    "Bankalar",                                      AccountType.Asset, 3, true),
        new("103",    "Verilen Çekler ve Ödeme Emirleri (-)",          AccountType.Asset, 3, true),
        new("108",    "Diğer Hazır Değerler",                          AccountType.Asset, 3, true),

        new("11",     "Menkul Kıymetler",                              AccountType.Asset, 2, false),
        new("110",    "Hisse Senetleri",                               AccountType.Asset, 3, true),
        new("111",    "Özel Kesim Tahvil, Senet ve Bonoları",          AccountType.Asset, 3, true),
        new("112",    "Kamu Kesimi Tahvil, Senet ve Bonoları",         AccountType.Asset, 3, true),
        new("118",    "Diğer Menkul Kıymetler",                        AccountType.Asset, 3, true),

        new("12",     "Ticari Alacaklar",                              AccountType.Asset, 2, false),
        new("120",    "Alıcılar",                                      AccountType.Asset, 3, true),
        new("121",    "Alacak Senetleri",                              AccountType.Asset, 3, true),
        new("122",    "Alacak Senetleri Reeskontu (-)",                AccountType.Asset, 3, true),
        new("126",    "Verilen Depozito ve Teminatlar",                AccountType.Asset, 3, true),
        new("127",    "Diğer Ticari Alacaklar",                        AccountType.Asset, 3, true),
        new("128",    "Şüpheli Ticari Alacaklar",                      AccountType.Asset, 3, true),
        new("129",    "Şüpheli Ticari Alacaklar Karşılığı (-)",        AccountType.Asset, 3, true),

        new("13",     "Diğer Alacaklar",                               AccountType.Asset, 2, false),
        new("131",    "Ortaklardan Alacaklar",                         AccountType.Asset, 3, true),
        new("132",    "İştiraklerden Alacaklar",                       AccountType.Asset, 3, true),
        new("133",    "Bağlı Ortaklıklardan Alacaklar",                AccountType.Asset, 3, true),
        new("135",    "Personelden Alacaklar",                         AccountType.Asset, 3, true),
        new("136",    "Diğer Çeşitli Alacaklar",                       AccountType.Asset, 3, true),

        new("15",     "Stoklar",                                       AccountType.Asset, 2, false),
        new("150",    "İlk Madde ve Malzeme",                          AccountType.Asset, 3, true),
        new("151",    "Yarı Mamuller - Üretim",                        AccountType.Asset, 3, true),
        new("152",    "Mamuller",                                      AccountType.Asset, 3, true),
        new("153",    "Ticari Mallar",                                 AccountType.Asset, 3, true),
        new("157",    "Diğer Stoklar",                                 AccountType.Asset, 3, true),
        new("158",    "Stok Değer Düşüklüğü Karşılığı (-)",            AccountType.Asset, 3, true),
        new("159",    "Verilen Sipariş Avansları",                     AccountType.Asset, 3, true),

        new("19",     "Diğer Dönen Varlıklar",                         AccountType.Asset, 2, false),
        new("190",    "Devreden KDV",                                  AccountType.Asset, 3, true),
        new("191",    "İndirilecek KDV",                               AccountType.Asset, 3, true),
        new("192",    "Diğer KDV",                                     AccountType.Asset, 3, true),
        new("193",    "Peşin Ödenen Vergi ve Fonlar",                  AccountType.Asset, 3, true),
        new("195",    "İş Avansları",                                  AccountType.Asset, 3, true),
        new("196",    "Personel Avansları",                            AccountType.Asset, 3, true),
        new("197",    "Sayım ve Tesellüm Noksanları",                  AccountType.Asset, 3, true),

        // ============ 2xx DURAN VARLIKLAR ============
        new("2",      "Duran Varlıklar",                               AccountType.Asset, 1, false),
        new("25",     "Maddi Duran Varlıklar",                         AccountType.Asset, 2, false),
        new("250",    "Arazi ve Arsalar",                              AccountType.Asset, 3, true),
        new("252",    "Binalar",                                       AccountType.Asset, 3, true),
        new("253",    "Tesis, Makine ve Cihazlar",                     AccountType.Asset, 3, true),
        new("254",    "Taşıtlar",                                      AccountType.Asset, 3, true),
        new("255",    "Demirbaşlar",                                   AccountType.Asset, 3, true),
        new("257",    "Birikmiş Amortismanlar (-)",                    AccountType.Asset, 3, true),

        new("26",     "Maddi Olmayan Duran Varlıklar",                 AccountType.Asset, 2, false),
        new("260",    "Haklar",                                        AccountType.Asset, 3, true),
        new("261",    "Şerefiye",                                      AccountType.Asset, 3, true),
        new("268",    "Birikmiş Amortismanlar (-)",                    AccountType.Asset, 3, true),

        // ============ 3xx KISA VADELİ YABANCI KAYNAKLAR ============
        new("3",      "Kısa Vadeli Yabancı Kaynaklar",                 AccountType.Liability, 1, false),
        new("30",     "Mali Borçlar",                                  AccountType.Liability, 2, false),
        new("300",    "Banka Kredileri",                               AccountType.Liability, 3, true),
        new("303",    "Uzun Vadeli Kredilerin Anapara Taksitleri",     AccountType.Liability, 3, true),

        new("32",     "Ticari Borçlar",                                AccountType.Liability, 2, false),
        new("320",    "Satıcılar",                                     AccountType.Liability, 3, true),
        new("321",    "Borç Senetleri",                                AccountType.Liability, 3, true),
        new("326",    "Alınan Depozito ve Teminatlar",                 AccountType.Liability, 3, true),
        new("329",    "Diğer Ticari Borçlar",                          AccountType.Liability, 3, true),

        new("33",     "Diğer Borçlar",                                 AccountType.Liability, 2, false),
        new("331",    "Ortaklara Borçlar",                             AccountType.Liability, 3, true),
        new("335",    "Personele Borçlar",                             AccountType.Liability, 3, true),

        new("34",     "Alınan Avanslar",                               AccountType.Liability, 2, false),
        new("340",    "Alınan Sipariş Avansları",                      AccountType.Liability, 3, true),

        new("36",     "Ödenecek Vergi ve Diğer Yükümlülükler",         AccountType.Liability, 2, false),
        new("360",    "Ödenecek Vergi ve Fonlar",                      AccountType.Liability, 3, true),
        new("361",    "Ödenecek Sosyal Güvenlik Kesintileri",          AccountType.Liability, 3, true),

        new("39",     "Diğer Kısa Vadeli Yabancı Kaynaklar",           AccountType.Liability, 2, false),
        new("391",    "Hesaplanan KDV",                                AccountType.Liability, 3, true),
        new("392",    "Diğer KDV",                                     AccountType.Liability, 3, true),
        new("393",    "Merkez ve Şubeler Cari Hesabı",                 AccountType.Liability, 3, true),

        // ============ 4xx UZUN VADELİ YABANCI KAYNAKLAR ============
        new("4",      "Uzun Vadeli Yabancı Kaynaklar",                 AccountType.Liability, 1, false),
        new("40",     "Mali Borçlar",                                  AccountType.Liability, 2, false),
        new("400",    "Banka Kredileri",                               AccountType.Liability, 3, true),

        new("42",     "Ticari Borçlar",                                AccountType.Liability, 2, false),
        new("420",    "Satıcılar",                                     AccountType.Liability, 3, true),

        // ============ 5xx ÖZ KAYNAKLAR ============
        new("5",      "Öz Kaynaklar",                                  AccountType.Equity, 1, false),
        new("50",     "Ödenmiş Sermaye",                               AccountType.Equity, 2, false),
        new("500",    "Sermaye",                                       AccountType.Equity, 3, true),

        new("54",     "Kâr Yedekleri",                                 AccountType.Equity, 2, false),
        new("540",    "Yasal Yedekler",                                AccountType.Equity, 3, true),

        new("57",     "Geçmiş Yıllar Kârları/Zararları",               AccountType.Equity, 2, false),
        new("570",    "Geçmiş Yıllar Kârları",                         AccountType.Equity, 3, true),
        new("580",    "Geçmiş Yıllar Zararları (-)",                   AccountType.Equity, 3, true),

        new("59",     "Dönem Net Kârı / Zararı",                       AccountType.Equity, 2, false),
        new("590",    "Dönem Net Kârı",                                AccountType.Equity, 3, true),
        new("591",    "Dönem Net Zararı (-)",                          AccountType.Equity, 3, true),

        // ============ 6xx GELİR TABLOSU HESAPLARI ============
        new("6",      "Gelir Tablosu Hesapları",                       AccountType.Revenue, 1, false),
        new("60",     "Brüt Satışlar",                                 AccountType.Revenue, 2, false),
        new("600",    "Yurtiçi Satışlar",                              AccountType.Revenue, 3, true),
        new("601",    "Yurtdışı Satışlar",                             AccountType.Revenue, 3, true),
        new("602",    "Diğer Gelirler",                                AccountType.Revenue, 3, true),

        new("61",     "Satış İndirimleri (-)",                         AccountType.Revenue, 2, false),
        new("610",    "Satıştan İadeler (-)",                          AccountType.Revenue, 3, true),
        new("611",    "Satış İskontoları (-)",                         AccountType.Revenue, 3, true),

        new("62",     "Satışların Maliyeti (-)",                       AccountType.CostOfGoodsSold, 2, false),
        new("620",    "Satılan Mamuller Maliyeti (-)",                 AccountType.CostOfGoodsSold, 3, true),
        new("621",    "Satılan Ticari Mallar Maliyeti (-)",            AccountType.CostOfGoodsSold, 3, true),
        new("622",    "Satılan Hizmet Maliyeti (-)",                   AccountType.CostOfGoodsSold, 3, true),

        new("63",     "Faaliyet Giderleri (-)",                        AccountType.Expense, 2, false),
        new("630",    "Araştırma ve Geliştirme Giderleri (-)",         AccountType.Expense, 3, true),
        new("631",    "Pazarlama, Satış ve Dağıtım Giderleri (-)",     AccountType.Expense, 3, true),
        new("632",    "Genel Yönetim Giderleri (-)",                   AccountType.Expense, 3, true),

        new("64",     "Diğer Faaliyetlerden Olağan Gelirler",          AccountType.Revenue, 2, false),
        new("642",    "Faiz Gelirleri",                                AccountType.Revenue, 3, true),
        new("646",    "Kambiyo Kârları",                               AccountType.Revenue, 3, true),

        new("65",     "Diğer Faaliyetlerden Olağan Giderler (-)",      AccountType.Expense, 2, false),
        new("656",    "Kambiyo Zararları (-)",                         AccountType.Expense, 3, true),
        new("659",    "Diğer Olağan Gider ve Zararlar (-)",            AccountType.Expense, 3, true),

        new("66",     "Finansman Giderleri (-)",                       AccountType.Expense, 2, false),
        new("660",    "Kısa Vadeli Borçlanma Giderleri (-)",           AccountType.Expense, 3, true),
        new("661",    "Uzun Vadeli Borçlanma Giderleri (-)",           AccountType.Expense, 3, true),

        new("67",     "Olağan Dışı Gelir ve Kârlar",                   AccountType.Revenue, 2, false),
        new("671",    "Önceki Dönem Gelir ve Kârları",                 AccountType.Revenue, 3, true),
        new("679",    "Diğer Olağan Dışı Gelir ve Kârlar",             AccountType.Revenue, 3, true),

        new("68",     "Olağan Dışı Gider ve Zararlar (-)",             AccountType.Expense, 2, false),
        new("681",    "Önceki Dönem Gider ve Zararları (-)",           AccountType.Expense, 3, true),
        new("689",    "Diğer Olağan Dışı Gider ve Zararlar (-)",       AccountType.Expense, 3, true),

        new("69",     "Dönem Net Kârı veya Zararı",                    AccountType.Revenue, 2, false),
        new("690",    "Dönem Kârı veya Zararı",                        AccountType.Revenue, 3, true),
        new("691",    "Dönem Kârı Vergi ve Diğer Yasal Yükümlülük Karş. (-)", AccountType.Expense, 3, true),
        new("692",    "Dönem Net Kârı veya Zararı",                    AccountType.Revenue, 3, true),

        // ============ 7xx MALİYET HESAPLARI (kısa) ============
        new("7",      "Maliyet Hesapları",                             AccountType.Expense, 1, false),
        new("70",     "Maliyet Muhasebesi Bağlantı Hesapları",         AccountType.Expense, 2, false),
        new("700",    "Maliyet Muhasebesi Bağlantı Hesabı",            AccountType.Expense, 3, true),
        new("710",    "Direkt İlk Madde ve Malzeme Giderleri",         AccountType.Expense, 3, true),
        new("720",    "Direkt İşçilik Giderleri",                      AccountType.Expense, 3, true),
        new("730",    "Genel Üretim Giderleri",                        AccountType.Expense, 3, true),

        // ============ 9xx NAZIM HESAPLARI ============
        new("9",      "Nazım Hesaplar",                                AccountType.Memorandum, 1, false),
        new("90",     "Garantili Olarak Üstlenilen Yükümlülükler",     AccountType.Memorandum, 2, false),
        new("900",    "Verilen Teminat Mektupları",                    AccountType.Memorandum, 3, true),
        new("901",    "Alınan Teminat Mektupları",                     AccountType.Memorandum, 3, true),
    };
}
