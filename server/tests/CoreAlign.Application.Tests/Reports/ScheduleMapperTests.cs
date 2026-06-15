using CoreAlign.Application.Reports.Schedules;
using CoreAlign.Domain.Entities.Reporting;

namespace CoreAlign.Application.Tests.Reports;

public class ScheduleMapperTests
{
    [Fact]
    public void ValidateRecipients_throws_when_empty()
    {
        Action act = () => InvokeValidate(Array.Empty<string>());
        act.Should().Throw<ScheduleValidationException>();
    }

    [Fact]
    public void ValidateRecipients_throws_when_invalid_email()
    {
        Action act = () => InvokeValidate(new[] { "not-an-email" });
        act.Should().Throw<ScheduleValidationException>();
    }

    [Fact]
    public void ValidateRecipients_passes_for_valid_addresses()
    {
        Action act = () => InvokeValidate(new[] { "ops@example.com", "cfo@example.com" });
        act.Should().NotThrow();
    }

    private static void InvokeValidate(IReadOnlyList<string> recipients)
    {
        var mapperType = typeof(CreateReportScheduleCommandHandler).Assembly
            .GetType("CoreAlign.Application.Reports.Schedules.ScheduleMapper")!;
        var method = mapperType.GetMethod("ValidateRecipients", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!;
        try
        {
            method.Invoke(null, new object[] { recipients });
        }
        catch (System.Reflection.TargetInvocationException ex) when (ex.InnerException is not null)
        {
            throw ex.InnerException;
        }
    }
}
