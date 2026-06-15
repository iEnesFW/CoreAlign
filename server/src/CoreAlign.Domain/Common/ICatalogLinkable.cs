namespace CoreAlign.Domain.Common;

public interface ICatalogLinkable
{
    Guid Id { get; }
    string Code { get; }
    string Name { get; }
    string Unit { get; }
    decimal UnitCost { get; }
    Guid? LinkedProductId { get; set; }
}
