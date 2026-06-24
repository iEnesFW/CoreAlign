using CoreAlign.Application.AiHelper;
using CoreAlign.Application.AiHelper.Retrieval;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Entities.AiHelper;
using CoreAlign.Domain.Interfaces;
using CoreAlign.Infrastructure.AiHelper;
using CoreAlign.Infrastructure.Persistence;
using MediatR;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CoreAlign.Application.Tests.AiHelper;

public class PostgresKnowledgeRetrieverScopeTests
{
    private static readonly float[] Vector = { 1f, 0f, 0f, 0f };

    private static (CoreAlignDbContext db, SqliteConnection conn) NewDb()
    {
        var conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();

        var options = new DbContextOptionsBuilder<CoreAlignDbContext>()
            .UseSqlite(conn)
            .Options;
        var db = new CoreAlignDbContext(options, Substitute.For<ITenantContext>(), Substitute.For<IPublisher>());
        db.Database.EnsureCreated();
        return (db, conn);
    }

    private static Guid SeedTenant(CoreAlignDbContext db, string slug)
    {
        var tenant = new Tenant(slug, slug) { Id = Guid.NewGuid() };
        db.Tenants.Add(tenant);
        db.SaveChanges();
        return tenant.Id;
    }

    private static Guid SeedDoc(
        CoreAlignDbContext db,
        AiKbScope scope,
        Guid? tenantId,
        string? requiredRole,
        string sourceRef)
    {
        var doc = new AiKbDocument
        {
            SourceType = AiKbSourceType.Article,
            SourceRef = sourceRef,
            Title = sourceRef,
            Locale = "en",
            Scope = scope,
            TenantId = tenantId,
            RequiredRole = requiredRole,
            ContentHash = sourceRef,
        };
        doc.Chunks.Add(new AiKbChunk
        {
            Ordinal = 0,
            Content = $"{sourceRef} content",
            Embedding = Vector,
            Locale = "en",
            Scope = scope,
            TenantId = tenantId,
            RequiredRole = requiredRole,
            TokenCount = 1,
        });
        db.Set<AiKbDocument>().Add(doc);
        db.SaveChanges();
        return doc.Id;
    }

    private static async Task<HashSet<Guid>> RetrieveDocIdsAsync(
        PostgresKnowledgeRetriever retriever,
        Guid tenantId,
        params string[] roles)
    {
        var query = new RetrievalQuery(Vector, "en", tenantId, roles, MaxChunks: 50, MinScore: 0.0);
        var result = await retriever.RetrieveAsync(query, CancellationToken.None);
        return result.Select(r => r.DocumentId).ToHashSet();
    }

    [Fact]
    public async Task RoleScopedChunks_AreFilteredByTenantAndCallerRoles()
    {
        var (db, conn) = NewDb();
        try
        {
            var tenantA = SeedTenant(db, "tenant-a");
            var tenantB = SeedTenant(db, "tenant-b");

            var publicDoc = SeedDoc(db, AiKbScope.Public, null, null, "public");
            var tenantADoc = SeedDoc(db, AiKbScope.Tenant, tenantA, null, "tenant-a-doc");
            var tenantBDoc = SeedDoc(db, AiKbScope.Tenant, tenantB, null, "tenant-b-doc");
            var roleAFinanceDoc = SeedDoc(db, AiKbScope.Role, tenantA, "FinanceManager", "role-a-finance");
            var roleAAdminDoc = SeedDoc(db, AiKbScope.Role, tenantA, "TenantAdmin", "role-a-admin");
            var roleBFinanceDoc = SeedDoc(db, AiKbScope.Role, tenantB, "FinanceManager", "role-b-finance");

            var retriever = new PostgresKnowledgeRetriever(db, Options.Create(new AiHelperOptions()));

            var financeManagerOfA = await RetrieveDocIdsAsync(retriever, tenantA, "FinanceManager");
            financeManagerOfA.Should().BeEquivalentTo(new[] { publicDoc, tenantADoc, roleAFinanceDoc });
            financeManagerOfA.Should().NotContain(roleAAdminDoc, "a role the caller lacks must not leak");
            financeManagerOfA.Should().NotContain(tenantBDoc);
            financeManagerOfA.Should().NotContain(roleBFinanceDoc, "another tenant's role content must never leak");

            var adminOfA = await RetrieveDocIdsAsync(retriever, tenantA, "TenantAdmin");
            adminOfA.Should().BeEquivalentTo(new[] { publicDoc, tenantADoc, roleAAdminDoc });
            adminOfA.Should().NotContain(roleAFinanceDoc);

            var roomlessAuthenticatedOfA = await RetrieveDocIdsAsync(retriever, tenantA);
            roomlessAuthenticatedOfA.Should().BeEquivalentTo(new[] { publicDoc, tenantADoc });
            roomlessAuthenticatedOfA.Should().NotContain(roleAFinanceDoc, "role-scoped chunks must be excluded when the caller has no roles");
            roomlessAuthenticatedOfA.Should().NotContain(roleAAdminDoc);
        }
        finally
        {
            db.Dispose();
            conn.Dispose();
        }
    }
}
