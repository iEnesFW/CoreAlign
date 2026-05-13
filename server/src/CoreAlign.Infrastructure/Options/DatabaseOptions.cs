namespace CoreAlign.Infrastructure.Options;

public class DatabaseOptions
{
    public const string SectionName = "Database";

    public string Provider { get; set; } = "Postgres";
}
