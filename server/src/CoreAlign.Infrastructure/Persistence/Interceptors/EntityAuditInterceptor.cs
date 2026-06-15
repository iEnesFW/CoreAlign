using System.Text.Json;
using CoreAlign.Application.B2B;
using CoreAlign.Domain.Common;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Entities.Compliance;
using CoreAlign.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace CoreAlign.Infrastructure.Persistence.Interceptors;

public sealed class EntityAuditInterceptor : SaveChangesInterceptor
{
    private static readonly HashSet<string> ExcludedEntityTypes = new(StringComparer.Ordinal)
    {
        nameof(EntityAuditLog),
        "OutboxMessage",
        "LoginAuditLog",
        "ActivityLog",
        "ProcessedWebhookEvent",
        "RefreshToken",
        "PasswordResetToken",
        "PasswordHistory",
        "EmailVerificationToken",
        "TwoFactorChallenge",
        "TwoFactorBackupCode",
        "UserSession",
    };

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.Never,
    };

    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUserAccessor _currentUserAccessor;

    public EntityAuditInterceptor(ITenantContext tenantContext, ICurrentUserAccessor currentUserAccessor)
    {
        _tenantContext = tenantContext;
        _currentUserAccessor = currentUserAccessor;
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is CoreAlignDbContext context)
        {
            CaptureAuditEntries(context);
        }
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        if (eventData.Context is CoreAlignDbContext context)
        {
            CaptureAuditEntries(context);
        }
        return base.SavingChanges(eventData, result);
    }

    private void CaptureAuditEntries(CoreAlignDbContext context)
    {
        var tenantId = _tenantContext.CurrentTenantId;
        if (!tenantId.HasValue || tenantId.Value == Guid.Empty) return;

        var pendingEntries = context.ChangeTracker.Entries()
            .Where(IsAuditable)
            .ToArray();
        if (pendingEntries.Length == 0) return;

        var userId = _currentUserAccessor.UserId;
        var now = DateTime.UtcNow;

        var sequences = new Dictionary<Guid, long>();
        var previousHashes = new Dictionary<Guid, string?>();

        foreach (var entry in pendingEntries)
        {
            var (action, beforeJson, afterJson) = BuildPayload(entry);
            if (action is null) continue;

            var entityId = TryReadEntityId(entry);
            if (entityId == Guid.Empty) continue;

            var attributedTenantId = EntityAuditAttribution.ResolveAttributedTenantId(entry.Entity, tenantId.Value);

            if (!sequences.TryGetValue(attributedTenantId, out var sequence))
            {
                sequence = ResolveStartingSequence(context, attributedTenantId);
                previousHashes[attributedTenantId] = ResolvePreviousHash(context, attributedTenantId);
            }

            sequence++;
            var previousHash = previousHashes[attributedTenantId];
            var hash = EntityAuditLog.ComputeRollingHash(
                previousHash,
                attributedTenantId,
                entry.Metadata.ClrType.Name,
                entityId,
                action.Value,
                beforeJson,
                afterJson,
                userId,
                now,
                sequence);

            var log = new EntityAuditLog
            {
                Id = Guid.NewGuid(),
                TenantId = attributedTenantId,
                EntityType = entry.Metadata.ClrType.Name,
                EntityId = entityId,
                Action = action.Value,
                BeforeJson = beforeJson,
                AfterJson = afterJson,
                UserId = userId,
                ChangedAtUtc = now,
                CreatedAtUtc = now,
                UpdatedAtUtc = now,
                Sequence = sequence,
                RollingHash = hash,
            };

            context.EntityAuditLogs.Add(log);
            sequences[attributedTenantId] = sequence;
            previousHashes[attributedTenantId] = hash;
        }
    }

    private static bool IsAuditable(EntityEntry entry)
    {
        if (entry.State is not (EntityState.Added or EntityState.Modified or EntityState.Deleted))
        {
            return false;
        }

        var name = entry.Metadata.ClrType.Name;
        if (ExcludedEntityTypes.Contains(name)) return false;

        if (entry.Entity is EntityAuditLog) return false;

        if (entry.Entity is not TenantEntity && entry.Entity is not Tenant) return false;

        return true;
    }

    private static (EntityAuditAction? Action, string? Before, string? After) BuildPayload(EntityEntry entry)
    {
        var props = entry.Properties
            .Where(p => !p.Metadata.IsShadowProperty())
            .ToArray();

        switch (entry.State)
        {
            case EntityState.Added:
            {
                var after = SerializeCurrent(props);
                return (EntityAuditAction.Create, null, after);
            }
            case EntityState.Modified:
            {
                var beforeDict = new Dictionary<string, object?>();
                var afterDict = new Dictionary<string, object?>();
                foreach (var prop in props)
                {
                    if (!prop.IsModified) continue;
                    if (Equals(prop.OriginalValue, prop.CurrentValue)) continue;
                    beforeDict[prop.Metadata.Name] = prop.OriginalValue;
                    afterDict[prop.Metadata.Name] = prop.CurrentValue;
                }
                if (beforeDict.Count == 0) return (null, null, null);
                return (
                    EntityAuditAction.Update,
                    JsonSerializer.Serialize(beforeDict, JsonOptions),
                    JsonSerializer.Serialize(afterDict, JsonOptions));
            }
            case EntityState.Deleted:
            {
                var before = SerializeOriginal(props);
                return (EntityAuditAction.Delete, before, null);
            }
        }

        return (null, null, null);
    }

    private static string SerializeCurrent(IEnumerable<PropertyEntry> props)
    {
        var dict = props.ToDictionary(p => p.Metadata.Name, p => p.CurrentValue);
        return JsonSerializer.Serialize(dict, JsonOptions);
    }

    private static string SerializeOriginal(IEnumerable<PropertyEntry> props)
    {
        var dict = props.ToDictionary(p => p.Metadata.Name, p => p.OriginalValue);
        return JsonSerializer.Serialize(dict, JsonOptions);
    }

    private static Guid TryReadEntityId(EntityEntry entry)
    {
        var idProp = entry.Properties.FirstOrDefault(p => string.Equals(p.Metadata.Name, "Id", StringComparison.Ordinal));
        if (idProp?.CurrentValue is Guid guid) return guid;
        return Guid.Empty;
    }

    private static long ResolveStartingSequence(CoreAlignDbContext context, Guid tenantId)
    {
        var pendingMax = context.ChangeTracker.Entries<EntityAuditLog>()
            .Where(e => e.State == EntityState.Added && e.Entity.TenantId == tenantId)
            .Select(e => e.Entity.Sequence)
            .DefaultIfEmpty(0L)
            .Max();
        if (pendingMax > 0) return pendingMax;

        var dbMax = context.EntityAuditLogs
            .IgnoreQueryFilters()
            .Where(a => a.TenantId == tenantId)
            .OrderByDescending(a => a.Sequence)
            .Select(a => (long?)a.Sequence)
            .FirstOrDefault() ?? 0L;
        return dbMax;
    }

    private static string? ResolvePreviousHash(CoreAlignDbContext context, Guid tenantId)
    {
        var pending = context.ChangeTracker.Entries<EntityAuditLog>()
            .Where(e => e.State == EntityState.Added && e.Entity.TenantId == tenantId)
            .OrderByDescending(e => e.Entity.Sequence)
            .Select(e => e.Entity.RollingHash)
            .FirstOrDefault();
        if (!string.IsNullOrEmpty(pending)) return pending;

        var dbHash = context.EntityAuditLogs
            .IgnoreQueryFilters()
            .Where(a => a.TenantId == tenantId)
            .OrderByDescending(a => a.Sequence)
            .Select(a => a.RollingHash)
            .FirstOrDefault();
        return dbHash;
    }
}
