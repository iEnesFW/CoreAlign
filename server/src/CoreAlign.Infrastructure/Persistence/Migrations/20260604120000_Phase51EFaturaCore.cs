using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreAlign.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Phase 5.1 — EFatura provider core scaffolding (F2.1).
    /// Schema-side this is a noop: the ProviderWebhookInbox and TenantProviderConfig
    /// tables that the F2.1 dispatcher / reconciliation pipeline persists into were
    /// already created by Phase 4.2 (Provider Registry & Configs). The new
    /// EFaturaSubmission / EFaturaReconciliationLog tracking tables are introduced
    /// once their Domain entities land — at that point a follow-up dated migration
    /// will add the columns. Keeping this slot reserved preserves migration
    /// ordering for the Phase 5 ledger.
    /// </summary>
    public partial class Phase51EFaturaCore : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("-- noop: Phase51 EFatura core (F2.1) — tracking tables wait for EFaturaSubmission/EFaturaReconciliationLog domain entities");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("-- noop: Phase51 EFatura core");
        }
    }
}
