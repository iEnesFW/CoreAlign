using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Interfaces;
using CoreAlign.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CoreAlign.API.HostedServices;

// WHY: GİB "UBL-TR Kod Listeleri V 1.42" (Mart 2026) ile birebir doğrulandı; oranlar dönemsel değişir — go-live öncesi SMMM onayı şart.
public static class GibCodeSeed
{
    private sealed record W(string Code, string Name, WithholdingKind Kind, int Num, int Denom, DateOnly ValidFrom);

    private sealed record E(string Code, string Name, string? LawRef, VatExemptionKind Kind);

    private static readonly DateOnly Teblig35 = new(2021, 3, 1);
    private static readonly DateOnly Teblig41 = new(2022, 5, 1);
    private static readonly DateOnly Teblig45 = new(2023, 7, 7);

    private static readonly W[] WithholdingCodes =
    [
        new("601", "Yapım İşleri ile Bu İşlerle Birlikte İfa Edilen Mühendislik-Mimarlık ve Etüt-Proje Hizmetleri", WithholdingKind.Partial, 4, 10, Teblig35),
        new("602", "Etüt, Plan-Proje, Danışmanlık, Denetim ve Benzeri Hizmetler", WithholdingKind.Partial, 9, 10, Teblig35),
        new("603", "Makine, Teçhizat, Demirbaş ve Taşıtlara Ait Tadil, Bakım ve Onarım Hizmetleri", WithholdingKind.Partial, 7, 10, Teblig35),
        new("604", "Yemek Servis Hizmeti", WithholdingKind.Partial, 5, 10, Teblig35),
        new("605", "Organizasyon Hizmeti", WithholdingKind.Partial, 5, 10, Teblig35),
        new("606", "İşgücü Temin Hizmetleri", WithholdingKind.Partial, 9, 10, Teblig35),
        new("607", "Özel Güvenlik Hizmeti", WithholdingKind.Partial, 9, 10, Teblig35),
        new("608", "Yapı Denetim Hizmetleri", WithholdingKind.Partial, 9, 10, Teblig35),
        new("609", "Fason Olarak Yaptırılan Tekstil ve Konfeksiyon İşleri, Çanta ve Ayakkabı Dikim İşleri ve Bu İşlere Aracılık Hizmetleri", WithholdingKind.Partial, 7, 10, Teblig35),
        new("610", "Turistik Mağazalara Verilen Müşteri Bulma / Götürme Hizmetleri", WithholdingKind.Partial, 9, 10, Teblig35),
        new("611", "Spor Kulüplerinin Yayın, Reklâm ve İsim Hakkı Gelirlerine Konu İşlemleri", WithholdingKind.Partial, 9, 10, Teblig35),
        new("612", "Temizlik Hizmeti", WithholdingKind.Partial, 9, 10, Teblig35),
        new("613", "Çevre ve Bahçe Bakım Hizmetleri", WithholdingKind.Partial, 9, 10, Teblig35),
        new("614", "Servis Taşımacılığı Hizmeti", WithholdingKind.Partial, 5, 10, Teblig35),
        new("615", "Her Türlü Baskı ve Basım Hizmetleri", WithholdingKind.Partial, 7, 10, Teblig35),
        new("616", "Diğer Hizmetler [KDVGUT-(I/C-2.1.3.2.13)]", WithholdingKind.Partial, 5, 10, Teblig35),
        new("617", "Hurda Metalden Elde Edilen Külçe Teslimleri", WithholdingKind.Partial, 7, 10, Teblig35),
        new("618", "Hurda Metalden Elde Edilenler Dışındaki Bakır, Çinko, Demir Çelik, Alüminyum ve Kurşun Külçe Teslimleri", WithholdingKind.Partial, 7, 10, Teblig35),
        new("619", "Bakır, Çinko ve Alüminyum Ürünlerinin Teslimi", WithholdingKind.Partial, 7, 10, Teblig35),
        new("620", "İstisnadan Vazgeçenlerin Hurda ve Atık Teslimi", WithholdingKind.Partial, 7, 10, Teblig35),
        new("621", "Metal, Plastik, Lastik, Kauçuk, Kâğıt ve Cam Hurda ve Atıklardan Elde Edilen Hammadde Teslimi", WithholdingKind.Partial, 9, 10, Teblig35),
        new("622", "Pamuk, Tiftik, Yün ve Yapağı ile Ham Post ve Deri Teslimleri", WithholdingKind.Partial, 9, 10, Teblig35),
        new("623", "Ağaç ve Orman Ürünleri Teslimi", WithholdingKind.Partial, 5, 10, Teblig35),
        new("624", "Yük Taşımacılığı Hizmeti [KDVGUT-(I/C-2.1.3.2.11)]", WithholdingKind.Partial, 2, 10, Teblig35),
        new("625", "Ticari Reklam Hizmetleri [KDVGUT-(I/C-2.1.3.2.15)]", WithholdingKind.Partial, 3, 10, Teblig35),
        new("626", "Diğer Teslimler [KDVGUT-(I/C-2.1.3.3.7)]", WithholdingKind.Partial, 2, 10, Teblig35),
        new("627", "Demir-Çelik Ürünlerinin Teslimi [KDVGUT-(I/C-2.1.3.3.8)]", WithholdingKind.Partial, 5, 10, Teblig45),
        new("801", "Yapım İşleri ile Bu İşlerle Birlikte İfa Edilen Mühendislik-Mimarlık ve Etüt-Proje Hizmetleri [KDVGUT-(I/C-2.1.3.2.1)]", WithholdingKind.Full, 10, 10, Teblig41),
        new("802", "Etüt, Plan-Proje, Danışmanlık, Denetim ve Benzeri Hizmetler [KDVGUT-(I/C-2.1.3.2.2)]", WithholdingKind.Full, 10, 10, Teblig41),
        new("803", "Makine, Teçhizat, Demirbaş ve Taşıtlara Ait Tadil, Bakım ve Onarım Hizmetleri [KDVGUT-(I/C-2.1.3.2.3)]", WithholdingKind.Full, 10, 10, Teblig41),
        new("804", "Yemek Servis Hizmeti [KDVGUT-(I/C-2.1.3.2.4)]", WithholdingKind.Full, 10, 10, Teblig41),
        new("805", "Organizasyon Hizmeti [KDVGUT-(I/C-2.1.3.2.4)]", WithholdingKind.Full, 10, 10, Teblig41),
        new("806", "İşgücü Temin Hizmetleri [KDVGUT-(I/C-2.1.3.2.5)]", WithholdingKind.Full, 10, 10, Teblig41),
        new("807", "Özel Güvenlik Hizmeti [KDVGUT-(I/C-2.1.3.2.5)]", WithholdingKind.Full, 10, 10, Teblig41),
        new("808", "Yapı Denetim Hizmetleri [KDVGUT-(I/C-2.1.3.2.6)]", WithholdingKind.Full, 10, 10, Teblig41),
        new("809", "Fason Olarak Yaptırılan Tekstil ve Konfeksiyon İşleri, Çanta ve Ayakkabı Dikim İşleri ve Bu İşlere Aracılık Hizmetleri [KDVGUT-(I/C-2.1.3.2.7)]", WithholdingKind.Full, 10, 10, Teblig41),
        new("810", "Turistik Mağazalara Verilen Müşteri Bulma/Götürme Hizmetleri [KDVGUT-(I/C-2.1.3.2.8)]", WithholdingKind.Full, 10, 10, Teblig41),
        new("811", "Spor Kulüplerinin Yayın, Reklâm ve İsim Hakkı Gelirlerine Konu İşlemleri [KDVGUT-(I/C-2.1.3.2.9)]", WithholdingKind.Full, 10, 10, Teblig41),
        new("812", "Temizlik Hizmeti [KDVGUT-(I/C-2.1.3.2.10)]", WithholdingKind.Full, 10, 10, Teblig41),
        new("813", "Çevre ve Bahçe Bakım Hizmetleri [KDVGUT-(I/C-2.1.3.2.10)]", WithholdingKind.Full, 10, 10, Teblig41),
        new("814", "Servis Taşımacılığı Hizmeti [KDVGUT-(I/C-2.1.3.2.11)]", WithholdingKind.Full, 10, 10, Teblig41),
        new("815", "Her Türlü Baskı ve Basım Hizmetleri [KDVGUT-(I/C-2.1.3.2.12)]", WithholdingKind.Full, 10, 10, Teblig41),
        new("816", "Hurda Metalden Elde Edilen Külçe Teslimleri [KDVGUT-(I/C-2.1.3.3.1)]", WithholdingKind.Full, 10, 10, Teblig41),
        new("817", "Hurda Metalden Elde Edilenler Dışındaki Bakır, Çinko, Demir Çelik, Alüminyum ve Kurşun Külçe Teslimi [KDVGUT-(I/C-2.1.3.3.1)]", WithholdingKind.Full, 10, 10, Teblig41),
        new("818", "Bakır, Çinko, Alüminyum ve Kurşun Ürünlerinin Teslimi [KDVGUT-(I/C-2.1.3.3.2)]", WithholdingKind.Full, 10, 10, Teblig41),
        new("819", "İstisnadan Vazgeçenlerin Hurda ve Atık Teslimi [KDVGUT-(I/C-2.1.3.3.3)]", WithholdingKind.Full, 10, 10, Teblig41),
        new("820", "Metal, Plastik, Lastik, Kauçuk, Kâğıt ve Cam Hurda ve Atıklardan Elde Edilen Hammadde Teslimi [KDVGUT-(I/C-2.1.3.3.4)]", WithholdingKind.Full, 10, 10, Teblig41),
        new("821", "Pamuk, Tiftik, Yün ve Yapağı ile Ham Post ve Deri Teslimleri [KDVGUT-(I/C-2.1.3.3.5)]", WithholdingKind.Full, 10, 10, Teblig41),
        new("822", "Ağaç ve Orman Ürünleri Teslimi [KDVGUT-(I/C-2.1.3.3.6)]", WithholdingKind.Full, 10, 10, Teblig41),
        new("823", "Yük Taşımacılığı Hizmeti [KDVGUT-(I/C-2.1.3.2.11)]", WithholdingKind.Full, 10, 10, Teblig41),
        new("824", "Ticari Reklam Hizmetleri [KDVGUT-(I/C-2.1.3.2.15)]", WithholdingKind.Full, 10, 10, Teblig41),
        new("825", "Demir-Çelik Ürünlerinin Teslimi [KDVGUT-(I/C-2.1.3.3.8)]", WithholdingKind.Full, 10, 10, Teblig41),
    ];

    private static readonly E[] ExemptionCodes =
    [
        new("201", "Kültür ve Eğitim Amacı Taşıyan İşlemler", "KDVK 17/1", VatExemptionKind.Partial),
        new("202", "Sağlık, Çevre ve Sosyal Yardım Amaçlı İşlemler", "KDVK 17/2-a", VatExemptionKind.Partial),
        new("204", "Yabancı Diplomatik Organ ve Hayır Kurumlarının Yapacakları Bağışlarla İlgili Mal ve Hizmet Alışları", "KDVK 17/2-c", VatExemptionKind.Partial),
        new("205", "Taşınmaz Kültür Varlıklarına İlişkin Teslimler ve Mimarlık Hizmetleri", "KDVK 17/2-d", VatExemptionKind.Partial),
        new("206", "Mesleki Kuruluşların İşlemleri", "KDVK 17/2-e", VatExemptionKind.Partial),
        new("207", "Askeri Fabrika, Tersane ve Atölyelerin İşlemleri", "KDVK 17/3", VatExemptionKind.Partial),
        new("208", "Birleşme, Devir, Dönüşüm ve Bölünme İşlemleri", "KDVK 17/4-c", VatExemptionKind.Partial),
        new("209", "Banka ve Sigorta Muameleleri Vergisi Kapsamına Giren İşlemler", "KDVK 17/4-e", VatExemptionKind.Partial),
        new("211", "Zirai Amaçlı Su Teslimleri ile Köy Tüzel Kişiliklerince Yapılan İçme Suyu Teslimleri", "KDVK 17/4-h", VatExemptionKind.Partial),
        new("212", "Serbest Bölgelerde Verilen Hizmetler", "KDVK 17/4-ı", VatExemptionKind.Partial),
        new("213", "Boru Hattı ile Yapılan Petrol ve Gaz Taşımacılığı", "KDVK 17/4-j", VatExemptionKind.Partial),
        new("214", "Organize Sanayi Bölgelerindeki Arsa ve İşyeri Teslimleri ile Konut Yapı Kooperatiflerinin Üyelerine Konut Teslimleri", "KDVK 17/4-k", VatExemptionKind.Partial),
        new("215", "Varlık Yönetim Şirketlerinin İşlemleri", "KDVK 17/4-l", VatExemptionKind.Partial),
        new("216", "Tasarruf Mevduatı Sigorta Fonunun İşlemleri", "KDVK 17/4-m", VatExemptionKind.Partial),
        new("217", "Basın-Yayın ve Enformasyon Genel Müdürlüğüne Verilen Haber Hizmetleri", "KDVK 17/4-n", VatExemptionKind.Partial),
        new("218", "Gümrük Antrepoları, Geçici Depolama Yerleri ile Gümrüklü Sahalarda Vergisiz Satış Yapılan İşyeri, Depo ve Ardiye Gibi Bağımsız Birimlerin Kiralanması", "KDVK 17/4-o", VatExemptionKind.Partial),
        new("219", "Hazine, Toplu Konut İdaresi Başkanlığı, Belediyeler, İl Özel İdareleri ve Yatırım İzleme ve Koordinasyon Başkanlıklarının İşlemleri", "KDVK 17/4-p", VatExemptionKind.Partial),
        new("220", "İki Tam Yıl Süreyle Sahip Olunan Taşınmaz ve İştirak Hisseleri ile 15/7/2023 Tarihinden Önce Kurumların Aktifinde Kayıtlı Taşınmaz Satışı", "KDVK 17/4-r", VatExemptionKind.Partial),
        new("221", "Konut Yapı Kooperatifleri, Belediyeler ve Sosyal Güvenlik Kuruluşlarına Verilen İnşaat Taahhüt Hizmeti", "KDVK Geçici 15", VatExemptionKind.Partial),
        new("223", "Teknoloji Geliştirme Bölgelerinde Yapılan İşlemler", "KDVK Geçici 20/1", VatExemptionKind.Partial),
        new("225", "Milli Eğitim Bakanlığına Yapılan Bilgisayar Bağışları ile İlgili Teslimler", "KDVK Geçici 23", VatExemptionKind.Partial),
        new("226", "Özel Okullar, Üniversite ve Yüksekokullar Tarafından Verilen Bedelsiz Eğitim ve Öğretim Hizmetleri", "KDVK 17/2-b", VatExemptionKind.Partial),
        new("227", "Kanunların Gösterdiği Gerek Üzerine Bedelsiz Olarak Yapılan Teslim ve Hizmetler", "KDVK 17/2-b", VatExemptionKind.Partial),
        new("228", "Kanunun (17/1) Maddesinde Sayılan Kurum ve Kuruluşlara Bedelsiz Olarak Yapılan Teslimler", "KDVK 17/2-b", VatExemptionKind.Partial),
        new("229", "Gıda Bankacılığı Faaliyetinde Bulunan Dernek ve Vakıflara Bağışlanan Gıda, Temizlik, Giyecek ve Yakacak Maddeleri", "KDVK 17/2-b", VatExemptionKind.Partial),
        new("230", "Külçe Altın, Külçe Gümüş ve Kıymetli Taşların Teslimi", "KDVK 17/4-g", VatExemptionKind.Partial),
        new("231", "Metal, Plastik, Lastik, Kauçuk, Kağıt, Cam Hurda ve Atıkların Teslimi", "KDVK 17/4-g", VatExemptionKind.Partial),
        new("232", "Döviz, Para, Damga Pulu, Değerli Kağıtlar, Hisse Senedi ve Tahvil Teslimleri", "KDVK 17/4-g", VatExemptionKind.Partial),
        new("234", "Konut Finansmanı Amacıyla Teminat Gösterilen ve İpotek Konulan Konutların Teslimi", "KDVK 17/4-ş", VatExemptionKind.Partial),
        new("235", "Transit ve Gümrük Antrepo Rejimleri ile Geçici Depolama ve Serbest Bölge Hükümlerinin Uygulandığı Malların Teslimi", "KDVK 16/1-c", VatExemptionKind.Partial),
        new("236", "Usulüne Göre Yürürlüğe Girmiş Uluslararası Anlaşmalar Kapsamındaki İstisnalar (İade Hakkı Tanınmayan)", "KDVK 19/2", VatExemptionKind.Partial),
        new("237", "5300 Sayılı Kanuna Göre Düzenlenen Ürün Senetlerinin İhtisas/Ticaret Borsaları Aracılığıyla İlk Teslimlerinden Sonraki Teslimi", "KDVK 17/4-t", VatExemptionKind.Partial),
        new("238", "Varlıkların Varlık Kiralama Şirketlerine Devri ile Bu Varlıkların Varlık Kiralama Şirketlerince Kiralanması ve Devralınan Kuruma Devri", "KDVK 17/4-u", VatExemptionKind.Partial),
        new("239", "Taşınmazların Finansal Kiralama Şirketlerine Devri, Finansal Kiralama Şirketi Tarafından Devredene Kiralanması ve Devri", "KDVK 17/4-y", VatExemptionKind.Partial),
        new("240", "Patentli veya Faydalı Model Belgeli Buluşa İlişkin Gayri Maddi Hakların Kiralanması, Devri ve Satışı", "KDVK 17/4-z", VatExemptionKind.Partial),
        new("241", "TürkAkım Gaz Boru Hattı Projesine İlişkin Anlaşmanın (9/b) Maddesinde Yer Alan Hizmetler", "Anlaşma 9/b", VatExemptionKind.Partial),
        new("242", "Gümrük Antrepoları, Geçici Depolama Yerleri ile Gümrüklü Sahalarda Verilen Ardiye, Depolama ve Terminal Hizmetleri", "KDVK 17/4-ö", VatExemptionKind.Partial),
        new("250", "Diğerleri (İade Hakkı Doğurmayan)", null, VatExemptionKind.Partial),
        new("301", "Mal İhracatı", "KDVK 11/1-a", VatExemptionKind.Full),
        new("302", "Hizmet İhracatı", "KDVK 11/1-a", VatExemptionKind.Full),
        new("303", "Roaming Hizmetleri", "KDVK 11/1-a", VatExemptionKind.Full),
        new("304", "Deniz, Hava ve Demiryolu Taşıma Araçlarının Teslimi ile İnşa, Tadil, Bakım ve Onarımları", "KDVK 13/a", VatExemptionKind.Full),
        new("305", "Deniz ve Hava Taşıma Araçları İçin Liman ve Hava Meydanlarında Yapılan Hizmetler", "KDVK 13/b", VatExemptionKind.Full),
        new("306", "Petrol Aramaları ve Petrol Boru Hatlarının İnşa ve Modernizasyonuna İlişkin Yapılan Teslim ve Hizmetler", "KDVK 13/c", VatExemptionKind.Full),
        new("307", "Maden Arama, Altın, Gümüş ve Platin Madenleri İçin İşletme, Zenginleştirme ve Rafinaj Faaliyetlerine İlişkin Teslim ve Hizmetler", "KDVK 13/c", VatExemptionKind.Full),
        new("308", "Teşvikli Yatırım Mallarının Teslimi", "KDVK 13/d", VatExemptionKind.Full),
        new("309", "Liman ve Hava Meydanlarının İnşası, Yenilenmesi ve Genişletilmesi", "KDVK 13/e", VatExemptionKind.Full),
        new("310", "Ulusal Güvenlik Amaçlı Teslim ve Hizmetler", "KDVK 13/f", VatExemptionKind.Full),
        new("311", "Uluslararası Taşımacılık", "KDVK 14/1", VatExemptionKind.Full),
        new("312", "Diplomatik Organ ve Misyonlara Yapılan Teslim ve Hizmetler", "KDVK 15/a", VatExemptionKind.Full),
        new("313", "Uluslararası Kuruluşlara Yapılan Teslim ve Hizmetler", "KDVK 15/b", VatExemptionKind.Full),
        new("314", "Usulüne Göre Yürürlüğe Girmiş Uluslararası Anlaşmalar Kapsamındaki İstisnalar", "KDVK 19/2", VatExemptionKind.Full),
        new("315", "İhraç Konusu Eşyayı Taşıyan Kamyon, Çekici ve Yarı Römorklara Yapılan Motorin Teslimleri", "KDVK 14/3", VatExemptionKind.Full),
        new("316", "Serbest Bölgelerdeki Müşteriler İçin Yapılan Fason Hizmetler", "KDVK 11/1-a", VatExemptionKind.Full),
        new("317", "Engellilerin Eğitimleri, Meslekleri ve Günlük Yaşamlarına İlişkin Araç-Gereç ve Bilgisayar Programları", "KDVK 17/4-s", VatExemptionKind.Full),
        new("318", "Yap-İşlet-Devret Projeleri, Kiralama Karşılığı Sağlık Tesisleri ve Eğitim Öğretim Tesisleri Projelerine İlişkin Teslim ve Hizmetler", "KDVK Geçici 29", VatExemptionKind.Full),
        new("319", "Başbakanlık Merkez Teşkilatına Yapılan Araç Teslimleri", "KDVK 13/g", VatExemptionKind.Full),
        new("320", "İSMEP Kapsamında İstanbul Proje Koordinasyon Birimine Yapılacak Teslim ve Hizmetler", "6111 s.K. Geçici 16", VatExemptionKind.Full),
        new("321", "BM, NATO Temsilcilikleri ve Bağlı Kuruluşları ile OECD'ye Resmi Kullanımları İçin Yapılacak Teslim ve Hizmetler", "KDVK Geçici 26", VatExemptionKind.Full),
        new("322", "Türkiye'de İkamet Etmeyenlere Özel Fatura ile Yapılan Teslimler (Bavul Ticareti)", "KDVK 11/1-a", VatExemptionKind.Full),
        new("323", "5300 Sayılı Kanuna Göre Düzenlenen Ürün Senetlerinin İhtisas/Ticaret Borsaları Aracılığıyla İlk Teslimi", "KDVK 13/ğ", VatExemptionKind.Full),
        new("324", "Türkiye Kızılay Derneğine Yapılan Teslim ve Hizmetler ile Türkiye Kızılay Derneğinin Teslim ve Hizmetleri", "KDVK 13/h", VatExemptionKind.Full),
        new("325", "Yem Teslimleri", "KDVK 13/ı", VatExemptionKind.Full),
        new("326", "Tescil Edilmiş Gübrelerin Teslimi", "KDVK 13/ı", VatExemptionKind.Full),
        new("327", "Tescilli Gübrelerin İçeriğindeki Hammaddelerin Gübre Üreticilerine Teslimi", "KDVK 13/ı", VatExemptionKind.Full),
        new("328", "Konut veya İşyeri Teslimleri", "KDVK 13/i", VatExemptionKind.Full),
        new("329", "FATİH Projesi Kapsamında Milli Eğitim Bakanlığına Yapılacak Mal Teslimi ve Hizmet İfası", "KDVK Geçici 38", VatExemptionKind.Full),
        new("330", "Organize Sanayi Bölgeleri ile Küçük Sanayi Sitelerinin İnşasına İlişkin Teslim ve Hizmetler", "KDVK 13/j", VatExemptionKind.Full),
        new("331", "Ar-Ge, Yenilik ve Tasarım Faaliyetlerinde Kullanılmak Üzere Yapılan Yeni Makina ve Teçhizat Teslimleri", "KDVK 13/m", VatExemptionKind.Full),
        new("332", "İmalat Sanayiinde Kullanılmak Üzere Yapılan Yeni Makina ve Teçhizat Teslimleri", "KDVK Geçici 39", VatExemptionKind.Full),
        new("333", "Genel/Özel Bütçeli Kamu İdarelerine, İl Özel İdarelerine, Belediyelere ve Köylere Bağışlanan Tesislerin İnşasına İlişkin İstisna", "KDVK 13/k", VatExemptionKind.Full),
        new("334", "Yabancılara Verilen Sağlık Hizmetlerinde İstisna", "KDVK 13/l", VatExemptionKind.Full),
        new("335", "Basılı Kitap ve Süreli Yayınların Teslimleri", "KDVK 13/n", VatExemptionKind.Full),
        new("336", "UEFA Müsabakaları Kapsamında Yapılacak Teslim ve Hizmetler", "KDVK Geçici 46", VatExemptionKind.Full),
        new("337", "TürkAkım Gaz Boru Hattı Projesine İlişkin Anlaşmanın (9/h) Maddesi Kapsamındaki Gaz Taşıma Hizmetleri", "Anlaşma 9/h", VatExemptionKind.Full),
        new("338", "İmalatçıların Mal İhracatları", "KDVK 11/1-a", VatExemptionKind.Full),
        new("339", "İmalat Sanayii ile Turizme Yönelik Yatırım Teşvik Belgesi Kapsamındaki İnşaat İşlerine İlişkin Teslim ve Hizmetler", "KDVK Geçici 37", VatExemptionKind.Full),
        new("340", "Elektrik Motorlu Taşıt Araçlarının Geliştirilmesine Yönelik Mühendislik Hizmetleri", "KDVK Geçici 42", VatExemptionKind.Full),
        new("341", "Afetzedelere Bağışlanacak Konutların İnşasına İlişkin İstisna", "KDVK Geçici 44", VatExemptionKind.Full),
        new("342", "Genel Bütçeli Kamu İdarelerine Bağışlanacak Taşınmazların İnşasına İlişkin İstisna", null, VatExemptionKind.Full),
        new("343", "Genel Bütçeli Kamu İdarelerine Bağışlanacak Konutların Yabancı Devlet Kurum ve Kuruluşlarına Teslimine İlişkin İstisna", null, VatExemptionKind.Full),
        new("344", "Milli Savunma ve İç Güvenlik İhtiyaçlarında Kullanılmak Üzere Taşıt Teslimi", "KDVK 13/o", VatExemptionKind.Full),
        new("350", "Diğerleri (İade Hakkı Doğuran)", null, VatExemptionKind.Full),
        new("351", "KDV - İstisna Olmayan Diğer", null, VatExemptionKind.NotSubject),
        new("701", "3065 Sayılı KDV Kanununun 11/1-c Maddesi Kapsamındaki İhraç Kayıtlı Satış", "KDVK 11/1-c", VatExemptionKind.ExportRegistered),
        new("702", "DİİB ve Geçici Kabul Rejimi Kapsamındaki Satışlar", "KDVK Geçici 17", VatExemptionKind.ExportRegistered),
        new("703", "4760 Sayılı ÖTV Kanununun 8/2 Maddesi Kapsamındaki İhraç Kayıtlı Satış", "ÖTVK 8/2", VatExemptionKind.ExportRegistered),
        new("704", "KDV Kanununun 11/1-c ve ÖTV Kanununun 8/2 Maddesi Kapsamındaki İhraç Kayıtlı Satış", "KDVK 11/1-c + ÖTVK 8/2", VatExemptionKind.ExportRegistered),
    ];

    public static async Task SeedGlobalAsync(IServiceProvider sp, CancellationToken ct)
    {
        var db = sp.GetRequiredService<CoreAlignDbContext>();
        var uow = sp.GetRequiredService<IUnitOfWork>();
        var changed = false;

        var existingWithholding = await db.WithholdingTaxCodes
            .IgnoreQueryFilters()
            .Where(x => x.TenantId == Guid.Empty)
            .ToDictionaryAsync(x => x.Code, ct);

        foreach (var entry in WithholdingCodes)
        {
            if (existingWithholding.TryGetValue(entry.Code, out var current))
            {
                if (current.Numerator != entry.Num || current.Denominator != entry.Denom)
                {
                    current.UpdateRate(entry.Num, entry.Denom, entry.ValidFrom);
                    changed = true;
                }

                continue;
            }

            await db.WithholdingTaxCodes.AddAsync(
                new WithholdingTaxCode(entry.Code, entry.Name, entry.Kind, entry.Num, entry.Denom, entry.ValidFrom)
                {
                    TenantId = Guid.Empty,
                },
                ct);
            changed = true;
        }

        var existingExemptions = await db.VatExemptionCodes
            .IgnoreQueryFilters()
            .Where(x => x.TenantId == Guid.Empty)
            .Select(x => x.Code)
            .ToListAsync(ct);
        var existingExemptionSet = existingExemptions.ToHashSet(StringComparer.Ordinal);

        foreach (var entry in ExemptionCodes)
        {
            if (existingExemptionSet.Contains(entry.Code))
            {
                continue;
            }

            await db.VatExemptionCodes.AddAsync(
                new VatExemptionCode(entry.Code, entry.Name, entry.LawRef, entry.Kind)
                {
                    TenantId = Guid.Empty,
                },
                ct);
            changed = true;
        }

        if (changed)
        {
            await uow.SaveChangesAsync(ct);
        }
    }
}
