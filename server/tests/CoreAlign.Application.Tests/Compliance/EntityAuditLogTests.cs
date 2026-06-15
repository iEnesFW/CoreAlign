using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Entities.Compliance;
using CoreAlign.Infrastructure.Persistence.Interceptors;

namespace CoreAlign.Application.Tests.Compliance;

public class EntityAuditLogTests
{
    [Fact]
    public void Attribution_for_tenant_entity_uses_target_tenant_id_not_actor_tenant()
    {
        var actorTenantId = Guid.NewGuid();
        var targetTenant = new Tenant("Globex", "globex");

        var attributed = EntityAuditAttribution.ResolveAttributedTenantId(targetTenant, actorTenantId);

        attributed.Should().Be(targetTenant.Id, because:
            "an admin editing a Tenant must produce an audit row owned by that tenant — not the admin's tenant — otherwise the target tenant cannot see the change in its own audit timeline and the admin's tenant would see another tenant's data.");
    }

    [Fact]
    public void Attribution_for_tenant_owned_entity_uses_its_TenantId()
    {
        var actorTenantId = Guid.NewGuid();
        var owned = new EntityAuditLog { TenantId = Guid.NewGuid(), EntityType = "X", EntityId = Guid.NewGuid(), Action = EntityAuditAction.Create };

        var attributed = EntityAuditAttribution.ResolveAttributedTenantId(owned, actorTenantId);

        attributed.Should().Be(owned.TenantId);
    }

    [Fact]
    public void Attribution_falls_back_to_actor_when_entity_has_no_tenant()
    {
        var actorTenantId = Guid.NewGuid();
        var orphan = new EntityAuditLog { TenantId = Guid.Empty, EntityType = "X", EntityId = Guid.NewGuid(), Action = EntityAuditAction.Create };

        var attributed = EntityAuditAttribution.ResolveAttributedTenantId(orphan, actorTenantId);

        attributed.Should().Be(actorTenantId);
    }

    [Fact]
    public void Rolling_hash_chains_when_previous_hash_supplied()
    {
        var tenantId = Guid.NewGuid();
        var entityId = Guid.NewGuid();
        var when = new DateTime(2026, 6, 3, 12, 0, 0, DateTimeKind.Utc);

        var first = EntityAuditLog.ComputeRollingHash(
            previousHash: null,
            tenantId, "Customer", entityId, EntityAuditAction.Create,
            beforeJson: null, afterJson: "{\"name\":\"A\"}", userId: null, changedAtUtc: when, sequence: 1);
        var second = EntityAuditLog.ComputeRollingHash(
            previousHash: first,
            tenantId, "Customer", entityId, EntityAuditAction.Update,
            beforeJson: "{\"name\":\"A\"}", afterJson: "{\"name\":\"B\"}", userId: null, changedAtUtc: when.AddMinutes(1), sequence: 2);

        first.Should().NotBeNullOrEmpty();
        second.Should().NotBe(first);
        second.Length.Should().Be(64, because: "SHA-256 hex digest is 64 chars");
    }

    [Fact]
    public void Tampering_with_any_field_invalidates_the_chain()
    {
        var tenantId = Guid.NewGuid();
        var entityId = Guid.NewGuid();
        var when = new DateTime(2026, 6, 3, 12, 0, 0, DateTimeKind.Utc);

        var original = EntityAuditLog.ComputeRollingHash(null, tenantId, "Customer", entityId, EntityAuditAction.Update,
            beforeJson: "{\"name\":\"A\"}", afterJson: "{\"name\":\"B\"}", userId: null, changedAtUtc: when, sequence: 1);
        var tampered = EntityAuditLog.ComputeRollingHash(null, tenantId, "Customer", entityId, EntityAuditAction.Update,
            beforeJson: "{\"name\":\"A\"}", afterJson: "{\"name\":\"C\"}", userId: null, changedAtUtc: when, sequence: 1);

        tampered.Should().NotBe(original);
    }

    [Fact]
    public void Identical_inputs_produce_identical_hash()
    {
        var tenantId = Guid.NewGuid();
        var entityId = Guid.NewGuid();
        var when = new DateTime(2026, 6, 3, 12, 0, 0, DateTimeKind.Utc);

        var a = EntityAuditLog.ComputeRollingHash("PREV", tenantId, "Order", entityId, EntityAuditAction.Create,
            null, "{}", null, when, 1);
        var b = EntityAuditLog.ComputeRollingHash("PREV", tenantId, "Order", entityId, EntityAuditAction.Create,
            null, "{}", null, when, 1);

        a.Should().Be(b);
    }
}
