namespace CoreAlign.Application.Compliance.Audit;

/// <summary>
/// Detective control for the per-tenant audit hash-chain. Re-walks
/// <c>entity_audit_logs</c> in sequence order, recomputes each
/// <c>RollingHash</c> from the prior row, and reports the first point of
/// divergence (tamper or gap). Without this, the chain is write-only and has
/// no detective value (SOC2 CC7.2 / ISO A.8.15 / GDPR Art.5(1)(f)).
/// </summary>
public interface IAuditChainVerifier
{
    Task<AuditChainVerificationResult> VerifyTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);
}

public sealed record AuditChainVerificationResult(
    Guid TenantId,
    long RowsVerified,
    bool IsValid,
    long? FirstBrokenSequence,
    string? Detail);
