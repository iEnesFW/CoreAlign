using System.Text;
using Microsoft.EntityFrameworkCore;

namespace CoreAlign.Infrastructure.Persistence;

public static class SnakeCaseNamingConvention
{
    public static void ApplySnakeCaseNaming(this ModelBuilder modelBuilder)
    {
        foreach (var entity in modelBuilder.Model.GetEntityTypes())
        {
            var tableName = entity.GetTableName();
            if (tableName is not null)
            {
                entity.SetTableName(ToSnakeCase(tableName));
            }

            foreach (var property in entity.GetProperties())
            {
                property.SetColumnName(ToSnakeCase(property.GetColumnName()));
            }

            foreach (var key in entity.GetKeys())
            {
                var keyName = key.GetName();
                if (keyName is not null)
                {
                    key.SetName(ToSnakeCase(keyName));
                }
            }

            foreach (var foreignKey in entity.GetForeignKeys())
            {
                var fkName = foreignKey.GetConstraintName();
                if (fkName is not null)
                {
                    foreignKey.SetConstraintName(ToSnakeCase(fkName));
                }
            }

            foreach (var index in entity.GetIndexes())
            {
                var indexName = index.GetDatabaseName();
                if (indexName is not null)
                {
                    index.SetDatabaseName(ToSnakeCase(indexName));
                }
            }
        }
    }

    private static string ToSnakeCase(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return input;
        }

        var builder = new StringBuilder(input.Length + 8);

        for (var i = 0; i < input.Length; i++)
        {
            var current = input[i];

            if (char.IsUpper(current))
            {
                var previous = i > 0 ? input[i - 1] : '\0';
                var next = i + 1 < input.Length ? input[i + 1] : '\0';

                var shouldInsertUnderscore =
                    i > 0 &&
                    previous != '_' &&
                    (char.IsLower(previous) || (char.IsUpper(previous) && char.IsLower(next)));

                if (shouldInsertUnderscore)
                {
                    builder.Append('_');
                }

                builder.Append(char.ToLowerInvariant(current));
            }
            else
            {
                builder.Append(current);
            }
        }

        return builder.ToString();
    }
}
