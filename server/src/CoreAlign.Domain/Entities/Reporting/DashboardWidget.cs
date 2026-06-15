using CoreAlign.Domain.Common;

namespace CoreAlign.Domain.Entities.Reporting;

public class DashboardWidget : TenantEntity
{
    public Guid? UserId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public DashboardWidgetType Type { get; private set; }
    public BIDataSource DataSource { get; private set; }
    public string QueryConfigJson { get; private set; } = "{}";
    public int GridX { get; private set; }
    public int GridY { get; private set; }
    public int Width { get; private set; } = 4;
    public int Height { get; private set; } = 3;
    public bool IsActive { get; private set; } = true;
    public int DisplayOrder { get; private set; }

    protected DashboardWidget() { }

    public DashboardWidget(
        Guid? userId,
        string title,
        DashboardWidgetType type,
        BIDataSource dataSource,
        string queryConfigJson,
        int gridX,
        int gridY,
        int width,
        int height,
        int displayOrder = 0)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("Widget title is required.", nameof(title));
        }
        if (width <= 0)
        {
            throw new ArgumentException("Width must be positive.", nameof(width));
        }
        if (height <= 0)
        {
            throw new ArgumentException("Height must be positive.", nameof(height));
        }
        UserId = userId;
        Title = title.Trim();
        Type = type;
        DataSource = dataSource;
        QueryConfigJson = queryConfigJson ?? "{}";
        GridX = gridX;
        GridY = gridY;
        Width = width;
        Height = height;
        DisplayOrder = displayOrder;
    }

    public void UpdateLayout(int gridX, int gridY, int width, int height, int displayOrder)
    {
        if (width <= 0)
        {
            throw new ArgumentException("Width must be positive.", nameof(width));
        }
        if (height <= 0)
        {
            throw new ArgumentException("Height must be positive.", nameof(height));
        }
        GridX = gridX;
        GridY = gridY;
        Width = width;
        Height = height;
        DisplayOrder = displayOrder;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void UpdateConfig(string title, DashboardWidgetType type, BIDataSource dataSource, string queryConfigJson)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("Widget title is required.", nameof(title));
        }
        Title = title.Trim();
        Type = type;
        DataSource = dataSource;
        QueryConfigJson = queryConfigJson ?? "{}";
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Activate()
    {
        IsActive = true;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
