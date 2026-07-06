using CoreAlign.Domain.Enums;

namespace CoreAlign.Domain.Exceptions;

public class SerialUnitTransitionException : ConflictException
{
    public SerialUnitTransitionException(string serialNumber, SerialStatus from, SerialStatus to)
        : base($"Serial '{serialNumber}' cannot transition from '{from}' to '{to}'.") { }
}
