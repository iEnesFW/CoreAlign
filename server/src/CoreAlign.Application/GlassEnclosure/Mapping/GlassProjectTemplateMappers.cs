using CoreAlign.Application.GlassEnclosure.DTOs;
using CoreAlign.Domain.Entities.GlassEnclosure;
using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Application.GlassEnclosure.Mapping;

public static class GlassProjectTemplateMappers
{
    public static GlassProjectTemplateDto ToDto(GlassProjectTemplate t) => new(
        t.Id, t.Name, t.PayloadJson, t.WallCount, t.SlabCount, t.RunCount, t.CreatedAtUtc, t.UpdatedAtUtc);

    public static GlassProjectTemplateSummaryDto ToSummary(GlassProjectTemplateListItem row) => new(
        row.Id, row.Name, row.WallCount, row.SlabCount, row.RunCount, row.CreatedAtUtc, row.UpdatedAtUtc);
}
