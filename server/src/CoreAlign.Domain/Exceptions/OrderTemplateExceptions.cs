namespace CoreAlign.Domain.Exceptions;

public class OrderTemplateNotFoundException : NotFoundException
{
    public OrderTemplateNotFoundException() : base("Order template not found.") { }
}
