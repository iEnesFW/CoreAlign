using System.Reflection;
using CoreAlign.Application.Reports.Custom;
using CoreAlign.Domain.Entities;
using CoreAlign.Domain.Entities.Reporting;
using CoreAlign.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CoreAlign.Infrastructure.Reports;

public sealed class CustomReportExecutor : ICustomReportExecutor
{
    private const int MaxRows = 5_000;
    private readonly CoreAlignDbContext _context;

    public CustomReportExecutor(CoreAlignDbContext context)
    {
        _context = context;
    }

    public Task<CustomReportPreviewDto> ExecuteAsync(CustomReportDefinitionDto definition, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(definition);
        CustomReportValidator.Validate(definition);

        return definition.EntityType switch
        {
            ReportEntityType.Invoice => ExecuteForAsync<Invoice>(definition, cancellationToken),
            ReportEntityType.Order => ExecuteForAsync<Order>(definition, cancellationToken),
            ReportEntityType.Customer => ExecuteForAsync<Customer>(definition, cancellationToken),
            ReportEntityType.Product => ExecuteForAsync<Product>(definition, cancellationToken),
            ReportEntityType.StockMovement => ExecuteForAsync<StockMovement>(definition, cancellationToken),
            _ => throw new CustomReportValidationException($"Unsupported entity type '{definition.EntityType}'."),
        };
    }

    private async Task<CustomReportPreviewDto> ExecuteForAsync<TEntity>(CustomReportDefinitionDto def, CancellationToken cancellationToken)
        where TEntity : class
    {
        var query = (IQueryable<TEntity>)_context.Set<TEntity>().AsNoTracking();

        if (def.Filters is not null)
        {
            foreach (var f in def.Filters)
            {
                query = ApplyFilter(query, f);
            }
        }

        var hasMeasures = def.Measures.Count > 0;
        var columnOrder = new List<string>();
        columnOrder.AddRange(def.Dimensions);
        foreach (var m in def.Measures)
        {
            columnOrder.Add(string.IsNullOrWhiteSpace(m.Alias) ? $"{m.Function}_{m.Field}" : m.Alias!);
        }

        var limit = Math.Min(def.Limit ?? 1000, MaxRows);

        if (hasMeasures && def.Dimensions.Count > 0)
        {
            var grouped = await ExecuteGroupedAsync(query, def, limit, cancellationToken);
            return new CustomReportPreviewDto(columnOrder, grouped, grouped.Count, grouped.Count >= limit);
        }
        if (hasMeasures)
        {
            var aggregate = await ExecuteAggregateOnlyAsync(query, def, cancellationToken);
            return new CustomReportPreviewDto(columnOrder, new[] { aggregate }, 1, false);
        }

        var projected = await ExecuteDimensionsOnlyAsync(query, def, limit, cancellationToken);
        return new CustomReportPreviewDto(columnOrder, projected, projected.Count, projected.Count >= limit);
    }

    private static IQueryable<TEntity> ApplyFilter<TEntity>(IQueryable<TEntity> query, CustomReportFilterDto filter)
        where TEntity : class
    {
        var prop = typeof(TEntity).GetProperty(filter.Field, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
        if (prop is null)
        {
            throw new CustomReportValidationException($"Field '{filter.Field}' not present on {typeof(TEntity).Name}.");
        }
        var op = Enum.Parse<ReportFilterOperator>(filter.Operator, ignoreCase: true);
        var coerced = CoerceValue(prop.PropertyType, filter.Value);
        var coerced2 = filter.Value2 is null ? null : CoerceValue(prop.PropertyType, filter.Value2);

        var parameter = System.Linq.Expressions.Expression.Parameter(typeof(TEntity), "e");
        var member = System.Linq.Expressions.Expression.Property(parameter, prop);
        var body = BuildPredicateBody(member, op, coerced, coerced2);
        var lambda = System.Linq.Expressions.Expression.Lambda<Func<TEntity, bool>>(body, parameter);
        return query.Where(lambda);
    }

    private static System.Linq.Expressions.Expression BuildPredicateBody(
        System.Linq.Expressions.MemberExpression member,
        ReportFilterOperator op,
        object? value,
        object? value2)
    {
        switch (op)
        {
            case ReportFilterOperator.Equals:
                return System.Linq.Expressions.Expression.Equal(member, BuildConstant(value, member.Type));
            case ReportFilterOperator.NotEquals:
                return System.Linq.Expressions.Expression.NotEqual(member, BuildConstant(value, member.Type));
            case ReportFilterOperator.GreaterThan:
                return System.Linq.Expressions.Expression.GreaterThan(member, BuildConstant(value, member.Type));
            case ReportFilterOperator.GreaterThanOrEqual:
                return System.Linq.Expressions.Expression.GreaterThanOrEqual(member, BuildConstant(value, member.Type));
            case ReportFilterOperator.LessThan:
                return System.Linq.Expressions.Expression.LessThan(member, BuildConstant(value, member.Type));
            case ReportFilterOperator.LessThanOrEqual:
                return System.Linq.Expressions.Expression.LessThanOrEqual(member, BuildConstant(value, member.Type));
            case ReportFilterOperator.Contains:
            {
                if (member.Type != typeof(string))
                {
                    throw new CustomReportValidationException("Contains is only supported on string fields.");
                }
                var containsMethod = typeof(string).GetMethod(nameof(string.Contains), new[] { typeof(string) })!;
                return System.Linq.Expressions.Expression.Call(member, containsMethod, BuildConstant(value, typeof(string)));
            }
            case ReportFilterOperator.StartsWith:
            {
                if (member.Type != typeof(string))
                {
                    throw new CustomReportValidationException("StartsWith is only supported on string fields.");
                }
                var startsMethod = typeof(string).GetMethod(nameof(string.StartsWith), new[] { typeof(string) })!;
                return System.Linq.Expressions.Expression.Call(member, startsMethod, BuildConstant(value, typeof(string)));
            }
            case ReportFilterOperator.Between:
            {
                if (value2 is null)
                {
                    throw new CustomReportValidationException("Between requires both Value and Value2.");
                }
                return System.Linq.Expressions.Expression.AndAlso(
                    System.Linq.Expressions.Expression.GreaterThanOrEqual(member, BuildConstant(value, member.Type)),
                    System.Linq.Expressions.Expression.LessThanOrEqual(member, BuildConstant(value2, member.Type)));
            }
            case ReportFilterOperator.In:
            {
                if (value is not string raw)
                {
                    throw new CustomReportValidationException("In filter requires a comma-separated value.");
                }
                var tokens = raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                if (tokens.Length == 0)
                {
                    throw new CustomReportValidationException("In filter must contain at least one value.");
                }
                var elementType = Nullable.GetUnderlyingType(member.Type) ?? member.Type;
                var listType = typeof(List<>).MakeGenericType(elementType);
                var typedList = (System.Collections.IList)Activator.CreateInstance(listType)!;
                foreach (var t in tokens)
                {
                    typedList.Add(CoerceValue(elementType, t));
                }
                var listExpr = System.Linq.Expressions.Expression.Constant(typedList, listType);
                var containsMethod = listType.GetMethod("Contains", new[] { elementType })!;
                var memberConverted = member.Type == elementType
                    ? (System.Linq.Expressions.Expression)member
                    : System.Linq.Expressions.Expression.Convert(member, elementType);
                return System.Linq.Expressions.Expression.Call(listExpr, containsMethod, memberConverted);
            }
        }
        throw new CustomReportValidationException($"Operator '{op}' is not supported.");
    }

    private static System.Linq.Expressions.Expression BuildConstant(object? value, Type memberType)
    {
        if (value is null)
        {
            return System.Linq.Expressions.Expression.Constant(null, memberType);
        }
        var underlying = Nullable.GetUnderlyingType(memberType) ?? memberType;
        if (value.GetType() == underlying)
        {
            var constant = System.Linq.Expressions.Expression.Constant(value, underlying);
            return underlying == memberType
                ? constant
                : System.Linq.Expressions.Expression.Convert(constant, memberType);
        }
        return System.Linq.Expressions.Expression.Constant(value, memberType);
    }

    private static object? CoerceValue(Type targetType, string? raw)
    {
        if (raw is null)
        {
            return null;
        }
        var underlying = Nullable.GetUnderlyingType(targetType) ?? targetType;
        if (underlying == typeof(string)) return raw;
        if (underlying == typeof(int)) return int.Parse(raw, System.Globalization.CultureInfo.InvariantCulture);
        if (underlying == typeof(long)) return long.Parse(raw, System.Globalization.CultureInfo.InvariantCulture);
        if (underlying == typeof(decimal)) return decimal.Parse(raw, System.Globalization.CultureInfo.InvariantCulture);
        if (underlying == typeof(double)) return double.Parse(raw, System.Globalization.CultureInfo.InvariantCulture);
        if (underlying == typeof(bool)) return bool.Parse(raw);
        if (underlying == typeof(Guid)) return Guid.Parse(raw);
        if (underlying == typeof(DateTime))
        {
            var dt = DateTime.Parse(
                raw,
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.AssumeUniversal | System.Globalization.DateTimeStyles.AdjustToUniversal);
            return DateTime.SpecifyKind(dt, DateTimeKind.Utc);
        }
        if (underlying.IsEnum) return Enum.Parse(underlying, raw, ignoreCase: true);
        throw new CustomReportValidationException($"Unsupported value type '{underlying.Name}' for filter.");
    }

    private static async Task<IReadOnlyList<CustomReportPreviewRowDto>> ExecuteDimensionsOnlyAsync<TEntity>(
        IQueryable<TEntity> query,
        CustomReportDefinitionDto def,
        int limit,
        CancellationToken cancellationToken) where TEntity : class
    {
        var list = await query.Take(limit).ToListAsync(cancellationToken);
        return list.Select(item =>
        {
            var cells = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            foreach (var dim in def.Dimensions)
            {
                cells[dim] = ReadProperty(item!, dim);
            }
            return new CustomReportPreviewRowDto(cells);
        }).ToList();
    }

    private static async Task<IReadOnlyList<CustomReportPreviewRowDto>> ExecuteGroupedAsync<TEntity>(
        IQueryable<TEntity> query,
        CustomReportDefinitionDto def,
        int limit,
        CancellationToken cancellationToken) where TEntity : class
    {
        var raw = await query.Take(MaxRows).ToListAsync(cancellationToken);
        var grouped = raw
            .GroupBy(item =>
            {
                var key = def.Dimensions.Select(d => ReadProperty(item!, d)?.ToString() ?? string.Empty).ToArray();
                return string.Join("||", key);
            })
            .Select(g =>
            {
                var firstItem = g.First();
                var cells = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
                foreach (var d in def.Dimensions)
                {
                    cells[d] = ReadProperty(firstItem!, d);
                }
                foreach (var m in def.Measures)
                {
                    var alias = string.IsNullOrWhiteSpace(m.Alias) ? $"{m.Function}_{m.Field}" : m.Alias!;
                    cells[alias] = AggregateValue(g, m.Field, Enum.Parse<ReportMeasureFunction>(m.Function, ignoreCase: true));
                }
                return new CustomReportPreviewRowDto(cells);
            })
            .Take(limit)
            .ToList();
        return grouped;
    }

    private static async Task<CustomReportPreviewRowDto> ExecuteAggregateOnlyAsync<TEntity>(
        IQueryable<TEntity> query,
        CustomReportDefinitionDto def,
        CancellationToken cancellationToken) where TEntity : class
    {
        var raw = await query.Take(MaxRows).ToListAsync(cancellationToken);
        var cells = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        foreach (var m in def.Measures)
        {
            var alias = string.IsNullOrWhiteSpace(m.Alias) ? $"{m.Function}_{m.Field}" : m.Alias!;
            cells[alias] = AggregateValue(raw, m.Field, Enum.Parse<ReportMeasureFunction>(m.Function, ignoreCase: true));
        }
        return new CustomReportPreviewRowDto(cells);
    }

    private static object? AggregateValue<TEntity>(IEnumerable<TEntity> source, string field, ReportMeasureFunction fn)
    {
        if (fn == ReportMeasureFunction.Count)
        {
            return source.Count();
        }
        var values = source
            .Select(s => ReadProperty(s!, field))
            .Where(v => v is not null)
            .Select(ConvertToDecimal)
            .Where(v => v.HasValue)
            .Select(v => v!.Value)
            .ToList();
        if (values.Count == 0) return 0m;
        return fn switch
        {
            ReportMeasureFunction.Sum => values.Sum(),
            ReportMeasureFunction.Avg => values.Average(),
            ReportMeasureFunction.Min => values.Min(),
            ReportMeasureFunction.Max => values.Max(),
            _ => values.Sum(),
        };
    }

    private static decimal? ConvertToDecimal(object? value)
    {
        if (value is null) return null;
        return value switch
        {
            decimal d => d,
            int i => i,
            long l => l,
            double db => (decimal)db,
            float f => (decimal)f,
            _ => null,
        };
    }

    private static object? ReadProperty(object instance, string name)
    {
        var prop = instance.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
        return prop?.GetValue(instance);
    }
}
