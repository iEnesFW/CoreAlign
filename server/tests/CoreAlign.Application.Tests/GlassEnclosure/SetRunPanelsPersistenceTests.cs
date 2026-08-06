using CoreAlign.Application.GlassEnclosure.BomFreshness;
using CoreAlign.Application.GlassEnclosure.Commands;
using CoreAlign.Application.GlassEnclosure.DTOs;
using CoreAlign.Application.GlassEnclosure.Handlers;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Entities.GlassEnclosure;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Interfaces;
using CoreAlign.Infrastructure.Persistence;
using CoreAlign.Infrastructure.Repositories;
using MediatR;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace CoreAlign.Application.Tests.GlassEnclosure;

public class SetRunPanelsPersistenceTests
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

    private static (Guid ProjectId, Guid RunId, Guid PanelId, Guid GlassTypeId, Guid HardwareId) Seed(
        CoreAlignDbContext db, Guid tenantId)
    {
        var glassTypeId = Guid.NewGuid();
        var project = new GlassProject("PRJ-SP", Guid.NewGuid(), "SetPanels", Guid.NewGuid()) { TenantId = tenantId };
        db.Set<GlassProject>().Add(project);
        var run = new GlassProjectRun(project.Id, 0, "R1", 3000, 2100, Guid.NewGuid()) { TenantId = tenantId };
        db.Set<GlassProjectRun>().Add(run);
        var panel = new GlassProjectPanel(run.Id, 0, 1000, GlassOpeningType.Fixed, glassTypeId) { TenantId = tenantId };
        run.AddPanel(panel);
        var hardware = new HardwareItem("HW-1", "Hinge", HardwareCategoryKind.Other, Guid.NewGuid(), "Piece", 25m)
        {
            TenantId = tenantId,
        };
        db.Set<HardwareItem>().Add(hardware);
        db.SaveChanges();
        db.Set<GlassProjectPanelHardware>().Add(new GlassProjectPanelHardware(panel.Id, hardware.Id, 2m) { TenantId = tenantId });
        db.SaveChanges();
        db.ChangeTracker.Clear();
        return (project.Id, run.Id, panel.Id, glassTypeId, hardware.Id);
    }

    private static SetRunPanelsCommandHandler Handler(CoreAlignDbContext db) =>
        new(new GlassProjectRunRepository(db), new GlassProjectPanelRepository(db), Substitute.For<IBomStaleSignal>());

    private static async Task<List<GlassProjectPanel>> Panels(CoreAlignDbContext db, Guid runId)
    {
        db.ChangeTracker.Clear();
        return await db.Set<GlassProjectPanel>().AsNoTracking()
            .Where(p => p.RunId == runId).OrderBy(p => p.PanelIndex).ToListAsync();
    }

    [Fact]
    public async Task Splitting_keeps_the_left_id_and_hardware_and_adds_the_right_without_a_conflict()
    {
        var tenantId = Guid.NewGuid();
        var (db, conn) = NewDb(tenantId);
        try
        {
            var s = Seed(db, tenantId);
            var newId = Guid.NewGuid();
            var dto = new SetRunPanelsDto(new List<PanelSpecDto>
            {
                new(s.PanelId, 400, GlassOpeningType.Fixed, s.GlassTypeId),
                new(newId, 600, GlassOpeningType.Fixed, s.GlassTypeId),
            });

            await Handler(db).Handle(new SetRunPanelsCommand(s.ProjectId, s.RunId, dto), default);
            var save = async () => await db.SaveChangesAsync();
            await save.Should().NotThrowAsync<DbUpdateConcurrencyException>();

            var panels = await Panels(db, s.RunId);
            panels.Should().HaveCount(2);
            panels[0].Id.Should().Be(s.PanelId);
            panels[0].WidthMm.Should().Be(400);
            panels[1].Id.Should().Be(newId);
            panels[1].WidthMm.Should().Be(600);

            var hardware = await db.Set<GlassProjectPanelHardware>().AsNoTracking()
                .Where(h => h.PanelId == s.PanelId).ToListAsync();
            hardware.Should().ContainSingle(h => h.HardwareItemId == s.HardwareId && h.Quantity == 2m);
        }
        finally
        {
            await db.DisposeAsync();
            conn.Dispose();
        }
    }

    [Fact]
    public async Task Dropping_a_panel_from_the_spec_removes_it()
    {
        var tenantId = Guid.NewGuid();
        var (db, conn) = NewDb(tenantId);
        try
        {
            var s = Seed(db, tenantId);
            var newId = Guid.NewGuid();
            await Handler(db).Handle(new SetRunPanelsCommand(s.ProjectId, s.RunId, new SetRunPanelsDto(new List<PanelSpecDto>
            {
                new(s.PanelId, 400, GlassOpeningType.Fixed, s.GlassTypeId),
                new(newId, 600, GlassOpeningType.Fixed, s.GlassTypeId),
            })), default);
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();

            await Handler(db).Handle(new SetRunPanelsCommand(s.ProjectId, s.RunId, new SetRunPanelsDto(new List<PanelSpecDto>
            {
                new(s.PanelId, 1000, GlassOpeningType.Fixed, s.GlassTypeId),
            })), default);
            await db.SaveChangesAsync();

            var panels = await Panels(db, s.RunId);
            panels.Should().ContainSingle();
            panels[0].Id.Should().Be(s.PanelId);
            panels[0].WidthMm.Should().Be(1000);
        }
        finally
        {
            await db.DisposeAsync();
            conn.Dispose();
        }
    }

    [Fact]
    public async Task Empty_spec_is_a_no_op_and_never_wipes_panels()
    {
        var tenantId = Guid.NewGuid();
        var (db, conn) = NewDb(tenantId);
        try
        {
            var s = Seed(db, tenantId);
            await Handler(db).Handle(
                new SetRunPanelsCommand(s.ProjectId, s.RunId, new SetRunPanelsDto(new List<PanelSpecDto>())), default);
            await db.SaveChangesAsync();

            var panels = await Panels(db, s.RunId);
            panels.Should().ContainSingle(p => p.Id == s.PanelId);
        }
        finally
        {
            await db.DisposeAsync();
            conn.Dispose();
        }
    }

    [Fact]
    public async Task Resizing_a_kept_shaped_panel_clears_a_silhouette_that_no_longer_fits()
    {
        var tenantId = Guid.NewGuid();
        var (db, conn) = NewDb(tenantId);
        try
        {
            var s = Seed(db, tenantId);
            var tracked = await db.Set<GlassProjectPanel>().SingleAsync(p => p.Id == s.PanelId);
            tracked.UpdateShape(null, null, null, null, null, null, null, null,
                "polygon", """[{"x":-500,"y":0},{"x":500,"y":0},{"x":500,"y":2000},{"x":-500,"y":2000}]""");
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();

            await Handler(db).Handle(new SetRunPanelsCommand(s.ProjectId, s.RunId, new SetRunPanelsDto(new List<PanelSpecDto>
            {
                new(s.PanelId, 400, GlassOpeningType.Fixed, s.GlassTypeId),
            })), default);
            await db.SaveChangesAsync();

            var panels = await Panels(db, s.RunId);
            panels.Should().ContainSingle();
            panels[0].WidthMm.Should().Be(400);
            panels[0].ShapeKind.Should().BeNull();
            panels[0].ShapePointsJson.Should().BeNull();
        }
        finally
        {
            await db.DisposeAsync();
            conn.Dispose();
        }
    }

    [Fact]
    public async Task An_unchanged_width_never_touches_a_kept_panels_shape()
    {
        var tenantId = Guid.NewGuid();
        var (db, conn) = NewDb(tenantId);
        try
        {
            var s = Seed(db, tenantId);
            var outline = """[{"x":-500,"y":0},{"x":500,"y":0},{"x":500,"y":2000},{"x":-500,"y":2000}]""";
            var tracked = await db.Set<GlassProjectPanel>().SingleAsync(p => p.Id == s.PanelId);
            tracked.UpdateShape(null, null, null, null, null, null, null, null, "polygon", outline);
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();

            await Handler(db).Handle(new SetRunPanelsCommand(s.ProjectId, s.RunId, new SetRunPanelsDto(new List<PanelSpecDto>
            {
                new(s.PanelId, 1000, GlassOpeningType.Fixed, s.GlassTypeId),
            })), default);
            await db.SaveChangesAsync();

            var panels = await Panels(db, s.RunId);
            panels[0].ShapeKind.Should().Be("polygon");
            panels[0].ShapePointsJson.Should().Be(outline);
        }
        finally
        {
            await db.DisposeAsync();
            conn.Dispose();
        }
    }
}
