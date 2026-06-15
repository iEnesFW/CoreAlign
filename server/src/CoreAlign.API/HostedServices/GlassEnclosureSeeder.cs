using System.Text.Json;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Entities.GlassEnclosure;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CoreAlign.API.HostedServices;

public static class GlassEnclosureSeeder
{
    public static async Task SeedGlobalAsync(IServiceProvider sp, CancellationToken ct)
    {
        var winds = sp.GetRequiredService<IWindZoneRepository>();
        var climates = sp.GetRequiredService<IClimateZoneRepository>();
        var uow = sp.GetRequiredService<IUnitOfWork>();
        var anyChange = false;

        foreach (var spec in WindZones)
        {
            var existing = await winds.GetByCodeAsync(spec.Code, ct);
            if (existing is not null) continue;
            await winds.AddAsync(new WindZone(
                spec.Code, spec.RegionTr, spec.RegionEn, spec.BasePa, spec.HeightFactor, spec.IsCoastal), ct);
            anyChange = true;
        }

        foreach (var spec in ClimateZones)
        {
            var existing = await climates.GetByCodeAsync(spec.Code, ct);
            if (existing is not null) continue;
            await climates.AddAsync(new ClimateZone(
                spec.Code, spec.NameTr, spec.NameEn, spec.WinterTempC, spec.HumidityPercent,
                spec.Corrosion, spec.RecDoubleGlazing, spec.RecCorrosionCoating, spec.RecSeismicSmaller,
                JsonSerializer.Serialize(spec.IlPrefixes)), ct);
            anyChange = true;
        }

        if (anyChange) await uow.SaveChangesAsync(ct);

        await ProjectTemplateSeeder.SeedSystemTemplatesAsync(sp, ct);
        await NotificationTemplateSeeder.SeedSystemTemplatesAsync(sp, ct);
    }

    public static async Task SeedTenantAsync(IServiceProvider sp, CancellationToken ct)
    {
        await SeedColorsAsync(sp, ct);
        await SeedGlassTypesAsync(sp, ct);
        await SeedBrandsAndSystemsAsync(sp, ct);
        await SeedHardwareAsync(sp, ct);
        await SeedNotificationTemplatesAsync(sp, ct);
        await SeedSettingsAsync(sp, ct);
    }

    private static async Task SeedColorsAsync(IServiceProvider sp, CancellationToken ct)
    {
        var repo = sp.GetRequiredService<IColorOptionRepository>();
        var uow = sp.GetRequiredService<IUnitOfWork>();
        var anyChange = false;

        foreach (var (code, name, ral, hex, finish, order) in RalColors)
        {
            var existing = await repo.GetByCodeAsync(code, ct);
            if (existing is not null) continue;
            await repo.AddAsync(new ColorOption(code, name, hex, finish, ral, 0m, order), ct);
            anyChange = true;
        }

        if (anyChange) await uow.SaveChangesAsync(ct);
    }

    private static async Task SeedGlassTypesAsync(IServiceProvider sp, CancellationToken ct)
    {
        var repo = sp.GetRequiredService<IGlassTypeRepository>();
        var uow = sp.GetRequiredService<IUnitOfWork>();
        var anyChange = false;

        foreach (var g in GlassTypes)
        {
            var existing = await repo.GetByCodeAsync(g.Code, ct);
            if (existing is not null) continue;
            await repo.AddAsync(new GlassType(
                g.Code, g.Name, g.ThicknessMm, g.Structure, g.PricePerM2, g.WeightKgPerM2,
                g.AllowablePa, g.MaxAreaM2, g.UValue, g.SoundDb, g.LayersJson), ct);
            anyChange = true;
        }

        if (anyChange) await uow.SaveChangesAsync(ct);
    }

    private static async Task SeedBrandsAndSystemsAsync(IServiceProvider sp, CancellationToken ct)
    {
        var brandRepo = sp.GetRequiredService<IBrandRepository>();
        var systemRepo = sp.GetRequiredService<IProfileSystemRepository>();
        var itemRepo = sp.GetRequiredService<IProfileItemRepository>();
        var colorRepo = sp.GetRequiredService<IColorOptionRepository>();
        var uow = sp.GetRequiredService<IUnitOfWork>();
        var anyChange = false;

        var existingBrands = (await brandRepo.ListAsync(isActive: null, ct))
            .ToDictionary(b => b.Code, StringComparer.OrdinalIgnoreCase);

        Brand EnsureBrand(string code, string name, string description)
        {
            if (existingBrands.TryGetValue(code, out var existing)) return existing;
            var brand = new Brand(code, name, description);
            brandRepo.AddAsync(brand, ct).GetAwaiter().GetResult();
            existingBrands[code] = brand;
            anyChange = true;
            return brand;
        }

        var albertGenau = EnsureBrand("ALBERTGENAU", "Albert Genau", "Premium cam balkon sistemleri");
        var vizyon = EnsureBrand("VIZYON", "Vizyon", "Cam balkon profil sistemleri");
        var winsa = EnsureBrand("WINSA", "Winsa", "Alüminyum doğrama ve cam balkon");

        if (anyChange) await uow.SaveChangesAsync(ct);

        var whiteColor = await colorRepo.GetByCodeAsync("WHT-9016", ct);
        var defaultColorId = whiteColor?.Id;

        foreach (var systemSpec in ProfileSystems)
        {
            var existing = await systemRepo.GetByCodeAsync(systemSpec.Code, ct);
            if (existing is not null) continue;

            var brand = systemSpec.BrandCode switch
            {
                "ALBERTGENAU" => albertGenau,
                "VIZYON" => vizyon,
                "WINSA" => winsa,
                _ => albertGenau,
            };

            var system = new ProfileSystem(
                code: systemSpec.Code,
                name: systemSpec.Name,
                brandId: brand.Id,
                systemType: systemSpec.SystemType,
                maxPanelWidthMm: systemSpec.MaxPanelWidthMm,
                maxPanelHeightMm: systemSpec.MaxPanelHeightMm,
                maxPanelWeightKg: systemSpec.MaxPanelWeightKg,
                supportedGlassThicknessesJson: JsonSerializer.Serialize(systemSpec.SupportedThicknesses),
                supportedOpeningsJson: JsonSerializer.Serialize(systemSpec.SupportedOpenings.Select(o => o.ToString()).ToArray()),
                certificationClass: systemSpec.Certification,
                thermalUValue: systemSpec.ThermalUValue,
                description: systemSpec.Description);
            await systemRepo.AddAsync(system, ct);
            await uow.SaveChangesAsync(ct);

            foreach (var i in systemSpec.Items)
            {
                var item = new ProfileItem(
                    systemId: system.Id,
                    role: i.Role,
                    code: i.Code,
                    name: i.Name,
                    stockBarLengthMm: i.StockBarLengthMm,
                    weightKgPerMeter: i.WeightKgPerMeter,
                    pricePerKg: i.PricePerKg,
                    defaultColorId: defaultColorId,
                    leadTimeDays: 7,
                    reorderPointMeters: 60m);
                await itemRepo.AddAsync(item, ct);
            }
            await uow.SaveChangesAsync(ct);
            anyChange = true;
        }
    }

    private static async Task SeedHardwareAsync(IServiceProvider sp, CancellationToken ct)
    {
        var brandRepo = sp.GetRequiredService<IBrandRepository>();
        var hardwareRepo = sp.GetRequiredService<IHardwareItemRepository>();
        var kitRepo = sp.GetRequiredService<IHardwareKitRepository>();
        var systemRepo = sp.GetRequiredService<IProfileSystemRepository>();
        var uow = sp.GetRequiredService<IUnitOfWork>();
        var anyChange = false;

        var albertGenau = (await brandRepo.ListAsync(isActive: null, ct)).FirstOrDefault(b => b.Code == "ALBERTGENAU");
        var vizyon = (await brandRepo.ListAsync(isActive: null, ct)).FirstOrDefault(b => b.Code == "VIZYON");
        if (albertGenau is null || vizyon is null) return;

        var slideMaster = await systemRepo.GetByCodeAsync("AG-SLIDEMASTER", ct);
        var vizyonGold = await systemRepo.GetByCodeAsync("VZ-GOLD", ct);
        if (slideMaster is null || vizyonGold is null) return;

        var compatibleAllJson = JsonSerializer.Serialize(new[] { slideMaster.Id, vizyonGold.Id });
        var compatibleSlidingJson = JsonSerializer.Serialize(new[] { slideMaster.Id });

        foreach (var h in HardwareItems(albertGenau.Id, vizyon.Id, compatibleAllJson, compatibleSlidingJson))
        {
            var existing = await hardwareRepo.GetByCodeAsync(h.Code, ct);
            if (existing is not null) continue;
            await hardwareRepo.AddAsync(new HardwareItem(
                h.Code, h.Name, h.Category, h.BrandId, h.Unit, h.UnitPrice,
                h.CompatibleSystemsJson, h.MaxLoadKg,
                preferredVendorId: null,
                vendorPartNumber: h.VendorPartNumber,
                leadTimeDays: 5,
                reorderPointQuantity: 20m), ct);
            anyChange = true;
        }
        if (anyChange) await uow.SaveChangesAsync(ct);

        await SeedHardwareKitAsync(sp, kitRepo, hardwareRepo, slideMaster.Id, "AG-SM-SLIDING-KIT", "Albert Genau SlideMaster Sürgü Kiti", new[]
        {
            ("AG-ROLLER-SPEED-HD", "panel_count * 2"),
            ("AG-LOCK-SPANISH", "panel_count - 1"),
            ("AG-HANDLE-STD", "panel_count - 1"),
            ("AG-BRUSH-SEAL", "ceil(run_length_mm / 1000) * 4"),
            ("AG-GASKET-EPDM", "ceil(run_length_mm / 1000) * 2"),
        }, ct);

        await SeedHardwareKitAsync(sp, kitRepo, hardwareRepo, vizyonGold.Id, "VZ-GOLD-SLIDING-KIT", "Vizyon Gold Sürgü Kiti", new[]
        {
            ("VZ-ROLLER-STD", "panel_count * 2"),
            ("AG-LOCK-SPANISH", "panel_count - 1"),
            ("AG-HANDLE-STD", "panel_count - 1"),
            ("AG-BRUSH-SEAL", "ceil(run_length_mm / 1000) * 4"),
        }, ct);
    }

    private static async Task SeedHardwareKitAsync(
        IServiceProvider sp,
        IHardwareKitRepository kitRepo,
        IHardwareItemRepository hardwareRepo,
        Guid systemId,
        string kitCode,
        string kitName,
        IEnumerable<(string HardwareCode, string Formula)> items,
        CancellationToken ct)
    {
        var uow = sp.GetRequiredService<IUnitOfWork>();
        var existing = await kitRepo.GetByCodeAsync(kitCode, ct);
        if (existing is not null) return;

        var kit = new HardwareKit(kitCode, kitName, systemId);
        await kitRepo.AddAsync(kit, ct);
        await uow.SaveChangesAsync(ct);

        var sort = 0;
        foreach (var (code, formula) in items)
        {
            var hardware = await hardwareRepo.GetByCodeAsync(code, ct);
            if (hardware is null) continue;
            kit.AddItem(new HardwareKitItem(kit.Id, hardware.Id, formula, sortOrder: sort++));
        }
        kitRepo.Update(kit);
        await uow.SaveChangesAsync(ct);
    }

    private static async Task SeedNotificationTemplatesAsync(IServiceProvider sp, CancellationToken ct)
    {
        var repo = sp.GetRequiredService<IGlassNotificationTemplateRepository>();
        var uow = sp.GetRequiredService<IUnitOfWork>();
        var anyChange = false;

        foreach (var t in NotificationTemplates)
        {
            var existing = (await repo.ListAsync(isActive: null, eventCode: t.EventCode, channel: t.Channel, locale: t.Locale, ct))
                .FirstOrDefault();
            if (existing is not null) continue;
            await repo.AddAsync(new GlassNotificationTemplate(
                code: $"GE-{t.EventCode}-{t.Channel}-{t.Locale}",
                eventCode: t.EventCode,
                channel: t.Channel,
                locale: t.Locale,
                bodyTemplate: t.Body,
                subjectTemplate: t.Subject), ct);
            anyChange = true;
        }

        if (anyChange) await uow.SaveChangesAsync(ct);
    }

    private static async Task SeedSettingsAsync(IServiceProvider sp, CancellationToken ct)
    {
        var repo = sp.GetRequiredService<IGlassEnclosureSettingsRepository>();
        var uow = sp.GetRequiredService<IUnitOfWork>();

        var existing = await repo.GetForCurrentTenantAsync(ct);
        if (existing is not null) return;

        var settings = await repo.GetOrCreateForCurrentTenantAsync(ct);
        settings.UpdateCore(
            defaultStockBarLengthMm: 6000,
            defaultJumboGlassWidthMm: 3210,
            defaultJumboGlassHeightMm: 2250,
            sawKerfMm: 5m,
            glassKerfMm: 4m,
            guillotineRequired: true,
            defaultWastePercent: 5m,
            laborCostPerM2: 450m,
            defaultMarginPercent: 22m);
        settings.UpdateField(fieldToleranceTopMm: 10, fieldToleranceSideMm: 5);
        settings.UpdateInstallation(
            transportRatePerKm: 8m,
            transportRatePerKg: 0.45m,
            scaffoldingRequiredFromFloor: 5,
            scaffoldingRatePerM2: 65m,
            craneRequiredFromFloor: 10,
            craneRatePerMeter: 22m,
            workshopDailyCapacityM2: 80m);
        settings.UpdateLocaleAndCommunication(
            defaultLocale: "tr-TR",
            defaultCurrency: "TRY",
            defaultPaymentTermsJson: JsonSerializer.Serialize(new[] { "pesin", "3_taksit", "6_taksit" }),
            whatsappBusinessPhoneId: null,
            notificationEmailFrom: null,
            quoteShareTokenTtlDays: 30,
            dataRetentionDays: 730);
        await uow.SaveChangesAsync(ct);
    }

    private record WindZoneSpec(string Code, string RegionTr, string RegionEn, decimal BasePa, decimal HeightFactor, bool IsCoastal);

    private static readonly WindZoneSpec[] WindZones =
    {
        new("TR-Z1-Inland", "Bölge 1 — İç", "Zone 1 — Inland", 800m, 1.00m, false),
        new("TR-Z2-Inland", "Bölge 2 — İç", "Zone 2 — Inland", 900m, 1.00m, false),
        new("TR-Z3-Inland", "Bölge 3 — İç", "Zone 3 — Inland", 1050m, 1.00m, false),
        new("TR-Z4-Inland", "Bölge 4 — İç", "Zone 4 — Inland", 1200m, 1.00m, false),
        new("TR-Z1-Coast", "Bölge 1 — Kıyı", "Zone 1 — Coast", 950m, 1.10m, true),
        new("TR-Z2-Coast", "Bölge 2 — Kıyı", "Zone 2 — Coast", 1100m, 1.10m, true),
        new("TR-Z3-Coast", "Bölge 3 — Kıyı", "Zone 3 — Coast", 1280m, 1.10m, true),
        new("TR-Z4-Coast", "Bölge 4 — Kıyı", "Zone 4 — Coast", 1500m, 1.15m, true),
    };

    private record ClimateZoneSpec(
        string Code, string NameTr, string NameEn,
        decimal WinterTempC, decimal HumidityPercent,
        CorrosionClass Corrosion,
        bool RecDoubleGlazing, bool RecCorrosionCoating, bool RecSeismicSmaller,
        string[] IlPrefixes);

    private static readonly ClimateZoneSpec[] ClimateZones =
    {
        new("TR-MARMARA", "Marmara", "Marmara", 6m, 75m, CorrosionClass.C3, true, false, false,
            new[] { "34", "16", "59", "39", "41", "11", "22", "10", "77", "54" }),
        new("TR-EGE", "Ege", "Aegean", 9m, 65m, CorrosionClass.C4, false, true, false,
            new[] { "35", "20", "45", "48", "09", "43", "64" }),
        new("TR-AKDENIZ", "Akdeniz", "Mediterranean", 11m, 70m, CorrosionClass.C5, false, true, true,
            new[] { "07", "33", "31", "46", "80" }),
        new("TR-KARADENIZ", "Karadeniz", "Black Sea", 7m, 80m, CorrosionClass.C4, true, true, false,
            new[] { "55", "61", "53", "52", "08", "57", "67", "74", "81", "28", "29", "37", "18", "60", "75", "14" }),
        new("TR-IC-ANADOLU", "İç Anadolu", "Central Anatolia", 2m, 60m, CorrosionClass.C2, true, false, false,
            new[] { "06", "26", "38", "40", "42", "50", "51", "58", "66", "68", "70", "71" }),
        new("TR-DOGU", "Doğu Anadolu", "Eastern Anatolia", -5m, 55m, CorrosionClass.C2, true, false, true,
            new[] { "04", "12", "13", "23", "24", "25", "30", "36", "44", "49", "62", "65", "76" }),
        new("TR-GUNEY-DOGU", "Güneydoğu Anadolu", "Southeastern Anatolia", 5m, 50m, CorrosionClass.C3, false, false, true,
            new[] { "02", "21", "27", "47", "56", "63", "72", "73", "79" }),
    };

    private static readonly (string Code, string Name, string Ral, string Hex, ColorFinishType Finish, int Order)[] RalColors =
    {
        ("WHT-9016", "Trafik Beyazı", "RAL 9016", "#F1F1F1", ColorFinishType.PowderCoated, 0),
        ("WHT-9010", "Saf Beyaz", "RAL 9010", "#FFFFFF", ColorFinishType.PowderCoated, 1),
        ("BLK-9005", "Jet Siyah", "RAL 9005", "#0A0A0A", ColorFinishType.PowderCoated, 10),
        ("ANT-7016", "Antrasit Gri", "RAL 7016", "#293133", ColorFinishType.PowderCoated, 20),
        ("ANT-7021", "Siyah Gri", "RAL 7021", "#23282B", ColorFinishType.PowderCoated, 21),
        ("GRY-7035", "Açık Gri", "RAL 7035", "#CBD0CC", ColorFinishType.PowderCoated, 22),
        ("BRZ-8019", "Koyu Bronz", "RAL 8019", "#3B3131", ColorFinishType.PowderCoated, 30),
        ("BRZ-8014", "Sepia Kahverengi", "RAL 8014", "#3D2B1F", ColorFinishType.PowderCoated, 31),
        ("BRN-8017", "Çikolata Kahverengi", "RAL 8017", "#45322E", ColorFinishType.PowderCoated, 32),
        ("ANO-NAT", "Doğal Anodize", null!, "#A8A8A8", ColorFinishType.Anodized, 40),
        ("ANO-BLK", "Siyah Anodize", null!, "#1C1C1C", ColorFinishType.Anodized, 41),
        ("ANO-BRZ", "Bronz Anodize", null!, "#6B4E3A", ColorFinishType.Anodized, 42),
        ("WD-OAK", "Ahşap — Meşe", null!, "#7A5230", ColorFinishType.WoodLook, 50),
        ("WD-WALNUT", "Ahşap — Ceviz", null!, "#4E2E1E", ColorFinishType.WoodLook, 51),
        ("WD-PINE", "Ahşap — Çam", null!, "#A07744", ColorFinishType.WoodLook, 52),
    };

    private record GlassTypeSpec(
        string Code, string Name, int ThicknessMm, GlassStructure Structure,
        decimal PricePerM2, decimal WeightKgPerM2, decimal AllowablePa,
        decimal MaxAreaM2, decimal UValue, decimal SoundDb, string LayersJson);

    private static readonly GlassTypeSpec[] GlassTypes =
    {
        new("GL-T6", "Temperli Cam 6 mm", 6, GlassStructure.Tempered, 320m, 15m, 1500m, 2.4m, 5.7m, 29m, "[6]"),
        new("GL-T8", "Temperli Cam 8 mm", 8, GlassStructure.Tempered, 410m, 20m, 2200m, 3.2m, 5.7m, 31m, "[8]"),
        new("GL-T10", "Temperli Cam 10 mm", 10, GlassStructure.Tempered, 510m, 25m, 2900m, 4.0m, 5.7m, 33m, "[10]"),
        new("GL-T12", "Temperli Cam 12 mm", 12, GlassStructure.Tempered, 640m, 30m, 3600m, 4.8m, 5.7m, 35m, "[12]"),
        new("GL-LAM-66-2", "Lamine 6+6.2 mm", 12, GlassStructure.Laminated, 720m, 30m, 3400m, 4.5m, 5.6m, 38m, "[6,1.52,6]"),
        new("GL-DG-4-12-4", "Isıcam 4+12Ar+4", 20, GlassStructure.DoubleGlazed, 850m, 22m, 2100m, 3.2m, 1.6m, 32m, "[4,12,4]"),
        new("GL-DG-6-16-4LE", "Isıcam 6+16Ar+4 Low-E", 26, GlassStructure.DoubleGlazed, 1120m, 27m, 2400m, 3.5m, 1.1m, 36m, "[6,16,4]"),
        new("GL-DG-66-12-4", "Isıcam 6+6.2 lamine + 12Ar + 4", 26, GlassStructure.DoubleGlazed, 1450m, 33m, 3000m, 4.0m, 1.3m, 41m, "[12,12,4]"),
    };

    private record ProfileSystemSpec(
        string Code, string Name, string BrandCode, GlassSystemType SystemType,
        int MaxPanelWidthMm, int MaxPanelHeightMm, decimal MaxPanelWeightKg,
        int[] SupportedThicknesses, GlassOpeningType[] SupportedOpenings,
        string? Certification, decimal? ThermalUValue, string Description,
        ProfileItemSpec[] Items);

    private record ProfileItemSpec(
        ProfileRole Role, string Code, string Name,
        int StockBarLengthMm, decimal WeightKgPerMeter, decimal PricePerKg);

    private static readonly ProfileSystemSpec[] ProfileSystems =
    {
        new("AG-SLIDEMASTER", "Albert Genau SlideMaster", "ALBERTGENAU",
            GlassSystemType.HeatInsulatedSliding,
            MaxPanelWidthMm: 1500, MaxPanelHeightMm: 2700, MaxPanelWeightKg: 150m,
            SupportedThicknesses: new[] { 20, 24, 26 },
            SupportedOpenings: new[] { GlassOpeningType.SlidingLeft, GlassOpeningType.SlidingRight, GlassOpeningType.Fixed },
            Certification: "EU Class 2A", ThermalUValue: 1.6m,
            Description: "Isıcamlı sürgülü cam balkon — Speed-HD makara, 150 kg panel kapasitesi.",
            Items: new[]
            {
                new ProfileItemSpec(ProfileRole.Top, "AG-SM-TOP", "SlideMaster Üst Ray", 6000, 1.45m, 220m),
                new ProfileItemSpec(ProfileRole.Bottom, "AG-SM-BOT", "SlideMaster Alt Ray", 6000, 1.60m, 220m),
                new ProfileItemSpec(ProfileRole.SideJamb, "AG-SM-JAMB", "SlideMaster Yan Kasa", 6000, 0.95m, 220m),
                new ProfileItemSpec(ProfileRole.Sash, "AG-SM-SASH", "SlideMaster Hareketli Kanat", 6000, 1.25m, 220m),
                new ProfileItemSpec(ProfileRole.Mullion, "AG-SM-MUL", "SlideMaster Orta Kayıt", 6000, 1.10m, 220m),
                new ProfileItemSpec(ProfileRole.Adapter, "AG-SM-ADP", "SlideMaster Adaptör", 6000, 0.55m, 220m),
                new ProfileItemSpec(ProfileRole.DripRail, "AG-SM-DRIP", "SlideMaster Damlalık", 6000, 0.40m, 220m),
            }),
        new("VZ-GOLD", "Vizyon Gold", "VIZYON",
            GlassSystemType.Sliding,
            MaxPanelWidthMm: 1200, MaxPanelHeightMm: 2400, MaxPanelWeightKg: 80m,
            SupportedThicknesses: new[] { 6, 8, 10 },
            SupportedOpenings: new[] { GlassOpeningType.SlidingLeft, GlassOpeningType.SlidingRight, GlassOpeningType.Folding, GlassOpeningType.Fixed },
            Certification: null, ThermalUValue: null,
            Description: "Klasik sürgü/katlanır cam balkon — 8-10 mm temperli cam, eşikli/eşiksiz.",
            Items: new[]
            {
                new ProfileItemSpec(ProfileRole.Top, "VZ-G-TOP", "Vizyon Gold Üst Ray", 6000, 0.95m, 180m),
                new ProfileItemSpec(ProfileRole.Bottom, "VZ-G-BOT", "Vizyon Gold Alt Ray", 6000, 1.10m, 180m),
                new ProfileItemSpec(ProfileRole.SideJamb, "VZ-G-JAMB", "Vizyon Gold Yan Kasa", 6000, 0.70m, 180m),
                new ProfileItemSpec(ProfileRole.Sash, "VZ-G-SASH", "Vizyon Gold Hareketli Kanat", 6000, 0.85m, 180m),
                new ProfileItemSpec(ProfileRole.Mullion, "VZ-G-MUL", "Vizyon Gold Orta Kayıt", 6000, 0.80m, 180m),
            }),
    };

    private record HardwareSpec(
        string Code, string Name, HardwareCategoryKind Category,
        Guid BrandId, string Unit, decimal UnitPrice,
        string CompatibleSystemsJson, decimal? MaxLoadKg, string? VendorPartNumber);

    private static IEnumerable<HardwareSpec> HardwareItems(
        Guid albertGenauId, Guid vizyonId,
        string compatibleAllJson, string compatibleSlidingJson) => new[]
    {
        new HardwareSpec("AG-ROLLER-SPEED-HD", "Speed-HD Makara (150 kg)", HardwareCategoryKind.Roller,
            albertGenauId, "Piece", 220m, compatibleSlidingJson, 150m, "AG-RH-150"),
        new HardwareSpec("VZ-ROLLER-STD", "Vizyon Standart Makara (80 kg)", HardwareCategoryKind.Roller,
            vizyonId, "Piece", 95m, compatibleAllJson, 80m, "VZ-R-80"),
        new HardwareSpec("AG-HINGE-HEAVY", "Ağır Kanat Menteşesi (60 kg)", HardwareCategoryKind.Hinge,
            albertGenauId, "Piece", 165m, compatibleAllJson, 60m, "AG-H-60"),
        new HardwareSpec("AG-LOCK-SPANISH", "İspanyol Kilit", HardwareCategoryKind.Lock,
            albertGenauId, "Piece", 110m, compatibleAllJson, null, "AG-L-SP"),
        new HardwareSpec("AG-HANDLE-STD", "Standart Tutamak", HardwareCategoryKind.Handle,
            albertGenauId, "Piece", 45m, compatibleAllJson, null, "AG-T-STD"),
        new HardwareSpec("AG-BRUSH-SEAL", "Kıl Fitil (metre)", HardwareCategoryKind.Brush,
            albertGenauId, "Meter", 12m, compatibleAllJson, null, "AG-B-12"),
        new HardwareSpec("AG-GASKET-EPDM", "EPDM Conta (metre)", HardwareCategoryKind.Gasket,
            albertGenauId, "Meter", 18m, compatibleAllJson, null, "AG-G-EPDM"),
        new HardwareSpec("AG-BUMPER-STD", "Stoplama Tamponu", HardwareCategoryKind.Bumper,
            albertGenauId, "Piece", 22m, compatibleAllJson, null, "AG-BMP"),
        new HardwareSpec("AG-WALLBR-STD", "Duvar Bağlantı Aparatı", HardwareCategoryKind.WallBracket,
            albertGenauId, "Piece", 38m, compatibleAllJson, null, "AG-WB"),
        new HardwareSpec("AG-DRIPCAP-STD", "Damlalık Kapağı", HardwareCategoryKind.DripCap,
            albertGenauId, "Piece", 28m, compatibleAllJson, null, "AG-DC"),
        new HardwareSpec("AG-CHAIN-STD", "Cam Balkon Zinciri", HardwareCategoryKind.Chain,
            albertGenauId, "Piece", 65m, compatibleAllJson, null, "AG-CH"),
        new HardwareSpec("AG-CORNERPOST", "Köşe Dikme Profili", HardwareCategoryKind.CornerPost,
            albertGenauId, "Piece", 145m, compatibleAllJson, null, "AG-CP"),
    };

    private record NotificationTemplateSpec(
        GlassNotificationEventCode EventCode,
        GlassNotificationChannel Channel,
        string Locale,
        string? Subject,
        string Body);

    private static readonly NotificationTemplateSpec[] NotificationTemplates =
    {
        new(GlassNotificationEventCode.QuoteSent, GlassNotificationChannel.Email, "tr-TR",
            "Cam Balkon Teklifiniz Hazır — {{project_code}}",
            "Merhaba {{customer_name}},\n\nCam balkon teklifiniz hazır. Tasarımı 3D olarak görüntülemek ve onaylamak için: {{share_url}}\n\nGeçerlilik tarihi: {{valid_until}}\n\nİyi günler dileriz."),
        new(GlassNotificationEventCode.QuoteSent, GlassNotificationChannel.Email, "en-US",
            "Your Glass Enclosure Quote — {{project_code}}",
            "Hello {{customer_name}},\n\nYour quote is ready. View the 3D design and approve: {{share_url}}\n\nValid until: {{valid_until}}\n\nBest regards."),
        new(GlassNotificationEventCode.QuoteSent, GlassNotificationChannel.WhatsApp, "tr-TR", null,
            "Cam balkon teklifiniz hazır! 3D tasarımı görüntüleyin ve onaylayın: {{share_url}}"),
        new(GlassNotificationEventCode.QuoteAccepted, GlassNotificationChannel.Email, "tr-TR",
            "Teklifiniz Onaylandı — {{project_code}}",
            "Merhaba {{customer_name}},\n\nTeklifiniz başarıyla onaylandı. Siparişiniz oluşturuldu.\n\nTeşekkür ederiz."),
        new(GlassNotificationEventCode.OrderConfirmed, GlassNotificationChannel.Email, "tr-TR",
            "Siparişiniz Üretime Alındı — {{project_code}}",
            "Merhaba {{customer_name}},\n\nSiparişiniz üretime alındı. Tahmini teslim: {{estimated_delivery}}."),
        new(GlassNotificationEventCode.ProductionStarted, GlassNotificationChannel.Sms, "tr-TR", null,
            "{{customer_name}}, cam balkon üretiminiz başladı. Takip: {{share_url}}"),
        new(GlassNotificationEventCode.ProductionCompleted, GlassNotificationChannel.WhatsApp, "tr-TR", null,
            "Cam balkonunuz hazır! Montaj randevusu için: {{contact_phone}}"),
        new(GlassNotificationEventCode.InstallationScheduled, GlassNotificationChannel.Sms, "tr-TR", null,
            "Montaj randevunuz: {{installation_date}}. Ekibimiz adrese gelecek."),
        new(GlassNotificationEventCode.InstallationCompleted, GlassNotificationChannel.Email, "tr-TR",
            "Montaj Tamamlandı — {{project_code}}",
            "Merhaba {{customer_name}},\n\nCam balkonunuz başarıyla monte edildi. Garanti belgenizi e-posta ekinde bulabilirsiniz."),
        new(GlassNotificationEventCode.StockLow, GlassNotificationChannel.InApp, "tr-TR",
            "Stok Uyarısı",
            "{{item_name}} stok altına düştü (mevcut: {{available}}, eşik: {{threshold}})."),
    };
}
