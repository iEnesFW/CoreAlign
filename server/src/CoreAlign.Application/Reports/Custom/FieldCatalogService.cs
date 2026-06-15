using CoreAlign.Domain.Entities.Reporting;

namespace CoreAlign.Application.Reports.Custom;

public interface IFieldCatalogService
{
    IReadOnlyList<CustomReportFieldGroupDto> GetCatalog();
    CustomReportFieldGroupDto? Get(ReportEntityType entityType);
    bool Validate(ReportEntityType entityType, string key);
}

public sealed class FieldCatalogService : IFieldCatalogService
{
    public IReadOnlyList<CustomReportFieldGroupDto> GetCatalog()
    {
        return FieldCatalog.SupportedEntities()
            .Select(et => Get(et)!)
            .ToList();
    }

    public CustomReportFieldGroupDto? Get(ReportEntityType entityType)
    {
        var fields = FieldCatalog.For(entityType);
        if (fields.Count == 0) return null;
        var mapped = fields.Select(f => new CustomReportFieldDto(
            f.Key,
            f.LabelEn,
            f.LabelTr,
            f.DataType.ToString(),
            f.IsDimension,
            f.IsMeasureEligible,
            f.AllowedOperators.Select(op => op.ToString()).ToList(),
            f.AllowedAggregations?.Select(a => a.ToString()).ToList()))
            .ToList();
        return new CustomReportFieldGroupDto(entityType.ToString(), mapped);
    }

    public bool Validate(ReportEntityType entityType, string key) =>
        FieldCatalog.IsKnown(entityType, key);
}
