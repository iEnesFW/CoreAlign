using CoreAlign.Domain.Common;

namespace CoreAlign.Domain.Entities.Reporting;

public class SavedReport : TenantEntity
{
    public Guid OwnerUserId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public BIDataSource DataSource { get; private set; }
    public string QueryConfigJson { get; private set; } = "{}";
    public bool IsPublic { get; private set; }
    public DateTime? LastRunAtUtc { get; private set; }
    public int? LastRunRowCount { get; private set; }

    protected SavedReport() { }

    public SavedReport(
        Guid ownerUserId,
        string name,
        BIDataSource dataSource,
        string queryConfigJson,
        bool isPublic,
        string? description = null)
    {
        if (ownerUserId == Guid.Empty)
        {
            throw new ArgumentException("OwnerUserId is required.", nameof(ownerUserId));
        }
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Report name is required.", nameof(name));
        }
        OwnerUserId = ownerUserId;
        Name = name.Trim();
        Description = description;
        DataSource = dataSource;
        QueryConfigJson = queryConfigJson ?? "{}";
        IsPublic = isPublic;
    }

    public void Update(string name, string? description, BIDataSource dataSource, string queryConfigJson, bool isPublic)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Report name is required.", nameof(name));
        }
        Name = name.Trim();
        Description = description;
        DataSource = dataSource;
        QueryConfigJson = queryConfigJson ?? "{}";
        IsPublic = isPublic;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void RecordRun(DateTime ranAtUtc, int rowCount)
    {
        LastRunAtUtc = DateTime.SpecifyKind(ranAtUtc, DateTimeKind.Utc);
        LastRunRowCount = rowCount;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
