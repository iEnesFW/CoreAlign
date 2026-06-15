using CoreAlign.Domain.Common;

namespace CoreAlign.Domain.Entities.Manufacturing;

public class WorkCenter : TenantEntity
{
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public decimal DailyCapacityMinutes { get; private set; }
    public bool IsActive { get; private set; } = true;

    protected WorkCenter() { }

    public WorkCenter(string code, string name, decimal dailyCapacityMinutes, bool isActive = true)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException("Work center code is required.", nameof(code));
        }
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Work center name is required.", nameof(name));
        }
        if (dailyCapacityMinutes < 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(dailyCapacityMinutes), "Daily capacity minutes cannot be negative.");
        }

        Code = code.Trim();
        Name = name.Trim();
        DailyCapacityMinutes = dailyCapacityMinutes;
        IsActive = isActive;
    }

    public void Update(string code, string name, decimal dailyCapacityMinutes, bool isActive)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException("Work center code is required.", nameof(code));
        }
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Work center name is required.", nameof(name));
        }
        if (dailyCapacityMinutes < 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(dailyCapacityMinutes), "Daily capacity minutes cannot be negative.");
        }

        Code = code.Trim();
        Name = name.Trim();
        DailyCapacityMinutes = dailyCapacityMinutes;
        IsActive = isActive;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
