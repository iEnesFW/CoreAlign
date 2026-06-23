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

/// <summary>
/// Real-DbContext proof that rebalancing a run's panels persists without a phantom
/// DbUpdateConcurrencyException. A mocked run repository (Substitute) makes Update() a
/// no-op so the EF graph-walk never runs and the bug hides — only a real repository +
/// real SaveChanges over the live model reproduces it.
/// </summary>
public class RebalancePanelsPersistenceTests
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
    public async Task Rebalancing_replaces_panels_without_a_concurrency_conflict()
    {
        var tenantId = Guid.NewGuid();
        var (db, conn) = NewDb(tenantId);
        try
        {
            var glassTypeId = Guid.NewGuid();
            var project = new GlassProject("PRJ-RB", Guid.NewGuid(), "Rebalance", Guid.NewGuid())
            {
                TenantId = tenantId,
            };
            db.Set<GlassProject>().Add(project);
            var run = new GlassProjectRun(project.Id, 0, "R1", 3000, 2100, Guid.NewGuid())
            {
                TenantId = tenantId,
            };
            db.Set<GlassProjectRun>().Add(run);
            run.AddPanel(new GlassProjectPanel(run.Id, 0, 1500, GlassOpeningType.Fixed, glassTypeId)
            {
                TenantId = tenantId,
            });
            run.AddPanel(new GlassProjectPanel(run.Id, 1, 1500, GlassOpeningType.Fixed, glassTypeId)
            {
                TenantId = tenantId,
            });
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();

            var runRepo = new GlassProjectRunRepository(db);
            var panelRepo = new GlassProjectPanelRepository(db);
            var handler = new BulkRebalancePanelsCommandHandler(
                runRepo, panelRepo, Substitute.For<IBomStaleSignal>());
            var dto = new BulkRebalancePanelsDto(
                PanelCount: 3,
                DefaultOpeningType: GlassOpeningType.Fixed,
                DefaultGlassTypeId: glassTypeId);

            await handler.Handle(new BulkRebalancePanelsCommand(project.Id, run.Id, dto), default);
            // The pipeline saves for an ITransactionalRequest; the test drives it directly.
            var save = async () => await db.SaveChangesAsync();
            await save.Should().NotThrowAsync<DbUpdateConcurrencyException>();

            db.ChangeTracker.Clear();
            var reloaded = await db.Set<GlassProjectPanel>()
                .AsNoTracking()
                .Where(p => p.RunId == run.Id)
                .OrderBy(p => p.PanelIndex)
                .ToListAsync();

            reloaded.Should().HaveCount(3);
            reloaded.Should().OnlyContain(p => p.WidthMm == 1000);
        }
        finally
        {
            await db.DisposeAsync();
            conn.Dispose();
        }
    }
}
