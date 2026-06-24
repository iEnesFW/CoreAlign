namespace CoreAlign.Domain.Entities.AiHelper;

public enum AiKbSourceType
{
    Route,
    I18n,
    ModuleDoc,
    Article,
    Sector,
    SourceCode
}

public enum AiKbScope
{
    Public,
    Tenant,
    Role
}
