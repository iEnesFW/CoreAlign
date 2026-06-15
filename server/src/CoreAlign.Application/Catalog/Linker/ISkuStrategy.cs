namespace CoreAlign.Application.Catalog.Linker;

public interface ISkuStrategy
{
    string BuildSku(SkuContext context);
}

public sealed record SkuContext(
    CatalogItemKind Kind,
    string CatalogCode,
    string? Brand,
    Guid TenantId);

public enum CatalogItemKind
{
    Glass = 0,
    Hardware = 1,
    Profile = 2,
    Mounting = 3,
    Color = 4,
    Connector = 5
}
