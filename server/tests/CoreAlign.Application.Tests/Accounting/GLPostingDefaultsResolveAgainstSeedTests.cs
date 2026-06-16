using CoreAlign.Application.Accounting.Handlers;
using CoreAlign.Application.Accounting.Services;
using CoreAlign.Application.Treasury.Fx;
using CoreAlign.Domain.Enums;

namespace CoreAlign.Application.Tests.Accounting;

/// <summary>
/// FIN-P0-002 guard: every auto-posting role and FX constant must resolve to a
/// postable leaf that actually exists in the REAL Turkish chart-of-accounts seed.
/// Pure in-memory — no DB, no migration. This is the test that would have caught
/// the missing 322 (GoodsReceiptClearing) row and will catch any future
/// missing-account regression because the theory iterates every enum value.
/// </summary>
public class GLPostingDefaultsResolveAgainstSeedTests
{
    private static readonly IReadOnlyDictionary<string, TurkishChartOfAccountsSeed.Entry> Seed =
        TurkishChartOfAccountsSeed.Entries.ToDictionary(e => e.Code, StringComparer.Ordinal);

    public static IEnumerable<object[]> AllKeys =>
        Enum.GetValues<GLPostingKey>().Select(k => new object[] { k });

    [Theory]
    [MemberData(nameof(AllKeys))]
    public void Every_posting_key_resolves_to_a_postable_seeded_account(GLPostingKey key)
    {
        var code = GLPostingDefaults.CodeFor(key);
        code.Should().NotBeNullOrWhiteSpace($"GLPostingKey.{key} must map to a default TDHP code");
        Seed.TryGetValue(code!, out var entry).Should()
            .BeTrue($"default code {code} for {key} must exist in TurkishChartOfAccountsSeed");
        entry!.IsPostable.Should().BeTrue($"account {code} for {key} must be a postable leaf");
    }

    public static IEnumerable<object[]> FxConstants =>
        new[]
        {
            new object[] { FxRevaluation.GainAccountCode },
            new object[] { FxRevaluation.LossAccountCode },
            new object[] { FxRevaluation.ArAccountCode },
            new object[] { FxRevaluation.ApAccountCode },
        };

    [Theory]
    [MemberData(nameof(FxConstants))]
    public void Every_fx_constant_resolves_to_a_postable_seeded_account(string code)
    {
        Seed.TryGetValue(code, out var entry).Should()
            .BeTrue($"FX account code {code} must exist in TurkishChartOfAccountsSeed");
        entry!.IsPostable.Should().BeTrue($"FX account {code} must be a postable leaf");
    }
}
