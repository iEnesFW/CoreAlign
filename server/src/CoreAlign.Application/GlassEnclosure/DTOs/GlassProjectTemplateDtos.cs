namespace CoreAlign.Application.GlassEnclosure.DTOs;

public record SaveGlassProjectTemplateDto(string Name, string PayloadJson);

public record GlassProjectTemplateSummaryDto(
    Guid Id,
    string Name,
    int WallCount,
    int SlabCount,
    int RunCount,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);

public record GlassProjectTemplateDto(
    Guid Id,
    string Name,
    string PayloadJson,
    int WallCount,
    int SlabCount,
    int RunCount,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);
