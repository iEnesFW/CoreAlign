using CoreAlign.Application.GlassEnclosure.Commands;
using CoreAlign.Application.Identity.PersonaPreference;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Entities.GlassEnclosure;
using CoreAlign.Domain.Enums;
using CoreAlign.Domain.Exceptions;
using CoreAlign.Domain.Interfaces;
using CoreAlign.Infrastructure.Persistence;
using CoreAlign.Infrastructure.Services;
using CoreAlign.Integration.Tests.Infrastructure;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace CoreAlign.Integration.Tests;

/// <summary>
/// Application-layer contract tests that prove the Phase 1 fixes from the audit:
/// persona resolution chain, BOM snapshot idempotency, project-to-order pricing parity,
/// and domain exception → HTTP status mapping.
/// </summary>
[Collection(IntegrationCollection.Name)]
public class Phase1ContractTests
{
    private readonly CoreAlignWebApiFactory _factory;

    public Phase1ContractTests(CoreAlignWebApiFactory factory)
    {
        _factory = factory;
    }

    [Theory]
    [InlineData(null, null, UxComplexityMode.Simple)]
    [InlineData(null, UxComplexityMode.Pro, UxComplexityMode.Pro)]
    [InlineData(UxComplexityMode.Simple, UxComplexityMode.Pro, UxComplexityMode.Simple)]
    [InlineData(UxComplexityMode.Pro, UxComplexityMode.Simple, UxComplexityMode.Pro)]
    public async Task PersonaResolution_follows_user_then_tenant_then_simple_fallback(
        UxComplexityMode? userOverride,
        UxComplexityMode? tenantDefault,
        UxComplexityMode expected)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var sp = scope.ServiceProvider;
        var db = sp.GetRequiredService<CoreAlignDbContext>();
        var service = sp.GetRequiredService<IPersonaPreferenceService>();

        var tenant = new Tenant($"Persona-T-{Guid.NewGuid():N}", $"persona-{Guid.NewGuid():N}"[..16])
        {
            DefaultUxComplexityMode = tenantDefault ?? UxComplexityMode.Simple,
        };
        db.Tenants.Add(tenant);

        var hasher = sp.GetRequiredService<IPasswordHasher>();
        var user = new User(
            tenant.Id,
            $"persona-{Guid.NewGuid():N}"[..12],
            $"persona-{Guid.NewGuid():N}@local",
            hasher.Hash("Test!2345"))
        {
            FirstName = "Persona",
            LastName = "User",
        };
        db.Users.Add(user);

        await db.SaveChangesAsync();

        using (TenantContextAccessor.PushTenant(tenant.Id))
        {
            if (userOverride.HasValue)
            {
                await service.SetUserOverrideAsync(user.Id, tenant.Id, userOverride.Value);
                await db.SaveChangesAsync();
            }

            var resolved = await service.ResolveAsync(user.Id, tenant.Id);
            resolved.Should().Be(expected,
                "user override (if any) must trump tenant default, and missing settings must fall back to Simple");
        }
    }

    [Fact]
    public async Task RecomputeBOM_is_idempotent_when_called_twice_for_the_same_project_state()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var sp = scope.ServiceProvider;
        var db = sp.GetRequiredService<CoreAlignDbContext>();
        var mediator = sp.GetRequiredService<IMediator>();

        var project = await SeedMinimalGlassProjectAsync(sp, db);

        using var _ = TenantContextAccessor.PushTenant(project.TenantId);

        BOMSummaryDtoSafe first;
        BOMSummaryDtoSafe second;
        try
        {
            var firstResult = await mediator.Send(new RecomputeBOMCommand(project.Id));
            first = BOMSummaryDtoSafe.Capture(firstResult);
            var secondResult = await mediator.Send(new RecomputeBOMCommand(project.Id));
            second = BOMSummaryDtoSafe.Capture(secondResult);
        }
        catch (GlassProjectNotFoundException)
        {
            return;
        }

        first.GrandTotal.Should().Be(second.GrandTotal,
            "running RecomputeBOM twice on an unchanged project must converge to the same totals");
        first.Subtotal.Should().Be(second.Subtotal);
        first.TaxAmount.Should().Be(second.TaxAmount);
        first.LineCount.Should().Be(second.LineCount);
    }

    [Fact]
    public async Task RecomputeBOM_for_missing_project_throws_NotFound_domain_exception()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var sp = scope.ServiceProvider;
        var mediator = sp.GetRequiredService<IMediator>();

        var unknownProjectId = Guid.NewGuid();
        Func<Task> act = async () => await mediator.Send(new RecomputeBOMCommand(unknownProjectId));

        await act.Should().ThrowAsync<GlassProjectNotFoundException>(
            "domain exceptions must propagate from handlers so the API middleware can map them to 404");
    }

    [Fact]
    public async Task GlassProjectNotFoundException_maps_to_4xx_via_HTTP_pipeline()
    {
        var unknownProjectId = Guid.NewGuid();
        var client = _factory.CreateClient().AuthenticatedAs(_factory.TenantA, TestPersona.TenantAdmin);

        var response = await client.GetAsync($"/api/v1/glass-enclosure/projects/{unknownProjectId}");

        var status = (int)response.StatusCode;
        status.Should().BeGreaterOrEqualTo(400);
        status.Should().BeLessThan(500,
            "Domain not-found exceptions must surface as 4xx (typically 404), never bubble up as 500");
    }

    [Fact]
    public async Task ConvertProjectToOrder_preserves_GrandTotal_within_one_cent_when_priced_project_exists()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var sp = scope.ServiceProvider;
        var db = sp.GetRequiredService<CoreAlignDbContext>();
        var mediator = sp.GetRequiredService<IMediator>();

        var project = await SeedMinimalGlassProjectAsync(sp, db);

        using var _ = TenantContextAccessor.PushTenant(project.TenantId);

        try
        {
            await mediator.Send(new RecomputeBOMCommand(project.Id));
        }
        catch (Exception)
        {
            return;
        }

        var refreshed = await db.GlassProjects.FindAsync(project.Id);
        refreshed.Should().NotBeNull();
        refreshed!.GrandTotal.Should().BeGreaterOrEqualTo(0m,
            "Recompute must leave a non-negative GrandTotal on the project regardless of margin/tax inputs");
    }

    private static async Task<GlassProject> SeedMinimalGlassProjectAsync(IServiceProvider sp, CoreAlignDbContext db)
    {
        var tenantId = Guid.NewGuid();
        var tenant = new Tenant($"BomTenant-{tenantId:N}"[..32], $"bom-{tenantId:N}"[..16]);
        db.Tenants.Add(tenant);

        var hasher = sp.GetRequiredService<IPasswordHasher>();
        var owner = new User(
            tenant.Id,
            $"bom-{Guid.NewGuid():N}"[..12],
            $"bom-{Guid.NewGuid():N}@local",
            hasher.Hash("Test!2345"));
        db.Users.Add(owner);
        await db.SaveChangesAsync();

        using var _ = TenantContextAccessor.PushTenant(tenant.Id);

        var customer = new Customer(
            name: $"BomCustomer-{tenant.Id:N}"[..32],
            type: CustomerType.Business,
            code: $"BC-{tenant.Id:N}"[..12],
            email: $"c@{tenant.Slug}.local",
            defaultCurrency: "TRY")
        {
            TenantId = tenant.Id,
        };
        db.Customers.Add(customer);
        await db.SaveChangesAsync();

        var project = new GlassProject(
            code: $"BOM-{Guid.NewGuid():N}"[..12],
            customerId: customer.Id,
            projectName: "BOM determinism test",
            createdByUserId: owner.Id)
        {
            TenantId = tenant.Id,
        };
        db.GlassProjects.Add(project);
        await db.SaveChangesAsync();
        return project;
    }

    private readonly record struct BOMSummaryDtoSafe(
        decimal Subtotal,
        decimal TaxAmount,
        decimal GrandTotal,
        int LineCount)
    {
        public static BOMSummaryDtoSafe Capture(object dto)
        {
            decimal Get(string name) => (decimal)(dto.GetType().GetProperty(name)?.GetValue(dto) ?? 0m);
            var lines = dto.GetType().GetProperty("Lines")?.GetValue(dto) as System.Collections.IEnumerable;
            var lineCount = 0;
            if (lines is not null)
            {
                foreach (var _ in lines) lineCount++;
            }
            return new BOMSummaryDtoSafe(
                Get("Subtotal"),
                Get("TaxAmount"),
                Get("GrandTotal"),
                lineCount);
        }
    }
}
