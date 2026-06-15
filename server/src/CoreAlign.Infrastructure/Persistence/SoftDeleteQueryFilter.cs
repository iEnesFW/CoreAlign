using System.Linq.Expressions;
using System.Reflection;
using CoreAlign.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace CoreAlign.Infrastructure.Persistence;

public static class SoftDeleteQueryFilter
{
    public static void ApplySoftDeleteFilters(this ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(ISoftDeletable).IsAssignableFrom(entityType.ClrType))
            {
                var method = typeof(SoftDeleteQueryFilter)
                    .GetMethod(nameof(ApplyFilter), BindingFlags.Static | BindingFlags.NonPublic)!
                    .MakeGenericMethod(entityType.ClrType);
                method.Invoke(null, new object[] { modelBuilder });
            }
        }
    }

    private static void ApplyFilter<T>(ModelBuilder modelBuilder) where T : class, ISoftDeletable
    {
        var entity = modelBuilder.Entity<T>();
        var existing = entity.Metadata.GetDeclaredQueryFilters().FirstOrDefault()?.Expression as LambdaExpression;
        Expression<Func<T, bool>> softDelete = e => !e.IsDeleted;

        if (existing is null)
        {
            entity.HasQueryFilter(softDelete);
            return;
        }

        var parameter = Expression.Parameter(typeof(T), "e");
        var existingBody = ReplaceParameter(existing.Body, existing.Parameters[0], parameter);
        var softDeleteBody = ReplaceParameter(softDelete.Body, softDelete.Parameters[0], parameter);
        var combined = Expression.Lambda<Func<T, bool>>(
            Expression.AndAlso(existingBody, softDeleteBody),
            parameter);
        entity.HasQueryFilter(combined);
    }

    private static Expression ReplaceParameter(Expression body, ParameterExpression source, ParameterExpression target)
    {
        return new ParameterReplacer(source, target).Visit(body);
    }

    private sealed class ParameterReplacer : ExpressionVisitor
    {
        private readonly ParameterExpression _source;
        private readonly ParameterExpression _target;

        public ParameterReplacer(ParameterExpression source, ParameterExpression target)
        {
            _source = source;
            _target = target;
        }

        protected override Expression VisitParameter(ParameterExpression node) =>
            node == _source ? _target : base.VisitParameter(node);
    }
}
