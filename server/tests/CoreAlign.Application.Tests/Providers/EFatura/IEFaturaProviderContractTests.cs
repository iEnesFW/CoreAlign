using CoreAlign.Application.Providers.EFatura;

namespace CoreAlign.Application.Tests.Providers.EFatura;

public abstract class IEFaturaProviderContractTests<TProvider>
    where TProvider : class, IEFaturaProvider
{
    protected abstract TProvider CreateProvider(IEFaturaContractTestHarness harness);

    protected virtual EFaturaCredentials BuildSandboxCredentials() =>
        new("sandbox-user", "sandbox-pass", "https://sandbox.test.local");

    private static EFaturaDocument BuildDocument(
        string number = "INV-001",
        string currency = "TRY",
        decimal total = 120m,
        IReadOnlyList<EFaturaLine>? lines = null,
        string buyerVkn = "1234567890",
        EFaturaDocumentType type = EFaturaDocumentType.Invoice) =>
        new(
            type,
            number,
            new DateTime(2026, 1, 15, 9, 30, 0, DateTimeKind.Utc),
            buyerVkn,
            "Buyer Co",
            lines ?? new[] { new EFaturaLine(1m, "Item", 100m, 20m) },
            currency,
            total);

    [Fact]
    public async Task T1_Issue_standard_b2b_invoice_returns_uuid_and_accepted_status()
    {
        var harness = new IEFaturaContractTestHarness();
        var sut = CreateProvider(harness);

        harness.NextIssueResult = new EFaturaIssueResult(Guid.NewGuid().ToString(), "Accepted", "1000", DateTime.UtcNow);
        var request = new EFaturaIssueRequest(BuildDocument(), Convert.ToBase64String(new byte[] { 1, 2, 3 }));

        var result = await sut.IssueAsync(request, CancellationToken.None);

        result.Uuid.Should().NotBeNullOrWhiteSpace();
        result.Status.Should().Be("Accepted");
    }

    [Fact]
    public async Task T2_Issue_zero_vat_invoice_is_accepted()
    {
        var harness = new IEFaturaContractTestHarness();
        var sut = CreateProvider(harness);

        harness.NextIssueResult = new EFaturaIssueResult(Guid.NewGuid().ToString(), "Accepted", "1000", DateTime.UtcNow);
        var lines = new[] { new EFaturaLine(1m, "Tax-exempt", 50m, 0m) };
        var request = new EFaturaIssueRequest(BuildDocument(total: 50m, lines: lines), "x");

        var result = await sut.IssueAsync(request, CancellationToken.None);

        result.Status.Should().Be("Accepted");
    }

    [Fact]
    public async Task T3_Issue_currency_usd_preserves_currency_round_trip()
    {
        var harness = new IEFaturaContractTestHarness();
        var sut = CreateProvider(harness);

        harness.NextIssueResult = new EFaturaIssueResult(Guid.NewGuid().ToString(), "Accepted", "1000", DateTime.UtcNow);
        var request = new EFaturaIssueRequest(BuildDocument(currency: "USD"), "x");

        var result = await sut.IssueAsync(request, CancellationToken.None);

        result.Should().NotBeNull();
        harness.LastIssueRequest!.Document.Currency.Should().Be("USD");
    }

    [Fact]
    public async Task T4_Issue_multiple_lines_invoice_propagates_line_count()
    {
        var harness = new IEFaturaContractTestHarness();
        var sut = CreateProvider(harness);

        harness.NextIssueResult = new EFaturaIssueResult(Guid.NewGuid().ToString(), "Accepted", "1000", DateTime.UtcNow);
        var lines = new[]
        {
            new EFaturaLine(2m, "Item A", 100m, 20m),
            new EFaturaLine(3m, "Item B", 50m, 20m),
            new EFaturaLine(1m, "Item C", 250m, 10m),
        };
        var request = new EFaturaIssueRequest(BuildDocument(total: 825m, lines: lines), "x");

        await sut.IssueAsync(request, CancellationToken.None);

        harness.LastIssueRequest!.Document.Lines.Should().HaveCount(3);
    }

    [Fact]
    public async Task T5_Issue_partial_discount_line_is_supported()
    {
        var harness = new IEFaturaContractTestHarness();
        var sut = CreateProvider(harness);

        harness.NextIssueResult = new EFaturaIssueResult(Guid.NewGuid().ToString(), "Accepted", "1000", DateTime.UtcNow);
        var lines = new[]
        {
            new EFaturaLine(1m, "Discounted", 95m, 20m),
            new EFaturaLine(1m, "Full price", 100m, 20m),
        };
        var request = new EFaturaIssueRequest(BuildDocument(total: 234m, lines: lines), "x");

        var result = await sut.IssueAsync(request, CancellationToken.None);

        result.Status.Should().Be("Accepted");
    }

    [Fact]
    public async Task T6_Cancel_within_window_succeeds()
    {
        var harness = new IEFaturaContractTestHarness();
        var sut = CreateProvider(harness);

        harness.NextCancelResult = new EFaturaCancelResult("uuid-1", true, "Buyer request");

        var result = await sut.CancelAsync(new EFaturaCancelInvoiceRequest("uuid-1", "Buyer request"), CancellationToken.None);

        result.Cancelled.Should().BeTrue();
    }

    [Fact]
    public async Task T7_Cancel_outside_window_is_rejected()
    {
        var harness = new IEFaturaContractTestHarness();
        var sut = CreateProvider(harness);

        harness.NextCancelResult = new EFaturaCancelResult("uuid-old", false, "Out of window");

        var result = await sut.CancelAsync(new EFaturaCancelInvoiceRequest("uuid-old", "Out of window"), CancellationToken.None);

        result.Cancelled.Should().BeFalse();
    }

    [Fact]
    public async Task T8_Cancel_already_cancelled_is_idempotent()
    {
        var harness = new IEFaturaContractTestHarness();
        var sut = CreateProvider(harness);

        harness.NextCancelResult = new EFaturaCancelResult("uuid-1", true, "Already cancelled");

        var first = await sut.CancelAsync(new EFaturaCancelInvoiceRequest("uuid-1", "reason"), CancellationToken.None);
        harness.NextCancelResult = new EFaturaCancelResult("uuid-1", true, "Already cancelled");
        var second = await sut.CancelAsync(new EFaturaCancelInvoiceRequest("uuid-1", "reason"), CancellationToken.None);

        first.Cancelled.Should().BeTrue();
        second.Cancelled.Should().BeTrue();
    }

    [Fact]
    public async Task T9_GetStatus_unknown_uuid_returns_not_found_marker()
    {
        var harness = new IEFaturaContractTestHarness();
        var sut = CreateProvider(harness);

        harness.NextStatusResult = new EFaturaProviderStatus("missing", "NotFound", null, null);

        var result = await sut.GetStatusAsync(new EFaturaGetStatusRequest("missing"), CancellationToken.None);

        result.CurrentStatus.Should().Be("NotFound");
    }

    [Fact]
    public async Task T10_GetStatus_pending_returns_pending()
    {
        var harness = new IEFaturaContractTestHarness();
        var sut = CreateProvider(harness);

        harness.NextStatusResult = new EFaturaProviderStatus("u1", "Pending", null, null);

        var result = await sut.GetStatusAsync(new EFaturaGetStatusRequest("u1"), CancellationToken.None);

        result.CurrentStatus.Should().Be("Pending");
    }

    [Fact]
    public async Task T11_GetStatus_accepted_returns_accepted_with_delivered_at()
    {
        var harness = new IEFaturaContractTestHarness();
        var sut = CreateProvider(harness);

        var delivered = DateTime.UtcNow;
        harness.NextStatusResult = new EFaturaProviderStatus("u1", "Accepted", "1000", delivered);

        var result = await sut.GetStatusAsync(new EFaturaGetStatusRequest("u1"), CancellationToken.None);

        result.CurrentStatus.Should().Be("Accepted");
        result.DeliveredAtUtc.Should().Be(delivered);
    }

    [Fact]
    public async Task T12_GetStatus_rejected_carries_gib_response_code()
    {
        var harness = new IEFaturaContractTestHarness();
        var sut = CreateProvider(harness);

        harness.NextStatusResult = new EFaturaProviderStatus("u1", "Rejected", "9001", null);

        var result = await sut.GetStatusAsync(new EFaturaGetStatusRequest("u1"), CancellationToken.None);

        result.CurrentStatus.Should().Be("Rejected");
        result.GibResponseCode.Should().Be("9001");
    }

    [Fact]
    public async Task T13_CreditNote_partial_refund_is_issued()
    {
        var harness = new IEFaturaContractTestHarness();
        var sut = CreateProvider(harness);

        harness.NextCreditNoteResult = new EFaturaCreditNoteResult("cn-1", "Accepted", DateTime.UtcNow);
        var req = new EFaturaCreditNoteRequest("u-orig", 50m, "TRY", "Partial refund");

        var result = await sut.CreditNoteAsync(req, CancellationToken.None);

        result.Status.Should().Be("Accepted");
        harness.LastCreditNoteRequest!.RefundAmount.Should().Be(50m);
    }

    [Fact]
    public async Task T14_CreditNote_full_refund_is_issued()
    {
        var harness = new IEFaturaContractTestHarness();
        var sut = CreateProvider(harness);

        harness.NextCreditNoteResult = new EFaturaCreditNoteResult("cn-2", "Accepted", DateTime.UtcNow);
        var req = new EFaturaCreditNoteRequest("u-orig", 1000m, "TRY", "Full refund");

        var result = await sut.CreditNoteAsync(req, CancellationToken.None);

        result.Status.Should().Be("Accepted");
        harness.LastCreditNoteRequest!.RefundAmount.Should().Be(1000m);
    }

    [Fact]
    public async Task T15_CreditNote_multiple_against_one_invoice_are_allowed()
    {
        var harness = new IEFaturaContractTestHarness();
        var sut = CreateProvider(harness);

        harness.NextCreditNoteResult = new EFaturaCreditNoteResult("cn-3a", "Accepted", DateTime.UtcNow);
        var first = await sut.CreditNoteAsync(new EFaturaCreditNoteRequest("u-orig", 25m, "TRY", null), CancellationToken.None);

        harness.NextCreditNoteResult = new EFaturaCreditNoteResult("cn-3b", "Accepted", DateTime.UtcNow);
        var second = await sut.CreditNoteAsync(new EFaturaCreditNoteRequest("u-orig", 75m, "TRY", null), CancellationToken.None);

        first.Uuid.Should().NotBe(second.Uuid);
    }

    [Fact]
    public async Task T16_ListReceived_empty_inbox_returns_empty_list()
    {
        var harness = new IEFaturaContractTestHarness();
        var sut = CreateProvider(harness);

        harness.NextInbox = Array.Empty<EFaturaInboxItem>();

        var result = await sut.ListReceivedAsync(new EFaturaListReceivedRequest(DateTime.UtcNow.AddDays(-1), DateTime.UtcNow), CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task T17_ListReceived_paginated_returns_items()
    {
        var harness = new IEFaturaContractTestHarness();
        var sut = CreateProvider(harness);

        harness.NextInbox = new[]
        {
            new EFaturaInboxItem("u-a", "1111111111", "INV-A", DateTime.UtcNow, "Received"),
            new EFaturaInboxItem("u-b", "2222222222", "INV-B", DateTime.UtcNow, "Received"),
        };

        var result = await sut.ListReceivedAsync(new EFaturaListReceivedRequest(DateTime.UtcNow.AddDays(-1), DateTime.UtcNow), CancellationToken.None);

        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task T18_ListReceived_date_range_is_forwarded()
    {
        var harness = new IEFaturaContractTestHarness();
        var sut = CreateProvider(harness);

        harness.NextInbox = Array.Empty<EFaturaInboxItem>();
        var from = DateTime.UtcNow.AddDays(-30);
        var to = DateTime.UtcNow;

        await sut.ListReceivedAsync(new EFaturaListReceivedRequest(from, to), CancellationToken.None);

        harness.LastInboxFrom.Should().Be(from);
        harness.LastInboxTo.Should().Be(to);
    }

    [Fact]
    public async Task T19_Issue_transient_5xx_is_retried_and_succeeds()
    {
        var harness = new IEFaturaContractTestHarness();
        var sut = CreateProvider(harness);

        harness.QueueIssueFailure(new IEFaturaContractTestHarness.TransientFailure(1));
        harness.NextIssueResult = new EFaturaIssueResult(Guid.NewGuid().ToString(), "Accepted", "1000", DateTime.UtcNow);

        var request = new EFaturaIssueRequest(BuildDocument(), "x");
        var result = await sut.IssueAsync(request, CancellationToken.None);

        result.Status.Should().Be("Accepted");
        harness.IssueAttempts.Should().BeGreaterThan(1);
    }

    [Fact]
    public async Task T20_Issue_permanent_error_is_not_retried()
    {
        var harness = new IEFaturaContractTestHarness();
        var sut = CreateProvider(harness);

        harness.NextIssueException = new InvalidOperationException("BAD_REQUEST");
        var request = new EFaturaIssueRequest(BuildDocument(), "x");

        var act = async () => await sut.IssueAsync(request, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
        harness.IssueAttempts.Should().Be(1);
    }

    [Fact]
    public async Task T21_Issue_timeout_propagates_cancellation()
    {
        var harness = new IEFaturaContractTestHarness();
        var sut = CreateProvider(harness);

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var request = new EFaturaIssueRequest(BuildDocument(), "x");
        var act = async () => await sut.IssueAsync(request, cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public void T22_Webhook_valid_signature_is_accepted()
    {
        var harness = new IEFaturaContractTestHarness();
        _ = CreateProvider(harness);

        var verified = harness.VerifyWebhook("payload-1", harness.SignFor("payload-1"));

        verified.Should().BeTrue();
    }

    [Fact]
    public void T23_Webhook_invalid_signature_is_rejected()
    {
        var harness = new IEFaturaContractTestHarness();
        _ = CreateProvider(harness);

        var verified = harness.VerifyWebhook("payload-2", "deadbeef");

        verified.Should().BeFalse();
    }

    [Fact]
    public void T24_Webhook_replay_attack_is_detected()
    {
        var harness = new IEFaturaContractTestHarness();
        _ = CreateProvider(harness);

        var sig = harness.SignFor("payload-3");
        harness.RegisterReplayGuard("payload-3");

        var first = harness.VerifyWebhook("payload-3", sig, enforceReplay: true);
        var replay = harness.VerifyWebhook("payload-3", sig, enforceReplay: true);

        first.Should().BeTrue();
        replay.Should().BeFalse();
    }

    [Fact]
    public async Task T25_Ubl_missing_tax_office_is_rejected()
    {
        var harness = new IEFaturaContractTestHarness();
        var sut = CreateProvider(harness);

        harness.NextIssueException = new InvalidOperationException("UBL_MISSING_TAX_OFFICE");
        var request = new EFaturaIssueRequest(BuildDocument(), "x");

        var act = async () => await sut.IssueAsync(request, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*TAX_OFFICE*");
    }

    [Fact]
    public async Task T26_Ubl_invalid_vkn_is_rejected()
    {
        var harness = new IEFaturaContractTestHarness();
        var sut = CreateProvider(harness);

        harness.NextIssueException = new InvalidOperationException("UBL_INVALID_VKN");
        var request = new EFaturaIssueRequest(BuildDocument(buyerVkn: "123"), "x");

        var act = async () => await sut.IssueAsync(request, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*VKN*");
    }

    [Fact]
    public async Task T27_Ubl_oversized_invoice_is_rejected()
    {
        var harness = new IEFaturaContractTestHarness();
        var sut = CreateProvider(harness);

        harness.NextIssueException = new InvalidOperationException("UBL_OVERSIZED");
        var bigLines = Enumerable.Range(0, 10_001)
            .Select(i => new EFaturaLine(1m, $"Item {i}", 1m, 20m))
            .ToArray();
        var request = new EFaturaIssueRequest(BuildDocument(lines: bigLines), "x");

        var act = async () => await sut.IssueAsync(request, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task T28_Tenant_cross_tenant_uuid_is_not_found()
    {
        var harness = new IEFaturaContractTestHarness();
        var sut = CreateProvider(harness);

        harness.NextStatusResult = new EFaturaProviderStatus("foreign-uuid", "NotFound", null, null);

        var result = await sut.GetStatusAsync(new EFaturaGetStatusRequest("foreign-uuid"), CancellationToken.None);

        result.CurrentStatus.Should().Be("NotFound");
    }

    [Fact]
    public async Task T29_Tenant_credential_isolation_throws_when_missing()
    {
        var harness = new IEFaturaContractTestHarness();
        var sut = CreateProvider(harness);

        harness.NextIssueException = new UnauthorizedAccessException("CREDENTIALS_MISSING");
        var request = new EFaturaIssueRequest(BuildDocument(), "x");

        var act = async () => await sut.IssueAsync(request, CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task T30_Edge_max_amount_is_accepted()
    {
        var harness = new IEFaturaContractTestHarness();
        var sut = CreateProvider(harness);

        harness.NextIssueResult = new EFaturaIssueResult(Guid.NewGuid().ToString(), "Accepted", "1000", DateTime.UtcNow);
        var lines = new[] { new EFaturaLine(1m, "MaxItem", 999_999_999m, 0m) };
        var request = new EFaturaIssueRequest(BuildDocument(total: 999_999_999m, lines: lines), "x");

        var result = await sut.IssueAsync(request, CancellationToken.None);

        result.Status.Should().Be("Accepted");
    }
}
