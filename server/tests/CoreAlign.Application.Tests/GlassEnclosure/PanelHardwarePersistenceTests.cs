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

public class PanelHardwarePersistenceTests
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
        var project = new GlassProject("PRJ-HW", Guid.NewGuid(), "Hardware", Guid.NewGuid()) { TenantId = tenantId };
        db.Set<GlassProject>().Add(project);
        var run = new GlassProjectRun(project.Id, 0, "R1", 3000, 2100, Guid.NewGuid()) { TenantId = tenantId };
        db.Set<GlassProjectRun>().Add(run);
        var panel = new GlassProjectPanel(run.Id, 0, 1500, GlassOpeningType.Fixed, glassTypeId) { TenantId = tenantId };
        run.AddPanel(panel);
        var hardware = new HardwareItem("HW-1", "Hinge", HardwareCategoryKind.Other, Guid.NewGuid(), "Piece", 25m)
        {
            TenantId = tenantId,
        };
        db.Set<HardwareItem>().Add(hardware);
        db.SaveChanges();
        db.ChangeTracker.Clear();
        return (project.Id, run.Id, panel.Id, glassTypeId, hardware.Id);
    }

    private static UpdatePanelDto UpdateDto(Guid glassTypeId, IReadOnlyList<PanelHardwareDto>? hardware) =>
        new(1500, GlassOpeningType.Fixed, glassTypeId, false, false, false, null, Hardware: hardware);

    private static async Task<List<GlassProjectPanelHardware>> HardwareRows(CoreAlignDbContext db, Guid panelId)
    {
        db.ChangeTracker.Clear();
        return await db.Set<GlassProjectPanelHardware>()
            .AsNoTracking().Where(h => h.PanelId == panelId).ToListAsync();
    }

    [Fact]
    public async Task Updating_a_panel_with_hardware_persists_without_a_concurrency_conflict()
    {
        var tenantId = Guid.NewGuid();
        var (db, conn) = NewDb(tenantId);
        try
        {
            var s = Seed(db, tenantId);
            var handler = new UpdatePanelCommandHandler(
                new GlassProjectPanelRepository(db), Substitute.For<IBomStaleSignal>());

            var result = await handler.Handle(
                new UpdatePanelCommand(s.ProjectId, s.RunId, s.PanelId,
                    UpdateDto(s.GlassTypeId, new List<PanelHardwareDto> { new(s.HardwareId, 3m) })),
                default);

            var save = async () => await db.SaveChangesAsync();
            await save.Should().NotThrowAsync<DbUpdateConcurrencyException>();

            result.Hardware.Should().ContainSingle(h => h.HardwareItemId == s.HardwareId && h.Quantity == 3m);
            var rows = await HardwareRows(db, s.PanelId);
            rows.Should().ContainSingle(h => h.HardwareItemId == s.HardwareId && h.Quantity == 3m);
        }
        finally
        {
            await db.DisposeAsync();
            conn.Dispose();
        }
    }

    [Fact]
    public async Task Adding_a_panel_with_hardware_persists_the_hardware_rows()
    {
        var tenantId = Guid.NewGuid();
        var (db, conn) = NewDb(tenantId);
        try
        {
            var s = Seed(db, tenantId);
            var handler = new AddPanelCommandHandler(
                new GlassProjectRunRepository(db), new GlassProjectPanelRepository(db),
                Substitute.For<IBomStaleSignal>());
            var dto = new AddPanelDto(1200, GlassOpeningType.Fixed, s.GlassTypeId, false, false, false, null,
                Hardware: new List<PanelHardwareDto> { new(s.HardwareId, 2m) });

            var result = await handler.Handle(new AddPanelCommand(s.ProjectId, s.RunId, dto), default);

            var save = async () => await db.SaveChangesAsync();
            await save.Should().NotThrowAsync<DbUpdateConcurrencyException>();

            result.Hardware.Should().ContainSingle(h => h.HardwareItemId == s.HardwareId && h.Quantity == 2m);
            var rows = await HardwareRows(db, result.Id);
            rows.Should().ContainSingle(h => h.HardwareItemId == s.HardwareId && h.Quantity == 2m);
        }
        finally
        {
            await db.DisposeAsync();
            conn.Dispose();
        }
    }

    [Fact]
    public async Task Updating_a_panel_with_null_hardware_leaves_existing_hardware_untouched()
    {
        var tenantId = Guid.NewGuid();
        var (db, conn) = NewDb(tenantId);
        try
        {
            var s = Seed(db, tenantId);
            var repo = new GlassProjectPanelRepository(db);
            var handler = new UpdatePanelCommandHandler(repo, Substitute.For<IBomStaleSignal>());

            await handler.Handle(new UpdatePanelCommand(s.ProjectId, s.RunId, s.PanelId,
                UpdateDto(s.GlassTypeId, new List<PanelHardwareDto> { new(s.HardwareId, 4m) })), default);
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();

            await handler.Handle(new UpdatePanelCommand(s.ProjectId, s.RunId, s.PanelId,
                UpdateDto(s.GlassTypeId, null)), default);
            await db.SaveChangesAsync();

            var rows = await HardwareRows(db, s.PanelId);
            rows.Should().ContainSingle(h => h.HardwareItemId == s.HardwareId && h.Quantity == 4m);
        }
        finally
        {
            await db.DisposeAsync();
            conn.Dispose();
        }
    }

    [Fact]
    public async Task Updating_a_panel_replaces_prior_hardware()
    {
        var tenantId = Guid.NewGuid();
        var (db, conn) = NewDb(tenantId);
        try
        {
            var s = Seed(db, tenantId);
            var otherHardware = new HardwareItem("HW-2", "Lock", HardwareCategoryKind.Other, Guid.NewGuid(), "Piece", 40m)
            {
                TenantId = tenantId,
            };
            db.Set<HardwareItem>().Add(otherHardware);
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();

            var repo = new GlassProjectPanelRepository(db);
            var handler = new UpdatePanelCommandHandler(repo, Substitute.For<IBomStaleSignal>());

            await handler.Handle(new UpdatePanelCommand(s.ProjectId, s.RunId, s.PanelId,
                UpdateDto(s.GlassTypeId, new List<PanelHardwareDto> { new(s.HardwareId, 1m) })), default);
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();

            await handler.Handle(new UpdatePanelCommand(s.ProjectId, s.RunId, s.PanelId,
                UpdateDto(s.GlassTypeId, new List<PanelHardwareDto> { new(otherHardware.Id, 5m) })), default);
            await db.SaveChangesAsync();

            var rows = await HardwareRows(db, s.PanelId);
            rows.Should().ContainSingle();
            rows[0].HardwareItemId.Should().Be(otherHardware.Id);
            rows[0].Quantity.Should().Be(5m);
        }
        finally
        {
            await db.DisposeAsync();
            conn.Dispose();
        }
    }
}
