using CoreAlign.Domain.Entities.Warranty;
using CoreAlign.Domain.Enums;

namespace CoreAlign.Application.Warranty;

public interface IMaintenanceScheduleService
{
    Task<MaintenanceSchedule> CreateAsync(
        Guid warrantyContractId,
        MaintenanceScheduleType type,
        DateTime nextDueDate,
        string? recurrencePattern,
        string? notes,
        CancellationToken cancellationToken = default);

    Task CompleteAsync(Guid scheduleId, DateTime completedAtUtc, CancellationToken cancellationToken = default);
    Task DeactivateAsync(Guid scheduleId, CancellationToken cancellationToken = default);
}

public class MaintenanceScheduleService : IMaintenanceScheduleService
{
    private readonly Domain.Interfaces.IMaintenanceScheduleRepository _repo;

    public MaintenanceScheduleService(Domain.Interfaces.IMaintenanceScheduleRepository repo)
    {
        _repo = repo;
    }

    public async Task<MaintenanceSchedule> CreateAsync(
        Guid warrantyContractId,
        MaintenanceScheduleType type,
        DateTime nextDueDate,
        string? recurrencePattern,
        string? notes,
        CancellationToken cancellationToken = default)
    {
        var pattern = string.IsNullOrWhiteSpace(recurrencePattern)
            ? DefaultPatternFor(type)
            : recurrencePattern;

        var schedule = new MaintenanceSchedule(warrantyContractId, type, nextDueDate, pattern, notes);
        await _repo.AddAsync(schedule, cancellationToken);
        return schedule;
    }

    public async Task CompleteAsync(Guid scheduleId, DateTime completedAtUtc, CancellationToken cancellationToken = default)
    {
        var schedule = await _repo.GetByIdAsync(scheduleId, cancellationToken)
            ?? throw new KeyNotFoundException($"Maintenance schedule {scheduleId} not found.");
        var recurrence = RecurrenceFor(schedule.Type);
        schedule.CompleteOccurrence(completedAtUtc, recurrence);
        _repo.Update(schedule);
    }

    public async Task DeactivateAsync(Guid scheduleId, CancellationToken cancellationToken = default)
    {
        var schedule = await _repo.GetByIdAsync(scheduleId, cancellationToken)
            ?? throw new KeyNotFoundException($"Maintenance schedule {scheduleId} not found.");
        schedule.Deactivate();
        _repo.Update(schedule);
    }

    private static string DefaultPatternFor(MaintenanceScheduleType type) => type switch
    {
        MaintenanceScheduleType.PreventiveAnnual => "0 0 1 1 *",
        MaintenanceScheduleType.SemiAnnual => "0 0 1 */6 *",
        MaintenanceScheduleType.Quarterly => "0 0 1 */3 *",
        _ => "0 0 1 1 *",
    };

    private static TimeSpan RecurrenceFor(MaintenanceScheduleType type) => type switch
    {
        MaintenanceScheduleType.PreventiveAnnual => TimeSpan.FromDays(365),
        MaintenanceScheduleType.SemiAnnual => TimeSpan.FromDays(182),
        MaintenanceScheduleType.Quarterly => TimeSpan.FromDays(91),
        _ => TimeSpan.FromDays(365),
    };
}
