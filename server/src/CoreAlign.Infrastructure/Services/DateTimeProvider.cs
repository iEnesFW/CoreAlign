using CoreAlign.Domain.Interfaces;

namespace CoreAlign.Infrastructure.Services;

public class DateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
}
