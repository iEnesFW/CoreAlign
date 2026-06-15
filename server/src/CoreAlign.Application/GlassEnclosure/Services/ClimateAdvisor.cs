using CoreAlign.Application.GlassEnclosure.DTOs;
using CoreAlign.Domain.Entities.GlassEnclosure;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Application.GlassEnclosure.Services;

public class ClimateAdvisor : IClimateAdvisor
{
    private readonly IClimateZoneRepository _climateRepository;

    public ClimateAdvisor(IClimateZoneRepository climateRepository)
    {
        _climateRepository = climateRepository;
    }

    public async Task<ClimateRecommendationDto> RecommendAsync(
        string? city,
        string? postalCode,
        CancellationToken cancellationToken = default)
    {
        var zone = await ResolveZoneAsync(city, postalCode, cancellationToken);
        var notes = BuildNotes(zone);

        if (zone is null)
        {
            return new ClimateRecommendationDto(
                ClimateZoneId: null,
                ClimateZoneCode: null,
                ClimateZoneNameTr: null,
                ClimateZoneNameEn: null,
                CorrosionClass: null,
                RecommendsDoubleGlazing: false,
                RecommendsCorrosionResistantCoating: false,
                RecommendsSeismicSmallerPanel: false,
                Notes: notes);
        }

        return new ClimateRecommendationDto(
            ClimateZoneId: zone.Id,
            ClimateZoneCode: zone.Code,
            ClimateZoneNameTr: zone.NameTr,
            ClimateZoneNameEn: zone.NameEn,
            CorrosionClass: zone.CorrosionClass,
            RecommendsDoubleGlazing: zone.RecommendsDoubleGlazing,
            RecommendsCorrosionResistantCoating: zone.RecommendsCorrosionResistantCoating,
            RecommendsSeismicSmallerPanel: zone.RecommendsSeismicSmallerPanel,
            Notes: notes);
    }

    private async Task<ClimateZone?> ResolveZoneAsync(
        string? city,
        string? postalCode,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(postalCode) && postalCode.Length >= 2)
        {
            var prefix = postalCode.Substring(0, 2);
            var byPostal = await _climateRepository.FindByIlPrefixAsync(prefix, cancellationToken);
            if (byPostal is not null) return byPostal;
        }

        if (!string.IsNullOrWhiteSpace(city) && TurkishCityToIlPrefix.TryGetValue(city.Trim(), out var ilPrefix))
        {
            var byCity = await _climateRepository.FindByIlPrefixAsync(ilPrefix, cancellationToken);
            if (byCity is not null) return byCity;
        }

        return null;
    }

    private static List<string> BuildNotes(ClimateZone? zone)
    {
        var notes = new List<string>();
        if (zone is null)
        {
            notes.Add("Climate.NoZoneResolved");
            return notes;
        }

        if (zone.RecommendsDoubleGlazing) notes.Add("Climate.Recommendation.DoubleGlazing");
        if (zone.RecommendsCorrosionResistantCoating) notes.Add("Climate.Recommendation.CorrosionCoating");
        if (zone.RecommendsSeismicSmallerPanel) notes.Add("Climate.Recommendation.SeismicSmallerPanel");
        return notes;
    }

    private static readonly Dictionary<string, string> TurkishCityToIlPrefix = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Adana"] = "01", ["Adıyaman"] = "02", ["Afyonkarahisar"] = "03", ["Ağrı"] = "04",
        ["Amasya"] = "05", ["Ankara"] = "06", ["Antalya"] = "07", ["Artvin"] = "08",
        ["Aydın"] = "09", ["Balıkesir"] = "10", ["Bilecik"] = "11", ["Bingöl"] = "12",
        ["Bitlis"] = "13", ["Bolu"] = "14", ["Burdur"] = "15", ["Bursa"] = "16",
        ["Çanakkale"] = "17", ["Çankırı"] = "18", ["Çorum"] = "19", ["Denizli"] = "20",
        ["Diyarbakır"] = "21", ["Edirne"] = "22", ["Elazığ"] = "23", ["Erzincan"] = "24",
        ["Erzurum"] = "25", ["Eskişehir"] = "26", ["Gaziantep"] = "27", ["Giresun"] = "28",
        ["Gümüşhane"] = "29", ["Hakkari"] = "30", ["Hatay"] = "31", ["Isparta"] = "32",
        ["Mersin"] = "33", ["İstanbul"] = "34", ["İzmir"] = "35", ["Kars"] = "36",
        ["Kastamonu"] = "37", ["Kayseri"] = "38", ["Kırklareli"] = "39", ["Kırşehir"] = "40",
        ["Kocaeli"] = "41", ["Konya"] = "42", ["Kütahya"] = "43", ["Malatya"] = "44",
        ["Manisa"] = "45", ["Kahramanmaraş"] = "46", ["Mardin"] = "47", ["Muğla"] = "48",
        ["Muş"] = "49", ["Nevşehir"] = "50", ["Niğde"] = "51", ["Ordu"] = "52",
        ["Rize"] = "53", ["Sakarya"] = "54", ["Samsun"] = "55", ["Siirt"] = "56",
        ["Sinop"] = "57", ["Sivas"] = "58", ["Tekirdağ"] = "59", ["Tokat"] = "60",
        ["Trabzon"] = "61", ["Tunceli"] = "62", ["Şanlıurfa"] = "63", ["Uşak"] = "64",
        ["Van"] = "65", ["Yozgat"] = "66", ["Zonguldak"] = "67", ["Aksaray"] = "68",
        ["Bayburt"] = "69", ["Karaman"] = "70", ["Kırıkkale"] = "71", ["Batman"] = "72",
        ["Şırnak"] = "73", ["Bartın"] = "74", ["Ardahan"] = "75", ["Iğdır"] = "76",
        ["Yalova"] = "77", ["Karabük"] = "78", ["Kilis"] = "79", ["Osmaniye"] = "80",
        ["Düzce"] = "81",
    };
}
