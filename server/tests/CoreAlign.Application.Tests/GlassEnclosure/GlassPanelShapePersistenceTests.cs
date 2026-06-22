using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Entities.GlassEnclosure;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Interfaces;
using CoreAlign.Infrastructure.Persistence;
using MediatR;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace CoreAlign.Application.Tests.GlassEnclosure;

/// <summary>
/// Tabula-rasa proof that the Phase93 panel-shape columns persist. The schema is
/// built from the live EF model via EnsureCreated (so this also guards the model +
/// configuration), a panel is saved with every shape field set, then reloaded from a
/// fresh context to confirm the round-trip — including a deliberately null corner.
/// </summary>
public class GlassPanelShapePersistenceTests
{
    private static (CoreAlignDbContext db, SqliteConnection conn) NewDb(Guid tenantId)
    {
        var conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();

        var tenant = Substitute.For<ITenantContext>();
        tenant.CurrentTenantId.Returns(tenantId);
        tenant.HasTenant.Returns(true);
        tenant.RequireTenantId().Returns(tenantId);

        var options = new DbContextOptionsBuilder<CoreAlignDbContext>().UseSqlite(conn).Options;
        var db = new CoreAlignDbContext(options, tenant, Substitute.For<IPublisher>());
        db.Database.EnsureCreated();
        db.Tenants.Add(new Tenant("Test", "test") { Id = tenantId });
        db.SaveChanges();
        return (db, conn);
    }

    [Fact]
    public async Task Panel_shape_columns_round_trip_through_a_fresh_database()
    {
        var tenantId = Guid.NewGuid();
        var (db, conn) = NewDb(tenantId);
        try
        {
            var project = new GlassProject("PRJ-1", Guid.NewGuid(), "Shape RT", Guid.NewGuid())
            {
                TenantId = tenantId,
            };
            db.Set<GlassProject>().Add(project);

            var run = new GlassProjectRun(project.Id, 0, "R1", 2000, 2100, Guid.NewGuid())
            {
                TenantId = tenantId,
            };
            db.Set<GlassProjectRun>().Add(run);

            var panel = new GlassProjectPanel(run.Id, 0, 600, GlassOpeningType.Fixed, Guid.NewGuid())
            {
                TenantId = tenantId,
            };
            panel.UpdateShape(
                heightMm: 2400,
                topShape: "arched",
                topRightHeightMm: 2200,
                archRiseMm: 180,
                cornerRadiusTlMm: 30,
                cornerRadiusTrMm: 40,
                cornerRadiusBrMm: 0,
                cornerRadiusBlMm: null);
            db.Set<GlassProjectPanel>().Add(panel);
            await db.SaveChangesAsync();

            var tenant = Substitute.For<ITenantContext>();
            tenant.CurrentTenantId.Returns(tenantId);
            tenant.HasTenant.Returns(true);
            tenant.RequireTenantId().Returns(tenantId);
            await using var verifyDb = new CoreAlignDbContext(
                new DbContextOptionsBuilder<CoreAlignDbContext>().UseSqlite(conn).Options,
                tenant,
                Substitute.For<IPublisher>());

            var reloaded = await verifyDb.Set<GlassProjectPanel>()
                .AsNoTracking()
                .SingleAsync(p => p.Id == panel.Id);

            reloaded.HeightMm.Should().Be(2400);
            reloaded.TopShape.Should().Be("arched");
            reloaded.TopRightHeightMm.Should().Be(2200);
            reloaded.ArchRiseMm.Should().Be(180);
            reloaded.CornerRadiusTlMm.Should().Be(30);
            reloaded.CornerRadiusTrMm.Should().Be(40);
            reloaded.CornerRadiusBrMm.Should().Be(0);
            reloaded.CornerRadiusBlMm.Should().BeNull();
        }
        finally
        {
            await db.DisposeAsync();
            conn.Dispose();
        }
    }

    [Fact]
    public async Task Plain_rectangular_panel_persists_null_shape()
    {
        var tenantId = Guid.NewGuid();
        var (db, conn) = NewDb(tenantId);
        try
        {
            var project = new GlassProject("PRJ-2", Guid.NewGuid(), "Plain RT", Guid.NewGuid())
            {
                TenantId = tenantId,
            };
            db.Set<GlassProject>().Add(project);
            var run = new GlassProjectRun(project.Id, 0, "R1", 2000, 2100, Guid.NewGuid())
            {
                TenantId = tenantId,
            };
            db.Set<GlassProjectRun>().Add(run);
            var panel = new GlassProjectPanel(run.Id, 0, 600, GlassOpeningType.Fixed, Guid.NewGuid())
            {
                TenantId = tenantId,
            };
            db.Set<GlassProjectPanel>().Add(panel);
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();

            var reloaded = await db.Set<GlassProjectPanel>()
                .AsNoTracking()
                .SingleAsync(p => p.Id == panel.Id);

            reloaded.HeightMm.Should().BeNull();
            reloaded.TopShape.Should().BeNull();
            reloaded.CornerRadiusTlMm.Should().BeNull();
        }
        finally
        {
            await db.DisposeAsync();
            conn.Dispose();
        }
    }
}
