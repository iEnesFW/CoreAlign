namespace CoreAlign.Domain.Exceptions;

public class UnmappedUnitCodeException : DomainException
{
    public UnmappedUnitCodeException(string unitOfMeasure)
        : base($"Unit of measure '{unitOfMeasure}' has no UBL-TR (UN/ECE Rec 20) unit code mapping.") { }
}
