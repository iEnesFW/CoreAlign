using CoreAlign.Domain.Common;

namespace CoreAlign.Domain.Entities.Reporting;

public class ReportDefinition : TenantEntity
{
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public ReportEntityType EntityType { get; private set; }
    public string DimensionsJson { get; private set; } = "[]";
    public string MeasuresJson { get; private set; } = "[]";
    public string FiltersJson { get; private set; } = "[]";
    public string? SortByJson { get; private set; }
    public int? Limit { get; private set; }
    public Guid? CreatedByUserId { get; private set; }

    protected ReportDefinition() { }

    public ReportDefinition(
        string name,
        ReportEntityType entityType,
        string dimensionsJson,
        string measuresJson,
        string filtersJson,
        string? sortByJson,
        int? limit,
        string? description = null,
        Guid? createdByUserId = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Report name is required.", nameof(name));
        }
        Name = name.Trim();
        Description = description;
        EntityType = entityType;
        DimensionsJson = dimensionsJson ?? "[]";
        MeasuresJson = measuresJson ?? "[]";
        FiltersJson = filtersJson ?? "[]";
        SortByJson = sortByJson;
        Limit = limit;
        CreatedByUserId = createdByUserId;
    }

    public void Update(
        string name,
        string? description,
        string dimensionsJson,
        string measuresJson,
        string filtersJson,
        string? sortByJson,
        int? limit)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Report name is required.", nameof(name));
        }
        Name = name.Trim();
        Description = description;
        DimensionsJson = dimensionsJson ?? "[]";
        MeasuresJson = measuresJson ?? "[]";
        FiltersJson = filtersJson ?? "[]";
        SortByJson = sortByJson;
        Limit = limit;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
