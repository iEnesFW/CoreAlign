using CoreAlign.Domain.Entities.Reporting;

namespace CoreAlign.Application.Reports.Custom;

public interface ICustomReportExecutor
{
    Task<CustomReportPreviewDto> ExecuteAsync(CustomReportDefinitionDto definition, CancellationToken cancellationToken = default);
}

public sealed class CustomReportValidationException : Exception
{
    public CustomReportValidationException(string message) : base(message) { }
}

public static class CustomReportValidator
{
    public static void Validate(CustomReportDefinitionDto def)
    {
        ArgumentNullException.ThrowIfNull(def);
        if (def.Dimensions.Count == 0 && def.Measures.Count == 0)
        {
            throw new CustomReportValidationException("At least one dimension or measure must be supplied.");
        }
        foreach (var dim in def.Dimensions)
        {
            var desc = FieldCatalog.Find(def.EntityType, dim);
            if (desc is null)
            {
                throw new CustomReportValidationException($"Unknown dimension field '{dim}' for {def.EntityType}.");
            }
            if (!desc.IsDimension)
            {
                throw new CustomReportValidationException($"Field '{dim}' is not eligible as a dimension.");
            }
        }
        foreach (var m in def.Measures)
        {
            var desc = FieldCatalog.Find(def.EntityType, m.Field);
            if (desc is null)
            {
                throw new CustomReportValidationException($"Unknown measure field '{m.Field}' for {def.EntityType}.");
            }
            if (!Enum.TryParse<ReportMeasureFunction>(m.Function, ignoreCase: true, out var fn))
            {
                throw new CustomReportValidationException($"Unknown aggregation '{m.Function}'.");
            }
            if (desc.AllowedAggregations is null || !desc.AllowedAggregations.Contains(fn))
            {
                throw new CustomReportValidationException($"Aggregation '{fn}' not allowed for '{m.Field}'.");
            }
            if (!desc.IsMeasureEligible && fn != ReportMeasureFunction.Count)
            {
                throw new CustomReportValidationException($"Field '{m.Field}' is not measure-eligible.");
            }
        }
        if (def.Filters is not null)
        {
            foreach (var f in def.Filters)
            {
                var desc = FieldCatalog.Find(def.EntityType, f.Field);
                if (desc is null)
                {
                    throw new CustomReportValidationException($"Unknown filter field '{f.Field}'.");
                }
                if (!Enum.TryParse<ReportFilterOperator>(f.Operator, ignoreCase: true, out var op))
                {
                    throw new CustomReportValidationException($"Unknown filter operator '{f.Operator}'.");
                }
                if (!desc.AllowedOperators.Contains(op))
                {
                    throw new CustomReportValidationException($"Operator '{op}' not allowed on '{f.Field}'.");
                }
            }
        }
        if (def.SortBy is not null && !FieldCatalog.IsKnown(def.EntityType, def.SortBy.Field))
        {
            throw new CustomReportValidationException($"Unknown sort field '{def.SortBy.Field}'.");
        }
    }
}
