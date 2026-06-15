using CoreAlign.Application.Reports.Custom;
using CoreAlign.Domain.Entities.Reporting;

namespace CoreAlign.Application.Tests.Reports;

public class CustomReportValidatorTests
{
    [Fact]
    public void Throws_when_no_dimension_or_measure()
    {
        var def = new CustomReportDefinitionDto(
            ReportEntityType.Invoice,
            Array.Empty<string>(),
            Array.Empty<CustomReportMeasureDto>());

        Action act = () => CustomReportValidator.Validate(def);
        act.Should().Throw<CustomReportValidationException>();
    }

    [Fact]
    public void Throws_on_unknown_dimension_field()
    {
        var def = new CustomReportDefinitionDto(
            ReportEntityType.Invoice,
            new[] { "DROP TABLE invoices" },
            Array.Empty<CustomReportMeasureDto>());

        Action act = () => CustomReportValidator.Validate(def);
        act.Should().Throw<CustomReportValidationException>()
            .WithMessage("*Unknown dimension*");
    }

    [Fact]
    public void Throws_when_measure_uses_disallowed_aggregation()
    {
        var def = new CustomReportDefinitionDto(
            ReportEntityType.Invoice,
            new[] { "CustomerName" },
            new[] { new CustomReportMeasureDto("CustomerName", "Sum") });

        Action act = () => CustomReportValidator.Validate(def);
        act.Should().Throw<CustomReportValidationException>();
    }

    [Fact]
    public void Throws_when_filter_operator_not_allowed_for_field()
    {
        var def = new CustomReportDefinitionDto(
            ReportEntityType.Invoice,
            new[] { "CustomerName" },
            Array.Empty<CustomReportMeasureDto>(),
            new[] { new CustomReportFilterDto("Status", "Contains", "Paid", null) });

        Action act = () => CustomReportValidator.Validate(def);
        act.Should().Throw<CustomReportValidationException>();
    }

    [Fact]
    public void Throws_when_sort_field_is_unknown()
    {
        var def = new CustomReportDefinitionDto(
            ReportEntityType.Invoice,
            new[] { "CustomerName" },
            Array.Empty<CustomReportMeasureDto>(),
            null,
            new CustomReportSortDto("BOGUS", false));
        Action act = () => CustomReportValidator.Validate(def);
        act.Should().Throw<CustomReportValidationException>();
    }

    [Fact]
    public void Passes_for_valid_grouped_definition()
    {
        var def = new CustomReportDefinitionDto(
            ReportEntityType.Invoice,
            new[] { "CustomerName", "Status" },
            new[] { new CustomReportMeasureDto("Total", "Sum") },
            new[] { new CustomReportFilterDto("Status", "Equals", "Paid", null) });

        Action act = () => CustomReportValidator.Validate(def);
        act.Should().NotThrow();
    }

    [Fact]
    public void Throws_for_unknown_aggregation_name()
    {
        var def = new CustomReportDefinitionDto(
            ReportEntityType.Invoice,
            new[] { "CustomerName" },
            new[] { new CustomReportMeasureDto("Total", "FRAUD_FN") });
        Action act = () => CustomReportValidator.Validate(def);
        act.Should().Throw<CustomReportValidationException>().WithMessage("*aggregation*");
    }

    [Fact]
    public void Throws_when_measure_field_is_unknown()
    {
        var def = new CustomReportDefinitionDto(
            ReportEntityType.Order,
            new[] { "OrderNumber" },
            new[] { new CustomReportMeasureDto("DROP_TABLE", "Sum") });
        Action act = () => CustomReportValidator.Validate(def);
        act.Should().Throw<CustomReportValidationException>().WithMessage("*Unknown measure*");
    }
}
